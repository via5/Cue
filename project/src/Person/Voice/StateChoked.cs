using SimpleJSON;

namespace Cue
{
	public class VoiceStateChoked : BasicVoiceState
	{
		private float minTimeForMoaning_ = 3;
		private float moaningAfter_ = 1;
		private float moaningTime_ = 3;

		private bool wasBreathing_ = false;
		private float elapsed_ = 0;

		private VoiceStateChoked()
		{
		}

		public VoiceStateChoked(JSONClass vo)
		{
			Load(vo, false);
		}

		protected override void DoLoad(JSONClass o, bool inherited)
		{
			minTimeForMoaning_ = J.ReqFloat(o, "minTimeForMoaning");
			moaningAfter_ = J.ReqFloat(o, "moaningAfter");
			moaningTime_ = J.ReqFloat(o, "moaningTime");
		}

		public override string Name
		{
			get { return "chokedState"; }
		}

		public override IVoiceState Clone()
		{
			var s = new VoiceStateChoked();
			s.CopyFrom(this);
			return s;
		}

		private void CopyFrom(VoiceStateChoked o)
		{
			base.CopyFrom(o);
			minTimeForMoaning_ = o.minTimeForMoaning_;
			moaningAfter_ = o.moaningAfter_;
			moaningTime_ = o.moaningTime_;
		}

		protected override void DoStart()
		{
			Set();
			elapsed_ = 0;
			wasBreathing_ = false;
		}

		private void Set()
		{
			v_.Provider.SetSilent();
			v_.MouthEnabled = false;
			v_.ChestEnabled = false;
		}

		private void Unset()
		{
			v_.MouthEnabled = true;
			v_.ChestEnabled = true;
		}

		protected override void DoUpdate(float s)
		{
			if (Person.Body.Breathing)
			{
				if (!wasBreathing_)
				{
					wasBreathing_ = true;
					Unset();

					if (elapsed_ >= minTimeForMoaning_)
					{
						v_.Provider.SetMoaning(moaningAfter_);
					}
					else
					{
						SetDone();
						return;
					}

					elapsed_ = 0;
				}

				elapsed_ += s;

				if (elapsed_ < moaningTime_)
				{
					v_.Provider.SetMoaning(moaningAfter_);
				}
				else
				{
					// no ramp down, just go to the next state, which will
					// decide what to do next; ramping down to 0 doesn't work
					// if excitement is high
					SetDone();
					return;
				}
			}
			else
			{
				if (wasBreathing_)
				{
					Set();
					wasBreathing_ = false;
					elapsed_ = 0;
				}

				elapsed_ += s;
			}
		}

		protected override int DoCanRun()
		{
			if (!Person.Body.Breathing)
			{
				SetLastState("ok, choked");
				return Emergency;
			}

			SetLastState("breathing");
			return CannotRun;
		}

		protected override void DoDebug(DebugLines debug)
		{
			debug.Add("minTimeForMoaning", $"{minTimeForMoaning_}");
			debug.Add("moaningAfter", $"{moaningAfter_}");
			debug.Add("moaningTime", $"{moaningTime_}");
			debug.Add("is breathing", $"{Person.Body.Breathing}");
			debug.Add("wasBreathing", $"{wasBreathing_}");
			debug.Add("elapsed", $"{elapsed_:0.00}");
		}
	}
}
