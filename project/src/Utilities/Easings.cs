using System;
using System.Collections.Generic;
using static System.Math;

// auto generated from Easings.tt

namespace Cue
{
	using EasingMap = Dictionary<string, Func<IEasing>>;

	public class EasingFactory
	{
		private static EasingMap map_ = new EasingMap()
		{
			{ "constantzero", () => new ConstantZeroEasing() },
			{ "constantone", () => new ConstantOneEasing() },
			{ "linear", () => new LinearEasing() },
			{ "sinusoidal", () => new SinusoidalEasing() },
			{ "quadin", () => new QuadInEasing() },
			{ "quadout", () => new QuadOutEasing() },
			{ "quadinout", () => new QuadInOutEasing() },
			{ "cubicin", () => new CubicInEasing() },
			{ "cubicout", () => new CubicOutEasing() },
			{ "cubicinout", () => new CubicInOutEasing() },
			{ "quartin", () => new QuartInEasing() },
			{ "quartout", () => new QuartOutEasing() },
			{ "quartinout", () => new QuartInOutEasing() },
			{ "quintin", () => new QuintInEasing() },
			{ "quintout", () => new QuintOutEasing() },
			{ "quintinout", () => new QuintInOutEasing() },
			{ "sinein", () => new SineInEasing() },
			{ "sineout", () => new SineOutEasing() },
			{ "sineinout", () => new SineInOutEasing() },
			{ "expoin", () => new ExpoInEasing() },
			{ "expoout", () => new ExpoOutEasing() },
			{ "expoinout", () => new ExpoInOutEasing() },
			{ "circin", () => new CircInEasing() },
			{ "circout", () => new CircOutEasing() },
			{ "circinout", () => new CircInOutEasing() },
			{ "backin", () => new BackInEasing() },
			{ "backout", () => new BackOutEasing() },
			{ "backinout", () => new BackInOutEasing() },
			{ "elasticin", () => new ElasticInEasing() },
			{ "elasticout", () => new ElasticOutEasing() },
			{ "elasticinout", () => new ElasticInOutEasing() },
			{ "bouncein", () => new BounceInEasing() },
			{ "bounceout", () => new BounceOutEasing() },
			{ "bounceinout", () => new BounceInOutEasing() },
		};

		public static IEasing FromString(string name)
		{
			Func<IEasing> f;
			if (map_.TryGetValue(name, out f))
				return f();

			return null;
		}
	}


	public interface IEasing
	{
		string GetDisplayName();
		string GetShortName();
		IEasing Clone(int cloneFlags = 0);
		float Magnitude(float f);
	}


	public abstract class BasicEasing : IEasing
	{
		public abstract string GetDisplayName();
		public abstract string GetShortName();
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


	public class ConstantZeroEasing : BasicEasing
	{
		public static string DisplayName
		{
			get { return "Constant zero"; }
		}

		public static string ShortName
		{
			get { return "constantzero"; }
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}

		public override string GetShortName()
		{
			return ShortName;
		}

		public override IEasing Clone(int cloneFlags = 0)
		{
			return new ConstantZeroEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(0);
		}
	}

	public class ConstantOneEasing : BasicEasing
	{
		public static string DisplayName
		{
			get { return "Constant one"; }
		}

		public static string ShortName
		{
			get { return "constantone"; }
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}

		public override string GetShortName()
		{
			return ShortName;
		}

		public override IEasing Clone(int cloneFlags = 0)
		{
			return new ConstantOneEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(1);
		}
	}

	public class LinearEasing : BasicEasing
	{
		public static string DisplayName
		{
			get { return "Linear"; }
		}

		public static string ShortName
		{
			get { return "linear"; }
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}

		public override string GetShortName()
		{
			return ShortName;
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
			get { return "Sinusoidal"; }
		}

		public static string ShortName
		{
			get { return "sinusoidal"; }
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}

		public override string GetShortName()
		{
			return ShortName;
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
			get { return "Quad in"; }
		}

		public static string ShortName
		{
			get { return "quadin"; }
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}

		public override string GetShortName()
		{
			return ShortName;
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
			get { return "Quad out"; }
		}

		public static string ShortName
		{
			get { return "quadout"; }
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}

		public override string GetShortName()
		{
			return ShortName;
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
			get { return "Quad in/out"; }
		}

		public static string ShortName
		{
			get { return "quadinout"; }
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}

		public override string GetShortName()
		{
			return ShortName;
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
			get { return "Cubic in"; }
		}

		public static string ShortName
		{
			get { return "cubicin"; }
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}

		public override string GetShortName()
		{
			return ShortName;
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
			get { return "Cubic out"; }
		}

		public static string ShortName
		{
			get { return "cubicout"; }
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}

		public override string GetShortName()
		{
			return ShortName;
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
			get { return "Cubic in/out"; }
		}

		public static string ShortName
		{
			get { return "cubicinout"; }
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}

		public override string GetShortName()
		{
			return ShortName;
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
			get { return "Quart in"; }
		}

		public static string ShortName
		{
			get { return "quartin"; }
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}

		public override string GetShortName()
		{
			return ShortName;
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
			get { return "Quart out"; }
		}

		public static string ShortName
		{
			get { return "quartout"; }
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}

		public override string GetShortName()
		{
			return ShortName;
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
			get { return "Quart in/out"; }
		}

		public static string ShortName
		{
			get { return "quartinout"; }
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}

		public override string GetShortName()
		{
			return ShortName;
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
			get { return "Quint in"; }
		}

		public static string ShortName
		{
			get { return "quintin"; }
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}

		public override string GetShortName()
		{
			return ShortName;
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
			get { return "Quint out"; }
		}

		public static string ShortName
		{
			get { return "quintout"; }
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}

		public override string GetShortName()
		{
			return ShortName;
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
			get { return "Quint in/out"; }
		}

		public static string ShortName
		{
			get { return "quintinout"; }
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}

		public override string GetShortName()
		{
			return ShortName;
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
			get { return "Sine in"; }
		}

		public static string ShortName
		{
			get { return "sinein"; }
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}

		public override string GetShortName()
		{
			return ShortName;
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
			get { return "Sine out"; }
		}

		public static string ShortName
		{
			get { return "sineout"; }
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}

		public override string GetShortName()
		{
			return ShortName;
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
			get { return "Sine in/out"; }
		}

		public static string ShortName
		{
			get { return "sineinout"; }
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}

		public override string GetShortName()
		{
			return ShortName;
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
			get { return "Expo in"; }
		}

		public static string ShortName
		{
			get { return "expoin"; }
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}

		public override string GetShortName()
		{
			return ShortName;
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
			get { return "Expo out"; }
		}

		public static string ShortName
		{
			get { return "expoout"; }
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}

		public override string GetShortName()
		{
			return ShortName;
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
			get { return "Expo in/out"; }
		}

		public static string ShortName
		{
			get { return "expoinout"; }
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}

		public override string GetShortName()
		{
			return ShortName;
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
			get { return "Circ in"; }
		}

		public static string ShortName
		{
			get { return "circin"; }
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}

		public override string GetShortName()
		{
			return ShortName;
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
			get { return "Circ out"; }
		}

		public static string ShortName
		{
			get { return "circout"; }
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}

		public override string GetShortName()
		{
			return ShortName;
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
			get { return "Circ in/out"; }
		}

		public static string ShortName
		{
			get { return "circinout"; }
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}

		public override string GetShortName()
		{
			return ShortName;
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
			get { return "Back in"; }
		}

		public static string ShortName
		{
			get { return "backin"; }
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}

		public override string GetShortName()
		{
			return ShortName;
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
			get { return "Back out"; }
		}

		public static string ShortName
		{
			get { return "backout"; }
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}

		public override string GetShortName()
		{
			return ShortName;
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
			get { return "Back in/out"; }
		}

		public static string ShortName
		{
			get { return "backinout"; }
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}

		public override string GetShortName()
		{
			return ShortName;
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
			get { return "Elastic in"; }
		}

		public static string ShortName
		{
			get { return "elasticin"; }
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}

		public override string GetShortName()
		{
			return ShortName;
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
			get { return "Elastic out"; }
		}

		public static string ShortName
		{
			get { return "elasticout"; }
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}

		public override string GetShortName()
		{
			return ShortName;
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
			get { return "Elastic in/out"; }
		}

		public static string ShortName
		{
			get { return "elasticinout"; }
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}

		public override string GetShortName()
		{
			return ShortName;
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
			get { return "Bounce in"; }
		}

		public static string ShortName
		{
			get { return "bouncein"; }
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}

		public override string GetShortName()
		{
			return ShortName;
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
			get { return "Bounce out"; }
		}

		public static string ShortName
		{
			get { return "bounceout"; }
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}

		public override string GetShortName()
		{
			return ShortName;
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
			get { return "Bounce in/out"; }
		}

		public static string ShortName
		{
			get { return "bounceinout"; }
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}

		public override string GetShortName()
		{
			return ShortName;
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

