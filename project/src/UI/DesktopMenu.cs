using System.Collections.Generic;

namespace Cue
{
	class DesktopMenu
	{
		private bool visible_ = false;
		private VUI.Root root_ = null;
		private VUI.Label name_ = null;

		private VUI.Panel selButtons_ = null;
		private List<UIActions.IItem> items_ = new List<UIActions.IItem>();
		private VUI.CheckBox forceExcitement_ = null;
		private VUI.FloatTextSlider excitement_ = null;
		private VUI.Label fps_ = null;

		private IObject sel_ = null;
		private IObject hov_ = null;
		private VUI.IgnoreFlag ignore_ = new VUI.IgnoreFlag();

		public void Create()
		{
			foreach (var i in UIActions.All())
				items_.Add(i);

			root_ = new VUI.Root(new VUI.OverlayRootSupport(10, 1000, 220));

			var p = new VUI.Panel(new VUI.VerticalFlow(10));

			name_ = p.Add(new VUI.Label(
				"", VUI.Label.AlignCenter | VUI.Label.AlignVCenter,
				UnityEngine.FontStyle.Bold));

			// sel row
			{
				selButtons_ = new VUI.Panel(new VUI.HorizontalFlow());

				foreach (var i in items_)
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
			get { return hov_; }
			set { hov_ = value; }
		}

		public void Destroy()
		{
			if (root_ != null)
			{
				root_.Destroy();
				root_ = null;
			}

			items_.Clear();
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

			for (int i = 0; i < items_.Count; ++i)
				items_[i].Update();

			UpdateWidgets();
			root_?.Update();
		}

		private void OnSelected(IObject o)
		{
			var p = o as Person;
			if (selButtons_ != null)
				selButtons_.Visible = (p != null);

			foreach (var i in items_)
				i.Person = p;
		}

		private void UpdateWidgets()
		{
			ignore_.Do(() =>
			{
				var p = sel_ as Person;

				if (p != null)
				{
					if (forceExcitement_ != null)
						forceExcitement_.Checked = p.Mood.FlatExcitementValue.IsForced;

					if (excitement_ != null)
						excitement_.Value = p.Mood.FlatExcitementValue.Value;
				}
			});
		}

		private void OnReload()
		{
			Cue.Instance.ReloadPlugin();
		}

		private void OnTest()
		{
			if (ignore_) return;

			var p = Selected as Person;
			if (p != null)
				p.Mood.ForceOrgasm();
				//p.Animator.PlayType(Animations.Penetrated);
				//p.Clothing.Dump();
		}

		private void OnForceExcitement(bool b)
		{
			if (ignore_) return;

			var p = Selected as Person;
			if (p != null)
			{
				if (b)
					p.Mood.FlatExcitementValue.SetForced(excitement_.Value);
				else
					p.Mood.FlatExcitementValue.UnsetForced();
			}
		}

		private void OnExcitement(float f)
		{
			if (ignore_) return;

			var p = Selected as Person;
			if (p != null)
			{
				if (p.Mood.FlatExcitementValue.IsForced)
					p.Mood.FlatExcitementValue.SetForced(f);
				else
					p.Mood.FlatExcitementValue.Value = f;
			}
		}
	}
}
