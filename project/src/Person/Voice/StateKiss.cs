using SimpleJSON;

namespace Cue
{
	public class VoiceStateKiss : VoiceState
	{
		private bool enabled_ = false;
		private float voiceChance_ = 0;
		private float voiceTime_ = 0;
		private float elapsed_ = 0;
		private KissEvent e_ = null;
		private bool leading_ = false;

		private bool moaning_ = false;
		private float lastRng_ = 0;

		private VoiceStateKiss()
		{
		}

		public VoiceStateKiss(JSONClass vo)
		{
			Load(vo, false);
		}

		public override void Load(JSONClass vo, bool inherited)
		{
			if (vo.HasKey("kissState"))
			{
				var o = J.ReqObject(vo, "kissState");

				if (o.HasKey("enabled"))
					enabled_ = J.ReqBool(o, "enabled");
				else if (!inherited)
					throw new LoadFailed("missing enabled");

				if (o.HasKey("voiceChance"))
					voiceChance_ = J.ReqFloat(o, "voiceChance");
				else if (!inherited)
					throw new LoadFailed("missing voiceChance");

				if (o.HasKey("voiceTime"))
					voiceTime_ = J.ReqFloat(o, "voiceTime");
				else if (!inherited)
					throw new LoadFailed("missing voiceTime");
			}
			else if (!inherited)
			{
				throw new LoadFailed("missing kissState");
			}
		}

		public override string Name
		{
			get { return "kiss"; }
		}

		public override IVoiceState Clone()
		{
			var s = new VoiceStateKiss();
			s.CopyFrom(this);
			return s;
		}

		private void CopyFrom(VoiceStateKiss o)
		{
			enabled_ = o.enabled_;
			voiceChance_ = o.voiceChance_;
			voiceTime_ = o.voiceTime_;
		}

		protected override void DoStart()
		{
			leading_ = e_.Leading;
			SetKissingSound();
		}

		protected override void DoUpdate(float s)
		{
			if (!IsKissing())
			{
				SetDone();
				return;
			}

			elapsed_ += s;
			if (elapsed_ >= voiceTime_)
			{
				elapsed_ = 0;

				lastRng_ = U.RandomFloat(0, 1);
				if (lastRng_ <= voiceChance_)
				{
					moaning_ = true;
					v_.Provider.SetMoaning(v_.MaxIntensity);
				}
				else
				{
					moaning_ = false;
					SetKissingSound();
				}
			}
		}

		private void SetKissingSound()
		{
			if (leading_)
				v_.Provider.SetKissing();
			else
				v_.Provider.SetSilent();
		}

		public override int CanRun()
		{
			if (HasEmergency())
			{
				SetLastState("ok");
				return Emergency;
			}

			SetLastState("not kissing");
			return CannotRun;
		}

		public override bool HasEmergency()
		{
			if (!enabled_)
				return false;

			if (IsKissing())
			{
				SetLastState("ok");
				return true;
			}

			return false;
		}

		private bool IsKissing()
		{
			if (e_ == null)
				e_ = v_.Person.AI.GetEvent<KissEvent>();

			return e_.Active;
		}

		protected override void DoDebug(DebugLines debug)
		{
			if (leading_)
				debug.Add("kiss audio", "yes, leading");
			else
				debug.Add("kiss audio", "no, not leading");

			debug.Add("elapsed", $"{elapsed_:0.00}/{voiceTime_:0.00}");
			debug.Add("moaning", $"{moaning_:0.00}");
			debug.Add("lastRng", $"{lastRng_:0.00}/{voiceChance_:0.00}");
		}
	}
}
