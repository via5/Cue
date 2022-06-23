using SimpleJSON;

namespace Cue
{
	public class VoiceStateKiss : VoiceStateWithMoaning
	{
		private KissEvent e_ = null;
		private bool leading_ = false;

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
				LoadWithMoaning(o, inherited);
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

		protected override void DoStart()
		{
			leading_ = e_.Leading;
			DoSetSound();
		}

		protected override bool DoCanRun()
		{
			return IsKissing();
		}

		protected override void DoSetSound()
		{
			if (leading_)
				v_.Provider.SetKissing();
			else
				v_.Provider.SetSilent();
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

			base.DoDebug(debug);
		}
	}
}
