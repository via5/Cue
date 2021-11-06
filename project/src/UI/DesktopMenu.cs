using System.Collections.Generic;

namespace Cue
{
	class DesktopMenu : BasicMenu
	{
		private readonly Sys.ISys sys_;

		private VUI.Label name_ = null;
		private VUI.Panel selButtons_ = null;
		private VUI.CheckBox forceExcitement_ = null;
		private VUI.FloatTextSlider excitement_ = null;
		private VUI.Label fps_ = null;
		private bool ignore_ = false;

		public DesktopMenu()
		{
			sys_ = Cue.Instance.Sys;

			var root = new VUI.Root(new VUI.OverlayRootSupport(10, 1200, 220));

			var p = new VUI.Panel(new VUI.VerticalFlow(10));

			{
				var top = new VUI.Panel(new VUI.BorderLayout());

				top.Add(
					new VUI.ToolButton("<", PreviousPerson),
					VUI.BorderLayout.Left);

				name_ = top.Add(new VUI.Label(
					"", VUI.Label.AlignCenter | VUI.Label.AlignVCenter,
					UnityEngine.FontStyle.Bold),
					VUI.BorderLayout.Center);

				top.Add(
					new VUI.ToolButton(">", NextPerson),
					VUI.BorderLayout.Right);

				p.Add(top);
			}

			// sel row
			{
				selButtons_ = new VUI.Panel(new VUI.HorizontalFlow());

				foreach (var i in Items)
					selButtons_.Add(i.Panel);

				p.Add(selButtons_);
			}

			// debug row
			{
				var tools = new VUI.Panel(new VUI.HorizontalFlow(5));
				tools.Add(new VUI.ToolButton("Reload", OnReload));
				forceExcitement_ = tools.Add(new VUI.CheckBox("Ex", OnForceExcitement));
				excitement_ = tools.Add(new VUI.FloatTextSlider(OnExcitement));
				tools.Add(new VUI.ToolButton("test", OnTest));
				fps_ = tools.Add(new VUI.Label());
				p.Add(tools);
			}

			root.ContentPanel.Layout = new VUI.BorderLayout();
			root.ContentPanel.Add(p, VUI.BorderLayout.Center);

			SetRoot(root);
			PersonChanged();
		}

		public override void CheckInput()
		{
			if (sys_.Input.Select)
			{
				var o = sys_.Input.GetMouseHovered().o as Person;
				if (o != null)
				{
					SelectedPerson = o;
					Cue.Instance.Save();
				}
			}
		}

		public override void Update()
		{
			if (fps_ != null)
				fps_.Text = sys_.Fps;

			UpdateWidgets();

			base.Update();
		}

		protected override void PersonChanged()
		{
			var p = SelectedPerson;

			name_.Text = p?.ID ?? "";
			selButtons_.Visible = (p != null);
		}

		private void UpdateWidgets()
		{
			try
			{
				ignore_ = true;

				var p = SelectedPerson;

				if (p != null)
				{
					if (forceExcitement_ != null)
						forceExcitement_.Checked = p.Mood.GetValue(Moods.Excited).IsForced;

					if (excitement_ != null)
						excitement_.Value = p.Mood.GetValue(Moods.Excited).Value;
				}
			}
			finally
			{
				ignore_ = false;
			}
		}

		private void OnReload()
		{
			Cue.Instance.ReloadPlugin();
		}

		private void OnTest()
		{
			if (ignore_) return;

			var p = SelectedPerson;
			if (p != null)
				p.Mood.ForceOrgasm();
		}

		private void OnForceExcitement(bool b)
		{
			if (ignore_) return;

			var p = SelectedPerson;
			if (p != null)
			{
				if (b)
					p.Mood.GetValue(Moods.Excited).SetForced(excitement_.Value);
				else
					p.Mood.GetValue(Moods.Excited).UnsetForced();
			}
		}

		private void OnExcitement(float f)
		{
			if (ignore_) return;

			var p = SelectedPerson;
			if (p != null)
			{
				if (p.Mood.GetValue(Moods.Excited).IsForced)
					p.Mood.GetValue(Moods.Excited).SetForced(f);
				else
					p.Mood.GetValue(Moods.Excited).Value = f;
			}
		}
	}
}
