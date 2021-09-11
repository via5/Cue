using UnityEngine;
using UnityEngine.UI;

namespace VUI
{
	class Panel : Widget
	{
		public override string TypeName { get { return "Panel"; } }

		private GameObject bgObject_ = null;
		private Image bgImage_ = null;
		private Color bgColor_ = new Color(0, 0, 0, 0);
		private bool clickthrough_ = true;

		public Panel(string name = "")
			: base(name)
		{
		}

		public Panel(Layout ly)
		{
			Layout = ly;
		}

		public bool Clickthrough
		{
			get
			{
				return clickthrough_;
			}

			set
			{
				if (value != clickthrough_)
				{
					clickthrough_ = value;
					SetBackground();
				}
			}
		}

		public Color BackgroundColor
		{
			get
			{
				return bgColor_;
			}

			set
			{
				bgColor_ = value;
				SetBackground();
			}
		}

		protected override void DoCreate()
		{
			base.DoCreate();
			SetBackground();
		}

		public override void UpdateBounds()
		{
			base.UpdateBounds();
			SetBackground();
		}

		protected override void Destroy()
		{
			if (bgObject_ != null)
				bgObject_ = null;

			base.Destroy();
		}

		private void SetBackground()
		{
			if (MainObject == null)
				return;

			if (bgColor_.a == 0 && clickthrough_ && bgObject_ == null)
				return;

			if (bgColor_.a > 0 && bgObject_ == null)
			{
				bgObject_ = new GameObject("WidgetBackground");
				bgObject_.transform.SetParent(MainObject.transform, false);
				bgImage_ = bgObject_.AddComponent<Image>();
			}

			if (bgObject_ != null)
			{
				bgObject_.transform.SetAsFirstSibling();
				bgImage_.color = bgColor_;
				bgImage_.raycastTarget = !clickthrough_;

				var r = new Rectangle(0, 0, Bounds.Size);
				r.Deflate(Margins);

				Utilities.SetRectTransform(bgObject_, r);
			}
		}
	}
}
