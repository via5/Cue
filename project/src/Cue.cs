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
		//public delegate void ObjectHandler(IObject o);
		//public event ObjectHandler SelectionChanged;
		//public event ObjectHandler HoveredChanged;

		private static Cue instance_ = null;

		private Person player_ = null;
		private readonly List<Person> persons_ = new List<Person>();
		private readonly List<IObject> objects_ = new List<IObject>();
		private readonly List<IObject> allObjects_ = new List<IObject>();
		private Hud hud_ = null;
		private Menu leftMenu_ = null;
		private Menu rightMenu_ = null;
		private Menu desktopMenu_ = null;
		private Controls controls_ = null;
		private bool paused_ = false;
		private bool vr_ = false;
		private Tickers tickers_ = new Tickers();

		public Cue()
		{
			instance_ = this;
			LogInfo("cue: ctor");

			vr_ = Sys.IsVR;
			hud_ = new Hud();
			leftMenu_ = new Menu();
			rightMenu_ = new Menu();
			desktopMenu_ = new Menu();
			controls_ = new Controls();
		}

		public static Cue Instance
		{
			get { return instance_; }
		}

		public Tickers Tickers
		{
			get { return tickers_; }
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

		public Controls Controls
		{
			get { return controls_; }
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
					TestStuff();
				}
				catch (Exception e)
				{
					LogError("cue: test stuff failed, " + e.ToString());
				}
			}

			LogInfo("cue: init finished");
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
			//a.Clothing.GenitalsVisible = true;
			//Select(a);

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
			tickers_.update.Do(s, () =>
			{
				tickers_.input.Do(s, () =>
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

					var vr = Sys.IsVR;
					if (vr_ != vr)
					{
						vr_ = vr;
						DestroyUI();
						CreateUI();
					}

					CheckInput();
				});

				tickers_.objects.Do(s, () =>
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
				});

				tickers_.ui.Do(s, () =>
				{
					controls_?.Update();
					hud_?.Update();
					leftMenu_?.Update();
					rightMenu_?.Update();
					desktopMenu_?.Update();
				});
			});
		}

		private void CheckInput()
		{
			if (Sys.IsPlayMode)
			{
				//if (Sys.Input.ToggleMenu)
				//	menu_?.Toggle();

				if (Sys.IsVR)
					CheckVRInput();
				else
					CheckDesktopInput();
			}
		}

		private void CheckVRInput()
		{
			var lh = Sys.Input.GetLeftHovered();
			var rh = Sys.Input.GetRightHovered();

			if (Sys.Input.ShowLeftMenu)
			{
				controls_.HoverTargetVisible = true;
				controls_.HoverTargetPosition = rh.pos;
			}
			else if (Sys.Input.ShowRightMenu)
			{
				controls_.HoverTargetVisible = true;
				controls_.HoverTargetPosition = lh.pos;
			}


			if (lh.o != null)
			{
				leftMenu_.Visible = Sys.Input.ShowLeftMenu;
				leftMenu_.Object = lh.o as Person;

				rightMenu_.Visible = false;
				rightMenu_.Object = null;

				if (Sys.Input.RightAction)
				{
					LogInfo($"right action {lh.o} {rh.o}");

					if (rh.o != null)
					{
						LogInfo($"interacting {lh.o} with {rh.o}");
						lh.o.InteractWith(rh.o);
					}
					else if (rh.hit)
					{
						LogInfo($"hit for {lh.o} on {rh.pos}");
						lh.o.MoveTo(
							rh.pos,
							Vector3.Bearing(rh.pos - lh.o.Position));
					}
					else
					{
						LogInfo($"nothing");
					}
				}
			}
			else if (rh.o != null)
			{
				leftMenu_.Visible = false;
				leftMenu_.Object = null;

				rightMenu_.Visible = Sys.Input.ShowRightMenu;
				rightMenu_.Object = rh.o as Person;

				if (Sys.Input.LeftAction)
				{
					LogInfo($"left action {lh.o} {rh.o}");

					controls_.HoverTargetVisible = true;
					controls_.HoverTargetPosition = lh.pos;

					if (lh.o != null)
					{
						LogInfo($"interacting {rh.o} with {lh.o}");
						rh.o.InteractWith(lh.o);
					}
					else if (lh.hit)
					{
						LogInfo($"hit for {rh.o} on {lh.pos}");
						rh.o.MoveTo(
							lh.pos,
							Vector3.Bearing(lh.pos - rh.o.Position));
					}
					else
					{
						LogInfo($"nothing");
					}
				}
			}
			else
			{
				leftMenu_.Visible = false;
				leftMenu_.Object = null;

				rightMenu_.Visible = false;
				rightMenu_.Object = null;
			}
		}

		private void CheckDesktopInput()
		{
			var h = Sys.Input.GetLeftHovered();

			if (h.hit)
			{
				controls_.HoverTargetVisible = true;
				controls_.HoverTargetPosition = h.pos;
			}
			else
			{
				controls_.HoverTargetVisible = false;
			}

			if (Sys.Input.LeftAction)
			{
				LogInfo("Action");

				//var p = Selected as Person;
				//if (p != null)
				//{
				//	var o = Hovered;
				//	if (o != null)
				//	{
				//		p.InteractWith(o);
				//	}
				//	else if (h.hit)
				//	{
				//		p.MoveTo(
				//			h.pos,
				//			Vector3.Bearing(h.pos - p.Position));
				//	}
				//}
			}

			if (Sys.Input.ToggleControls)
				controls_.Visible = !controls_.Visible;
		}

		public void OnPluginState(bool b)
		{
			LogInfo($"cue: plugin state {b}");
			Sys.OnPluginState(b);

			if (b)
				CreateUI();
			else
				DestroyUI();

			for (int i = 0; i < allObjects_.Count; ++i)
				allObjects_[i].OnPluginState(b);

			LogInfo($"cue: plugin state {b} finished");
		}

		private void DestroyUI()
		{
			controls_?.Destroy();
			hud_?.Destroy();
			leftMenu_?.Destroy();
			rightMenu_?.Destroy();
			desktopMenu_?.Destroy();
		}

		private void CreateUI()
		{
			controls_.Create();
			hud_?.Create(vr_);

			if (vr_)
			{
				leftMenu_?.Create(vr_, true);
				rightMenu_?.Create(vr_, false);
			}
			else
			{
				desktopMenu_?.Create(false, false);
				desktopMenu_.Visible = true;
			}
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
	}
}
