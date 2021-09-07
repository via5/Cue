namespace Cue
{
	class Menu
	{
		private bool visible_ = false;
		private VUI.Root root_ = null;
		private VUI.Label label_ = null;
		private VUI.Panel selButtons_ = null;
		private VUI.Panel playerButtons_ = null;
		private VUI.CheckBox canKiss_ = null;
		private VUI.CheckBox forceExcitement_ = null;
		private VUI.FloatTextSlider excitement_ = null;
		private bool force_ = false;
		private IObject sel_ = null;
		private IObject hov_ = null;
		private VUI.Label fps_ = null;

		public void Create(bool vr, bool left)
		{
			if (vr)
			{
				root_ = Cue.Instance.Sys.CreateAttached(
					left,
					new Vector3(0, 0.1f, 0),
					new Point(0, 0),
					new Size(1000, 250));
			}
			else
			{
				root_ = Cue.Instance.Sys.Create2D(10, new Size(1000, 220));
			}

			canKiss_ = new VUI.CheckBox("Can kiss");
			forceExcitement_ = new VUI.CheckBox("Ex");
			excitement_ = new VUI.FloatTextSlider();
			fps_ = new VUI.Label();

			var p = new VUI.Panel(new VUI.VerticalFlow());

			label_ = new VUI.Label();
			p.Add(label_);

			selButtons_ = new VUI.Panel(new VUI.VerticalFlow());
			selButtons_.Visible = false;
			p.Add(selButtons_);

			playerButtons_ = new VUI.Panel(new VUI.VerticalFlow());
			p.Add(playerButtons_);

			if (!Cue.Instance.Sys.IsVR)
			{
				var tools = new VUI.Panel(new VUI.HorizontalFlow(5));
				tools.Add(new VUI.ToolButton("Reload", OnReload));
				tools.Add(new VUI.CheckBox("Navmesh", (b) => Cue.Instance.Sys.Nav.Render = b));
				tools.Add(new VUI.ToolButton("Reload anims", OnReloadAnimations));
				p.Add(tools);
			}

			var row = new VUI.Panel(new VUI.HorizontalFlow(5));
			row.Add(new VUI.ToolButton("Call", OnCall));
			row.Add(new VUI.ToolButton("Straddle", OnStraddle));
			row.Add(new VUI.ToolButton("Handjob", OnHandjob));
			row.Add(new VUI.ToolButton("Blowjob", OnBlowjob));
			row.Add(new VUI.ToolButton("Stand", OnStand));
			row.Add(new VUI.ToolButton("Sex", OnSex));
			row.Add(canKiss_);
			selButtons_.Add(row);

			row = new VUI.Panel(new VUI.HorizontalFlow(5));
			row.Add(new VUI.ToolButton("Stop kiss", OnStopKiss));
			row.Add(new VUI.ToolButton("Make idle", OnMakeIdle));
			row.Add(new VUI.ToolButton("Crouch", OnCrouch));
			row.Add(new VUI.ToolButton("Genitals", OnToggleGenitals));
			row.Add(new VUI.ToolButton("Breasts", OnToggleBreasts));
			row.Add(new VUI.ToolButton("test", OnTest));
			selButtons_.Add(row);


			row = new VUI.Panel(new VUI.HorizontalFlow(5));
			row.Add(new VUI.CheckBox("Move player", OnMovePlayer));
			row.Add(forceExcitement_);
			row.Add(excitement_);
			row.Add(fps_);
			playerButtons_.Add(row);


			root_.ContentPanel.Layout = new VUI.BorderLayout();
			root_.ContentPanel.Add(p, VUI.BorderLayout.Center);
			root_.Visible = visible_;

			canKiss_.Changed += OnCanKiss;
			forceExcitement_.Changed += (b) => OnForceExcitement(b);
			excitement_.ValueChanged += (f) => OnExcitement(f);
		}

		public bool Visible
		{
			get
			{
				return visible_;
			}

			set
			{
				visible_ = value;
				if (root_ != null)
					root_.Visible = visible_;
			}
		}

		public IObject Selected
		{
			get
			{
				return sel_;
			}

			set
			{
				sel_ = value;
				OnSelected(value);
			}
		}

		public IObject Hovered
		{
			get
			{
				return hov_;
			}

			set
			{
				hov_ = value;
				OnHovered(value);
			}
		}

		public void Destroy()
		{
			if (root_ != null)
			{
				root_.Destroy();
				root_ = null;
			}
		}

		public void Update()
		{
			if (label_ != null)
			{
				string s = "";

				if (sel_ != null)
					s += sel_.ID;

				if (hov_ != null)
					s += " (" + hov_.ID + ")";

				label_.Text = s;
			}

			if (fps_ != null)
				fps_.Text = Cue.Instance.Sys.Fps;

			root_?.Update();
		}

		public void Toggle()
		{
			visible_ = !visible_;

			if (root_ != null)
				root_.Visible = visible_;
		}

		private void OnSelected(IObject o)
		{
			var p = o as Person;

			selButtons_.Visible = (p != null);

			if (p != null)
				canKiss_.Checked = p.Options.CanKiss;
		}

		private void OnHovered(IObject p)
		{
		}

		private void OnReload()
		{
			Cue.Instance.ReloadPlugin();
		}

		private void OnReloadAnimations()
		{
			Resources.Animations.Load();

			foreach (var p in Cue.Instance.ActivePersons)
				p.Animator.Stop();
		}

		private void OnCall()
		{
			var p = Selected as Person;
			if (p != null && Cue.Instance.Player != null && p != Cue.Instance.Player)
				p.AI.RunEvent(new CallEvent(p, Cue.Instance.Player));
		}

		private void OnStraddle()
		{
			var p = Selected as Person;
			if (p != null && Cue.Instance.Player != null && p != Cue.Instance.Player)
			{
				if (!Cue.Instance.Player.State.Is(PersonState.Sitting))
				{
					Cue.LogError("can't straddle, player not sitting");
					return;
				}

				p.MakeIdle();
				p.AI.RunEvent(new CallEvent(
					p, Cue.Instance.Player,
					() => { p.SetState(PersonState.SittingStraddling); }));
			}
		}

		private void OnCrouch()
		{
			var p = Selected as Person;
			if (p != null)
			{
				p.MakeIdle();
				p.SetState(PersonState.Crouching);
			}
		}

		private void OnHandjob()
		{
			var p = Selected as Person;
			if (p != null && p != Cue.Instance.Player)
			{
				if (p.Handjob.Active)
					p.Handjob.Stop();
				else
					p.Handjob.Start();
			}
		}

		private void OnBlowjob()
		{
			var p = Selected as Person;
			if (p != null && p != Cue.Instance.Player)
			{
				if (p.Blowjob.Active)
					p.Blowjob.Stop();
				else
					p.Blowjob.Start();
			}
		}

		private void OnStand()
		{
			var p = Selected as Person;
			if (p != null)
			{
				p.MakeIdle();
				p.SetState(PersonState.Standing);
			}
		}

		private void OnSex()
		{
			var p = Selected as Person;
			if (p != null)
			{
				var s = p.AI.ForcedEvent as SexEvent;

				if (s == null)
				{
					p.AI.RunEvent(new SexEvent(p));
				}
				else
				{
					p.AI.RunEvent(null);
					p.Animator.StopType(Animation.SexType);
				}
			}
		}

		private void OnCanKiss(bool b)
		{
			var p = Selected as Person;
			if (p != null)
				p.Options.CanKiss = b;
		}

		private void OnMakeIdle()
		{
			var p = Selected as Person;
			if (p != null)
			{
				p.MakeIdle();
				p.AI.MakeIdle();
			}
		}

		private void OnStopKiss()
		{
			var p = Selected as Person;
			if (p != null)
				p.Kisser.Stop();
		}

		private void OnToggleGenitals()
		{
			var p = Selected as Person;
			if (p != null)
			{
				p.Clothing.GenitalsVisible = !p.Clothing.GenitalsVisible;
			}
		}

		private void OnToggleBreasts()
		{
			var p = Selected as Person;
			if (p != null)
			{
				p.Clothing.BreastsVisible = !p.Clothing.BreastsVisible;
			}
		}

		private void OnTest()
		{
			var p = Selected as Person;
			if (p != null)
				p.Clothing.Dump();
		}

		private void OnMovePlayer(bool b)
		{
			var p = Cue.Instance.Player;
			if (p != null)
				p.VamAtom?.SetControlsForMoving(b);
		}

		private void OnForceExcitement(bool b)
		{
			var p = Selected as Person;
			if (p != null && force_ != b)
			{
				force_ = b;

				if (force_)
					p.Mood.ExcitementValue.SetForced(excitement_.Value);
				else
					p.Mood.ExcitementValue.UnsetForced();
			}
		}

		private void OnExcitement(float f)
		{
			var p = Selected as Person;
			if (p != null)
			{
				if (force_)
					p.Mood.ExcitementValue.SetForced(f);
			}
		}
	}
}
