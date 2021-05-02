namespace Cue
{
	class Menu
	{
		private bool visible_ = false;
		private VUI.Root root_ = null;
		private VUI.Label label_ = null;
		private VUI.Panel buttons_ = null;
		private IObject sel_ = null;
		private IObject hov_ = null;

		public void Create(bool vr, bool left)
		{
			if (vr)
			{
				root_ = Cue.Instance.Sys.CreateAttached(
					left,
					new Vector3(0, 0.1f, 0),
					new Point(0, 0),
					new Size(1300, 100));
			}
			else
			{
				root_ = Cue.Instance.Sys.Create2D(10, new Size(1000, 170));
			}


			var p = new VUI.Panel(new VUI.VerticalFlow());

			label_ = new VUI.Label();
			p.Add(label_);

			buttons_ = new VUI.Panel(new VUI.VerticalFlow());
			buttons_.Visible = false;
			p.Add(buttons_);

			if (!Cue.Instance.Sys.IsVR)
			{
				var tools = new VUI.Panel(new VUI.HorizontalFlow(5));
				tools.Add(new VUI.ToolButton("Reload", OnReload));
				p.Add(tools);
			}

			var row = new VUI.Panel(new VUI.HorizontalFlow(5));
			//p.Add(new VUI.ToolButton("Call", OnCall));
			//p.Add(new VUI.ToolButton("Sit", OnSit));
			//p.Add(new VUI.ToolButton("Kneel", OnKneel));
			row.Add(new VUI.ToolButton("Handjob", OnHandjob));
			//p.Add(new VUI.ToolButton("Sex", OnSex));
			//p.Add(new VUI.ToolButton("Stand", OnStand));
			row.Add(new VUI.ToolButton("Stop kiss", OnStopKiss));
			buttons_.Add(row);

			row = new VUI.Panel(new VUI.HorizontalFlow(5));
			row.Add(new VUI.ToolButton("Genitals", OnToggleGenitals));
			row.Add(new VUI.ToolButton("Breasts", OnToggleBreasts));
			row.Add(new VUI.ToolButton("Dump clothes", OnDumpClothes));
			row.Add(new VUI.ToolButton("Dump morphs", OnDumpMorphs));
			buttons_.Add(row);

			root_.ContentPanel.Layout = new VUI.BorderLayout();
			root_.ContentPanel.Add(p, VUI.BorderLayout.Center);
			root_.Visible = visible_;
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

			root_?.Update();
		}

		public void Toggle()
		{
			visible_ = !visible_;

			if (root_ != null)
				root_.Visible = visible_;
		}

		private void OnSelected(IObject p)
		{
			buttons_.Visible = (p as Person != null);
		}

		private void OnHovered(IObject p)
		{
		}

		private void OnCall()
		{
			var p = Selected as Person;
			if (p != null && Cue.Instance.Player != null)
			{
				p.MakeIdle();
				p.AI.RunEvent(new CallEvent(p, Cue.Instance.Player));
			}
		}

		private void OnSit()
		{
			// sit on player
			//Cue.Instance.Persons[0].AI.Enabled = false;
			//Cue.Instance.Persons[0].MakeIdle();
			//Cue.Instance.Persons[0].Animator.Play(
			//	Resources.Animations.GetAny(
			//		Resources.Animations.SitOnSitting,
			//		Cue.Instance.Persons[0].Sex));

			var p = Selected as Person;
			if (p != null)
			{
				p.MakeIdle();
				p.Sit();
			}
		}

		private void OnKneel()
		{
			var p = Selected as Person;
			if (p != null)
			{
				p.MakeIdle();
				p.Kneel();
			}
		}

		private void OnReload()
		{
			Cue.Instance.ReloadPlugin();
		}

		private void OnHandjob()
		{
			var p = Selected as Person;
			if (p != null && Cue.Instance.Player != null)
			{
				p.MakeIdle();
				p.AI.RunEvent(new HandjobEvent(p, Cue.Instance.Player));
			}
		}

		private void OnSex()
		{
			var p = Selected as Person;
			if (p != null)
			{
				var s = p.AI.Event as SexEvent;

				if (s == null && Cue.Instance.Player != null)
				{
					p.MakeIdle();
					p.AI.RunEvent(new SexEvent(p, Cue.Instance.Player));
				}
				else
				{
					s.ForceState(SexEvent.PlayState);
				}
			}
		}

		private void OnStand()
		{
			var p = Selected as Person;
			if (p != null)
			{
				p.MakeIdle();
				p.AI.RunEvent(new StandEvent(p));
			}
		}

		private void OnStopKiss()
		{
			var p = Selected as Person;
			if (p != null)
			{
				//sel_.Kisser.Start(Cue.Instance.Player);
				p.Kisser.Stop();
			}
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

		private void OnDumpClothes()
		{
			var p = Selected as Person;
			if (p != null)
			{
				p.Clothing.Dump();
			}
		}

		private void OnDumpMorphs()
		{
			var p = Selected as Person;
			if (p != null)
			{
				p.Expression.DumpActive();
			}
		}
	}
}
