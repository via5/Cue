using SimpleJSON;

namespace Cue
{
	public class VoiceStateKiss : VoiceStateWithMoaning
	{
		private KissEvent e_ = null;
		private bool leading_ = false;
		private bool audio_ = true;

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

		protected override int DoCanRun()
		{
			if (IsKissing())
			{
				SetLastState("ok");
				return HighPriority;
			}

			SetLastState("kissing not active");
			return CannotRun;
		}

		protected override void DoSetSound()
		{
			audio_ = Cue.Instance.Options.KissAudio;

			if (audio_)
			{
				if (leading_)
					v_.Provider.SetKissing();
				else
					v_.Provider.SetSilent();
			}
			else
			{
				v_.Provider.SetSilent();
			}
		}

		protected override void DoUpdate(float s)
		{
			base.DoUpdate(s);

			if (Cue.Instance.Options.KissAudio != audio_)
				DoSetSound();
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
