using System.Collections.Generic;

namespace Cue
{
	class VRMenu
	{
		private VUI.Root root_ = null;
		private VUI.Label name_ = null;
		private List<UIActions.IItem> items_ = new List<UIActions.IItem>();
		private int widgetSel_ = -1;
		private int personSel_ = -1;
		private bool visible_ = false;
		private bool left_ = false;
		private VUI.IgnoreFlag ignore_ = new VUI.IgnoreFlag();

		public void Create(bool debugDesktop)
		{
			foreach (var i in UIActions.All())
				items_.Add(i);

			if (debugDesktop)
			{
				root_ = new VUI.Root(new VUI.OverlayRootSupport(10, 300, 350));
			}
			else
			{
				root_ = new VUI.Root(new VUI.VRHandRootSupport(
					VUI.VRHandRootSupport.LeftHand,
					new UnityEngine.Vector3(0, 0.1f, 0),
					new UnityEngine.Vector2(0, 0),
					new UnityEngine.Vector2(300, 350)));
			}

			var ly = new VUI.VerticalFlow();
			var p = new VUI.Panel(ly);

			name_ = p.Add(new VUI.Label(
				"", VUI.Label.AlignCenter | VUI.Label.AlignVCenter,
				UnityEngine.FontStyle.Bold));

			p.Add(new VUI.Spacer(10));

			foreach (var i in items_)
				p.Add(i.Panel);

			if (UI.VRMenuDebug)
			{
				var gl = new VUI.GridLayout(2, 2);
				gl.HorizontalStretch = new List<bool> { true, false };
				var dp = new VUI.Panel(gl);
				dp.Add(new VUI.ToolButton("Reload", () => Cue.Instance.ReloadPlugin()));
				dp.Add(new VUI.ToolButton("UI", () => Cue.Instance.OpenScriptUI()));
				p.Add(dp);
			}

			root_.ContentPanel.Layout = new VUI.BorderLayout();
			root_.ContentPanel.Add(p, VUI.BorderLayout.Center);
			UpdateVisibility();

			if (Cue.Instance.ActivePersons.Length == 0)
				SetPerson(-1);
			else if (personSel_ < 0 || personSel_ >= Cue.Instance.ActivePersons.Length)
				SetPerson(NextPerson(-1, +1));
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
			if (root_?.Visible ?? false)
			{
				CheckInput();

				for (int i = 0; i < items_.Count; ++i)
					items_[i].Update();
			}

			root_?.Update();
		}

		private void CheckInput()
		{
			CheckItemInput();
			CheckPersonInput();

			if (Cue.Instance.Sys.Input.MenuSelect)
			{
				if (widgetSel_ >= 0 && widgetSel_ < items_.Count)
					items_[widgetSel_].Activate();
			}
		}

		private void CheckItemInput()
		{
			var old = widgetSel_;

			if (Cue.Instance.Sys.Input.MenuDown)
			{
				++widgetSel_;
				if (widgetSel_ >= items_.Count)
					widgetSel_ = 0;
			}
			else if (Cue.Instance.Sys.Input.MenuUp)
			{
				--widgetSel_;
				if (widgetSel_ < 0)
					widgetSel_ = items_.Count - 1;
			}

			if (widgetSel_ == -1)
				widgetSel_ = 0;

			if (widgetSel_ != old)
			{
				for (int i = 0; i < items_.Count; ++i)
					items_[i].Selected = (i == widgetSel_);
			}
		}

		private void CheckPersonInput()
		{
			int newSel = personSel_;

			if (Cue.Instance.Sys.Input.MenuRight)
				newSel = NextPerson(personSel_, +1);
			else if (Cue.Instance.Sys.Input.MenuLeft)
				newSel = NextPerson(personSel_, -1);

			if (newSel >= Cue.Instance.ActivePersons.Length)
				newSel = -1;

			if (newSel != personSel_)
				SetPerson(newSel);
		}

		private int NextPerson(int current, int dir)
		{
			var ps = Cue.Instance.ActivePersons;

			int s = current;

			if (s < 0 || s >= ps.Length)
			{
				current = 0;
				s = 0;
			}
			else
			{
				s += dir;
			}


			for (; ; )
			{
				if (s < 0)
					s = ps.Length - 1;
				else if (s >= ps.Length)
					s = 0;

				if (s == current)
					break;

				if (ValidPerson(ps[s]))
					return s;

				s += dir;
			}

			return current;
		}

		private bool ValidPerson(Person p)
		{
			return p.IsInteresting;
		}

		public void ShowLeft()
		{
			left_ = true;
			visible_ = true;
			UpdateVisibility();
		}

		public void ShowRight()
		{
			left_ = false;
			visible_ = true;
			UpdateVisibility();
		}

		public void Hide()
		{
			visible_ = false;
			UpdateVisibility();
		}

		private void UpdateVisibility()
		{
			if (root_ != null)
			{
				if (visible_)
				{
					root_.Visible = true;

					var s = root_.RootSupport as VUI.VRHandRootSupport;

					if (s != null)
					{
						if (left_)
							s.Attach(VUI.VRHandRootSupport.LeftHand);
						else
							s.Attach(VUI.VRHandRootSupport.RightHand);
					}
				}
				else
				{
					root_.Visible = false;
				}
			}
		}

		public IObject Selected
		{
			get
			{
				if (personSel_ < 0 || personSel_ >= Cue.Instance.ActivePersons.Length)
					return null;
				else
					return Cue.Instance.ActivePersons[personSel_];
			}
		}

		private void SetPerson(int index)
		{
			personSel_ = index;

			Person p = Selected as Person;

			if (name_ != null)
			{
				if (p != null)
					name_.Text = p.ID;
				else
					name_.Text = "";
			}

			for (int i = 0; i < items_.Count; ++i)
				items_[i].Person = p;
		}
	}
}
