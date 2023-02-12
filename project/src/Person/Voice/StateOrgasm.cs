using SimpleJSON;

namespace Cue
{
	public class VoiceStateOrgasm : BasicVoiceState
	{
		private const int OrgasmAction = 1;
		private const int MoaningAction = 2;

		private int action_ = OrgasmAction;
		private float moaningIntensity_ = 0;

		private VoiceStateOrgasm()
		{
		}

		public VoiceStateOrgasm(JSONClass vo)
		{
			Load(vo, false);
		}

		protected override void DoLoad(JSONClass o, bool inherited)
		{
			string v = J.OptString(o, "voice", "");
			if (v == "orgasm")
			{
				action_ = OrgasmAction;
			}
			else if (v == "moaning")
			{
				action_ = MoaningAction;

				if (o.HasKey("moaningIntensity"))
					moaningIntensity_ = J.ReqFloat(o, "moaningIntensity");
				else if (!inherited)
					throw new LoadFailed("missing moaningIntensity");
			}
			else if (v != "")
			{
				throw new LoadFailed("bad orgasmState voice, must be 'orgasm' or 'moaning'");
			}
		}

		public override string Name
		{
			get { return "orgasmState"; }
		}

		public override IVoiceState Clone()
		{
			var s = new VoiceStateOrgasm();
			s.CopyFrom(this);
			return s;
		}

		private void CopyFrom(VoiceStateOrgasm o)
		{
			base.CopyFrom(o);
			action_ = o.action_;
			moaningIntensity_ = o.moaningIntensity_;
		}

		protected override void DoStart()
		{
			if (action_ == OrgasmAction)
				v_.Provider.SetOrgasm();
			else if (action_ == MoaningAction)
				v_.Provider.SetMoaning(moaningIntensity_);
		}

		protected override void DoUpdate(float s)
		{
			if (v_.Person.Mood.State != Mood.OrgasmState)
				SetDone();
		}

		protected override int DoCanRun()
		{
			if (v_.Person.Mood.State == Mood.OrgasmState)
			{
				SetLastState("ok");
				return Emergency;
			}

			SetLastState("orgasm not active");
			return CannotRun;
		}

		protected override void DoDebug(DebugLines debug)
		{
			debug.Add("action", (action_ == OrgasmAction ? "orgasm" : "moaning"));
			debug.Add("moaning", $"{moaningIntensity_:0.00}");
		}
	}
}
