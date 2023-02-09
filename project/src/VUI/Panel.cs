using UnityEngine;
using UnityEngine.UI;

namespace VUI
{
	public class Panel : Widget
	{
		public override string TypeName { get { return "Panel"; } }

		private GameObject bgObject_ = null;
		private UnityEngine.UI.Image bgImage_ = null;
		private Color bgColor_ = new Color(0, 0, 0, 0);
		private bool clickthrough_ = true;

		public Panel(Layout ly)
			: this("", ly)
		{
		}

		public Panel(string name = "", Layout ly = null)
			: base(name)
		{
			if (ly != null)
				Layout = ly;

			WantsFocus = false;
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

		protected override bool IsTransparent()
		{
			return clickthrough_;
		}

		public Color BackgroundColor
		{
			get
			{
				return bgColor_;
			}

			set
			{
				if (bgColor_ != value)
				{
					bgColor_ = value;
					SetBackground();
				}
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

			if ((bgColor_.a > 0 || !clickthrough_) && bgObject_ == null)
			{
				bgObject_ = new GameObject("WidgetBackground");
				bgObject_.transform.SetParent(MainObject.transform, false);
				bgImage_ = bgObject_.AddComponent<UnityEngine.UI.Image>();
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
