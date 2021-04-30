namespace Cue
{
	class Menu
	{
		private VUI.Root root_ = null;

		public void Create(bool vr)
		{
			if (vr)
			{
				root_ = Cue.Instance.Sys.CreateAttached(
					new Vector3(0, 0.1f, 0),
					new Point(0, 0),
					new Size(1300, 100));
			}
			else
			{
				root_ = Cue.Instance.Sys.Create2D(130, new Size(1000, 100));
			}

			root_.ContentPanel.Layout = new VUI.BorderLayout();

			var bottom = new VUI.Panel(new VUI.VerticalFlow());

			var p = new VUI.Panel(new VUI.HorizontalFlow(5));
			p.Add(new VUI.ToolButton("Call", OnCall));
			p.Add(new VUI.ToolButton("Sit", OnSit));
			p.Add(new VUI.ToolButton("Kneel", OnKneel));
			p.Add(new VUI.ToolButton("Reload", OnReload));
			p.Add(new VUI.ToolButton("Handjob", OnHandjob));
			p.Add(new VUI.ToolButton("Sex", OnSex));
			p.Add(new VUI.ToolButton("Stand", OnStand));
			p.Add(new VUI.ToolButton("Kiss", OnKiss));
			bottom.Add(p);

			p = new VUI.Panel(new VUI.HorizontalFlow(5));
			p.Add(new VUI.ToolButton("Genitals", OnToggleGenitals));
			p.Add(new VUI.ToolButton("Breasts", OnToggleBreasts));
			p.Add(new VUI.ToolButton("Dump clothes", OnDumpClothes));
			p.Add(new VUI.ToolButton("Dump morphs", OnDumpMorphs));
			bottom.Add(p);

			root_.ContentPanel.Add(bottom, VUI.BorderLayout.Bottom);
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
			root_.Update();
		}

		public void Toggle()
		{
			root_.Visible = !root_.Visible;
		}

		private void OnCall()
		{
			if (Cue.Instance.Selected is Person)
			{
				var p = ((Person)Cue.Instance.Selected);

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

			if (Cue.Instance.Selected is Person)
			{
				var p = (Person)Cue.Instance.Selected;
				p.MakeIdle();
				p.Sit();
			}
		}

		private void OnKneel()
		{
			if (Cue.Instance.Selected is Person)
			{
				var p = (Person)Cue.Instance.Selected;
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
			if (Cue.Instance.Selected is Person)
			{
				var p = ((Person)Cue.Instance.Selected);

				p.MakeIdle();
				p.AI.RunEvent(new HandjobEvent(p, Cue.Instance.Player));
			}
		}

		private void OnSex()
		{
			if (Cue.Instance.Selected is Person)
			{
				var p = ((Person)Cue.Instance.Selected);
				var s = p.AI.Event as SexEvent;

				if (s == null)
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
			if (Cue.Instance.Selected is Person)
			{
				var p = ((Person)Cue.Instance.Selected);

				p.MakeIdle();
				p.AI.RunEvent(new StandEvent(p));
			}
		}

		private void OnKiss()
		{
			if (Cue.Instance.Selected is Person)
			{
				var p = ((Person)Cue.Instance.Selected);
				p.Kisser.Start(Cue.Instance.Player);
			}
		}

		private void OnToggleGenitals()
		{
			if (Cue.Instance.Selected is Person)
			{
				var p = ((Person)Cue.Instance.Selected);
				p.Clothing.GenitalsVisible = !p.Clothing.GenitalsVisible;
			}
		}

		private void OnToggleBreasts()
		{
			if (Cue.Instance.Selected is Person)
			{
				var p = ((Person)Cue.Instance.Selected);
				p.Clothing.BreastsVisible = !p.Clothing.BreastsVisible;
			}
		}

		private void OnDumpClothes()
		{
			if (Cue.Instance.Selected is Person)
			{
				var p = ((Person)Cue.Instance.Selected);
				p.Clothing.Dump();
			}
		}

		private void OnDumpMorphs()
		{
			if (Cue.Instance.Selected is Person)
			{
				var p = ((Person)Cue.Instance.Selected);
				p.Expression.DumpActive();
			}
		}
	}

}
