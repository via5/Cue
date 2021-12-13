using System;
using System.Collections.Generic;

namespace Cue.Sys.Vam
{
	class VamHair : IHair
	{
		class HairItem
		{
			private HairSimControl c_;
			private JSONStorableFloat styleCling_;
			private JSONStorableFloat rigidityRolloff_;

			public HairItem(VamHair h, HairSimControl c)
			{
				c_ = c;

				styleCling_ = c_.GetFloatJSONParam("cling");
				if (styleCling_ == null)
					h.Log.Info("cling not found");

				rigidityRolloff_ = c_.GetFloatJSONParam("rigidityRolloffPower");
				if (rigidityRolloff_ == null)
					h.Log.Info("rigidityRolloffPower not found");
			}

			public void Reset()
			{
				if (styleCling_ != null)
					styleCling_.val = styleCling_.defaultVal;

				if (rigidityRolloff_ != null)
					rigidityRolloff_.val = rigidityRolloff_.defaultVal;
			}

			public void SetLoose(float f)
			{
				// both these values are expensive to change; cling is [0, 1]
				// and rolloff is [0, 16], so only change them if they're
				// different enough to make a difference
				//
				// when a value changes, the hair joints are rebuilt, which
				// takes time; it'd be nice to be able to set both and rebuild
				// once, but vam doesn't support that
				const float ClingTreshold = 0.02f;
				const float RolloffTreshold = 0.1f;

				if (styleCling_ != null)
				{
					float min = 0.01f;
					float max = styleCling_.defaultVal;

					if (min < max)
					{
						float range = max - min;
						float nv = max - (range * f);

						if (Math.Abs(styleCling_.val - nv) > ClingTreshold)
							styleCling_.val = nv;
					}
				}

				if (rigidityRolloff_ != null)
				{
					float min = rigidityRolloff_.defaultVal;
					float max = rigidityRolloff_.max;

					if (min < max)
					{
						float range = Math.Min(max - min, 4);
						float nv = min + (range * f);

						if (Math.Abs(rigidityRolloff_.val - nv) > RolloffTreshold)
							rigidityRolloff_.val = nv;
					}
				}
			}
		}


		private VamAtom atom_;
		private DAZCharacterSelector char_;
		private float loose_ = 0;
		private bool enabled_ = false;
		private List<HairItem> list_ = new List<HairItem>();
		private Logger log_;

		public VamHair(VamAtom a)
		{
			atom_ = a;

			if (atom_ == null)
			{
				log_ = new Logger(Logger.Sys, "vamHair");
				return;
			}
			else
			{
				log_ = new Logger(Logger.Sys, atom_, "vamHair");
			}

			char_ = atom_.Atom.GetComponentInChildren<DAZCharacterSelector>();
			if (char_ == null)
				Log.Error("no DAZCharacterSelector for hair");

			foreach (var g in char_.hairItems)
			{
				if (!g.isActiveAndEnabled)
					continue;

				var h = g.GetComponentInChildren<HairSimControl>();
				if (h != null)
					list_.Add(new HairItem(this, h));
			}

			Cue.Instance.Options.Changed += CheckOptions;
			CheckOptions();
		}

		public Logger Log
		{
			get { return log_; }
		}

		public void OnPluginState(bool b)
		{
			if (!b)
				Reset();
		}

		public float Loose
		{
			get
			{
				return loose_;
			}

			set
			{
				if (loose_ != value)
				{
					loose_ = value;
					SetLoose(value);
				}
			}
		}

		private void SetLoose(float v)
		{
			if (Cue.Instance.Options.HairLoose)
			{
				for (int i = 0; i < list_.Count; ++i)
					list_[i].SetLoose(v);
			}
		}

		public void Update(float s)
		{
		}

		private void Reset()
		{
			if (Cue.Instance.Options.HairLoose)
			{
				for (int i = 0; i < list_.Count; ++i)
					list_[i].Reset();
			}
		}

		private void CheckOptions()
		{
			if (enabled_ != Cue.Instance.Options.HairLoose)
			{
				enabled_ = Cue.Instance.Options.HairLoose;

				if (Cue.Instance.Options.HairLoose)
				{
					SetLoose(loose_);
				}
				else
				{
					for (int i = 0; i < list_.Count; ++i)
						list_[i].Reset();
				}
			}
		}
	}
}
