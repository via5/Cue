using SimpleJSON;

namespace Cue
{
	public class VoiceStateBJ : VoiceStateWithMoaning
	{
		private MouthEvent e_ = null;

		private VoiceStateBJ()
		{
		}

		public VoiceStateBJ(JSONClass vo)
		{
			Load(vo, false);
		}

		public override void Load(JSONClass vo, bool inherited)
		{
			if (vo.HasKey("bjState"))
			{
				var o = J.ReqObject(vo, "bjState");
				LoadWithMoaning(o, inherited);
			}
			else if (!inherited)
			{
				throw new LoadFailed("missing bjState");
			}
		}

		public override string Name
		{
			get { return "bj"; }
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

		protected override bool DoCanRun()
		{
			return BJActive();
		}

		protected override void DoSetSound()
		{
			if (Cue.Instance.Options.BJAudio)
				v_.Provider.SetBJ(v_.MaxIntensity);
			else
				v_.Provider.SetSilent();
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
