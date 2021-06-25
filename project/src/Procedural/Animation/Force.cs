using SimpleJSON;
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
		private SlidingMovement movement_;
		private IEasing excitement_;
		private SlidingDuration fwdDuration_, bwdDuration_;
		private Duration fwdDelay_, bwdDelay_;
		private IEasing fwdDelayExcitement_, bwdDelayExcitement_;
		private int flags_;

		private int state_ = Forwards;
		private Rigidbody rb_ = null;
		private IEasing fwdEasing_ = new SinusoidalEasing();
		private IEasing bwdEasing_ = new SinusoidalEasing();
		private Person person_ = null;
		private bool wasBusy_ = false;
		private bool oneFrameFinished_ = false;

		public Force(
			int type, int bodyPart, string rbId,
			SlidingMovement m, IEasing excitement,
			SlidingDuration fwdDuration, SlidingDuration bwdDuration,
			Duration fwdDelay, Duration bwdDelay,
			IEasing fwdDelayExcitement, IEasing bwdDelayExcitement,
			int flags)
		{
			type_ = type;
			bodyPart_ = bodyPart;
			rbId_ = rbId;
			movement_ = m;
			excitement_ = excitement;
			fwdDuration_ = fwdDuration;
			bwdDuration_ = bwdDuration;
			fwdDelay_ = fwdDelay;
			bwdDelay_ = bwdDelay;
			fwdDelayExcitement_ = fwdDelayExcitement;
			bwdDelayExcitement_ = bwdDelayExcitement;
			flags_ = flags;
		}

		public static IEasing EasingFromJson(JSONClass o, string key)
		{
			if (!o.HasKey(key) || o[key].Value == "")
				return new ConstantOneEasing();

			var e = EasingFactory.FromString(o[key]);
			if (e == null)
				throw new LoadFailed($"easing type {o[key].Value} not found");

			return e;
		}

		public static Force Create(int type, JSONClass o)
		{
			try
			{
				var bodyPart = BodyParts.FromString(o["bodyPart"]);
				if (bodyPart == BodyParts.None)
					throw new LoadFailed($"bad body part '{o["bodyPart"]}'");

				int flags = NoFlags;

				if (o["loop"].AsBool)
					flags |= Loop;

				if (o["resetBetween"].AsBool)
					flags |= ResetBetween;

				SlidingDuration fwd, bwd;

				if (o.HasKey("duration"))
				{
					fwd = SlidingDuration.FromJSON(o, "duration");
					bwd = null;
				}
				else
				{
					fwd = SlidingDuration.FromJSON(o, "fwdDuration");
					bwd = SlidingDuration.FromJSON(o, "bwdDuration");
				}

				return new Force(
					type, bodyPart, o["rigidbody"],
					SlidingMovement.FromJSON(o, "movement", true),
					EasingFromJson(o, "excitement"),
					fwd, bwd,
					Duration.FromJSON(o, "fwdDelay"),
					Duration.FromJSON(o, "bwdDelay"),
					EasingFromJson(o, "fwdDelayExcitement"),
					EasingFromJson(o, "bwdDelayExcitement"),
					flags);
			}
			catch (LoadFailed e)
			{
				throw new LoadFailed($"force type {type}/{e.Message}");
			}
		}

		public bool Done
		{
			get { return oneFrameFinished_; }
		}

		public ITarget Clone()
		{
			return new Force(
				type_, bodyPart_, rbId_,
				new SlidingMovement(movement_), excitement_,
				new SlidingDuration(fwdDuration_),
				bwdDuration_ == null ? null : new SlidingDuration(bwdDuration_),
				new Duration(fwdDelay_), new Duration(bwdDelay_),
				fwdDelayExcitement_, bwdDelayExcitement_,
				flags_);
		}

		public void Start(Person p)
		{
			person_ = p;

			if (p.VamAtom != null)
			{
				rb_ = Cue.Instance.VamSys.FindRigidbody(p.VamAtom.Atom, rbId_);
				if (rb_ == null)
				{
					Cue.LogError($"Force: rigidbody {rbId_} not found");
					return;
				}

				Reset();
			}
		}

		public void Reset()
		{
			movement_.Reset();
			fwdDuration_.Reset();
			bwdDuration_?.Reset();
			fwdDelay_.Reset();
			bwdDelay_.Reset();
			state_ = Forwards;
		}

		public void FixedUpdate(float s)
		{
			oneFrameFinished_ = false;

			if (bodyPart_ != BodyParts.None && person_.Body.Get(bodyPart_).Busy)
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
					movement_.Update(s);
					CurrentDuration().Update(s);
					Apply();

					if (CurrentDuration().Finished)
					{
						movement_.WindowMagnitude = person_.Excitement.Value;
						CurrentDuration().WindowMagnitude = person_.Excitement.Value;

						if (bwdDuration_ == null)
							fwdDuration_.Restart();

						if (fwdDelay_.Enabled)
						{
							state_ = ForwardsDelay;
						}
						else if (Bits.IsSet(flags_, ResetBetween))
						{
							movement_.SetNext(Vector3.Zero);
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
					movement_.Update(s);
					fwdDelay_.Update(s);
					Apply();

					if (fwdDelay_.Finished)
					{
						if (Bits.IsSet(flags_, ResetBetween))
						{
							movement_.SetNext(Vector3.Zero);
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
					movement_.Update(s);
					CurrentDuration().Update(s);
					Apply();

					if (CurrentDuration().Finished)
					{
						movement_.WindowMagnitude = person_.Excitement.Value;
						CurrentDuration().WindowMagnitude = person_.Excitement.Value;

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
					movement_.Update(s);
					bwdDelay_.Update(s);
					Apply();

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
					rb_?.AddRelativeForce(W.VamU.ToUnity(v));
					break;
				}

				case RelativeTorque:
				{
					rb_?.AddRelativeTorque(W.VamU.ToUnity(v));
					break;
				}
			}
		}

		private SlidingDuration CurrentDuration()
		{
			switch (state_)
			{
				case Forwards:
				case ForwardsDelay:
					return fwdDuration_;

				case Backwards:
				case BackwardsDelay:
					return bwdDuration_ == null ? fwdDuration_ : bwdDuration_;

				default:
					Cue.LogError("??");
					return fwdDuration_;
			}
		}

		private float Progress()
		{
			return CurrentDuration().Progress;
		}

		private IEasing Easing()
		{
			switch (state_)
			{
				case Forwards:
				case ForwardsDelay:
					return fwdEasing_;

				case Backwards:
				case BackwardsDelay:
					return bwdEasing_;

				default:
					Cue.LogError("??");
					return null;
			}
		}

		private float RawMagnitude()
		{
			return Easing()?.Magnitude(Progress()) ?? 0;
		}

		private float Magnitude()
		{
			return RawMagnitude();
		}

		private Vector3 Lerped()
		{
			return movement_.Lerped(Magnitude());
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
				$"{movement_}\n" +
				$"fdur={fwdDuration_}\n" +
				$"bdur={bwdDuration_}\n" +
				$"fdel={fwdDelay_} bdel={bwdDelay_}\n" +
				$"p={Progress():0.00} mag={Magnitude():0.00} ex={ExcitementFactor():0.00}\n" +
				$"lerped={Lerped()} state={state_} busy={wasBusy_}";
		}

		private float ExcitementFactor()
		{
			return excitement_.Magnitude(person_.Excitement.Value);
		}

		private void Next()
		{
			if (!movement_.Next())
			{
				movement_.SetNext(movement_.Last);
			}
		}

		private float Random(float min, float max, float f)
		{
			return U.RandomFloat(min, max) * f;
		}
	}
}
