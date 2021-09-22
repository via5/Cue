using System.Collections.Generic;

namespace Cue
{
	class BodyPart
	{
		private const float TriggerCheckDelay = 1;

		private Person person_;
		private int type_;
		private Sys.IBodyPart part_;
		private bool forceBusy_ = false;
		private Sys.IGraphic render_ = null;
		private Sys.TriggerInfo[] triggers_ = null;
		private List<Sys.TriggerInfo> forcedTriggers_ = new List<Sys.TriggerInfo>();
		private float lastTriggerCheck_ = 0;

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
			get { return BP.ToString(type_); }
		}

		public int Type
		{
			get { return type_; }
		}

		public bool CanTrigger
		{
			get { return part_?.CanTrigger ?? false; }
		}

		public void AddForcedTrigger(
			int sourcePersonIndex, int sourceBodyPart, float value = 1)
		{
			forcedTriggers_.Add(new Sys.TriggerInfo(
				sourcePersonIndex, sourceBodyPart, value, true));
		}

		public void RemoveForcedTrigger(int sourcePersonIndex, int sourceBodyPart)
		{
			for (int i = 0; i < forcedTriggers_.Count; ++i)
			{
				if (forcedTriggers_[i].personIndex == sourcePersonIndex)
				{
					if (forcedTriggers_[i].sourcePartIndex == sourceBodyPart)
					{
						forcedTriggers_.RemoveAt(i);
						return;
					}
				}
			}

			Cue.LogError(
				$"{this} RemoveForcedTrigger: not found for " +
				$"pi={sourcePersonIndex} bp={sourceBodyPart}");
		}

		public Sys.TriggerInfo[] GetTriggers()
		{
			// todo
			if (UnityEngine.Time.realtimeSinceStartup >= (lastTriggerCheck_ + TriggerCheckDelay))
			{
				lastTriggerCheck_ = UnityEngine.Time.realtimeSinceStartup;
				triggers_ = part_?.GetTriggers();

				if (forcedTriggers_.Count > 0)
				{
					if (triggers_ == null)
					{
						triggers_ = forcedTriggers_.ToArray();
					}
					else
					{
						var copy = new List<Sys.TriggerInfo>(triggers_);

						for (int i = 0; i < forcedTriggers_.Count; ++i)
						{
							bool found = false;

							for (int j = 0; j < triggers_.Length; ++j)
							{
								if (triggers_[j].personIndex == forcedTriggers_[i].personIndex)
								{
									if (triggers_[j].sourcePartIndex == forcedTriggers_[i].sourcePartIndex)
									{
										found = true;
										break;
									}
								}
							}

							if (!found)
								copy.Add(forcedTriggers_[i]);
						}

						triggers_ = copy.ToArray();
					}
				}
			}

			return triggers_;
		}

		public bool Triggered
		{
			get
			{
				if (part_ == null)
					return false;

				var ts = GetTriggers();
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

		public bool IsLinkedTo(BodyPart other)
		{
			if (!Exists || other == null || !other.Exists)
				return false;

			return Sys.IsLinkedTo(other.Sys);
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

			s += $" ({BP.ToString(type_)})";

			return s;
		}
	}
}
