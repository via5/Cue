using SimpleJSON;
using System;
using System.Collections.Generic;

namespace Cue
{
	class PluginGone : Exception { }

	public class Cue
	{
		private const float UpdateActivePersonsInterval = 5.0f;

		private static Cue instance_ = null;

		public delegate void PlayerChangedCallback(Person oldPerson, Person newPerson);
		public event PlayerChangedCallback PlayerChanged;

		private Logger log_, vuiLog_;
		private Options options_;
		private float saveElapsed_ = 0;
		private bool needSave_ = false;
		private const float SaveLaterDelay = 0.2f;

		private readonly List<Person> persons_ = new List<Person>();
		private readonly List<Person> activePersons_ = new List<Person>();
		private Person[] activePersonsArray_ = new Person[0];
		private float updateActivePersonsElapsed_ = 0;

		private readonly List<IObject> objects_ = new List<IObject>();
		private readonly List<IObject> activeObjects_ = new List<IObject>();
		private IObject[] activeObjectsArray_ = new IObject[0];

		private readonly List<IObject> everything_ = new List<IObject>();
		private readonly List<IObject> everythingActive_ = new List<IObject>();
		private IObject[] everythingActiveArray_ = new IObject[0];

		private UI ui_;
		private bool loaded_ = false;
		private bool objectsFound_ = false;

		private Person player_ = null;
		private Person forcedPlayer_ = null;
		private int frame_ = 0;
		private Finish finish_ = new Finish();

		private Sys.ILiveSaver saver_;


		public Cue()
		{
			instance_ = this;
			log_ = new Logger(Logger.Main, "cue");
			vuiLog_ = new Logger(Logger.Main, "vui");

			Log.Verbose("cue: ctor");
		}

		public static Cue Instance { get { return instance_; } }

		public Logger Log { get { return log_; } }

		public Sys.ISys Sys { get { return CueMain.Instance.Sys; } }
		public Sys.Vam.VamSys VamSys { get { return Sys as Sys.Vam.VamSys; } }

		public List<Person> AllPersons
		{
			get
			{
				Assert(objectsFound_);
				return persons_;
			}
		}

		public Person[] ActivePersons
		{
			get
			{
				Assert(objectsFound_);
				return activePersonsArray_;
			}
		}

		public List<IObject> AllObjects
		{
			get
			{
				Assert(objectsFound_);
				return objects_;
			}
		}

		public IObject[] ActiveObjects
		{
			get
			{
				Assert(objectsFound_);
				return activeObjectsArray_;
			}
		}

		public List<IObject> Everything
		{
			get
			{
				Assert(objectsFound_);
				return everything_;
			}
		}

		public IObject[] EverythingActive
		{
			get
			{
				Assert(objectsFound_);
				return everythingActiveArray_;
			}
		}


		public Options Options { get { return options_; } }
		public UI UI { get { return ui_; } }
		public int Frame { get { return frame_; } }
		public Finish Finish { get { return finish_; } }

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
					Log.Info($"unforcing player");
				else
					Log.Info($"forcing player to {value}");

				SetPlayer(value, true);
			}
		}

		public IObject GetObject(int objectIndex)
		{
			Assert(objectsFound_);

			if (objectIndex < 0 || objectIndex >= everything_.Count)
				return null;
			else
				return everything_[objectIndex];
		}

		public Person GetPerson(int personIndex)
		{
			Assert(objectsFound_);
			return persons_[personIndex];
		}

		public Person FindPerson(string id)
		{
			Assert(objectsFound_);

			for (int i = 0; i < persons_.Count; ++i)
			{
				if (persons_[i].ID == id)
					return persons_[i];
			}

			return null;
		}

		public Person PersonForAtom(Sys.IAtom a)
		{
			Assert(objectsFound_);

			for (int i = 0; i < persons_.Count; ++i)
			{
				if (persons_[i].Atom == a)
					return persons_[i];
			}

			return null;
		}

		public void Init()
		{
			var start = Sys.RealtimeSinceStartup;
			Log.Verbose($"cue: init (token {CueMain.Instance.Token})");

			options_ = new Options();
			saver_ = Sys.CreateLiveSaver();

			Sys.Init();

#if !MOCK
			VUI.Root.Init(
				"Cue",
				() => CueMain.Instance.Impl.MVRPluginManager,
				(s, ps) => Strings.Get(s, ps),
				(s) => vuiLog_.Verbose(s),
				(s) => vuiLog_.Info(s),
				(s) => vuiLog_.Warning(s),
				(s) => vuiLog_.Error(s));
#endif


			Log.Verbose("cue: loading resources");
			Resources.LoadAll();

			Log.Verbose("cue: finding objects");
			FindObjects();

			Log.Verbose("cue: initializing persons");
			InitPersons();

			if (Sys.HasUI)
				ui_ = new UI(Sys);

			Log.Verbose("cue: enabling plugin state");
			OnPluginState(true);

			Log.Verbose("cue: checking config");
			CheckConfig();

			if (Sys.GetAtom("cue!test") != null)
			{
				try
				{
					Log.Verbose("cue: test stuff");
					test();
				}
				catch (Exception e)
				{
					Log.Error("cue: test stuff failed, " + e.ToString());
				}
			}

			finish_.Init();

			var end = Cue.Instance.Sys.RealtimeSinceStartup;
			Log.Info($"running, version {Version.String}, init {(end - start):0.00}s");
		}

		public bool IsSceneIdle()
		{
			foreach (var p in ActivePersons)
			{
				if (!p.Mood.IsIdle)
					return false;
			}

			return true;
		}

		public bool IsSceneEmpty()
		{
			return (ActivePersons.Length == 2);
		}

		private void CheckConfig()
		{
			var o = saver_.Load();

			if (o == null)
			{
				Log.Verbose("no livesaver, trying defaults");

				var oo = GetDefaults();
				if (oo != null)
					Options.Load(oo);
			}
			else
			{
				Load(o);
			}

			loaded_ = true;
			Save();
		}

		private JSONClass GetDefaults()
		{
			string path = Sys.MakePluginDataPath(Options.DefaultFile);

			if (!Sys.FileExists(path))
			{
				Log.Verbose($"no default options at {path}");
				return null;
			}

			var o = Sys.ReadJSON(path)?.AsObject;
			if (o == null)
				Log.Error($"can't read default options at {path}");

			Log.Info($"loading default options from {path}");

			return o;
		}

		public void Save()
		{
			if (!loaded_)
				return;

			Log.Verbose("saving");

			var oo = CreateSaveJSON();
			saver_.Save(oo);
		}

		public void SaveLater()
		{
			needSave_ = true;
			saveElapsed_ = 0;
		}

		private JSONClass CreateSaveJSON()
		{
			var json = new JSONClass();

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

			var ui = ui_?.ToJSON();
			if (ui != null)
				json.Add("ui", ui);

			json.Add("options", options_.ToJSON());

			return json;
		}

		private void Load(JSONClass c)
		{
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

			objectsFound_ = true;
		}

		private void InitPersons()
		{
			for (int i = 0; i < persons_.Count; ++i)
				persons_[i].Init();

			for (int i = 0; i < persons_.Count; ++i)
			{
				if (persons_[i].ID == "cue#forceplayer")
					ForcedPlayer = persons_[i];
			}
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

				Instrumentation.Reset();

				Instrumentation.Start(I.FixedUpdate);
				{
					DoFixedUpdate(s);
				}
				Instrumentation.End();
			}
		}

		private void DoFixedUpdate(float s)
		{
			Instrumentation.Start(I.FUSys);
			{
				Sys.FixedUpdate(s);
			}
			Instrumentation.End();


			Instrumentation.Start(I.FUObjects);
			{
				for (int i = 0; i < everythingActive_.Count; ++i)
					everythingActive_[i].FixedUpdate(s);
			}
			Instrumentation.End();
		}

		public void Update(float s)
		{
			Instrumentation.Reset();

			if (needSave_)
			{
				saveElapsed_ += s;
				if (saveElapsed_ > SaveLaterDelay)
				{
					Save();
					needSave_ = false;
				}
			}

			UpdateActivePersons(s);

			Instrumentation.Start(I.Update);
			{
				DoUpdate(s);
			}
			Instrumentation.End();


			Instrumentation.UpdateTickers(s);
			ui_?.PostUpdate();
		}

		private void DoUpdate(float s)
		{
			Instrumentation.Start(I.Input);
			{
				DoUpdateInput(s);
			}
			Instrumentation.End();


			Instrumentation.Start(I.Objects);
			{
				DoUpdateObjects(s);
			}
			Instrumentation.End();


			Instrumentation.Start(I.Finish);
			{
				Finish.Update(s);
			}
			Instrumentation.End();


			Instrumentation.Start(I.UI);
			{
				DoUpdateUI(s);
			}
			Instrumentation.End();

			Sys.Update(s);
		}

		public void LateUpdate(float s)
		{
			// don't check paused, LateUpdate is used to keep some objects in
			// the right place, like the strapon, and for hand linking

			Instrumentation.Reset();

			Instrumentation.Start(I.LateUpdate);
			{
				DoLateUpdate(s);
			}
			Instrumentation.End();
		}

		private void DoLateUpdate(float s)
		{
			for (int i = 0; i < activePersonsArray_.Length; ++i)
				activePersonsArray_[i].LateUpdate(s);

			Sys.LateUpdate(s);
		}

		private void DoUpdateInput(float s)
		{
			Sys.Input.Update(s);

			if (Options.DevMode)
			{
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
			}

			ui_?.CheckInput(s);
		}

		private void DoUpdateObjects(float s)
		{
			if (forcedPlayer_ == null)
				CheckPossess(s);

			if (Sys.Paused)
			{
				for (int i = 0; i < everythingActive_.Count; ++i)
				{
					if (everythingActive_[i].Visible)
						everythingActive_[i].UpdatePaused(s);
				}
			}
			else
			{
				for (int i = 0; i < everythingActive_.Count; ++i)
				{
					if (everythingActive_[i].Visible)
						everythingActive_[i].Update(s);
				}
			}
		}

		private void UpdateActivePersons(float s)
		{
			updateActivePersonsElapsed_ += s;

			if (updateActivePersonsElapsed_ >= UpdateActivePersonsInterval)
			{
				updateActivePersonsElapsed_ = 0;
				bool needsUpdate = false;

				foreach (var p in persons_)
				{
					if (p.Visible != activePersons_.Contains(p))
					{
						needsUpdate = true;
						break;
					}
				}

				if (needsUpdate)
				{
					Log.Info("active persons changed");

					activePersons_.Clear();

					foreach (var o in everything_)
					{
						if (o is Person && o.Visible)
							activePersons_.Add(o as Person);
					}

					activePersonsArray_ = activePersons_.ToArray();
				}
			}
		}

		private void CheckPossess(float s)
		{
			if (player_ != null && !player_.Possessed)
			{
				Log.Info($"{player_} no longer possessed");
				SetPlayer(null, false);
			}

			if (player_ == null)
			{
				for (int i = 0; i < persons_.Count; ++i)
				{
					var p = persons_[i];
					if (p.Possessed)
					{
						Log.Info($"{p} now possessed");
						SetPlayer(p, false);
						break;
					}
				}
			}
		}

		private void SetPlayer(Person p, bool forced)
		{
			if (p != null)
			{
				foreach (var e in p.AI.Events)
					e.ForceStop();
			}

			Person old;

			if (forced)
			{
				old = forcedPlayer_;
				forcedPlayer_ = p;
			}
			else
			{
				old = player_;
				player_ = p;
			}

			if (p != old)
				PlayerChanged?.Invoke(old, p);
		}

		private void DoUpdateUI(float s)
		{
			ui_?.Update(s);
		}

		public void OnPluginState(bool b)
		{
			Log.Verbose($"cue: plugin state {b}");

			Sys.OnPluginState(b);
			ui_?.OnPluginState(b);

			for (int i = 0; i < everythingActive_.Count; ++i)
				everythingActive_[i].OnPluginState(b);

			Log.Verbose($"cue: plugin state {b} finished");
		}

		static public void Assert(bool b, string s = null)
		{
			if (!b)
			{
				if (s == null)
					Logger.Global.ErrorST("assertion failed");
				else
					Logger.Global.ErrorST($"assertion failed: {s}");

				if (Instance != null)
					Instance.DisablePlugin();

				throw new PluginGone();
			}
		}

		private void test()
		{
			var p = FindPerson("Player");
			if (p != null)
				ForcedPlayer = p;

			{
				//p.Gaze.Render.FrontPlane = true;
				//p.Gaze.ForceLook = ForceLooks.Camera;
				//p.Gaze.ForceLook = ForceLooks.Camera;
				//p.Mood.FlatExcitementValue.SetForced(0);
			}
		}
	}
}
