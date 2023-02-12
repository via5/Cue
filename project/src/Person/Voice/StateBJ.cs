using SimpleJSON;

namespace Cue
{
	public class VoiceStateBJ : VoiceStateWithMoaning
	{
		private MouthEvent e_ = null;
		private bool audio_ = true;

		private VoiceStateBJ()
		{
		}

		public VoiceStateBJ(JSONClass vo)
		{
			Load(vo, false);
		}

		public override string Name
		{
			get { return "bjState"; }
		}

		public override IVoiceState Clone()
		{
			var s = new VoiceStateBJ();
			s.CopyFrom(this);
			return s;
		}

		protected override void DoStart()
		{
			DoSetSound();
		}

		protected override int DoCanRun()
		{
			if (BJActive())
			{
				SetLastState("ok");
				return HighPriority;
			}

			SetLastState("bj not active");
			return CannotRun;
		}

		protected override void DoSetSound()
		{
			audio_ = Cue.Instance.Options.BJAudio;

			if (audio_)
				v_.Provider.SetBJ(v_.MaxIntensity);
			else
				v_.Provider.SetBreathing();
		}

		protected override void DoUpdate(float s)
		{
			base.DoUpdate(s);

			if (Cue.Instance.Options.BJAudio != audio_)
				DoSetSound();
		}

		private bool BJActive()
		{
			if (e_ == null)
				e_ = v_.Person.AI.GetEvent<MouthEvent>();

			return e_.Active;
		}

		protected override void DoDebug(DebugLines debug)
		{
			base.DoDebug(debug);
		}
	}
}
