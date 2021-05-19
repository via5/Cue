using UnityEngine;

namespace Cue.Proc
{
	class Force : ITarget
	{
		public const int NoFlags = 0x00;
		public const int Loop = 0x01;
		public const int ResetBetween = 0x02;

		public const int RelativeForce = 1;
		public const int RelativeTorque = 2;

		private const int Forwards = 1;
		private const int ForwardsDelay = 2;
		private const int Backwards = 3;
		private const int BackwardsDelay = 4;
		private const int Finished = 5;

		private int type_;
		private int bodyPart_;
		private string rbId_;
		private Vector3 min_, max_;
		private Duration fwdDuration_, bwdDuration_;
		private Duration fwdDelay_, bwdDelay_;
		private int flags_;

		private int state_ = Forwards;
		private Rigidbody rb_ = null;
		private Vector3 last_ = Vector3.Zero;
		private Vector3 current_ = Vector3.Zero;
		private IEasing easing_ = new SinusoidalEasing();
		private Person person_ = null;
		private bool wasBusy_ = false;
		private bool oneFrameFinished_ = false;

		public Force(
			int type, int bodyPart, string rbId, Vector3 min, Vector3 max,
			Duration fwdDuration, Duration bwdDuration,
			Duration fwdDelay, Duration bwdDelay, int flags)
		{
			type_ = type;
			bodyPart_ = bodyPart;
			rbId_ = rbId;
			min_ = min;
			max_ = max;
			fwdDuration_ = fwdDuration;
			bwdDuration_ = bwdDuration;
			fwdDelay_ = fwdDelay;
			bwdDelay_ = bwdDelay;
			flags_ = flags;
		}

		public bool Done
		{
			get { return oneFrameFinished_; }
		}

		public ITarget Clone()
		{
			return new Force(
				type_, bodyPart_, rbId_, min_, max_,
				fwdDuration_, bwdDuration_, fwdDelay_, bwdDelay_, flags_);
		}

		public void Start(Person p)
		{
			person_ = p;

			rb_ = Cue.Instance.VamSys.FindRigidbody(p.VamAtom.Atom, rbId_);
			if (rb_ == null)
			{
				Cue.LogError($"Force: rigidbody {rbId_} not found");
				return;
			}

			last_ = Vector3.Zero;
			state_ = Forwards;
			Next();
		}

		public void Reset()
		{
			fwdDuration_.Reset();
			bwdDuration_.Reset();
			fwdDelay_.Reset();
			bwdDelay_.Reset();
			current_ = Vector3.Zero;
			state_ = Forwards;
			Next();
		}

		public void FixedUpdate(float s)
		{
			oneFrameFinished_ = false;

			if (person_.Body.Get(bodyPart_).Busy)
			{
				wasBusy_ = true;
				return;
			}
			else if (wasBusy_)
			{
				Reset();
				wasBusy_ = false;
			}

			switch (state_)
			{
				case Forwards:
				{
					fwdDuration_.Update(s);
					Apply();

					if (fwdDuration_.Finished)
					{
						if (fwdDelay_.Enabled)
						{
							state_ = ForwardsDelay;
						}
						else if (Bits.IsSet(flags_, ResetBetween))
						{
							last_ = current_;
							current_ = Vector3.Zero;
							state_ = Backwards;
						}
						else if (Bits.IsSet(flags_, Loop))
						{
							Next();
						}
						else
						{
							state_ = Finished;
							oneFrameFinished_ = true;
						}
					}

					break;
				}

				case ForwardsDelay:
				{
					fwdDelay_.Update(s);

					if (fwdDelay_.Finished)
					{
						if (Bits.IsSet(flags_, ResetBetween))
						{
							last_ = current_;
							current_ = Vector3.Zero;
							state_ = Backwards;
						}
						else if (Bits.IsSet(flags_, Loop))
						{
							state_ = Forwards;
							Next();
						}
						else
						{
							state_ = Finished;
							oneFrameFinished_ = true;
						}
					}

					break;
				}

				case Backwards:
				{
					bwdDuration_.Update(s);
					Apply();

					if (bwdDuration_.Finished)
					{
						if (bwdDelay_.Enabled)
						{
							state_ = BackwardsDelay;
						}
						else if (Bits.IsSet(flags_, Loop))
						{
							state_ = Forwards;
							Next();
						}
						else
						{
							state_ = Finished;
							oneFrameFinished_ = true;
						}
					}

					break;
				}

				case BackwardsDelay:
				{
					bwdDelay_.Update(s);

					if (bwdDelay_.Finished)
					{
						if (Bits.IsSet(flags_, Loop))
						{
							state_ = Forwards;
							Next();
						}
						else
						{
							state_ = Finished;
							oneFrameFinished_ = true;
						}
					}

					break;
				}

				case Finished:
				{
					break;
				}
			}
		}

		private void Apply()
		{
			var v = Lerped();

			switch (type_)
			{
				case RelativeForce:
				{
					rb_.AddRelativeForce(W.VamU.ToUnity(v));
					break;
				}

				case RelativeTorque:
				{
					rb_.AddRelativeTorque(W.VamU.ToUnity(v));
					break;
				}
			}
		}

		private float Progress()
		{
			if (state_ == Forwards)
				return fwdDuration_.Progress;
			else
				return bwdDuration_.Progress;
		}

		private float Magnitude()
		{
			return easing_.Magnitude(Progress());
		}

		private Vector3 Lerped()
		{
			return Vector3.Lerp(last_, current_, Magnitude());
		}

		public static string TypeToString(int i)
		{
			switch (i)
			{
				case RelativeForce: return "rforce";
				case RelativeTorque: return "rtorque";
				default: return $"?{i}";
			}
		}

		public override string ToString()
		{
			return $"{rbId_} ({BodyParts.ToString(bodyPart_)})";
		}

		public virtual string ToDetailedString()
		{
			return
				$"{TypeToString(type_)} {rbId_} ({BodyParts.ToString(bodyPart_)})\n" +
				$"min={min_} max={max_}\n" +
				$"last={last_} current={current_}\n" +
				$"fdur={fwdDuration_} bdur={bwdDuration_}\n" +
				$"fdel={fwdDelay_} bdel={bwdDelay_}\n" +
				$"p={Progress():0.00} mag={Magnitude():0.00} lerped={Lerped()} " +
				$"state={state_} busy={wasBusy_}";
		}

		private void Next()
		{
			last_ = current_;

			current_ = new Vector3(
				U.RandomFloat(min_.X, max_.X),
				U.RandomFloat(min_.Y, max_.Y),
				U.RandomFloat(min_.Z, max_.Z));
		}
	}
}
