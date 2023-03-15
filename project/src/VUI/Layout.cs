using System.Collections.Generic;

namespace VUI
{
	public interface LayoutData
	{
	}

	public abstract class Layout
	{
		public abstract string TypeName { get; }

		public const float DontCare = -1;

		private Widget parent_ = null;
		private readonly List<Widget> children_ = new List<Widget>();
		private float spacing_ = 0;

		public Logger Log
		{
			get
			{
				if (parent_ == null)
					return Logger.Global;
				else
					return parent_.Log;
			}
		}

		public Widget Parent
		{
			get { return parent_; }
			set { parent_ = value; }
		}

		public List<Widget> Children
		{
			get { return children_; }
		}

		public virtual float Spacing
		{
			get { return spacing_; }
			set { spacing_ = value; }
		}

		public void Add(Widget w, LayoutData data = null)
		{
			if (Contains(w))
			{
				Log.Error("layout already has widget " + w.Name);
				return;
			}

			children_.Add(w);
			AddImpl(w, data);
		}

		public void Remove(Widget w)
		{
			if (!children_.Remove(w))
			{
				Log.Error(
					"can't remove '" + w.Name + "' from layout, not found");

				return;
			}

			RemoveImpl(w);
		}

		public Size GetPreferredSize(float maxWidth, float maxHeight)
		{
			return DoGetPreferredSize(maxWidth, maxHeight);
		}

		public Size GetMinimumSize()
		{
			return DoGetMinimumSize();
		}

		public void DoLayout()
		{
			LayoutImpl();
		}

		public bool Contains(Widget w)
		{
			return children_.Contains(w);
		}

		protected virtual void AddImpl(Widget w, LayoutData data)
		{
			// no-op
		}

		protected virtual void RemoveImpl(Widget w)
		{
			// no-op
		}

		protected virtual Size DoGetPreferredSize(float maxWidth, float maxHeight)
		{
			return new Size(Widget.DontCare, Widget.DontCare);
		}

		protected virtual Size DoGetMinimumSize()
		{
			return new Size(Widget.DontCare, Widget.DontCare);
		}

		protected abstract void LayoutImpl();
	}


	class AbsoluteLayout : Layout
	{
		public override string TypeName { get { return "abs"; } }

		protected override void LayoutImpl()
		{
			// no-op
		}
	}
}
