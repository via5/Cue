using SimpleJSON;

namespace Cue
{
	public class VoiceStateChoked : VoiceState
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

		public override void Load(JSONClass vo, bool inherited)
		{
			if (vo.HasKey("chokedState"))
			{
				var o = J.ReqObject(vo, "chokedState");

				minTimeForMoaning_ = J.ReqFloat(o, "minTimeForMoaning");
				moaningAfter_ = J.ReqFloat(o, "moaningAfter");
				moaningTime_ = J.ReqFloat(o, "moaningTime");
			}
			else if (!inherited)
			{
				throw new LoadFailed("missing chokedState");
			}
		}

		public override string Name
		{
			get { return "notBreathing"; }
		}

		public override IVoiceState Clone()
		{
			var s = new VoiceStateChoked();
			s.CopyFrom(this);
			return s;
		}

		private void CopyFrom(VoiceStateChoked o)
		{
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

				if (elapsed_ >= moaningTime_)
				{
					v_.Provider.SetBreathing();
					SetDone();
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
				SetLastState("ok, not breathing");
				return Emergency;
			}

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
