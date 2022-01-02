using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Cue
{
	class Version
	{
		public static readonly int Major = 1;
		public static readonly int Minor = 0;
		public static readonly int Patch = 1;

		public static string String
		{
			get
			{
				string s = $"{Major}.{Minor}";

				if (Patch != 0)
					s += $".{Patch}";

				return s;
			}
		}

		public static string DisplayString
		{
			get
			{
				return "Cue " + String;
			}
		}
	}


	class PluginGone : Exception { }


	class CustomMenu
	{
		private string caption_;
		private Sys.IActionTrigger trigger_;

		private CustomMenu(string caption, Sys.IActionTrigger trigger)
		{
			caption_ = caption;
			trigger_ = trigger;
		}

		public CustomMenu()
		{
			caption_ = "Button";
			trigger_ = Cue.Instance.Sys.CreateActionTrigger();
		}

		public static CustomMenu FromJSON(JSONClass n)
		{
			try
			{
				var c = J.ReqString(n, "caption");

				if (!n.HasKey("trigger"))
					throw new LoadFailed("missing trigger");

				var t = Cue.Instance.Sys.LoadActionTrigger(n["trigger"].AsObject);
				if (t == null)
					throw new LoadFailed("failed to create trigger");

				return new CustomMenu(c, t);
			}
			catch (Exception e)
			{
				Cue.LogError("failed to load custom menu, " + e.ToString());
				return null;
			}
		}

		public string Caption
		{
			get
			{
				return caption_;
			}

			set
			{
				caption_ = value;
				trigger_.Name = value;
				Cue.Instance.Options.ForceMenusChanged();
			}
		}

		public Sys.IActionTrigger Trigger
		{
			get { return trigger_; }
		}

		public JSONNode ToJSON()
		{
			var o = new JSONClass();

			o["caption"] = caption_;
			o["trigger"] = trigger_.ToJSON();

			return o;
		}
	}


	class Options
	{
		public delegate void Handler();
		public event Handler Changed, MenusChanged;

		private bool muteSfx_ = false;
		private bool skinColor_ = true;
		private bool skinGloss_ = true;
		private bool hairLoose_ = true;
		private bool handLinking_ = true;
		private bool devMode_ = false;
		private float excitement_ = 1.0f;
		private bool leftMenu_ = true;
		private bool rightMenu_ = true;
		private List<CustomMenu> menus_ = new List<CustomMenu>();

		public Options()
		{
			//menus_.Add(new CustomMenu(
			//	"test", Cue.Instance.Sys.CreateActionTrigger()));
		}

		public bool MuteSfx
		{
			get { return muteSfx_; }
			set { muteSfx_ = value; OnChanged(); }
		}

		public bool SkinColor
		{
			get { return skinColor_; }
			set { skinColor_ = value; OnChanged(); }
		}

		public bool SkinGloss
		{
			get { return skinGloss_; }
			set { skinGloss_ = value; OnChanged(); }
		}

		public bool HairLoose
		{
			get { return hairLoose_; }
			set { hairLoose_ = value; OnChanged(); }
		}

		public bool HandLinking
		{
			get { return handLinking_; }
			set { handLinking_ = value; OnChanged(); }
		}

		public bool LeftMenu
		{
			get { return leftMenu_; }
			set { leftMenu_ = value; OnChanged(); }
		}

		public bool RightMenu
		{
			get { return rightMenu_; }
			set { rightMenu_ = value; OnChanged(); }
		}

		public bool DevMode
		{
			get { return devMode_; }
			set { devMode_ = value; OnChanged(); }
		}

		public float Excitement
		{
			get { return excitement_; }
			set { excitement_ = value; OnChanged(); }
		}

		public CustomMenu[] Menus
		{
			get { return menus_.ToArray(); }
		}

		public CustomMenu AddCustomMenu()
		{
			var m = new CustomMenu();
			menus_.Add(m);
			OnMenusChanged();
			return m;
		}

		public void RemoveCustomMenu(CustomMenu m)
		{
			if (!menus_.Contains(m))
			{
				Cue.LogError($"custom menu '{m.Caption}' not found");
				return;
			}

			menus_.Remove(m);
			OnMenusChanged();
		}

		public void ForceMenusChanged()
		{
			OnMenusChanged();
		}

		public JSONNode ToJSON()
		{
			var o = new JSONClass();

			o["muteSfx"] = new JSONData(muteSfx_);
			o["skinColor"] = new JSONData(skinColor_);
			o["skinGloss"] = new JSONData(skinGloss_);
			o["handLinking"] = new JSONData(handLinking_);
			o["hairLoose"] = new JSONData(hairLoose_);
			o["devMode"] = new JSONData(devMode_);
			o["excitement"] = new JSONData(excitement_);
			o["leftMenu"] = new JSONData(leftMenu_);
			o["rightMenu"] = new JSONData(rightMenu_);

			if (menus_.Count > 0)
			{
				var mo = new JSONArray();

				foreach (var m in menus_)
					mo.Add(m.ToJSON());

				o["menus"] = mo;
			}

			return o;
		}

		public void Load(JSONClass o)
		{
			J.OptBool(o, "muteSfx", ref muteSfx_);
			J.OptBool(o, "skinColor", ref skinColor_);
			J.OptBool(o, "skinGloss", ref skinGloss_);
			J.OptBool(o, "handLinking", ref handLinking_);
			J.OptBool(o, "hairLoose", ref hairLoose_);
			J.OptBool(o, "devMode", ref devMode_);
			J.OptFloat(o, "excitement", ref excitement_);
			J.OptBool(o, "leftMenu", ref leftMenu_);
			J.OptBool(o, "rightMenu", ref rightMenu_);

			if (o.HasKey("menus"))
			{
				var a = o["menus"].AsArray;

				foreach (var mo in a.Childs)
				{
					var m = CustomMenu.FromJSON(mo.AsObject);
					if (m != null)
						menus_.Add(m);
				}
			}

			OnChanged();
			OnMenusChanged();
		}

		private void OnChanged()
		{
			Cue.Instance.SaveLater();
			Changed?.Invoke();
		}

		private void OnMenusChanged()
		{
			Cue.Instance.SaveLater();
			MenusChanged?.Invoke();
		}
	}


	class Cue
	{
		private static Cue instance_ = null;

		private Options options_;
		private float saveElapsed_ = 0;
		private bool needSave_ = false;
		private const float SaveLaterDelay = 2;

		private readonly List<Person> persons_ = new List<Person>();
		private readonly List<Person> activePersons_ = new List<Person>();
		private Person[] activePersonsArray_ = new Person[0];

		private readonly List<IObject> objects_ = new List<IObject>();
		private readonly List<IObject> activeObjects_ = new List<IObject>();
		private IObject[] activeObjectsArray_ = new IObject[0];

		private readonly List<IObject> everything_ = new List<IObject>();
		private readonly List<IObject> everythingActive_ = new List<IObject>();
		private IObject[] everythingActiveArray_ = new IObject[0];

		private UI ui_;
		private bool loaded_ = false;

		private Person player_ = null;
		private Person forcedPlayer_ = null;
		private int frame_ = 0;

		private Sys.ILiveSaver saver_;


		public Cue()
		{
			instance_ = this;
			LogVerbose("cue: ctor");

			options_ = new Options();
			saver_ = Sys.CreateLiveSaver();
		}

		public static Cue Instance { get { return instance_; } }

		public Sys.ISys Sys { get { return CueMain.Instance.Sys; } }
		public Sys.Vam.VamSys VamSys { get { return Sys as Sys.Vam.VamSys; } }

		public List<Person> AllPersons { get { return persons_; } }
		public Person[] ActivePersons { get { return activePersonsArray_; } }

		public List<IObject> AllObjects { get { return objects_; } }
		public IObject[] ActiveObjects { get { return activeObjectsArray_; } }

		public List<IObject> Everything { get { return everything_; } }
		public IObject[] EverythingActive { get { return everythingActiveArray_; } }


		public Options Options { get { return options_; } }
		public UI UI { get { return ui_; } }
		public int Frame { get { return frame_; } }

		public Person Player
		{
			get { return forcedPlayer_ ?? player_; }
		}

		public bool IsPlayer(int personIndex)
		{
			return (Player.PersonIndex == personIndex);
		}

		public Person ForcedPlayer
		{
			get
			{
				return forcedPlayer_;
			}

			set
			{
				if (value == null)
					LogInfo($"unforcing player");
				else
					LogInfo($"forcing player to {value}");

				forcedPlayer_ = value;
			}
		}

		public IObject GetObject(int objectIndex)
		{
			if (objectIndex < 0 || objectIndex >= everything_.Count)
				return null;
			else
				return everything_[objectIndex];
		}

		public Person GetPerson(int personIndex)
		{
			return persons_[personIndex];
		}

		public Person FindPerson(string id)
		{
			for (int i = 0; i < persons_.Count; ++i)
			{
				if (persons_[i].ID == id)
					return persons_[i];
			}

			return null;
		}

		public Person PersonForAtom(Sys.IAtom a)
		{
			for (int i = 0; i < persons_.Count; ++i)
			{
				if (persons_[i].Atom == a)
					return persons_[i];
			}

			return null;
		}

		public void Init()
		{
			LogVerbose($"cue: init (token {CueMain.Instance.Token})");

			VUI.Root.Init(
				() => CueMain.Instance.MVRPluginManager,
				(s, ps) => Strings.Get(s, ps),
				(s) => LogVerbose(s),
				(s) => LogInfo(s),
				(s) => LogWarning(s),
				(s) => LogError(s));

			LogVerbose("cue: loading resources");
			Resources.LoadAll();

			LogVerbose("cue: finding objects");
			FindObjects();

			LogVerbose("cue: initializing persons");
			InitPersons();

			if (Sys.HasUI)
				ui_ = new UI(Sys);

			LogVerbose("cue: enabling plugin state");
			OnPluginState(true);

			LogVerbose("cue: checking config");
			CheckConfig();

			if (Sys.GetAtom("cue!test") != null)
			{
				try
				{
					LogVerbose("cue: test stuff");
					test();
				}
				catch (Exception e)
				{
					LogError("cue: test stuff failed, " + e.ToString());
				}
			}

			LogInfo($"cue: running, version {Version.String}");
		}

		private void CheckConfig()
		{
			var o = saver_.Load();

			if (o != null)
				Load(o);

			Save();
		}

		public void Save()
		{
			if (!loaded_)
				return;

			var oo = new JSONClass();
			Save(oo);
			saver_.Save(oo);
		}

		public void SaveLater()
		{
			needSave_ = true;
			saveElapsed_ = 0;
		}

		private void Save(JSONClass json)
		{
			json.Add("version", Version.String);

			var a = new JSONArray();

			foreach (var o in everything_)
			{
				var oo = o.ToJSON();

				if (oo == null)
					continue;

				if (oo.AsObject != null)
				{
					if (oo.AsObject.Count == 0)
						continue;
				}
				else if (oo.AsArray != null)
				{
					if (oo.AsArray.Count == 0)
						continue;
				}

				a.Add(oo);
			}

			if (a.Count > 0)
				json.Add("objects", a);

			json.Add("log", new JSONData(Logger.Enabled));

			var ui = ui_?.ToJSON();
			if (ui != null)
				json.Add("ui", ui);

			json.Add("options", options_.ToJSON());
		}

		private void Load(JSONClass c)
		{
			loaded_ = true;

			if (c.HasKey("options"))
				options_.Load(c["options"].AsObject);

			var objects = c["objects"].AsArray;

			foreach (var o in everything_)
			{
				foreach (JSONClass oj in objects)
				{
					if (oj["id"].Value == o.ID)
						o.Load(oj.AsObject);
				}
			}

			if (c.HasKey("log"))
				Logger.Enabled = c["log"].AsInt;

			if (c.HasKey("ui"))
				ui_.Load(c["ui"].AsObject);
		}

		private void FindObjects()
		{
			foreach (var a in Sys.GetAtoms())
			{
				if (a.IsPerson)
					AddPerson(a);
			}

			activePersonsArray_ = activePersons_.ToArray();
			activeObjectsArray_ = activeObjects_.ToArray();
			everythingActiveArray_ = everythingActive_.ToArray();
		}

		private void InitPersons()
		{
			for (int i = 0; i < persons_.Count; ++i)
				persons_[i].Init();
		}

		private void AddPerson(Sys.IAtom a)
		{
			var p = new Person(everything_.Count, persons_.Count, a);

			persons_.Add(p);

			if (p.Visible)
				activePersons_.Add(p);

			AddEverything(p);
		}

		private void AddObject(IObject o)
		{
			objects_.Add(o);

			if (o.Visible)
				activeObjects_.Add(o);

			AddEverything(o);
		}

		private void AddEverything(IObject o)
		{
			everything_.Add(o);

			if (o.Visible)
				everythingActive_.Add(o);
		}

		public void ReloadPlugin()
		{
			Sys.ReloadPlugin();
		}

		public void OpenScriptUI()
		{
			Sys.OpenScriptUI();
		}

		public void DisablePlugin()
		{
			CueMain.Instance.DisablePlugin();
		}

		public void FixedUpdate(float s)
		{
			if (!Sys.Paused)
			{
				++frame_;

				I.Instance.Reset();

				I.Start(I.FixedUpdate);
				{
					DoFixedUpdate(s);
				}
				I.End();
			}
		}

		private void DoFixedUpdate(float s)
		{
			for (int i = 0; i < everythingActive_.Count; ++i)
				everythingActive_[i].FixedUpdate(s);
		}

		public void Update(float s)
		{
			I.Instance.Reset();

			if (needSave_)
			{
				saveElapsed_ += s;
				if (saveElapsed_ > SaveLaterDelay)
				{
					Save();
					needSave_ = false;
				}
			}

			I.Start(I.Update);
			{
				DoUpdate(s);
			}
			I.End();

			I.Instance.UpdateTickers(s);
			ui_?.PostUpdate();
		}

		private void DoUpdate(float s)
		{
			I.Start(I.UpdateInput);
			{
				DoUpdateInput(s);
			}
			I.End();


			I.Start(I.UpdateObjects);
			{
				DoUpdateObjects(s);
			}
			I.End();


			I.Start(I.UpdateUi);
			{
				DoUpdateUI(s);
			}
			I.End();
		}

		public void LateUpdate(float s)
		{
			for (int i = 0; i < activePersonsArray_.Length; ++i)
				activePersonsArray_[i].LateUpdate(s);
		}

		private void DoUpdateInput(float s)
		{
			Sys.Input.Update(s);

			if (Sys.Input.HardReset)
			{
				Sys.HardReset();
				throw new PluginGone();
			}

			if (Sys.Input.ReloadPlugin)
			{
				ReloadPlugin();
				throw new PluginGone();
			}

			ui_?.CheckInput();
		}

		private void DoUpdateObjects(float s)
		{
			if (forcedPlayer_ == null)
				CheckPossess(s);

			if (!Sys.Paused)
			{
				for (int i = 0; i < everythingActive_.Count; ++i)
					everythingActive_[i].Update(s);
			}
		}

		private void CheckPossess(float s)
		{
			if (player_ != null && !player_.Possessed)
			{
				LogInfo($"{player_} no longer possessed");
				SetPlayer(null);
			}

			if (player_ == null)
			{
				for (int i = 0; i < persons_.Count; ++i)
				{
					var p = persons_[i];
					if (p.Possessed)
					{
						LogInfo($"{p} now possessed");
						SetPlayer(p);
						break;
					}
				}
			}
		}

		private void SetPlayer(Person p)
		{
			player_ = p;
		}

		private void DoUpdateUI(float s)
		{
			ui_?.Update(s);
		}

		public void OnPluginState(bool b)
		{
			LogVerbose($"cue: plugin state {b}");

			Sys.OnPluginState(b);
			ui_?.OnPluginState(b);

			for (int i = 0; i < everythingActive_.Count; ++i)
				everythingActive_[i].OnPluginState(b);

			LogVerbose($"cue: plugin state {b} finished");
		}

		static public bool LogVerboseEnabled
		{
			get { return false; }
		}

		static public void LogVerbose(string s)
		{
			if (LogVerboseEnabled)
				Instance.Sys.LogLines(s, global::Cue.Sys.LogLevels.Verbose);
		}

		static public void LogInfo(string s)
		{
			Instance.Sys.LogLines(s, global::Cue.Sys.LogLevels.Info);
		}

		static public void LogWarning(string s)
		{
			Instance.Sys.LogLines(s, global::Cue.Sys.LogLevels.Warning);
		}

		static public void LogError(string s)
		{
			Instance.Sys.LogLines(s, global::Cue.Sys.LogLevels.Error);
		}

		static public void LogErrorST(string s)
		{
			Instance.Sys.LogLines(
				s + "\n" + new StackTrace(1).ToString(),
				global::Cue.Sys.LogLevels.Error);
		}

		static public void Assert(bool b, string s = null)
		{
			if (!b)
			{
				if (s == null)
					LogErrorST("assertion failed");
				else
					LogErrorST($"assertion failed: {s}");

				Instance.DisablePlugin();
				throw new PluginGone();
			}
		}

		private void test()
		{
			var p = FindPerson("Person");
			if (p != null)
			{
				//p.Gaze.Render.FrontPlane = true;
				//p.Gaze.ForceLook = ForceLooks.Camera;
				//p.Gaze.ForceLook = ForceLooks.Camera;
				//p.Mood.FlatExcitementValue.SetForced(0);
			}
		}
	}
}
