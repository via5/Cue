namespace Cue
{
	class BodyPart
	{
		private Person person_;
		private int type_;
		private Sys.IBodyPart part_;
		private bool forceBusy_ = false;
		private Sys.IGraphic render_ = null;

		public BodyPart(Person p, int type, Sys.IBodyPart part)
		{
			person_ = p;
			type_ = type;
			part_ = part;
		}

		public bool Render
		{
			set
			{
				if (value)
				{
					if (render_ == null)
					{
						render_ = Cue.Instance.Sys.CreateBoxGraphic(
							Name + "_render",
							Vector3.Zero, new Vector3(0.005f, 0.005f, 0.005f),
							new Color(0, 0, 1, 0.1f));
					}

					render_.Visible = true;
					++person_.Body.RenderingParts;
				}
				else
				{
					if (render_ != null)
					{
						render_.Visible = false;
						--person_.Body.RenderingParts;
					}
				}
			}
		}

		public void UpdateRender()
		{
			if (render_ != null)
				render_.Position = part_.Position;
		}

		public Person Person
		{
			get { return person_; }
		}

		public Sys.IBodyPart Sys
		{
			get { return part_; }
		}

		public Sys.Vam.VamBodyPart VamSys
		{
			get { return part_ as Sys.Vam.VamBodyPart; }
		}

		public bool Exists
		{
			get { return (part_ != null); }
		}

		public string Name
		{
			get { return BodyParts.ToString(type_); }
		}

		public int Type
		{
			get { return type_; }
		}

		public bool CanTrigger
		{
			get { return part_?.CanTrigger ?? false; }
		}

		public Sys.TriggerInfo[] GetTriggers()
		{
			return part_?.GetTriggers();
		}

		public bool Triggered
		{
			get
			{
				if (part_ == null)
					return false;

				var ts = part_?.GetTriggers();
				if (ts == null)
					return false;

				return (ts.Length > 0);
			}
		}

		public bool Grabbed
		{
			get { return part_?.Grabbed ?? false; }
		}

		public bool CloseTo(BodyPart other)
		{
			if (!Exists || !other.Exists)
				return false;

			return DistanceToSurface(other) < 0.1f;
		}

		public float DistanceToSurface(BodyPart other)
		{
			if (!Exists || !other.Exists)
				return float.MaxValue;

			return Sys.DistanceToSurface(other.Sys);
		}

		public void ForceBusy(bool b)
		{
			forceBusy_ = b;
		}

		public void LinkTo(BodyPart other)
		{
			if (!Exists)
				return;

			if (other != null && !other.Exists)
				return;

			Sys.LinkTo(other?.Sys);
		}

		public void Unlink()
		{
			LinkTo(null);
		}

		public bool Linked
		{
			get
			{
				if (!Exists)
					return false;

				return Sys.Linked;
			}
		}

		public bool Busy
		{
			get
			{
				if (forceBusy_)
					return true;

				// todo
				return
					person_.Kisser.IsBusy(type_) ||
					person_.Blowjob.IsBusy(type_) ||
					person_.Handjob.IsBusy(type_);
			}
		}

		public Vector3 ControlPosition
		{
			get
			{
				return part_?.ControlPosition ?? Vector3.Zero;

			}

			set
			{
				if (part_ != null)
					part_.ControlPosition = value;
			}
		}

		public Quaternion ControlRotation
		{
			get
			{
				return part_?.ControlRotation ?? Quaternion.Zero;
			}

			set
			{
				if (part_ != null)
					part_.ControlRotation = value;
			}
		}

		public Vector3 Position
		{
			get
			{
				return part_?.Position ?? Vector3.Zero;

			}
		}

		public Quaternion Rotation
		{
			get
			{
				return part_?.Rotation ?? Quaternion.Zero;
			}
		}

		public void AddRelativeForce(Vector3 v)
		{
			part_?.AddRelativeForce(v);
		}

		public void AddRelativeTorque(Vector3 v)
		{
			part_?.AddRelativeTorque(v);
		}

		public override string ToString()
		{
			string s = "";

			if (part_ == null)
				s += "null";
			else
				s += part_.ToString();

			s += $" ({BodyParts.ToString(type_)})";

			return s;
		}
	}
}
