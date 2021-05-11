﻿using System;
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
		private Person forcedPlayer_ = null;
		private bool paused_ = false;

		public Cue()
		{
			instance_ = this;
			LogVerbose("cue: ctor");

			ui_ = new UI(Sys);
		}

		public static Cue Instance { get { return instance_; } }

		public Tickers Tickers { get { return tickers_; } }
		public W.ISys Sys { get { return CueMain.Instance.Sys; } }
		public W.VamSys VamSys { get { return Sys as W.VamSys; } }

		public List<IObject> AllObjects { get { return allObjects_; } }
		public List<IObject> Objects { get { return objects_; } }
		public List<Person> Persons { get { return persons_; } }
		public UI UI { get { return ui_; } }

		public Person Player
		{
			get
			{
				if (player_ != null)
					return player_;
				else
					return forcedPlayer_;
			}
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

		public Vector3 InteractiveLeftHandPosition
		{
			get
			{
				if (Sys.IsVR)
					return Sys.InteractiveLeftHandPosition;
				else if (Player != null)
					return Player.Body.Get(BodyParts.LeftHand).Position;
				else
					return Vector3.Zero;
			}
		}

		public Vector3 InteractiveRightHandPosition
		{
			get
			{
				if (Sys.IsVR)
					return Sys.InteractiveRightHandPosition;
				else if (Player != null)
					return Player.Body.Get(BodyParts.RightHand).Position;
				else
					return Vector3.Zero;
			}
		}

		public void Init()
		{
			LogVerbose("cue: init");

			VUI.Glue.Set(
				() => CueMain.Instance.MVRPluginManager,
				(s, ps) => Strings.Get(s, ps),
				(s) => LogVerbose(s),
				(s) => LogInfo(s),
				(s) => LogWarning(s),
				(s) => LogError(s));

			LogVerbose("cue: loading resources");
			Resources.Animations.Load();
			Resources.Clothing.Load();

			LogVerbose("cue: updating nav");
			Sys.Nav.Update();

			LogVerbose("cue: finding objects");
			FindObjects();

			LogVerbose("cue: initializing persons");
			InitPersons();

			LogVerbose("cue: enabling plugin state");
			OnPluginState(true);

			if (Sys.GetAtom("cuetest") != null)
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

			foreach (var p in persons_)
			{
				if (p.ID == "Player")
					forcedPlayer_ = p;
			}
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

				p.Gaze.LookAtDefault();
				p.SetState(PersonState.Standing);
				p.Clothing.Init();

				if (p == Player)
				{
					p.AI.EventsEnabled = false;
					p.AI.InteractionsEnabled = false;
				}
				else
				{
					p.Personality = new TsunderePersonality(p);
				}

				p.Atom.Init();
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
				CheckPossess(s);

				for (int i = 0; i < allObjects_.Count; ++i)
					allObjects_[i].Update(s);
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
					if (persons_[i].Possessed)
					{
						LogInfo($"{persons_[i]} now possessed");
						SetPlayer(persons_[i]);
						break;
					}
				}
			}
		}

		private void SetPlayer(Person p)
		{
			player_ = p;

			for (int i = 0; i < persons_.Count; ++i)
			{
				if (!persons_[i].Gaze.HasInterestingTarget)
					persons_[i].Gaze.LookAtDefault();
			}
		}

		private void DoUpdateUI(float s)
		{
			ui_.Update(s);
		}

		public void OnPluginState(bool b)
		{
			LogVerbose($"cue: plugin state {b}");

			Sys.OnPluginState(b);
			ui_.OnPluginState(b);

			for (int i = 0; i < allObjects_.Count; ++i)
				allObjects_[i].OnPluginState(b);

			LogVerbose($"cue: plugin state {b} finished");
		}

		static public void LogVerbose(string s)
		{
			//Instance.Sys.Log(s, W.LogLevels.Verbose);
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
