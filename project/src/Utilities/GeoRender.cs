namespace Cue
{
	class FrustumRenderer
	{
		private Person person_;
		private Frustum frustum_;
		private Color color_ = Color.Zero;
		private Sys.IGraphic near_ = null;
		private Sys.IGraphic far_ = null;
		private int offset_, rot_;

		public FrustumRenderer(Person p, Frustum f, int offsetBodyPart, int rotationBodyPart)
		{
			person_ = p;
			frustum_ = f;
			offset_ = offsetBodyPart;
			rot_ = rotationBodyPart;
		}

		public bool Visible
		{
			set
			{
				if (near_ != null)
					near_.Visible = value;

				if (far_ != null)
					far_.Visible = value;
			}
		}

		public void Update(float s)
		{
			if (near_ == null)
			{
				near_ = Create(frustum_.NearSize(), "near");
				far_ = Create(frustum_.FarSize(), "far");
			}

			near_.Position =
				RefOffset +
				RefRotation.Rotate(frustum_.NearCenter());

			near_.Rotation = RefRotation;


			far_.Position =
				RefOffset +
				RefRotation.Rotate(frustum_.FarCenter());

			far_.Rotation = RefRotation;
		}

		public Color Color
		{
			set
			{
				color_ = value;

				if (near_ != null)
					near_.Color = color_;

				if (far_ != null)
					far_.Color = color_;
			}
		}

		private Sys.IGraphic Create(Vector3 size, string name)
		{
			var g = Cue.Instance.Sys.CreateBoxGraphic(
				$"FrustumRender.{name}", Vector3.Zero, size, Color.Zero);

			g.Collision = false;
			g.Color = color_;
			g.Visible = true;

			return g;
		}

		private Vector3 RefOffset
		{
			get
			{
				if (offset_ == BodyParts.None)
					return person_.Position;
				else
					return person_.Body.Get(offset_).Position;
			}
		}

		private Quaternion RefRotation
		{
			get
			{
				if (rot_ == BodyParts.None)
					return person_.Rotation;
				else
					return person_.Body.Get(rot_).Rotation;
			}
		}
	}
}
