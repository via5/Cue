namespace Cue
{
	class Menu
	{
		private bool visible_ = false;
		private VUI.Root root_ = null;
		private VUI.Label name_ = null;

		private VUI.Panel selButtons_ = null;
		private VUI.CheckBox hj_ = null;
		private VUI.CheckBox bj_ = null;
		private VUI.CheckBox thrust_ = null;
		private VUI.CheckBox canKiss_ = null;

		private VUI.Panel playerButtons_ = null;
		private VUI.CheckBox movePlayer_ = null;

		private VUI.CheckBox forceExcitement_ = null;
		private VUI.FloatTextSlider excitement_ = null;
		private VUI.Label fps_ = null;

		private bool force_ = false;
		private IObject sel_ = null;
		private IObject hov_ = null;
		private VUI.IgnoreFlag ignore_ = new VUI.IgnoreFlag();

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



			var p = new VUI.Panel(new VUI.VerticalFlow());

			// top row
			{
				name_ = p.Add(new VUI.Label());
			}

			// sel row
			{
				selButtons_ = new VUI.Panel(new VUI.VerticalFlow());
				selButtons_.Visible = false;
				p.Add(selButtons_);

				var row = new VUI.Panel(new VUI.HorizontalFlow(5));
				hj_ = row.Add(new VUI.CheckBox("Handjob", OnHandjob));
				bj_ = row.Add(new VUI.CheckBox("Blowjob", OnBlowjob));
				thrust_ = row.Add(new VUI.CheckBox("Thrust", OnThrust));
				canKiss_ = row.Add(new VUI.CheckBox("Can kiss", OnCanKiss));
				selButtons_.Add(row);

				row = new VUI.Panel(new VUI.HorizontalFlow(5));
				row.Add(new VUI.ToolButton("Genitals", OnToggleGenitals));
				row.Add(new VUI.ToolButton("Breasts", OnToggleBreasts));
				selButtons_.Add(row);
			}

			// player row
			{
				playerButtons_ = new VUI.Panel(new VUI.VerticalFlow());
				playerButtons_.Visible = false;
				p.Add(playerButtons_);

				var row = new VUI.Panel(new VUI.HorizontalFlow(5));
				movePlayer_ = row.Add(new VUI.CheckBox("Move player", OnMovePlayer));
				playerButtons_.Add(row);
			}

			// debug row
			{
				if (!Cue.Instance.Sys.IsVR)
				{
					var tools = new VUI.Panel(new VUI.HorizontalFlow(5));
					tools.Add(new VUI.ToolButton("Reload", OnReload));
					forceExcitement_ = tools.Add(new VUI.CheckBox("Ex", OnForceExcitement));
					excitement_ = tools.Add(new VUI.FloatTextSlider(OnExcitement));
					tools.Add(new VUI.ToolButton("test", OnTest));
					fps_ = tools.Add(new VUI.Label());
					p.Add(tools);
				}
			}


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
			if (name_ != null)
			{
				string s = "";

				if (sel_ != null)
					s += sel_.ID;

				if (hov_ != null)
					s += " (" + hov_.ID + ")";

				name_.Text = s;
			}

			if (fps_ != null)
				fps_.Text = Cue.Instance.Sys.Fps;

			if (playerButtons_ != null)
			{
				var p = Cue.Instance.Player;

				// todo, player is the pseudo camera atom if not possessed,
				// find a better way
				bool playerPerson = (p != null && p.Atom is Sys.Vam.VamAtom);

				playerButtons_.Visible = playerPerson;
			}

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

			ignore_.Do(() =>
			{
				selButtons_.Visible = (p != null);

				if (p != null)
				{
					hj_.Checked = p.Handjob.Active;
					bj_.Checked = p.Blowjob.Active;
					thrust_.Checked = p.AI.GetInteraction<SexInteraction>().Active;
					canKiss_.Checked = p.Options.CanKiss;
					forceExcitement_.Checked = p.Mood.ExcitementValue.IsForced;
					excitement_.Value = p.Mood.ExcitementValue.Value;
				}
			});
		}

		private void OnHovered(IObject p)
		{
		}

		private void OnReload()
		{
			Cue.Instance.ReloadPlugin();
		}

		private void OnHandjob(bool b)
		{
			if (ignore_) return;

			var p = Selected as Person;
			if (p != null && p != Cue.Instance.Player)
			{
				if (b)
					p.Handjob.Start();
				else
					p.Handjob.Stop();
			}
		}

		private void OnBlowjob(bool b)
		{
			if (ignore_) return;

			var p = Selected as Person;
			if (p != null && p != Cue.Instance.Player)
			{
				if (b)
					p.Blowjob.Start();
				else
					p.Blowjob.Stop();
			}
		}

		private void OnThrust(bool b)
		{
			if (ignore_) return;

			var p = Selected as Person;
			if (p != null)
			{
				p.AI.GetInteraction<SexInteraction>().Active = b;
			}
		}

		private void OnCanKiss(bool b)
		{
			if (ignore_) return;

			var p = Selected as Person;
			if (p != null)
				p.Options.CanKiss = b;
		}

		private void OnToggleGenitals()
		{
			if (ignore_) return;

			var p = Selected as Person;
			if (p != null)
			{
				p.Clothing.GenitalsVisible = !p.Clothing.GenitalsVisible;
			}
		}

		private void OnToggleBreasts()
		{
			if (ignore_) return;

			var p = Selected as Person;
			if (p != null)
			{
				p.Clothing.BreastsVisible = !p.Clothing.BreastsVisible;
			}
		}

		private void OnTest()
		{
			if (ignore_) return;

			var p = Selected as Person;
			if (p != null)
				p.Clothing.Dump();
		}

		private void OnMovePlayer(bool b)
		{
			if (ignore_) return;

			var p = Cue.Instance.Player;
			if (p != null)
				p.VamAtom?.SetControlsForMoving(b);
		}

		private void OnForceExcitement(bool b)
		{
			if (ignore_) return;

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
			if (ignore_) return;

			var p = Selected as Person;
			if (p != null)
			{
				if (force_)
					p.Mood.ExcitementValue.SetForced(f);
			}
		}
	}
}
