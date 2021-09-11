using System.Collections.Generic;

namespace Cue
{
	class ObjectControls
	{
		private IObject object_;
		private Sys.IGraphic graphic_;
		private bool hovered_ = false;

		public ObjectControls(IObject o)
		{
			object_ = o;

			graphic_ = Cue.Instance.Sys.CreateBoxGraphic(
				"Control (" + object_.ID + ")",
				object_.Position, new Vector3(0.5f, 0.05f, 0.5f),
				new Color(0, 0, 1, 0.05f));

			graphic_.Visible = false;
			graphic_.Rotation = object_.Rotation;

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

		public Sys.IGraphic Graphic
		{
			get { return graphic_; }
		}

		public void Update()
		{
			UpdateColor();
			graphic_.Position = object_.Position;
			graphic_.Rotation = object_.Rotation;
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
		private Sys.IGraphic moveTarget_ = null;

		public Controls()
		{
		}

		public List<ObjectControls> All
		{
			get { return controls_; }
		}

		public Vector3 HoverTargetPosition
		{
			set
			{
				moveTarget_.Position = value;
			}
		}

		public bool HoverTargetVisible
		{
			set
			{
				//moveTarget_.Visible = value;
			}
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

		public IObject Hovered
		{
			set
			{
				OnHoveredChanged(value);
			}
		}

		public void Create()
		{
			moveTarget_ = Cue.Instance.Sys.CreateSphereGraphic(
				"MoveTarget", Vector3.Zero, 0.1f, new Color(1, 1, 1, 1));

			moveTarget_.Collision = false;
			moveTarget_.Visible = false;

			foreach (var o in Cue.Instance.AllObjects)
				controls_.Add(new ObjectControls(o));
		}

		public void Destroy()
		{
			moveTarget_.Destroy();

			foreach (var c in controls_)
				c.Destroy();

			controls_.Clear();
		}

		public void Update()
		{
			for (int i = 0; i < controls_.Count; ++i)
				controls_[i].Update();
		}

		private void OnHoveredChanged(IObject o)
		{
			for (int i = 0; i < controls_.Count; ++i)
			{
				if (controls_[i].Object == o && o.Slots.AnyInteractable)
				{
					controls_[i].Hovered = true;
					controls_[i].Visible = true;
				}
				else
				{
					controls_[i].Hovered = false;
					controls_[i].Visible = visible_;
				}
			}
		}
	}
}
