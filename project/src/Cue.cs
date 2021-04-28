﻿using System.Collections.Generic;
using System.Diagnostics;

namespace Cue
{
	class Cue
	{
		public delegate void ObjectHandler(IObject o);
		public event ObjectHandler SelectionChanged;
		public event ObjectHandler HoveredChanged;

		private static Cue instance_ = null;

		private Person player_ = null;
		private readonly List<Person> persons_ = new List<Person>();
		private readonly List<IObject> objects_ = new List<IObject>();
		private readonly List<IObject> allObjects_ = new List<IObject>();
		private Hud hud_ = null;
		private Menu menu_ = null;
		private Controls controls_ = null;
		private bool paused_ = false;
		private bool vr_ = false;

		private IObject hovered_ = null;
		private IObject sel_ = null;

		public Cue()
		{
			instance_ = this;
			vr_ = Sys.IsVR;
			hud_ = new Hud();
			menu_ = new Menu();
			controls_ = new Controls();
		}

		public static Cue Instance
		{
			get { return instance_; }
		}

		public W.ISys Sys
		{
			get { return CueMain.Instance.Sys; }
		}

		public W.VamSys VamSys
		{
			get { return CueMain.Instance.Sys as W.VamSys; }
		}

		public List<IObject> AllObjects
		{
			get { return allObjects_; }
		}

		public List<IObject> Objects
		{
			get { return objects_; }
		}

		public List<Person> Persons
		{
			get { return persons_; }
		}

		public Person Player
		{
			get { return player_; }
		}

		public IObject Selected
		{
			get { return sel_; }
		}

		public IObject Hovered
		{
			get { return hovered_; }
		}

		public Controls Controls
		{
			get { return controls_; }
		}

		public void Select(IObject o)
		{
			if (sel_ != o)
			{
				sel_ = o;
				SelectionChanged?.Invoke(sel_);

				foreach (var p in persons_)
				{
					if (p == o)
					{
						p.Gaze.LookAt = GazeSettings.LookAtDisabled;
					}
					else if (o is Person)
					{
						p.Gaze.LookAt = GazeSettings.LookAtTarget;
						p.Gaze.Target = ((Person)o).HeadPosition;
					}
				}
			}
		}

		public void Hover(IObject o)
		{
			if (hovered_ != o)
			{
				hovered_ = o;
				HoveredChanged?.Invoke(hovered_);
			}
		}

		public void Init()
		{
			VUI.Glue.Set(
				() => CueMain.Instance.MVRPluginManager,
				(s, ps) => Strings.Get(s, ps),
				(s) => LogVerbose(s),
				(s) => LogInfo(s),
				(s) => LogWarning(s),
				(s) => LogError(s));

			Resources.Animations.Load();
			Resources.Clothing.Load();

			FindObjects();
			FindPlayer();
			Sys.Nav.Update();
			MoveToSpawnPoints();
			OnPluginState(true);

			if (Sys.GetAtom("cuetest") != null)
				TestStuff();
		}

		private void MoveToSpawnPoints()
		{
			var spawnPoints = new List<Slot>();
			foreach (var o in objects_)
				spawnPoints.AddRange(o.Slots.GetAll(Slot.Spawn));

			for (int i = 0; i < persons_.Count; ++i)
			{
				if (i >= spawnPoints.Count)
					break;

				persons_[i].TeleportTo(
					spawnPoints[i].Position, spawnPoints[i].Bearing);
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

		private void TestStuff()
		{
			var a = FindPerson("A");

			a.Personality = new QuirkyPersonality(a);
			a.AI.Mood.State = Mood.Happy;
			//a.Clothing.GenitalsVisible = true;
			Select(a);

			//U.DumpComponentsAndDown(t);

			//foreach (var a in Sys.GetAtoms())
			//{
			//	var va = ((W.VamAtom)a).Atom;
			//	if (va.type == "Person")
			//		va.GetStorableByID("PosePresets").GetAction("LoadPreset").actionCallback.Invoke();
			//}
			//
			//Player.State.Set(PersonState.Sitting);
			//Player.Clothing.GenitalsVisible = true;
			//
			//foreach (var p in persons_)
			//{
			//	if (p.ID == "A")
			//	{
			//		p.State.Set(PersonState.SittingStraddling);
			//		p.Clothing.GenitalsVisible = true;
			//		p.AI.RunEvent(new SexEvent(p, Player, SexEvent.ActiveState));
			//	}
			//}
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

			for (int i = 0; i < allObjects_.Count; ++i)
				allObjects_[i].FixedUpdate(s);
		}

		public void Update(float s)
		{
			Sys.Input.Update();

			if (Sys.Input.HardReset)
			{
				Sys.HardReset();
				return;
			}

			if (Sys.Input.ReloadPlugin)
			{
				ReloadPlugin();
				return;
			}

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

			var vr = Sys.IsVR;
			if (vr_ != vr)
			{
				vr_ = vr;
				hud_?.Destroy();
				hud_?.Create(vr_);
				menu_?.Destroy();
				menu_?.Create(vr_);
			}

			CheckInput();

			controls_?.Update();
			hud_?.Update();
			menu_?.Update();
		}

		private void CheckInput()
		{
			if (Sys.IsPlayMode)
			{
				if (Sys.Input.ToggleMenu)
					menu_?.Toggle();

				var h = Sys.Input.GetHovered();

				Hover(h.o);

				if (Sys.Input.Select)
					Select(Hovered);

				if (h.hit)
				{
					controls_.HoverTargetVisible = true;
					controls_.HoverTargetPosition = h.pos;
				}
				else
				{
					controls_.HoverTargetVisible = false;
				}

				if (Sys.Input.Action)
				{
					var p = Selected as Person;
					if (p != null)
					{
						var o = Hovered;
						if (o != null)
							p.InteractWith(o);
						else if (h.hit)
							p.MoveTo(h.pos, BasicObject.NoBearing);
					}
				}
			}

			if (Sys.Input.ToggleControls)
				controls_.Visible = !controls_.Visible;
		}

		public void OnPluginState(bool b)
		{
			Sys.OnPluginState(b);

			if (b)
			{
				hud_?.Create(Sys.IsVR);
				menu_?.Create(Sys.IsVR);
				controls_.Create();
			}
			else
			{
				hud_?.Destroy();
				menu_?.Destroy();
				controls_.Destroy();
			}

			for (int i = 0; i < allObjects_.Count; ++i)
				allObjects_[i].OnPluginState(b);
		}

		static public void LogVerbose(string s)
		{
			//Instance.Sys.Log.Verbose(s);
		}

		static public void LogInfo(string s)
		{
			Instance.Sys.Log.Info(s);
		}

		static public void LogWarning(string s)
		{
			Instance.Sys.Log.Error(s);
		}

		static public void LogError(string s)
		{
			if (Instance?.Sys?.Log == null)
				SuperController.LogError(s);
			else
				Instance.Sys.Log.Error(s);
		}

		static public void LogErrorST(string s)
		{
			Instance.Sys.Log.Error(
				s + "\n" + new StackTrace(1).ToString());
		}
	}
}
