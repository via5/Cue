using SimpleJSON;

namespace Cue
{
	public class VoiceStateKiss : VoiceState
	{
		private bool kissEnabled_ = false;
		private float kissVoiceChance_ = 0;
		private KissEvent e_ = null;


		private VoiceStateKiss()
		{
		}

		public VoiceStateKiss(JSONClass o)
		{
			if (o.HasKey("kiss"))
			{
				var ko = o["kiss"].AsObject;

				kissEnabled_ = J.ReqBool(ko, "enabled");
				kissVoiceChance_ = J.ReqFloat(ko, "voiceChance");
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
		}

		protected override void DoUpdate(float s)
		{
			if (!IsKissing())
				SetDone();
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
		}
	}
}
