﻿using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Cue
{
	class Version
	{
		public const int Major = 0;
		public const int Minor = 1;

		public static string String
		{
			get
			{
				return Major.ToString() + "." + Minor.ToString();
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


	class Instrumentation
	{
		private Ticker[] tickers_ = new Ticker[I.TickerCount];
		private int[] depth_ = new int[I.TickerCount]
		{
			0, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1
		};
		private int[] stack_ = new int[3];
		private int current_ = 0;

		public Instrumentation()
		{
			tickers_[I.Update] = new Ticker("Update");
			tickers_[I.UpdateInput] = new Ticker("Input");
			tickers_[I.UpdateObjects] = new Ticker("Objects");
			tickers_[I.UpdateObjectsAtoms] = new Ticker("Atoms");
			tickers_[I.UpdateObjectsMove] = new Ticker("Move");
			tickers_[I.UpdatePersonTransitions] = new Ticker("Transitions");
			tickers_[I.UpdatePersonActions] = new Ticker("Actions");
			tickers_[I.UpdatePersonGaze] = new Ticker("Gaze");
			tickers_[I.UpdatePersonEvents] = new Ticker("Events");
			tickers_[I.UpdatePersonExcitement] = new Ticker("Excitement");
			tickers_[I.UpdatePersonPersonality] = new Ticker("Personality");
			tickers_[I.UpdatePersonMood] = new Ticker("Mood");
			tickers_[I.UpdatePersonBody] = new Ticker("Body");
			tickers_[I.UpdatePersonAI] = new Ticker("AI");
			tickers_[I.UpdateUi] = new Ticker("UI");
			tickers_[I.FixedUpdate] = new Ticker("Fixed update");
		}

		public bool Updated
		{
			get { return tickers_[I.Update].Updated; }
		}

		public void Start(int i)
		{
			stack_[current_] = i;
			++current_;

			tickers_[i].Start();
		}

		public void End()
		{
			--current_;

			int i = stack_[current_];
			stack_[current_] = -1;

			tickers_[i].End();
		}

		public int Depth(int i)
		{
			return depth_[i];
		}

		public string Name(int i)
		{
			return tickers_[i].Name;
		}

		public Ticker Get(int i)
		{
			return tickers_[i];
		}

		public void UpdateTickers(float s)
		{
			for (int i = 0; i < tickers_.Length; ++i)
				tickers_[i].Update(s);
		}
	}


	static class I
	{
		public const int Update = 0;
		public const int UpdateInput = 1;
		public const int UpdateObjects = 2;
		public const int UpdateObjectsAtoms = 3;
		public const int UpdateObjectsMove = 4;
		public const int UpdatePersonTransitions = 5;
		public const int UpdatePersonActions = 6;
		public const int UpdatePersonGaze = 7;
		public const int UpdatePersonEvents = 8;
		public const int UpdatePersonExcitement = 9;
		public const int UpdatePersonPersonality = 10;
		public const int UpdatePersonMood = 11;
		public const int UpdatePersonBody = 12;
		public const int UpdatePersonAI = 13;
		public const int UpdateUi = 14;
		public const int FixedUpdate = 15;
		public const int TickerCount = 16;



		private static Instrumentation instance_ = new Instrumentation();

		public static Instrumentation Instance
		{
			get { return instance_; }
		}

		public static void Start(int i)
		{
			instance_.Start(i);
		}

		public static void End()
		{
			instance_.End();
		}
	}


	class Options
	{
		private bool allowMovement_ = false;

		public void Init(JSONClass r)
		{
			var o = r["options"].AsObject;

			allowMovement_ = o["allowMovement"].AsBool;
		}

		public bool AllowMovement
		{
			get { return allowMovement_; }
		}
	}


	class Cue
	{
		private static Cue instance_ = null;

		private readonly List<Person> persons_ = new List<Person>();
		private readonly List<Person> activePersons_ = new List<Person>();
		private Person[] activePersonsArray_ = new Person[0];

		private readonly List<IObject> objects_ = new List<IObject>();
		private readonly List<IObject> activeObjects_ = new List<IObject>();
		private IObject[] activeObjectsArray_ = new IObject[0];

		private readonly List<IObject> everything_ = new List<IObject>();
		private readonly List<IObject> everythingActive_ = new List<IObject>();
		private IObject[] everythingActiveArray_ = new IObject[0];

		private readonly UI ui_;
		private readonly Options options_ = new Options();

		private Person player_ = null;
		private bool paused_ = false;
		private int frame_ = 0;

		private Sys.ILiveSaver saver_ = new Sys.Vam.LiveSaver();


		public Cue()
		{
			instance_ = this;
			LogVerbose("cue: ctor");

			if (Sys.HasUI)
				ui_ = new UI(Sys);
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


		public UI UI { get { return ui_; } }
		public Options Options { get { return options_; } }

		public int Frame { get { return frame_; } }

		public Person Player
		{
			get { return player_; }
		}

		public Person GetPerson(int i)
		{
			return persons_[i];
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

		public void Init()
		{
			LogVerbose($"cue: init (token {CueMain.Instance.Token})");

			VUI.Glue.Set(
				() => CueMain.Instance.MVRPluginManager,
				(s, ps) => Strings.Get(s, ps),
				(s) => LogVerbose(s),
				(s) => LogInfo(s),
				(s) => LogWarning(s),
				(s) => LogError(s));

			var conf = Sys.GetConfig() ?? new JSONClass();
			options_.Init(conf);

			LogVerbose("cue: loading resources");
			Resources.LoadAll();

			LogVerbose("cue: updating nav");
			Sys.Nav.Update();

			LogVerbose("cue: finding objects");
			FindObjects();

			LogVerbose("cue: initializing persons");
			InitPersons();

			LogVerbose("cue: checking config");
			CheckConfig();

			LogVerbose("cue: enabling plugin state");
			OnPluginState(true);

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

			LogInfo("cue: running");
		}

		private void CheckConfig()
		{
			Load(saver_.Load());
			Save();
		}

		public void Save()
		{
			var oo = new JSONClass();
			Save(oo);
			saver_.Save(oo);
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

			var ui = ui_.ToJSON();
			if (ui != null)
				json.Add("ui", ui);
		}

		private void Load(JSONClass c)
		{
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
				{
					AddPerson(a);
				}
				else
				{
					var o = BasicObject.TryCreateFromSlot(everything_.Count, a);
					if (o != null)
						AddObject(o);
				}
			}

			activePersonsArray_ = activePersons_.ToArray();
			activeObjectsArray_ = activeObjects_.ToArray();
			everythingActiveArray_ = everythingActive_.ToArray();
		}

		private void InitPersons()
		{
			var spawnPoints = new List<Slot>();
			foreach (var o in objects_)
				spawnPoints.AddRange(o.Slots.GetAll(Slot.Spawn));

			for (int i = 0; i < persons_.Count; ++i)
			{
				var p = persons_[i];

				if (Options.AllowMovement)
				{
					if (i < spawnPoints.Count)
					{
						p.TeleportTo(
							spawnPoints[i].Position,
							spawnPoints[i].Rotation.Bearing);
					}
				}

				p.Init();
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
			if (Sys.Paused)
				return;

			++frame_;

			I.Start(I.FixedUpdate);
			{
				DoFixedUpdate(s);
			}
			I.End();
		}

		private void DoFixedUpdate(float s)
		{
			for (int i = 0; i < everythingActive_.Count; ++i)
				everythingActive_[i].FixedUpdate(s);
		}

		public void Update(float s)
		{
			I.Start(I.Update);
			{
				DoUpdate(s);
			}
			I.End();

			I.Instance.UpdateTickers(s);
			ui_.PostUpdate();
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
			if (Sys.Paused != paused_)
			{
				paused_ = Sys.Paused;
				for (int i = 0; i < everythingActive_.Count; ++i)
					everythingActive_[i].SetPaused(Sys.Paused);
			}

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
				Instance.Sys.Log(s, global::Cue.Sys.LogLevels.Verbose);
		}

		static public void LogInfo(string s)
		{
			Instance.Sys.Log(s, global::Cue.Sys.LogLevels.Info);
		}

		static public void LogWarning(string s)
		{
			Instance.Sys.Log(s, global::Cue.Sys.LogLevels.Warning);
		}

		static public void LogError(string s)
		{
			Instance.Sys.Log(s, global::Cue.Sys.LogLevels.Error);
		}

		static public void LogErrorST(string s)
		{
			Instance.Sys.Log(
				s + "\n" + new StackTrace(1).ToString(),
				global::Cue.Sys.LogLevels.Error);
		}

		static public void Assert(bool b)
		{
			if (!b)
			{
				LogErrorST("assertion failed");
				Instance.DisablePlugin();
				throw new PluginGone();
			}
		}

		private void test()
		{
			var p = FindPerson("A");
			if (p != null)
			{
				//p.Gaze.ForceLook = ForceLooks.Camera;
				//p.Mood.FlatExcitementValue.SetForced(0);
			}
		}
	}
}
