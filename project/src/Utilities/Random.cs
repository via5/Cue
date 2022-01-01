using System;
using SimpleJSON;

namespace Cue
{
	interface IRandom
	{
		IRandom Clone();
		float RandomFloat(float first, float last, float magnitude);
	}


	abstract class BasicRandom : IRandom
	{
		public static IRandom FromJSON(JSONClass o)
		{
			if (!o.HasKey("type"))
				throw new LoadFailed("missing 'type'");

			var type = o["type"].Value;

			if (type == "uniform")
				return UniformRandom.FromJSON(o);
			else if (type == "normal")
				return NormalRandom.FromJSON(o);
			else
				throw new LoadFailed($"unknown type '{type}'");
		}

		public abstract IRandom Clone();
		public abstract float RandomFloat(float first, float last, float magnitude);

		// [first, last]
		//
		public static int RandomInt(int first, int last)
		{
			return Cue.Instance.Sys.RandomInt(first, last);
		}

		// [first, last]
		//
		public static float RandomFloat(float first, float last)
		{
			return Cue.Instance.Sys.RandomFloat(first, last);
		}

		// inclusive
		//
		public static float RandomFloat(Pair<float, float> p)
		{
			return RandomFloat(p.first, p.second);
		}

		public static float RandomNormal(
			float first, float last, float center = 0.5f, float width = 1.0f)
		{
			float u, v, S;
			int tries = 0;

			do
			{
				u = 2.0f * RandomFloat(0, 1) - 1.0f;
				v = 2.0f * RandomFloat(0, 1) - 1.0f;
				S = u * u + v * v;

				++tries;
				if (tries > 20)
					return RandomFloat(first, last);
			}
			while (S >= 1.0f);

			// Standard Normal Distribution
			float std = (float)(u * Math.Sqrt(-2.0f * Math.Log(S) / S));

			// Normal Distribution centered between the min and max value
			// and clamped following the "three-sigma rule"
			float mean = (first + last) * center;
			float sigma = (last - mean) / (3.0f / width);

			return U.Clamp(std * sigma + mean, first, last);
		}

		public static bool RandomBool()
		{
			return RandomBool(0.5f);
		}

		public static bool RandomBool(float trueChance)
		{
			return (RandomFloat(0, 1) <= trueChance);
		}

	}


	class UniformRandom : BasicRandom
	{
		public new static UniformRandom FromJSON(JSONClass o)
		{
			return new UniformRandom();
		}

		public override IRandom Clone()
		{
			return new UniformRandom();
		}

		public override float RandomFloat(float first, float last, float magnitude)
		{
			return U.RandomFloat(first, last);
		}

		public override string ToString()
		{
			return "uniform";
		}
	}


	class NormalRandom : BasicRandom
	{
		private readonly float centerMin_;
		private readonly float centerMax_;
		private readonly float widthMin_;
		private readonly float widthMax_;

		public NormalRandom(float centerMin, float centerMax, float widthMin, float widthMax)
		{
			centerMin_ = centerMin;
			centerMax_ = centerMax;
			widthMin_ = widthMin;
			widthMax_ = widthMax;
		}

		public override string ToString()
		{
			return $"normal:c=({centerMin_},{centerMax_}),w=({widthMin_},{widthMax_})";
		}

		public new static NormalRandom FromJSON(JSONClass o)
		{
			float centerMin = 0.5f;
			float centerMax = 0.5f;

			if (o.HasKey("center"))
			{
				float c;
				if (!float.TryParse(o["center"].Value, out c))
					throw new LoadFailed($"center is not a float");

				centerMin = c;
				centerMax = c;
			}
			else
			{
				if (o.HasKey("centerMin"))
				{
					if (!float.TryParse(o["centerMin"].Value, out centerMin))
						throw new LoadFailed($"centerMin is not a float");
				}

				if (o.HasKey("centerMax"))
				{
					if (!float.TryParse(o["centerMax"].Value, out centerMax))
						throw new LoadFailed($"centerMax is not a float");
				}
			}

			float widthMin = 1.0f;
			float widthMax = 1.0f;

			if (o.HasKey("width"))
			{
				float w;
				if (!float.TryParse(o["width"].Value, out w))
					throw new LoadFailed($"width is not a float");

				widthMin = w;
				widthMax = w;
			}
			else
			{
				if (o.HasKey("widthMin"))
				{
					if (!float.TryParse(o["widthMin"].Value, out widthMin))
						throw new LoadFailed($"widthMin is not a float");
				}

				if (o.HasKey("widthMax"))
				{
					if (!float.TryParse(o["widthMax"].Value, out widthMax))
						throw new LoadFailed($"widthMax is not a float");
				}
			}

			return new NormalRandom(centerMin, centerMax, widthMin, widthMax);
		}

		public override IRandom Clone()
		{
			return new NormalRandom(centerMin_, centerMax_, widthMin_, widthMax_);
		}

		public override float RandomFloat(float first, float last, float magnitude)
		{
			float center;

			if (centerMin_ < centerMax_)
				center = centerMin_ + (centerMax_ - centerMin_) * magnitude;
			else
				center = centerMax_ + (centerMin_ - centerMax_) * magnitude;

			float width;

			if (widthMin_ < widthMax_)
				width = widthMin_ + (widthMax_ - widthMin_) * magnitude;
			else
				width = widthMax_ + (widthMin_ - widthMax_) * magnitude;

			return U.RandomNormal(first, last, center, width);
		}
	}
}
