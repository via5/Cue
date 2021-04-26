using System.Collections.Generic;
using static System.Math;

// auto generated from Easings.tt

namespace Cue
{
	public interface IEasing
	{
		IEasing Clone(int cloneFlags = 0);
		float Magnitude(float f);
	}


	public abstract class BasicEasing : IEasing
	{
		public abstract float Magnitude(float f);
		public abstract IEasing Clone(int cloneFlags = 0);

		protected float BounceOut(float x)
		{
			float n1 = 7.5625f;
			float d1 = 2.75f;

			if (x < 1 / d1) {
				return n1 * x * x;
			} else if (x < 2 / d1) {
				return n1 * (x -= 1.5f / d1) * x + 0.75f;
			} else if (x < 2.5 / d1) {
				return n1 * (x -= 2.25f / d1) * x + 0.9375f;
			} else {
				return n1 * (x -= 2.625f / d1) * x + 0.984375f;
			}
		}
	}


	public class LinearEasing : BasicEasing
	{
		public static string DisplayName
		{
			get
			{
				return "Linear";
			}
		}

		public string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new LinearEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x);
		}
	}

	public class SinusoidalEasing : BasicEasing
	{
		public static string DisplayName
		{
			get
			{
				return "Sinusoidal";
			}
		}

		public string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new SinusoidalEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(-(Cos(PI * x) - 1) / 2);
		}
	}

	public class QuadInEasing : BasicEasing
	{
		public static string DisplayName
		{
			get
			{
				return "Quad in";
			}
		}

		public string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new QuadInEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x * x);
		}
	}

	public class QuadOutEasing : BasicEasing
	{
		public static string DisplayName
		{
			get
			{
				return "Quad out";
			}
		}

		public string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new QuadOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(1 - (1 - x) * (1 - x));
		}
	}

	public class QuadInOutEasing : BasicEasing
	{
		public static string DisplayName
		{
			get
			{
				return "Quad in/out";
			}
		}

		public string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new QuadInOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x < 0.5 ? 2 * x * x : 1 - Pow(-2 * x + 2, 2) / 2);
		}
	}

	public class CubicInEasing : BasicEasing
	{
		public static string DisplayName
		{
			get
			{
				return "Cubic in";
			}
		}

		public string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new CubicInEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x * x * x);
		}
	}

	public class CubicOutEasing : BasicEasing
	{
		public static string DisplayName
		{
			get
			{
				return "Cubic out";
			}
		}

		public string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new CubicOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(1 - Pow(1 - x, 3));
		}
	}

	public class CubicInOutEasing : BasicEasing
	{
		public static string DisplayName
		{
			get
			{
				return "Cubic in/out";
			}
		}

		public string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new CubicInOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x < 0.5 ? 4 * x * x * x : 1 - Pow(-2 * x + 2, 3) / 2);
		}
	}

	public class QuartInEasing : BasicEasing
	{
		public static string DisplayName
		{
			get
			{
				return "Quart in";
			}
		}

		public string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new QuartInEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x * x * x * x);
		}
	}

	public class QuartOutEasing : BasicEasing
	{
		public static string DisplayName
		{
			get
			{
				return "Quart out";
			}
		}

		public string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new QuartOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(1 - Pow(1 - x, 4));
		}
	}

	public class QuartInOutEasing : BasicEasing
	{
		public static string DisplayName
		{
			get
			{
				return "Quart in/out";
			}
		}

		public string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new QuartInOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x < 0.5 ? 8 * x * x * x * x : 1 - Pow(-2 * x + 2, 4) / 2);
		}
	}

	public class QuintInEasing : BasicEasing
	{
		public static string DisplayName
		{
			get
			{
				return "Quint in";
			}
		}

		public string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new QuintInEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x * x * x * x * x);
		}
	}

	public class QuintOutEasing : BasicEasing
	{
		public static string DisplayName
		{
			get
			{
				return "Quint out";
			}
		}

		public string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new QuintOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(1 - Pow(1 - x, 5));
		}
	}

	public class QuintInOutEasing : BasicEasing
	{
		public static string DisplayName
		{
			get
			{
				return "Quint in/out";
			}
		}

		public string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new QuintInOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x < 0.5 ? 16 * x * x * x * x * x : 1 - Pow(-2 * x + 2, 5) / 2);
		}
	}

	public class SineInEasing : BasicEasing
	{
		public static string DisplayName
		{
			get
			{
				return "Sine in";
			}
		}

		public string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new SineInEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(1 - Cos((x * PI) / 2));
		}
	}

	public class SineOutEasing : BasicEasing
	{
		public static string DisplayName
		{
			get
			{
				return "Sine out";
			}
		}

		public string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new SineOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(Sin((x * PI) / 2));
		}
	}

	public class SineInOutEasing : BasicEasing
	{
		public static string DisplayName
		{
			get
			{
				return "Sine in/out";
			}
		}

		public string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new SineInOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(-(Cos(PI * x) - 1) / 2);
		}
	}

	public class ExpoInEasing : BasicEasing
	{
		public static string DisplayName
		{
			get
			{
				return "Expo in";
			}
		}

		public string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new ExpoInEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x == 0 ? 0 : Pow(2, 10 * x - 10));
		}
	}

	public class ExpoOutEasing : BasicEasing
	{
		public static string DisplayName
		{
			get
			{
				return "Expo out";
			}
		}

		public string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new ExpoOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x == 1 ? 1 : 1 - Pow(2, -10 * x));
		}
	}

	public class ExpoInOutEasing : BasicEasing
	{
		public static string DisplayName
		{
			get
			{
				return "Expo in/out";
			}
		}

		public string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new ExpoInOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x == 0 ? 0 : x == 1 ? 1 : x < 0.5 ? Pow(2, 20 * x - 10) / 2 : (2 - Pow(2, -20 * x + 10)) / 2);
		}
	}

	public class CircInEasing : BasicEasing
	{
		public static string DisplayName
		{
			get
			{
				return "Circ in";
			}
		}

		public string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new CircInEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(1 - Sqrt(1 - Pow(x, 2)));
		}
	}

	public class CircOutEasing : BasicEasing
	{
		public static string DisplayName
		{
			get
			{
				return "Circ out";
			}
		}

		public string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new CircOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(Sqrt(1 - Pow(x - 1, 2)));
		}
	}

	public class CircInOutEasing : BasicEasing
	{
		public static string DisplayName
		{
			get
			{
				return "Circ in/out";
			}
		}

		public string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new CircInOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x < 0.5 ? (1 - Sqrt(1 - Pow(2 * x, 2))) / 2 : (Sqrt(1 - Pow(-2 * x + 2, 2)) + 1) / 2);
		}
	}

	public class BackInEasing : BasicEasing
	{
		public static string DisplayName
		{
			get
			{
				return "Back in";
			}
		}

		public string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new BackInEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(2.70158f * x * x * x - 1.70158f * x * x);
		}
	}

	public class BackOutEasing : BasicEasing
	{
		public static string DisplayName
		{
			get
			{
				return "Back out";
			}
		}

		public string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new BackOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(1 + 2.70158f * Pow(x - 1, 3) + 1.70158f * Pow(x - 1, 2));
		}
	}

	public class BackInOutEasing : BasicEasing
	{
		public static string DisplayName
		{
			get
			{
				return "Back in/out";
			}
		}

		public string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new BackInOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x < 0.5 ? (Pow(2 * x, 2) * ((2.5949f + 1) * 2 * x - 2.5949f)) / 2 : (Pow(2 * x - 2, 2) * ((2.5949f + 1) * (x * 2 - 2) + 2.5949f) + 2) / 2);
		}
	}

	public class ElasticInEasing : BasicEasing
	{
		public static string DisplayName
		{
			get
			{
				return "Elastic in";
			}
		}

		public string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new ElasticInEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x == 0 ? 0 : x == 1 ? 1 : -Pow(2, 10 * x - 10) * Sin((x * 10 - 10.75) * 2.0944f));
		}
	}

	public class ElasticOutEasing : BasicEasing
	{
		public static string DisplayName
		{
			get
			{
				return "Elastic out";
			}
		}

		public string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new ElasticOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x == 0 ? 0 : x == 1 ? 1 : Pow(2, -10 * x) * Sin((x * 10 - 0.75) * 2.0944f) + 1);
		}
	}

	public class ElasticInOutEasing : BasicEasing
	{
		public static string DisplayName
		{
			get
			{
				return "Elastic in/out";
			}
		}

		public string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new ElasticInOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x == 0 ? 0 : x == 1 ? 1 : x < 0.5 ? -(Pow(2, 20 * x - 10) * Sin((20 * x - 11.125) * 1.3963f)) / 2 : (Pow(2, -20 * x + 10) * Sin((20 * x - 11.125) * 1.3963f)) / 2 + 1);
		}
	}

	public class BounceInEasing : BasicEasing
	{
		public static string DisplayName
		{
			get
			{
				return "Bounce in";
			}
		}

		public string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new BounceInEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(1 - BounceOut(1 - x));
		}
	}

	public class BounceOutEasing : BasicEasing
	{
		public static string DisplayName
		{
			get
			{
				return "Bounce out";
			}
		}

		public string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new BounceOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(BounceOut(x));
		}
	}

	public class BounceInOutEasing : BasicEasing
	{
		public static string DisplayName
		{
			get
			{
				return "Bounce in/out";
			}
		}

		public string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new BounceInOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x < 0.5 ? (1 - BounceOut(1 - 2 * x)) / 2 : (1 + BounceOut(2 * x - 1)) / 2);
		}
	}
}

