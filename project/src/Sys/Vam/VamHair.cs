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

			public HairItem(HairSimControl c)
			{
				c_ = c;

				styleCling_ = c_.GetFloatJSONParam("cling");
				if (styleCling_ == null)
					Cue.LogInfo("cling not found");

				rigidityRolloff_ = c_.GetFloatJSONParam("rigidityRolloffPower");
				if (rigidityRolloff_ == null)
					Cue.LogInfo("rigidityRolloffPower not found");
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
		private List<HairItem> list_ = new List<HairItem>();

		public VamHair(VamAtom a)
		{
			atom_ = a;
			if (atom_ == null)
				return;

			char_ = atom_.Atom.GetComponentInChildren<DAZCharacterSelector>();
			if (char_ == null)
				atom_.Log.Error("no DAZCharacterSelector for hair");

			foreach (var g in char_.hairItems)
			{
				if (!g.isActiveAndEnabled)
					continue;

				var h = g.GetComponentInChildren<HairSimControl>();
				if (h != null)
					list_.Add(new HairItem(h));
			}
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
				loose_ = value;
				for (int i = 0; i < list_.Count; ++i)
					list_[i].SetLoose(loose_);
			}
		}

		public void Update(float s)
		{
		}

		private void Reset()
		{
			for (int i = 0; i < list_.Count; ++i)
				list_[i].Reset();
		}
	}
}
