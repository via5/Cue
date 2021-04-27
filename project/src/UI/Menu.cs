namespace Cue
{
	class Menu
	{
		private W.ICanvas canvas_ = null;
		private VUI.Root root_ = null;

		public void Create(bool vr)
		{
			if (vr)
			{
				canvas_ = Cue.Instance.Sys.CreateAttached(
					new Vector3(0, 0.1f, 0),
					new Point(0, 0),
					new Size(1300, 100));

				canvas_.Create();
			}
			else
			{
				canvas_ = Cue.Instance.Sys.Create2D();
				canvas_.Create();
			}

			root_ = canvas_.CreateRoot();
			root_.ContentPanel.Layout = new VUI.BorderLayout();

			var bottom = new VUI.Panel(new VUI.VerticalFlow());

			var p = new VUI.Panel(new VUI.HorizontalFlow());
			p.Add(new VUI.Button("Call", OnCall));
			p.Add(new VUI.Button("Sit", OnSit));
			p.Add(new VUI.Button("Kneel", OnKneel));
			p.Add(new VUI.Button("Reload", OnReload));
			p.Add(new VUI.Button("Handjob", OnHandjob));
			p.Add(new VUI.Button("Sex", OnSex));
			p.Add(new VUI.Button("Stand", OnStand));
			bottom.Add(p);

			p = new VUI.Panel(new VUI.HorizontalFlow());
			p.Add(new VUI.Button("Toggle genitals", OnToggleGenitals));
			p.Add(new VUI.Button("Toggle breasts", OnToggleBreasts));
			p.Add(new VUI.Button("Dump clothes", OnDumpClothes));
			bottom.Add(p);

			root_.ContentPanel.Add(bottom, VUI.BorderLayout.Bottom);
		}

		public bool IsHovered(float x, float y)
		{
			if (canvas_ == null)
				return false;

			return canvas_.IsHovered(x, y);
		}

		public void Destroy()
		{
			if (root_ != null)
			{
				root_.Destroy();
				root_ = null;
			}

			if (canvas_ != null)
			{
				canvas_.Destroy();
				canvas_ = null;
			}
		}

		public void Update()
		{
			root_.Update();
		}

		public void Toggle()
		{
			canvas_.Toggle();
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
	}

}
