using SimpleJSON;
using System;
using System.Collections.Generic;

namespace Cue.VamMoan
{
	public class Breather : IBreather
	{
		private const string PluginName = "VAMMoanPlugin.VAMMoan";
		private const float DefaultBreathingMax = 0.2f;

		struct Parameters
		{
			public Sys.Vam.BoolParameter autoJaw;
			public Sys.Vam.StringChooserParameter voice;
			public Sys.Vam.ActionParameter breathing;
			public Sys.Vam.ActionParameter[] intensities;
			public Sys.Vam.FloatParameter availableIntensities;
			public bool hasAvailableIntensities;
		}

		private Person person_;
		private Logger log_;
		private float intensity_ = 0;
		private int lastIntensityIndex_ = -1;
		private string last_ = "";
		private int intensitiesCount_ = -1;
		private float forcedPitch_ = -1;
		private Parameters p_;

		private float breathingMax_ = DefaultBreathingMax;
		private IEasing intensitiesEasing_ = new LinearEasing();


		private Breather()
		{
		}

		public Breather(JSONClass o)
		{
			breathingMax_ = U.Clamp(
				J.OptFloat(o, "breathingMax", DefaultBreathingMax),
				0, 1);

			if (o.HasKey("intensitiesEasing"))
			{
				var en = o["intensitiesEasing"].Value;
				if (en != "")
				{
					var e = EasingFactory.FromString(en);
					if (e == null)
						log_.Error($"bad intensitiesEasing '{en}'");
					else
						intensitiesEasing_ = e;
				}
			}
		}

		public IBreather Clone()
		{
			var b = new Breather();
			b.CopyFrom(this);
			return b;
		}

		private void CopyFrom(Breather b)
		{
			breathingMax_ = b.breathingMax_;
		}

		public void Init(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.Integration, p, "vammoan");

			p_.autoJaw = BP("Enable auto-jaw animation");
			p_.voice = SCP("voice");
			p_.intensities = GetIntensities();
			p_.breathing = AP("Voice breathing");
			p_.availableIntensities = FP("VAMM IntensitiesCount");

			CheckVersion();
			SetVoice("Lia");
		}

		private void CheckVersion()
		{
			if (p_.voice.Check() && !p_.availableIntensities.Check())
			{
				log_.Error($"Cue requires VAMMoan 11 or above");
				p_.hasAvailableIntensities = false;
			}
			else
			{
				p_.hasAvailableIntensities = true;
			}
		}

		private Sys.Vam.ActionParameter[] GetIntensities()
		{
			var actions = new List<Sys.Vam.ActionParameter>();

			for (int i = 0; i < 5; ++i)
				actions.Add(AP($"Voice intensity {i}"));

			return actions.ToArray();
		}

		private Sys.Vam.BoolParameter BP(string name)
		{
			return new Sys.Vam.BoolParameter(person_, PluginName, name);
		}

		private Sys.Vam.StringChooserParameter SCP(string name)
		{
			return new Sys.Vam.StringChooserParameter(person_, PluginName, name);
		}

		private Sys.Vam.ActionParameter AP(string name)
		{
			return new Sys.Vam.ActionParameter(person_, PluginName, name);
		}

		private Sys.Vam.FloatParameter FP(string name)
		{
			return new Sys.Vam.FloatParameter(person_, PluginName, name);
		}

		private void SetVoice(string name)
		{
			p_.voice.Value = name;

			if (p_.hasAvailableIntensities)
			{
				float c = p_.availableIntensities.Value;
				intensitiesCount_ = U.Clamp((int)c, 0, p_.intensities.Length);
			}
			else
			{
				intensitiesCount_ = p_.intensities.Length;
			}
		}

		public void Destroy()
		{
			// no-op
		}

		public float ForcedPitch
		{
			get { return forcedPitch_; }
		}

		public void ForcePitch(float f)
		{
			// todo
		}

		public bool MouthEnabled
		{
			get { return p_.autoJaw.Value; }
			set { p_.autoJaw.Value = value; }
		}

		public float Intensity
		{
			get
			{
				return intensity_;
			}

			set
			{
				intensity_ = value;
				SetIntensity(intensity_);
			}
		}

		private void SetIntensity(float i)
		{
			if (i >= 0 && i <= breathingMax_)
			{
				log_.Info($"setting to breathing");
				last_ = "breathing";
				p_.breathing.Fire();
			}
			else
			{
				float range = 1 - breathingMax_;
				float v = i - breathingMax_;
				float p = intensitiesEasing_.Magnitude(v / range);

				int index = (int)(p * intensitiesCount_);
				index = U.Clamp(index, 0, intensitiesCount_ - 1);

				if (index != lastIntensityIndex_)
				{
					log_.Info($"setting to intensity {p_.intensities[index].ParameterName}");
					last_ = $"{index}/{intensitiesCount_ - 1}";
					p_.intensities[index].Fire();
					lastIntensityIndex_ = index;
				}
			}
		}

		public override string ToString()
		{
			return
				$"VAMMoan " +
				$"i={last_} " +
				$"bm={breathingMax_}";
		}
	}
}
