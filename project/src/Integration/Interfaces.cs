using SimpleJSON;
using System;
using System.Collections.Generic;

namespace Cue
{
	class LoadFailed : Exception
	{
		public LoadFailed(string message)
			: base(message)
		{
		}
	}

	class Integration
	{
		public static List<IPlayer> CreateAnimationPlayers(Person p)
		{
			return new List<IPlayer>()
			{
				new BVH.Player(p),
				new TimelinePlayer(p),
				new Proc.Player(p),
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

		public static IClothing CreateClothing(Person p)
		{
			return new ClothingManager(p);
		}

		public static ISmoke CreateSmoke(string id, bool existsOnly = false)
		{
			return VamSmoke.Create(id, existsOnly);
		}
	}


	class AnimationContext
	{
		public object ps;
		public ulong key;

		public AnimationContext(object ps, ulong key = BodyPartLock.NoKey)
		{
			this.ps = ps;
			this.key = key;
		}
	}


	interface IPlayer
	{
		string Name { get; }
		bool UsesFrames { get; }

		IAnimation[] GetPlaying();
		bool CanPlay(IAnimation a);
		bool Play(IAnimation a, int flags, AnimationContext cx);
		void RequestStop(IAnimation a);
		void StopNow(IAnimation a);
		void Seek(IAnimation a, float where);
		void FixedUpdate(float s);
		void Update(float s);
		bool IsPlaying(IAnimation a);
		void OnPluginState(bool b);
	}

	interface IAnimation
	{
		string Name { get; }
		float InitFrame{ get; }
		float FirstFrame { get; }
		float LastFrame { get; }
		bool HasMovement { get; }
		string[] GetAllForcesDebug();
		string[] Debug();
		string ToDetailedString();
	}

	interface IOrgasmer
	{
		void Orgasm();
	}

	interface IBreather
	{
		bool MouthEnabled { get; set; }
		float Intensity { get; set; }
		float Speed { get; set; }
	}

	interface IGazer
	{
		string Name { get; }

		bool Enabled { get; set; }
		float Duration { get; set; }
		float Variance { get; set; }

		void Update(float s);
	}

	interface IEyes
	{
		bool Blink { get; set; }
		Vector3 TargetPosition { get; }

		void LookAt(Vector3 p);
		void LookAtNothing();

		void Update(float s);
	}

	interface ISpeaker
	{
		void Say(string s);
	}

	interface IClothing
	{
		float HeelsAngle { get; }
		float HeelsHeight { get; }
		bool GenitalsVisible { get; set; }
		bool BreastsVisible { get; set; }
		void Dump();
	}

	interface ISmoke
	{
		Vector3 Position { get; set; }
		Quaternion Rotation { get; set; }
		float Opacity { get; set; }
		void Destroy();
	}
}
