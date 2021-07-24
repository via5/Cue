using SimpleJSON;
using System;
using UnityEngine;

namespace Cue.Proc
{
	class AnimatedMorph : BasicTarget
	{
		private int bodyPart_;
		private string morphId_;
		private float min_, max_;
		private Duration duration_;
		private Duration delay_;
		private ClampableMorph m_ = null;

		public AnimatedMorph(
			int bodyPart, string morphId, float min, float max,
			Duration d, Duration delay)
				: base(new NoSync())
		{
			bodyPart_ = bodyPart;
			morphId_ = morphId;
			min_ = min;
			max_ = max;
			duration_ = d;
			delay_ = delay;
		}

		public static AnimatedMorph Create(JSONClass o)
		{
			string id = o["morph"];

			try
			{
				var bodyPart = BodyParts.FromString(o["bodyPart"]);
				if (bodyPart == BodyParts.None)
					throw new LoadFailed($"bad body part '{o["bodyPart"]}'");

				float min;
				if (!float.TryParse(o["min"], out min))
					throw new LoadFailed("min is not a number");

				float max;
				if (!float.TryParse(o["max"], out max))
					throw new LoadFailed("max is not a number");

				return new AnimatedMorph(
					bodyPart, id, min, max,
					Duration.FromJSON(o, "duration"),
					Duration.FromJSON(o, "delay"));
			}
			catch (LoadFailed e)
			{
				throw new LoadFailed($"morph '{id}'/{e.Message}");
			}
		}

		public override bool Done
		{
			get { return m_?.Finished ?? true; }
		}

		public override ITarget Clone()
		{
			return new AnimatedMorph(
				bodyPart_, morphId_, min_, max_,
				new Duration(duration_), new Duration(delay_));
		}

		public override void Start(Person p)
		{
			if (m_ == null)
			{
				m_ = new ClampableMorph(
					p, bodyPart_, morphId_, min_, max_,
					new Duration(duration_), new Duration(delay_));

				m_.Set(float.MaxValue);
			}
		}

		public override void Reset()
		{
			base.Reset();
			m_?.Reset();
		}

		public override void FixedUpdate(float s)
		{
			if (m_ != null)
			{
				m_.FixedUpdate(s, 1, false);
				m_.Set(float.MaxValue);
			}
		}

		public override string ToString()
		{
			return $"morph {morphId_} ({BodyParts.ToString(bodyPart_)})";
		}

		public override string ToDetailedString()
		{
			return
				$"morph {morphId_} ({BodyParts.ToString(bodyPart_)})\n" +
				$"min={min_} max={max_} d={duration_} delay={delay_}\n" +
				(m_?.ToString() ?? "nomorph");
		}
	}



	class ClampableMorph
	{
		public const float NoDisableBlink = 10000;

		private const int NoState = 0;
		private const int ForwardState = 1;
		private const int DelayOnState = 2;
		private const int BackwardState = 3;
		private const int DelayOffState = 4;

		private Person person_;
		private int bodyPart_;
		private string id_;
		private Morph morph_ = null;
		private float start_, end_, mid_;
		private Duration forward_, backward_;
		private Duration delayOff_, delayOn_;
		private int state_ = NoState;
		private float r_ = 0;
		private float mag_ = 0;
		private IEasing easing_;
		private bool finished_ = false;
		private bool resetBetween_;
		private float last_;
		private bool closeToMid_ = false;
		private bool awayFromMid_ = false;
		private float timeActive_ = 0;
		private float intensity_ = 0;

		private float disableBlinkAbove_ = NoDisableBlink;

		private ClampableMorph(
			Person p, int bodyPart, string id, float min, float max,
			Duration fwdDuration, Duration bwdDuration,
			Duration delayOff, Duration delayOn, bool resetBetween)
		{
			person_ = p;
			bodyPart_ = bodyPart;
			id_ = id;
			morph_ = new Morph(p.Atom.GetMorph(id));
			start_ = min;
			end_ = max;
			mid_ = Mid();
			last_ = mid_;
			r_ = mid_;

			forward_ = fwdDuration;
			backward_ = bwdDuration;
			delayOff_ = delayOff;
			delayOn_ = delayOn;
			easing_ = new SinusoidalEasing();
			resetBetween_ = resetBetween;

			Reset();
		}

		public ClampableMorph(
			Person p, int bodyPart, string id, float min, float max,
			Duration d, Duration delay)
				: this(p, bodyPart, id, min, max, d, d, delay, delay, false)
		{
		}

		public ClampableMorph(
			Person p, int bodyPart, string id, float start, float end,
			float minTime, float maxTime,
			float delayOff, float delayOn,
			bool resetBetween = false)
				: this(
					  p, bodyPart, id, start, end,
					  new Duration(minTime, maxTime),
					  new Duration(minTime, maxTime),
					  new Duration(0, delayOff),
					  new Duration(0, delayOn),
					  resetBetween)
		{
		}

		public string Name
		{
			get { return morph_.Name; }
		}

		public override string ToString()
		{
			return
				$"start={start_:0.##} end={end_:0.##} mid={mid_} last={last_}\n" +
				$"fwd={forward_} bwd={backward_}\n" +
				$"dOff={delayOff_} dOn={delayOn_}\n" +
				$"state={state_} r={r_:0.##} mag={mag_:0.##} f={finished_}\n" +
				morph_.ToString();
		}

		public IEasing Easing
		{
			get { return easing_; }
			set { easing_ = value; }
		}

		public bool Finished
		{
			get { return finished_; }
		}

		public bool CloseToMid
		{
			get { return closeToMid_; }
		}

		public float DisableBlinkAbove
		{
			get { return disableBlinkAbove_; }
			set { disableBlinkAbove_ = value; }
		}

		public void Reset()
		{
			ForceChange();

			if (morph_ != null)
			{
				if (bodyPart_ == BodyParts.None || !person_.Body.Get(bodyPart_).Busy)
					morph_.Reset();
			}
		}

		public void ForceChange()
		{
			state_ = NoState;
			finished_ = false;
			timeActive_ = 0;

			forward_.Reset();
			backward_.Reset();
			delayOff_.Reset();
			delayOn_.Reset();
		}

		public void FixedUpdate(float s, float intensity, bool limitHit)
		{
			intensity_ = intensity;

			if (morph_ == null)
			{
				finished_ = true;
				return;
			}

			finished_ = false;
			timeActive_ += s;

			switch (state_)
			{
				case NoState:
				{
					mag_ = 0;
					state_ = ForwardState;
					Next(limitHit);
					break;
				}

				case ForwardState:
				{
					forward_.Update(s);

					if (forward_.Finished)
					{
						mag_ = 1;

						if (delayOn_.Enabled)
							state_ = DelayOnState;
						else
							state_ = BackwardState;
					}
					else
					{
						mag_ = forward_.Progress;
					}

					break;
				}

				case DelayOnState:
				{
					delayOn_.Update(s);

					if (delayOn_.Finished)
						state_ = BackwardState;

					break;
				}

				case BackwardState:
				{
					if (!resetBetween_)
					{
						Next(limitHit);
						mag_ = 0;
						state_ = ForwardState;
						finished_ = true;
						break;
					}

					backward_.Update(s);
					mag_ = 1 - backward_.Progress;

					if (backward_.Finished)
					{
						Next(limitHit);

						if (delayOff_.Enabled)
						{
							state_ = DelayOffState;
						}
						else
						{
							state_ = ForwardState;
							finished_ = true;
						}
					}

					break;
				}

				case DelayOffState:
				{
					delayOff_.Update(s);

					if (delayOff_.Finished)
					{
						state_ = ForwardState;
						finished_ = true;
					}

					break;
				}
			}
		}

		private float Mid()
		{
			if (morph_ == null)
				return 0;

			return morph_.DefaultValue;
		}

		public float Set(float max)
		{
			closeToMid_ = false;

			if (morph_ == null)
				return 0;

			var v = Mathf.Lerp(last_, r_, easing_.Magnitude(mag_));
			if (Math.Abs(v - mid_) > max)
				v = mid_ + Math.Sign(v) * max;

			var d = v - mid_;

			if (bodyPart_ == BodyParts.None || !person_.Body.Get(bodyPart_).Busy)
				morph_.Value = v;

			if (disableBlinkAbove_ != NoDisableBlink)
				person_.Gaze.Eyes.Blink = (d < disableBlinkAbove_);

			d = Math.Abs(d);

			if (d < 0.01f)
				timeActive_ = 0;

			if (awayFromMid_ && d < 0.01f)
			{
				closeToMid_ = true;
				awayFromMid_ = false;
			}
			else if (d > 0.01f)
			{
				awayFromMid_ = true;
			}

			return d;
		}

		private void Next(bool limitHit)
		{
			if (resetBetween_)
				last_ = mid_;
			else
				last_ = morph_.Value;

			if (limitHit && timeActive_ >= 10)
			{
				Cue.LogVerbose(
					$"{person_.ID} {Name}: active for {timeActive_:0.00}, " +
					$"too long, forcing to 0");

				// force a reset to allow other morphs to take over the prio
				r_ = mid_;
				timeActive_ = 0;
			}
			else
			{
				r_ = U.RandomFloat(start_, end_) * intensity_;
			}
		}
	}
}
