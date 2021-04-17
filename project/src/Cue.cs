using System;
using System.Collections.Generic;
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
		private Hud hud_ = new Hud();
		private Controls controls_ = new Controls();
		private bool paused_ = false;

		private IObject hovered_ = null;
		private IObject sel_ = null;

		public Cue(CueMain main)
		{
			instance_ = this;
			main_ = main;
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

		public Hud Hud
		{
			get { return hud_; }
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
			Resources.Animations.Load();

			FindObjects();
			FindPlayer();
			Sys.Nav.Update();

			// todo
			persons_[0].TeleportTo(new Vector3(0, 0, 0));
			persons_[1].TeleportTo(new Vector3(1.7f, 0, 0));
			persons_[2].TeleportTo(new Vector3(0, 0, 1.7f));

			controls_.Create(objects_);
			OnPluginState(true);

			Select(Player);
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
						AddObject(a, m.Groups[1].Value);
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

		private void AddObject(W.IAtom a, string type)
		{
			BasicObject o = new BasicObject(a);
			var s = new Slot();

			if (type == "sit")
				o.SitSlot = s;
			if (type == "stand")
				o.StandSlot = s;

			objects_.Add(o);
			allObjects_.Add(o);
		}

		public void ReloadPlugin()
		{
			Sys.ReloadPlugin();
		}

		public void Update()
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
					allObjects_[i].Update(Sys.Time.deltaTime);
			}

			controls_.Update();
			hud_.Update();
		}

		public void FixedUpdate()
		{
			if (Sys.Paused)
				return;

			for (int i = 0; i < allObjects_.Count; ++i)
				allObjects_[i].FixedUpdate(Sys.Time.deltaTime);
		}

		public void OnPluginState(bool b)
		{
			Sys.OnPluginState(b);
			controls_.Enabled = b;

			if (b)
				hud_.Create();
			else
				hud_.Destroy();

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
	}
}
