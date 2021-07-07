using SimpleJSON;

namespace Cue
{
	class Sensitivity
	{
		private Person person_;

		public Sensitivity(Person p)
		{
			person_ = p;
		}

		public float MouthRate { get { return 0.1f; } }
		public float MouthMax { get { return 0.3f; } }

		public float BreastsRate { get { return 0.01f; } }
		public float BreastsMax { get { return 0.4f; } }

		public float GenitalsRate { get { return 0.06f; } }
		public float GenitalsMax { get { return 0.8f; } }

		public float PenetrationRate { get { return 0.05f; } }
		public float PenetrationMax { get { return 1.0f; } }

		public float DecayPerSecond { get { return -0.01f; } }
		public float ExcitementPostOrgasm { get { return 0.4f; } }
		public float OrgasmTime { get { return 8; } }
		public float PostOrgasmTime { get { return 10; } }
		public float RateAdjustment { get { return 0.3f; } }
	}


	class Physiology
	{
		private Person person_;
		private Sensitivity sensitivity_;
		private float pitch_ = 0.5f;

		public Physiology(Person p, JSONClass config)
		{
			person_ = p;
			sensitivity_ = new Sensitivity(p);

			if (config.HasKey("physiology"))
			{
				string vp = config["physiology"]?["voicePitch"]?.Value ?? "";

				if (vp != "" && !float.TryParse(vp, out pitch_))
					person_.Log.Error($"bad voice pitch '{vp}'");
			}
		}

		public Sensitivity Sensitivity
		{
			get { return sensitivity_; }
		}

		public float MaxSweat
		{
			get { return 1; }
		}

		public float MaxFlush
		{
			get { return 1; }
		}

		public float TemperatureExcitementRate
		{
			get { return 0.01f; }
		}

		public float TemperatureDecayRate
		{
			get { return 0.005f; }
		}

		// excitement at which temperature is at max
		//
		public float TemperatureExcitementMax
		{
			get { return 0.8f; }
		}

		public float TirednessRateDuringPostOrgasm
		{
			get { return 0.01f; }
		}

		public float TirednessMaxExcitementDecay
		{
			get { return 0.2f; }
		}

		public float TirednessExcitementRate
		{
			get { return 0.001f; }
		}

		public float TirednessDecayRate
		{
			get { return 0.01f; }
		}

		public float DelayAfterOrgasmUntilTirednessDecay
		{
			get { return 10; }
		}

		public float VoicePitch
		{
			get { return pitch_; }
		}

		// todo
		//
		public string Voice
		{
			get { return "Original"; }
		}
	}
}
