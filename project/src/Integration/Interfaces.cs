namespace Cue
{
	class GazeSettings
	{
		public const int LookAtDisabled = 0;
		public const int LookAtTarget = 1;
		public const int LookAtPlayer = 2;
	}


	interface IPlayer
	{
		bool Playing { get; }
		bool Play(IAnimation a, int flags);
		void Stop(bool rewind);
		void FixedUpdate(float s);
		void Update(float s);
	}

	interface IAnimation
	{
		int Sex { get; set; }
	}

	abstract class BasicAnimation : IAnimation
	{
		private int sex_ = Sexes.Any;

		public int Sex
		{
			get
			{
				return sex_;
			}

			set
			{
				sex_ = value;
			}
		}
	}


	class Integration
	{
		public static IBreather CreateBreather(Person p)
		{
			return new MacGruberBreather(p);
		}

		public static ISpeaker CreateSpeaker(Person p)
		{
			return new VamSpeaker(p);
		}

		public static IGazer CreateGazer(Person p)
		{
			return new MacGruberGaze(p);
		}

		public static IKisser CreateKisser(Person p)
		{
			return new ClockwiseSilverKiss(p);
		}

		public static IHandjob CreateHandjob(Person p)
		{
			return new ClockwiseSilverHandjob(p);
		}

		public static IClothing CreateClothing(Person p)
		{
			return new VamClothing(p);
		}

		public static IExpression CreateExpression(Person p)
		{
			return new ProceduralExpression(p);
		}
	}


	interface IBreather
	{
		float Intensity { get; set; }
		float Speed { get; set; }
	}

	interface IGazer
	{
		int LookAt { get; set; }
		Vector3 Target { get; set; }
		void LookInFront();
		void Update(float s);
	}

	interface ISpeaker
	{
		void Say(string s);
	}

	interface IKisser
	{
		void Update(float s);
	}

	interface IHandjob
	{
		bool Active { get; set; }
		Person Target { get; set; }
	}

	interface IClothing
	{
		bool GenitalsVisible { get; set; }
		bool BreastsVisible { get; set; }
		void OnPluginState(bool b);
		void Dump();
	}


	struct Pair<First, Second>
	{
		public First first;
		public Second second;

		public Pair(First f, Second s)
		{
			first = f;
			second = s;
		}
	}


	class Expressions
	{
		public const int Common = 1;
		public const int Happy = 2;
		public const int Mischievous = 3;
	}

	interface IExpression
	{
		void MakeNeutral();
		void Set(Pair<int, float>[] intensities, bool resetOthers = false);
		bool Enabled { get; set; }
		void Update(float s);
		void OnPluginState(bool b);
	}
}
