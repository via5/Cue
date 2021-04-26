using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Cue
{
	class Cue
	{
		public delegate void ObjectHandler(IObject o);
		public event ObjectHandler SelectionChanged;
		public event ObjectHandler HoveredChanged;

		private static Cue instance_ = null;

		private CueMain main_;
		private Person player_ = null;
		private readonly List<Person> persons_ = new List<Person>();
		private readonly List<IObject> objects_ = new List<IObject>();
		private readonly List<IObject> allObjects_ = new List<IObject>();
		private IHud hud_;
		private IMenu menu_;
		private IControls controls_;
		private bool paused_ = false;
		private bool vr_ = false;

		private IObject hovered_ = null;
		private IObject sel_ = null;

		public Cue(CueMain main)
		{
			instance_ = this;
			main_ = main;
			vr_ = Sys.IsVR;

			if (Sys is W.MockSys)
			{
				hud_ = new MockHud();
				controls_ = new MockControls();
			}
			else
			{
				hud_ = new Hud();
				menu_ = new Menu();
				controls_ = new Controls();
			}
		}

		public static Cue Instance
		{
			get { return instance_; }
		}

		public W.ISys Sys
		{
			get { return main_.Sys; }
		}

		public W.VamSys VamSys
		{
			get { return main_.Sys as W.VamSys; }
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

		public IHud Hud
		{
			get { return hud_; }
		}

		public IControls Controls
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


			var spawnPoints = new List<Slot>();

			foreach (var o in objects_)
				spawnPoints.AddRange(o.Slots.GetAll(Slot.Spawn));

			for (int i = 0; i < persons_.Count; ++i)
			{
				if (i >= spawnPoints.Count)
					break;

				persons_[i].TeleportTo(spawnPoints[i].Position, spawnPoints[i].Bearing);
			}

			OnPluginState(true);
			Select(Player);

			if (Sys.GetAtom("cuetest") != null)
				TestStuff();
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
			a.AI.Mood.State = Mood.Idle;

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
			var re = new Regex(@"cue!([a-zA-Z]+)#?.*");

			foreach (var a in Sys.GetAtoms())
			{
				if (a.IsPerson)
				{
					AddPerson(a);
				}
				else
				{
					var m = re.Match(a.ID);
					if (m != null && m.Success)
					{
						var typeName = m.Groups[1].Value;

						var type = Slot.TypeFromString(typeName);
						if (type == Slot.NoType)
							LogError("bad object type '" + typeName + "'");
						else
							AddObject(a, type);
					}
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

		private void AddObject(W.IAtom a, int type)
		{
			BasicObject o = new BasicObject(a);
			o.Slots.Add(type);

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
				hud_.Destroy();
				hud_.Create(vr_);

				menu_.Destroy();
				menu_.Create(vr_);
			}

			CheckInput();

			controls_.Update();
			hud_.Update();
			menu_.Update();
		}

		private void CheckInput()
		{
			if (!Sys.IsPlayMode)
				return;

			if (Sys.Input.MenuToggle)
				menu_.Toggle();

			Hover(Sys.Input.GetHovered());

			if (Sys.Input.Select)
				Select(Hovered);

			if (Sys.Input.Action)
			{
				var p = Selected as Person;
				if (p == null)
					return;

				var o = Hovered;
				if (o == null)
					return;

				p.InteractWith(o);
			}

			controls_.Visible = Sys.Input.ShowControls;
		}

		public void OnPluginState(bool b)
		{
			Sys.OnPluginState(b);

			if (b)
			{
				hud_.Create(Sys.IsVR);
				menu_.Create(Sys.IsVR);
				controls_.Create();
			}
			else
			{
				hud_.Destroy();
				menu_.Destroy();
				controls_.Destroy();
			}

			for (int i = 0; i < allObjects_.Count; ++i)
				allObjects_[i].OnPluginState(b);
		}

		static public void LogVerbose(string s)
		{
			//Instance.sys_.Log.Verbose(s);
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
			Instance.Sys.Log.Error(s);
		}

		static public void LogErrorST(string s)
		{
			Instance.Sys.Log.Error(
				s + "\n" + new StackTrace(1).ToString());
		}
	}
}
