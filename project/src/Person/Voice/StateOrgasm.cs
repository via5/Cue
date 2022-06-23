using SimpleJSON;

namespace Cue
{
	public class VoiceStateOrgasm : VoiceState
	{
		private VoiceStateOrgasm()
		{
		}

		public VoiceStateOrgasm(JSONClass o)
		{
		}

		public override string Name
		{
			get { return "orgasm"; }
		}

		public override IVoiceState Clone()
		{
			var s = new VoiceStateOrgasm();
			s.CopyFrom(this);
			return s;
		}

		private void CopyFrom(VoiceStateOrgasm o)
		{
		}

		protected override void DoStart()
		{
			v_.Provider.StartOrgasm();
		}

		protected override void DoUpdate(float s)
		{
			if (v_.Person.Mood.State != Mood.OrgasmState)
			{
				v_.Provider.StopOrgasm();
				SetDone();
			}
		}

		public override int CanRun()
		{
			if (HasEmergency())
			{
				SetLastState("ok");
				return Emergency;
			}

			SetLastState("no orgasm");
			return CannotRun;
		}

		public override bool HasEmergency()
		{
			if (v_.Person.Mood.State == Mood.OrgasmState)
			{
				SetLastState("ok");
				return true;
			}

			return false;
		}

		protected override void DoDebug(DebugLines debug)
		{
		}
	}
}
