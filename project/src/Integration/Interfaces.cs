namespace Cue
{
	class GazeSettings
	{
		public const int LookAtDisabled = 0;
		public const int LookAtTarget = 1;
		public const int LookAtPlayer = 2;

		public static string ToString(int i)
		{
			switch (i)
			{
				case LookAtDisabled:
					return "disabled";

				case LookAtTarget:
					return "target";

				case LookAtPlayer:
					return "player";

				default:
					return $"?{i}";
			}
		}
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
		int Type { get; }
		int Sex { get; set; }
	}

	abstract class BasicAnimation : IAnimation
	{
		private readonly int type_;
		private int sex_ = Sexes.Any;

		protected BasicAnimation(int type)
		{
			type_ = type;
		}

		public int Type
		{
			get { return type_; }
		}

		public int Sex
		{
			get { return sex_; }
			set { sex_ = value; }
		}
	}


	class Integration
	{
		public static IBreather CreateBreather(Person p)
		{
			return new MacGruberBreather(p);
		}

		public static IOrgasmer CreateOrgasmer(Person p)
		{
			return new MacGruberOrgasmer(p);
		}

		public static ISpeaker CreateSpeaker(Person p)
		{
			return new VamSpeaker(p);
		}

		public static IEyes CreateEyes(Person p)
		{
			return new VamEyes(p);
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

	interface IOrgasmer
	{
		void Orgasm();
	}

	interface IBreather
	{
		float Intensity { get; set; }
		float Speed { get; set; }
	}

	interface IGazer
	{
		bool Enabled { get; set; }
		void Update(float s);
	}

	interface IEyes
	{
		bool Blink { get; set; }

		void Update(float s);
		void LookAt(IObject o);
		void LookAt(Vector3 p);
		void LookInFront();
		void LookAtNothing();
		void LookAtCamera();
	}

	interface ISpeaker
	{
		void Say(string s);
	}

	interface IKisser
	{
		bool Active { get; }
		Person Target { get; }
		float Elapsed { get; }
		bool OnCooldown { get; }

		void Update(float s);
		void Start(Person p);
		void StartReciprocal(Person p);
		void Stop();
		void StopSelf();
	}

	interface IHandjob
	{
		bool Active { get; set; }
		Person Target { get; set; }
	}

	interface IClothing
	{
		float HeelsAngle { get; }
		float HeelsHeight { get; }
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
		public const int Pleasure = 4;

		public static string ToString(int i)
		{
			switch (i)
			{
				case Common: return "common";
				case Happy: return "happy";
				case Mischievous: return "mischievous";
				case Pleasure: return "pleasure";
				default: return $"?{i}";
			}
		}
	}

	interface IExpression
	{
		void MakeNeutral();
		void Set(Pair<int, float>[] intensities, bool resetOthers = false);
		bool Enabled { get; set; }
		void Update(float s);
		void OnPluginState(bool b);
		void DumpActive();
	}
}
