using SimpleJSON;

namespace Cue
{
	public class VoiceStateOrgasm : VoiceState
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

		public override void Load(JSONClass vo, bool inherited)
		{
			if (vo.HasKey("orgasmState"))
			{
				var o = vo["orgasmState"].AsObject;

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
			else if (!inherited)
			{
				throw new LoadFailed("missing orgasmState");
			}
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
			debug.Add("action", (action_ == OrgasmAction ? "orgasm" : "moaning"));
			debug.Add("moaning", $"{moaningIntensity_:0.00}");
		}
	}
}
