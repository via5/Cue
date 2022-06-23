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
					kissEnabled_ = J.ReqBool(o, "enabled");
				else if (!inherited)
					throw new LoadFailed("missing enabled");

				if (o.HasKey("voiceChance"))
					kissVoiceChance_ = J.ReqFloat(o, "voiceChance");
				else if (!inherited)
					throw new LoadFailed("missing voiceChance");
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
