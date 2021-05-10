using SimpleJSON;
using System.Collections.Generic;

namespace Cue
{
	class Integration
	{
		public static List<IPlayer> CreateAnimationPlayers(Person p)
		{
			return new List<IPlayer>()
			{
				new BVH.Player(p),
				new TimelinePlayer(p),
				new ProceduralPlayer(p),
				new SynergyPlayer(p)
			};
		}

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

		public static IBlowjob CreateBlowjob(Person p)
		{
			return new ClockwiseSilverBlowjob(p);
		}

		public static IExpression CreateExpression(Person p)
		{
			return new ProceduralExpression(p);
		}
	}


	interface IPlayer
	{
		bool Playing { get; }
		bool Paused { get; set; }
		bool UsesFrames { get; }

		bool Play(IAnimation a, int flags);
		void Stop(bool rewind);
		void Seek(float where);
		void FixedUpdate(float s);
		void Update(float s);
	}

	interface IAnimation
	{
		float InitFrame{ get; }
		float FirstFrame { get; }
		float LastFrame { get; }
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

		void LookAt(IObject o);
		void LookAt(Vector3 p);
		void LookInFront();
		void LookAtNothing();
		void LookAtCamera();

		void Update(float s);
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

		void Start(Person p);
		void StartReciprocal(Person p);
		void StopSelf();
		void Stop();

		void Update(float s);
		void OnPluginState(bool b);
	}

	interface IHandjob
	{
		bool Active { get; }
		void Start(Person target);
		void Stop();
		void Update(float s);
		void OnPluginState(bool b);
	}

	interface IBlowjob
	{
		bool Active { get; }
		void Start(Person target);
		void Stop();
		void Update(float s);
		void OnPluginState(bool b);
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


	public struct ExpressionIntensity
	{
		public int type;
		public float intensity;

		public ExpressionIntensity(int type, float intensity)
		{
			this.type = type;
			this.intensity = intensity;
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
		void Set(int type, float intensity, bool resetOthers = false);
		void Set(ExpressionIntensity[] intensities, bool resetOthers = false);
		bool Enabled { get; set; }
		void Update(float s);
		void OnPluginState(bool b);
		void DumpActive();
	}
}
