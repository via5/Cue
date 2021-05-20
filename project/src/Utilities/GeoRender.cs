﻿namespace Cue
{
	class FrustumRenderer
	{
		private Person person_;
		private Frustum frustum_;
		private Color color_ = Color.Zero;
		private W.IGraphic near_ = null;
		private W.IGraphic far_ = null;
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
				near_ = Create(frustum_.NearSize());
				far_ = Create(frustum_.FarSize());
				Cue.LogInfo($"{frustum_.nearTL} {frustum_.nearTR} {frustum_.nearBL} {frustum_.nearBR}");
			}

			near_.Position =
				RefOffset +
				Vector3.Rotate(frustum_.NearCenter(), RefDirection);

			near_.Direction = RefDirection;


			far_.Position =
				RefOffset +
				Vector3.Rotate(frustum_.FarCenter(), RefDirection);

			far_.Direction = RefDirection;
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

		private W.IGraphic Create(Vector3 size)
		{
			var g = Cue.Instance.Sys.CreateBoxGraphic(
				"FrustumRender.near", Vector3.Zero, size, Color.Zero);

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

		private Vector3 RefDirection
		{
			get
			{
				if (rot_ == BodyParts.None)
					return person_.Direction;
				else
					return person_.Body.Get(rot_).Direction;
			}
		}
	}
}