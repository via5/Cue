using SimpleJSON;
using System.Collections.Generic;

namespace Cue
{
	public class VAMMoanBreather : IBreather
	{
		private Person person_;
		private Logger log_;
		private float intensity_ = 0;
		private int lastIntensityIndex_ = -1;
		private float forcedPitch_ = -1;

		private Sys.Vam.BoolParameter autoJaw_;
		private Sys.Vam.StringChooserParameter voice_;
		private Sys.Vam.ActionParameter[] intensities_;


		public VAMMoanBreather(JSONClass options)
		{
		}

		public IBreather Clone()
		{
			throw new System.NotImplementedException();
		}

		public void Init(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.Integration, p, "vammoan");

			autoJaw_ = new Sys.Vam.BoolParameter(
				p, "VAMMoanPlugin.VAMMoan", "Enable auto-jaw animation");

			voice_ = new Sys.Vam.StringChooserParameter(
				p, "VAMMoanPlugin.VAMMoan", "voice");


			var actions = new List<Sys.Vam.ActionParameter>();

			actions.Add(new Sys.Vam.ActionParameter(
				p, "VAMMoanPlugin.VAMMoan", $"Voice breathing"));

			for (int i = 0; i < 5; ++i)
			{
				actions.Add(new Sys.Vam.ActionParameter(
					p, "VAMMoanPlugin.VAMMoan", $"Voice intensity {i}"));
			}

			intensities_ = actions.ToArray();

			voice_.Value = "Abby";
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
			get { return autoJaw_.Value; }
			set { autoJaw_.Value = value; }
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

				int i = (int)(intensity_ * intensities_.Length);
				i = U.Clamp(i, 0, intensities_.Length - 1);

				if (i != lastIntensityIndex_)
				{
					log_.Info($"setting intensity to {intensities_[i].ParameterName}");
					intensities_[i].Fire();
					lastIntensityIndex_ = i;
				}
			}
		}
	}
}
