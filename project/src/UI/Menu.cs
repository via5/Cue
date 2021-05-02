namespace Cue
{
	class Menu
	{
		private bool visible_ = false;
		private VUI.Root root_ = null;
		private VUI.Stack stack_ = null;
		private VUI.Label label_ = null;
		private Person sel_ = null;

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

		public Person Object
		{
			get
			{
				return sel_;
			}

			set
			{
				sel_ = value;
				OnHovered(value);
			}
		}

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
				root_ = Cue.Instance.Sys.Create2D(130, new Size(1000, 100));
			}

			root_.ContentPanel.Layout = new VUI.BorderLayout();

			stack_ = new VUI.Stack();

			stack_.AddToStack(new VUI.Panel());

			var bottom = new VUI.Panel(new VUI.VerticalFlow());

			label_ = new VUI.Label();
			bottom.Add(label_);

			var p = new VUI.Panel(new VUI.HorizontalFlow(5));
			//p.Add(new VUI.ToolButton("Call", OnCall));
			//p.Add(new VUI.ToolButton("Sit", OnSit));
			//p.Add(new VUI.ToolButton("Kneel", OnKneel));
			//p.Add(new VUI.ToolButton("Reload", OnReload));
			p.Add(new VUI.ToolButton("Handjob", OnHandjob));
			//p.Add(new VUI.ToolButton("Sex", OnSex));
			//p.Add(new VUI.ToolButton("Stand", OnStand));
			p.Add(new VUI.ToolButton("Stop kiss", OnStopKiss));
			bottom.Add(p);

			p = new VUI.Panel(new VUI.HorizontalFlow(5));
			p.Add(new VUI.ToolButton("Genitals", OnToggleGenitals));
			p.Add(new VUI.ToolButton("Breasts", OnToggleBreasts));
			p.Add(new VUI.ToolButton("Dump clothes", OnDumpClothes));
			p.Add(new VUI.ToolButton("Dump morphs", OnDumpMorphs));
			bottom.Add(p);

			stack_.AddToStack(bottom);

			root_.ContentPanel.Add(stack_, VUI.BorderLayout.Bottom);
			root_.Visible = visible_;
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
				if (sel_ == null)
					label_.Text = "";
				else
					label_.Text = sel_.ID;
			}

			root_?.Update();
		}

		public void Toggle()
		{
			visible_ = !visible_;

			if (root_ != null)
				root_.Visible = visible_;
		}

		private void OnHovered(IObject o)
		{
			var p = o as Person;
			if (p == null)
			{
				stack_.Select(0);
			}
			else
			{
				stack_.Select(1);
			}
		}

		private void OnCall()
		{
			if (sel_ != null)
			{
				sel_.MakeIdle();
				sel_.AI.RunEvent(new CallEvent(sel_, Cue.Instance.Player));
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

			if (sel_ != null)
			{
				sel_.MakeIdle();
				sel_.Sit();
			}
		}

		private void OnKneel()
		{
			if (sel_ != null)
			{
				sel_.MakeIdle();
				sel_.Kneel();
			}
		}

		private void OnReload()
		{
			Cue.Instance.ReloadPlugin();
		}

		private void OnHandjob()
		{
			if (sel_ != null)
			{
				sel_.MakeIdle();
				sel_.AI.RunEvent(new HandjobEvent(sel_, Cue.Instance.Player));
			}
		}

		private void OnSex()
		{
			if (sel_ != null)
			{
				var s = sel_.AI.Event as SexEvent;

				if (s == null)
				{
					sel_.MakeIdle();
					sel_.AI.RunEvent(new SexEvent(sel_, Cue.Instance.Player));
				}
				else
				{
					s.ForceState(SexEvent.PlayState);
				}
			}
		}

		private void OnStand()
		{
			if (sel_ != null)
			{
				sel_.MakeIdle();
				sel_.AI.RunEvent(new StandEvent(sel_));
			}
		}

		private void OnStopKiss()
		{
			if (sel_ != null)
			{
				//sel_.Kisser.Start(Cue.Instance.Player);
				sel_.Kisser.Stop();
			}
		}

		private void OnToggleGenitals()
		{
			if (sel_ != null)
			{
				sel_.Clothing.GenitalsVisible = !sel_.Clothing.GenitalsVisible;
			}
		}

		private void OnToggleBreasts()
		{
			if (sel_ != null)
			{
				sel_.Clothing.BreastsVisible = !sel_.Clothing.BreastsVisible;
			}
		}

		private void OnDumpClothes()
		{
			if (sel_ != null)
			{
				sel_.Clothing.Dump();
			}
		}

		private void OnDumpMorphs()
		{
			if (sel_ != null)
			{
				sel_.Expression.DumpActive();
			}
		}
	}
}
