using System;
using System.Collections.Generic;
using SimpleJSON;

namespace Cue.MacGruber
{
	public class Dataset
	{
		private string name_;

		public Dataset(string name)
		{
			name_ = name;
		}

		public Dataset(Dataset d)
		{
			name_ = d.name_;
		}

		public string Name
		{
			get { return name_; }
		}
	}

	public class DatasetForIntensity
	{
		public Dataset dataset;
		public float intensityMin;
		public float intensityMax;

		public DatasetForIntensity(Dataset ds, float intensityMin, float intensityMax)
		{
			this.dataset = ds;
			this.intensityMin = intensityMin;
			this.intensityMax = intensityMax;
		}

		public DatasetForIntensity(DatasetForIntensity d)
		{
			dataset = new Dataset(d.dataset);
			intensityMin = d.intensityMin;
			intensityMax = d.intensityMax;
		}
	}


	sealed class Voice : IVoice
	{
		struct Pair
		{
			public Sys.Vam.FloatParameter min, max;

			public Pair(Sys.Vam.FloatParameter min, Sys.Vam.FloatParameter max)
			{
				this.min = min;
				this.max = max;
			}
		}

		struct Parameters
		{
			public Sys.Vam.BoolParameter breathingEnabled;
			public Sys.Vam.BoolParameter driverEnabled;

			public Sys.Vam.StringChooserParameter breathDataset;
			public Sys.Vam.FloatParameter intensity;
			public Sys.Vam.FloatParameter desktopVolume;
			public Sys.Vam.FloatParameter vrVolume;
			public Sys.Vam.FloatParameter volume;

			public Sys.Vam.ActionParameter orgasmAction;
			public Sys.Vam.StringChooserParameter orgasmDataset;

			public Pair chestMorph, chestJointDrive, stomach, mouthMorph;
			public Sys.Vam.FloatParameter chestJointDriveSpring;
			public Sys.Vam.FloatParameter mouthOpenTime, mouthCloseTime;
			public Sys.Vam.FloatParameter lipsMorphMax;
			public Sys.Vam.FloatParameter noseInMorphMax, noseOutMorphMax;
		}


		private Person person_ = null;
		private Logger log_ = new Logger(Logger.Integration, "mg.breather");

		private DatasetForIntensity[] datasets_ = new DatasetForIntensity[0];
		private Dataset orgasmDataset_ = new Dataset("");
		private Dataset dummy_ = new Dataset("");
		private bool warned_ = false;

		private Parameters p_ = new Parameters();
		private bool mouthEnabled_ = true;

		private float oldVolume_ = 0;
		private bool muted_ = false;

		private Voice()
		{
		}

		public Voice(JSONClass options)
		{
			Load(options, false);
		}

		public void Load(JSONClass options, bool inherited)
		{
			if (!options.HasKey("datasets") && !inherited)
				throw new LoadFailed("mg missing datasets");

			var dss = new List<DatasetForIntensity>();

			foreach (JSONClass dn in options["datasets"].AsArray.Childs)
			{
				var ds = new DatasetForIntensity(
					new Dataset(J.ReqString(dn, "dataset")),
					J.ReqFloat(dn, "intensityMin"),
					J.ReqFloat(dn, "intensityMax"));

				dss.Add(ds);
			}

			datasets_ = dss.ToArray();

			if (!options.HasKey("orgasm") && !inherited)
				throw new LoadFailed("mg missing orgasm");

			var od = options["orgasm"].AsObject;

			orgasmDataset_ = new Dataset(J.ReqString(od, "dataset"));
		}

		public IVoice Clone()
		{
			var b = new Voice();
			b.CopyFrom(this);
			return b;
		}

		private void CopyFrom(Voice v)
		{
			datasets_ = new DatasetForIntensity[v.datasets_.Length];
			for (int i = 0; i < datasets_.Length; ++i)
				datasets_[i] = new DatasetForIntensity(v.datasets_[i]);
			orgasmDataset_ = new Dataset(v.orgasmDataset_);
		}

		public void Init(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.Integration, p, "mg.breather");

			p_.breathingEnabled = new Sys.Vam.BoolParameter(
				p, "MacGruber.Breathing", "enabled");

			p_.driverEnabled = new Sys.Vam.BoolParameter(
				p, "MacGruber.DriverBreathing", "enabled");

			p_.breathDataset = BSC("Breath Dataset");
			p_.intensity = BF("Intensity");
			p_.desktopVolume = AAF("Volume Desktop");
			p_.vrVolume = AAF("Volume VR");
			p_.chestMorph = DBPF("ChestMorph Min", "ChestMorph Max");
			p_.chestJointDrive = DBPF("ChestJointDrive Min", "ChestJointDrive Max");
			p_.stomach = DBPF("StomachMin", "StomachMax");
			p_.mouthMorph = DBPF("MouthMorph Min", "MouthMorph Max");
			p_.chestJointDriveSpring = DBF("ChestJointDrive Spring");
			p_.mouthOpenTime = DBF("Mouth Open Time");
			p_.mouthCloseTime = DBF("Mouth Close Time");
			p_.lipsMorphMax = DBF("LipsMorph Max");
			p_.noseInMorphMax = DBF("NoseInMorph Max");
			p_.noseOutMorphMax = DBF("NoseOutMorph Max");
			p_.orgasmAction = BA("QueueOrgasm");
			p_.orgasmDataset = BSC("Orgasm Dataset");
			p_.volume = BF("Volume");

			p_.vrVolume.Value = p_.desktopVolume.Value;

			VamMoan.Voice.Disable(p);
			p_.breathingEnabled.Value = true;
			p_.driverEnabled.Value = true;

			Apply();
		}

		public static void Disable(Person p)
		{
			var e = Sys.Vam.Parameters.GetBool(
				p, "MacGruber.Breathing", "enabled");

			if (e != null)
				e.val = false;

			e  = Sys.Vam.Parameters.GetBool(
				p, "MacGruber.DriverBreathing", "enabled");

			if (e != null)
				e.val = false;
		}

		public void Update(float s)
		{
			if (person_.IsPlayer && Cue.Instance.Options.MutePlayer)
			{
				if (!muted_)
				{
					log_.Info("person is player, muting");
					muted_ = true;
				}
			}
			else
			{
				if (muted_)
				{
					log_.Info("person is not player, unmuting");
					p_.volume.Value = oldVolume_;
					muted_ = false;
				}
			}
		}

		public void SetMoaning(float v)
		{
			p_.intensity.Value = v;
			Apply();
		}

		public void SetBreathing()
		{
			SetMoaning(0);
		}

		public void SetSilent()
		{
			// todo
		}

		public void SetKissing()
		{
			// not supported
			SetMoaning(0.2f);
		}

		public void SetOrgasm()
		{
			var ds = orgasmDataset_;

			if (ds.Name != "")
			{
				if (p_.orgasmDataset.Value != ds.Name)
					p_.orgasmDataset.Value = ds.Name;

				p_.orgasmAction.Fire();
			}
		}

		public void Debug(DebugLines debug)
		{
			debug.Add("provider", "macgruber");
		}

		private Sys.Vam.FloatParameter DBF(string name)
		{
			return new Sys.Vam.FloatParameter(
				person_, "MacGruber.DriverBreathing", name);
		}

		private Pair DBPF(string min, string max)
		{
			return new Pair(
				new Sys.Vam.FloatParameter(
					person_, "MacGruber.DriverBreathing", min),
				new Sys.Vam.FloatParameter(
					person_, "MacGruber.DriverBreathing", max));
		}

		private Sys.Vam.StringChooserParameter BSC(string name)
		{
			return new Sys.Vam.StringChooserParameter(
				person_, "MacGruber.Breathing", name);
		}

		private Sys.Vam.FloatParameter BF(string name)
		{
			return new Sys.Vam.FloatParameter(
				person_, "MacGruber.Breathing", name);
		}

		private Sys.Vam.ActionParameter BA(string name)
		{
			return new Sys.Vam.ActionParameter(
				person_, "MacGruber.Breathing", name);
		}

		private Sys.Vam.FloatParameter AAF(string name)
		{
			return new Sys.Vam.FloatParameter(
				person_, "MacGruber.AudioAttenuation", name);
		}

		public string Name
		{
			get { return "macgruber"; }
		}

		public bool Muted
		{
			set
			{
				if (value)
				{
					oldVolume_ = p_.volume.Value;
					p_.volume.Value = 0;
				}
				else
				{
					p_.volume.Value = oldVolume_;
				}
			}
		}

		public bool MouthEnabled
		{
			get
			{
				return mouthEnabled_;
			}

			set
			{
				mouthEnabled_ = value;

				if (!mouthEnabled_)
				{
					p_.mouthMorph.min.Value = 0;
					p_.mouthMorph.max.Value = 0;
					p_.lipsMorphMax.Value = 0;
				}
			}
		}

		public string Warning
		{
			get { return ""; }
		}

		public void Destroy()
		{
			// no-op
		}

		private Dataset GetDatasetForIntensity(float e)
		{
			for (int i = 0; i < datasets_.Length; ++i)
			{
				if (e >= datasets_[i].intensityMin &&
					e <= datasets_[i].intensityMax)
				{
					return datasets_[i].dataset;
				}
			}

			if (!warned_)
			{
				log_.Error($"missing voice for excitement {e}");
				warned_ = true;
			}

			if (datasets_.Length == 0)
				return dummy_;
			else
				return datasets_[0].dataset;
		}

		private void Apply()
		{
			var f = p_.intensity.Value;
			var ds = GetDatasetForIntensity(f);

			// taken over by MacGruberOrgasmer during orgasm
			if (person_.Mood.State != Mood.OrgasmState)
				p_.breathDataset.Value = ds.Name;

			if (mouthEnabled_)
			{
				var p = p_.mouthMorph.max.Parameter;
				if (p != null)
					p.val = p.defaultVal * f;

				p = p_.lipsMorphMax.Parameter;
				if (p != null)
					p.val = p.defaultVal * f;
			}

			{
				var p = p_.chestMorph.max.Parameter;
				if (p != null)
					p.val = p.defaultVal * f;

				p = p_.stomach.max.Parameter;
				if (p != null)
					p.val = p.defaultVal * f;
			}
		}

		public override string ToString()
		{
			return $"MacGruber: v={p_.intensity}";
		}
	}


	class Gaze : IGazer
	{
		private const int NotFound = 0;
		private const int Target = 1;
		private const int Control = 2;

		private Person person_;
		private Logger log_;
		private float variance_ = 1;

		private Sys.Vam.FloatParameter headRotationSpring_;

		private int version_ = NotFound;
		private Sys.Vam.BoolParameter enabled_;
		public Sys.Vam.StringChooserParameter refAtom_;
		public Sys.Vam.StringChooserParameter refControl_;
		private Sys.Vam.FloatParameter gazeDuration_;
		private Sys.Vam.FloatParameter maxAngleHor_;
		private Sys.Vam.FloatParameter maxAngleVer_;
		private Sys.Vam.FloatParameter verOffset_;
		private Sys.Vam.FloatParameter rollAngleMin_;
		private Sys.Vam.FloatParameter rollAngleMax_;
		private Sys.Vam.FloatParameter rollDurationMin_;
		private Sys.Vam.FloatParameter rollDurationMax_;

		// 12
		private Sys.Vam.BoolParameter lookatTarget_;

		// 13+
		private Sys.Vam.StringChooserParameter lookatAtom_;
		private Sys.Vam.StringChooserParameter lookatControl_;


		public Gaze(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.Integration, p, "mgGaze");

			headRotationSpring_ = new Sys.Vam.FloatParameter(
				p, "headControl", "holdRotationSpring");

			enabled_ = new Sys.Vam.BoolParameter(p, "MacGruber.Gaze", "enabled");
			refAtom_ = new Sys.Vam.StringChooserParameter(p, "MacGruber.Gaze", "Reference Atom");
			refControl_ = new Sys.Vam.StringChooserParameter(p, "MacGruber.Gaze", "Reference Control");
			gazeDuration_ = new Sys.Vam.FloatParameter(p, "MacGruber.Gaze", "Gaze Duration");
			maxAngleHor_ = new Sys.Vam.FloatParameter(p, "MacGruber.Gaze", "Max Angle Horizontal");
			maxAngleVer_ = new Sys.Vam.FloatParameter(p, "MacGruber.Gaze", "Max Angle Vertical");
			verOffset_ = new Sys.Vam.FloatParameter(p, "MacGruber.Gaze", "Eye Angle Vertical Offset");
			rollAngleMin_ = new Sys.Vam.FloatParameter(p, "MacGruber.Gaze", "Roll Angle Min");
			rollAngleMax_ = new Sys.Vam.FloatParameter(p, "MacGruber.Gaze", "Roll Angle Max");
			rollDurationMin_ = new Sys.Vam.FloatParameter(p, "MacGruber.Gaze", "Roll Duration Min");
			rollDurationMax_ = new Sys.Vam.FloatParameter(p, "MacGruber.Gaze", "Roll Duration Max");

			lookatTarget_ = new Sys.Vam.BoolParameter(p, "MacGruber.Gaze", "LookAt EyeTarget");
			lookatAtom_ = new Sys.Vam.StringChooserParameter(p, "MacGruber.Gaze", "LookAt Atom");
			lookatControl_ = new Sys.Vam.StringChooserParameter(p, "MacGruber.Gaze", "LookAt Control");

			enabled_.Value = false;
			refAtom_.Value = p.ID;
			refControl_.Value = "chestControl";
			maxAngleVer_.Value = 70;
			rollDurationMin_.Value = 1;
		}

		public string Name
		{
			get { return "MacGruber's Gaze"; }
		}

		public bool Enabled
		{
			get
			{
				return enabled_.Value;
			}

			set
			{
				if (value != enabled_.Value)
				{
					if (value)
						log_.Verbose("enabling");
					else
						log_.Verbose("disabling");

					enabled_.Value = value;
				}
			}
		}

		public float Duration
		{
			get
			{
				return gazeDuration_.Value;
			}

			set
			{
				gazeDuration_.Value = value;
			}
		}

		public float Variance
		{
			get { return variance_; }
			set { variance_ = value; }
		}

		public void Update(float s)
		{
			if (ParameterChanged())
				SetParameter();

			if (Enabled)
			{
				UpdateVerticalOffset();
				UpdateVariance(s);
			}
		}

		private void UpdateVerticalOffset()
		{
			// gaze has a a problem with the head vertical angle when the
			// camera is really close, making the head point downwards
			//
			// increase the vertical offset when the head is closer than
			// `far`

			var eyes = person_.Body.Get(BP.Eyes).Position;
			var target = person_.Gaze.Eyes.TargetPosition;
			var d = Vector3.Distance(eyes, target);

			float far = 1;
			float def = verOffset_.DefaultValue;

			if (d <= far)
			{
				float max = verOffset_.Maximum;
				float range = (max - def);
				float p = (far - d) / far;
				float v = def + range * p;

				verOffset_.Value = v;
			}
			else
			{
				verOffset_.Value = def;
			}
		}

		private void UpdateVariance(float s)
		{
			maxAngleHor_.SetValueInRange(variance_);
			rollAngleMin_.Value = -15 * variance_;
			rollAngleMax_.Value = 15 * variance_;

			var range = headRotationSpring_.Maximum - headRotationSpring_.DefaultValue;
			var targetSpring = headRotationSpring_.DefaultValue + range * (1 - variance_);

			var current = headRotationSpring_.Value;
			float v;

			if (current < targetSpring)
				v = Math.Min(targetSpring, current + s * 2000);
			else
				v = Math.Max(targetSpring, current - s * 2000);

			headRotationSpring_.Value = v;
		}

		private void SetParameter()
		{
			switch (version_)
			{
				case Target:
				{
					lookatTarget_.Value = true;
					break;
				}

				case Control:
				{
					lookatAtom_.Value = person_.ID;
					lookatControl_.Value = "eyeControlTarget";
					break;
				}
			}
		}

		private bool ParameterChanged()
		{
			switch (version_)
			{
				case NotFound:
				{
					return FindParameter(false);
				}

				case Target:
				{
					if (!lookatTarget_.Check())
					{
						log_.Error("target param gone");
						return FindParameter(true);
					}

					break;
				}

				case Control:
				{
					if (!lookatControl_.Check())
					{
						log_.Error("control param gone");
						return FindParameter(true);
					}

					break;
				}
			}

			return false;
		}

		private bool FindParameter(bool force)
		{
			if (lookatTarget_.Check(force))
			{
				log_.Info("using target (<13)");
				version_ = Target;
				return true;
			}
			else if (lookatControl_.Check(force))
			{
				log_.Info("using control (>12)");
				version_ = Control;
				return true;
			}
			else
			{
				version_ = NotFound;
			}

			return false;
		}

		public override string ToString()
		{
			return $"MacGruber: enabled={enabled_} d={gazeDuration_}";
		}
	}
}
