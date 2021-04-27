using Cue.W;
using System.Collections.Generic;
using UnityEngine;

namespace Cue
{
	class ObjectControls
	{
		private IObject object_;
		private IBoxGraphic graphic_;
		private bool hovered_ = false;

		public ObjectControls(IObject o)
		{
			object_ = o;
			graphic_ = Cue.Instance.Sys.CreateBoxGraphic(object_.Position);
			UpdateColor();
		}

		public IObject Object
		{
			get { return object_; }
		}

		public bool Hovered
		{
			get { return hovered_; }
			set { hovered_ = value; }
		}

		public bool Visible
		{
			set { graphic_.Visible = value; }
		}

		public IBoxGraphic Graphic
		{
			get { return graphic_; }
		}

		public void Update()
		{
			UpdateColor();
		}

		public void Destroy()
		{
			graphic_.Destroy();
			graphic_ = null;
		}

		private void UpdateColor()
		{
			if (hovered_)
				graphic_.Color = new Color(0, 1, 0, 0.3f);
			else if (object_.Slots.AnyLocked)
				graphic_.Color = new Color(1, 0, 0, 0.1f);
			else
				graphic_.Color = new Color(0, 0, 1, 0.1f);
		}
	}


	class Controls
	{
		private bool visible_ = false;
		private List<ObjectControls> controls_ = new List<ObjectControls>();

		public Controls()
		{
			Cue.Instance.HoveredChanged += OnHoveredChanged;
		}

		public List<ObjectControls> All
		{
			get { return controls_; }
		}

		public bool Visible
		{
			get
			{
				return visible_;
			}

			set
			{
				if (visible_ != value)
				{
					visible_ = value;

					foreach (var c in controls_)
						c.Visible = value;
				}
			}
		}

		public void Create()
		{
			foreach (var o in Cue.Instance.Objects)
				controls_.Add(new ObjectControls(o));
		}

		public void Destroy()
		{
			foreach (var c in controls_)
				c.Destroy();

			controls_.Clear();
		}

		public void Update()
		{
			if (!visible_)
				return;

			for (int i = 0; i < controls_.Count; ++i)
				controls_[i].Update();
		}

		private void OnHoveredChanged(IObject o)
		{
			for (int i = 0; i < controls_.Count; ++i)
				controls_[i].Hovered = (controls_[i].Object == o);
		}
	}
}
