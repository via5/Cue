using System;
using UnityEngine;

namespace Cue.Proc
{
	class Morph
	{
		public const float NoDisableBlink = 10000;

		private const int NoState = 0;
		private const int ForwardState = 1;
		private const int DelayOnState = 2;
		private const int BackwardState = 3;
		private const int DelayOffState = 4;

		private Person person_;
		private string id_;
		private DAZMorph morph_ = null;
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

		public Morph(
			Person p, string id, float start, float end, float minTime, float maxTime,
			float delayOff, float delayOn, bool resetBetween = false)
		{
			person_ = p;
			id_ = id;

			morph_ = p.VamAtom.FindMorph(id);
			if (morph_ == null)
				Cue.LogError($"{p.ID}: morph '{id}' not found");

			start_ = start;
			end_ = end;
			mid_ = Mid();
			last_ = mid_;
			r_ = mid_;
			forward_ = new Duration(minTime, maxTime);
			backward_ = new Duration(minTime, maxTime);
			delayOff_ = new Duration(0, delayOff);
			delayOn_ = new Duration(0, delayOn);
			easing_ = new SinusoidalEasing();
			resetBetween_ = resetBetween;

			Reset();
		}

		public string Name
		{
			get
			{
				if (morph_ == null)
					return $"{id_} (not found)";
				else
					return morph_.morphName;
			}
		}

		public override string ToString()
		{
			return
				$"start={start_:0.##} end={end_:0.##} mid={mid_} last={last_}\n" +
				$"fwd={forward_} bwd={backward_} dOff={delayOff_} dOn={delayOn_}\n" +
				$"state={state_} r={r_:0.##} mag={mag_:0.##} f={finished_} morphValue={morph_?.morphValue ?? -1}";
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
			state_ = NoState;
			finished_ = false;

			forward_.Reset();
			backward_.Reset();
			delayOff_.Reset();
			delayOn_.Reset();

			if (morph_ != null)
				morph_.morphValue = morph_.startValue;
		}

		public void Update(float s, bool limitHit)
		{
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

			return morph_.startValue;
		}

		public float Set(float intensity, float max)
		{
			closeToMid_ = false;

			if (morph_ == null)
				return 0;

			intensity_ = intensity;

			var v = Mathf.Lerp(last_, r_, easing_.Magnitude(mag_));
			if (Math.Abs(v - mid_) > max)
				v = mid_ + Math.Sign(v) * max;

			var d = v - mid_;

			morph_.morphValue = v;

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
				last_ = r_;

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
