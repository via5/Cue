using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Cue
{
	class PluginGone : Exception { }

	class Tickers
	{
		public Ticker update = new Ticker();
		public Ticker fixedUpdate = new Ticker();
		public Ticker input = new Ticker();
		public Ticker objects = new Ticker();
		public Ticker ui = new Ticker();
	}

	class Cue
	{
		private static Cue instance_ = null;

		private readonly List<Person> persons_ = new List<Person>();
		private readonly List<IObject> objects_ = new List<IObject>();
		private readonly List<IObject> allObjects_ = new List<IObject>();
		private readonly UI ui_;
		private readonly Tickers tickers_ = new Tickers();

		private Person player_ = null;
		private bool paused_ = false;

		public Cue()
		{
			instance_ = this;
			LogInfo("cue: ctor");

			ui_ = new UI(Sys);
		}

		public static Cue Instance { get { return instance_; } }

		public Tickers Tickers { get { return tickers_; } }
		public W.ISys Sys { get { return CueMain.Instance.Sys; } }
		public W.VamSys VamSys { get { return Sys as W.VamSys; } }

		public List<IObject> AllObjects { get { return allObjects_; } }
		public List<IObject> Objects { get { return objects_; } }
		public List<Person> Persons { get { return persons_; } }
		public Person Player { get { return player_; } }
		public UI UI { get { return ui_; } }

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
			LogInfo("cue: init");

			VUI.Glue.Set(
				() => CueMain.Instance.MVRPluginManager,
				(s, ps) => Strings.Get(s, ps),
				(s) => LogVerbose(s),
				(s) => LogInfo(s),
				(s) => LogWarning(s),
				(s) => LogError(s));

			LogInfo("cue: loading resources");
			Resources.Animations.Load();
			Resources.Clothing.Load();

			LogInfo("cue: updating nav");
			Sys.Nav.Update();

			LogInfo("cue: finding objects");
			FindObjects();
			FindPlayer();

			LogInfo("cue: initializing persons");
			InitPersons();

			LogInfo("cue: enabling plugin state");
			OnPluginState(true);

			if (Sys.GetAtom("cuetest") != null)
			{
				try
				{
					LogInfo("cue: test stuff");
					test();
				}
				catch (Exception e)
				{
					LogError("cue: test stuff failed, " + e.ToString());
				}
			}

			LogInfo("cue: init finished");
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
					var o = BasicObject.TryCreateFromSlot(a);
					if (o != null)
						AddObject(o);
				}
			}
		}

		private void FindPlayer()
		{
			foreach (var p in persons_)
			{
				if (p.ID == "Player")
					player_ = p;
			}

			if (player_ == null)
				LogError("no atom 'Player' found");
		}

		private void InitPersons()
		{
			var spawnPoints = new List<Slot>();
			foreach (var o in objects_)
				spawnPoints.AddRange(o.Slots.GetAll(Slot.Spawn));

			for (int i = 0; i < persons_.Count; ++i)
			{
				var p = persons_[i];

				if (i < spawnPoints.Count)
					p.TeleportTo(spawnPoints[i].Position, spawnPoints[i].Bearing);

				p.LookAtDefault();
			}
		}

		private void AddPerson(W.IAtom a)
		{
			var p = new Person(a);
			persons_.Add(p);
			allObjects_.Add(p);
		}

		private void AddObject(IObject o)
		{
			objects_.Add(o);
			allObjects_.Add(o);
		}

		public void ReloadPlugin()
		{
			Sys.ReloadPlugin();
		}

		public void DisablePlugin()
		{
			CueMain.Instance.DisablePlugin();
		}

		public void FixedUpdate(float s)
		{
			if (Sys.Paused)
				return;

			tickers_.fixedUpdate.Do(s, () => DoFixedUpdate(s));
		}

		private void DoFixedUpdate(float s)
		{
			for (int i = 0; i < allObjects_.Count; ++i)
				allObjects_[i].FixedUpdate(s);
		}

		public void Update(float s)
		{
			tickers_.update.Do(s, () => DoUpdate(s));
		}

		private void DoUpdate(float s)
		{
			tickers_.input.Do(s, () => DoUpdateInput(s));
			tickers_.objects.Do(s, () => DoUpdateObjects(s));
			tickers_.ui.Do(s, () => DoUpdateUI(s));
		}

		private void DoUpdateInput(float s)
		{
			Sys.Input.Update();

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

			ui_.CheckInput();
		}

		private void DoUpdateObjects(float s)
		{
			if (Sys.Paused != paused_)
			{
				paused_ = Sys.Paused;
				for (int i = 0; i < allObjects_.Count; ++i)
					allObjects_[i].SetPaused(Sys.Paused);
			}

			if (!Sys.Paused)
			{
				for (int i = 0; i < allObjects_.Count; ++i)
					allObjects_[i].Update(s);
			}
		}

		private void DoUpdateUI(float s)
		{
			ui_.Update(s);
		}

		public void OnPluginState(bool b)
		{
			LogInfo($"cue: plugin state {b}");

			Sys.OnPluginState(b);
			ui_.OnPluginState(b);

			for (int i = 0; i < allObjects_.Count; ++i)
				allObjects_[i].OnPluginState(b);

			LogInfo($"cue: plugin state {b} finished");
		}

		static public void LogVerbose(string s)
		{
			//Instance.Sys.Log(s, LogLevels.Verbose);
		}

		static public void LogInfo(string s)
		{
			Instance.Sys.Log(s, W.LogLevels.Info);
		}

		static public void LogWarning(string s)
		{
			Instance.Sys.Log(s, W.LogLevels.Warning);
		}

		static public void LogError(string s)
		{
			Instance.Sys.Log(s, W.LogLevels.Error);
		}

		static public void LogErrorST(string s)
		{
			Instance.Sys.Log(s + "\n" + new StackTrace(1).ToString(), W.LogLevels.Error);
		}

		private void test()
		{
		}
	}
}
