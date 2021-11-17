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

	public interface IOrgasmer
	{
		void Orgasm();
	}

	public interface IBreather
	{
		bool MouthEnabled { get; set; }
		float Intensity { get; set; }
		float Speed { get; set; }
	}

	public interface IGazer
	{
		string Name { get; }

		bool Enabled { get; set; }
		float Duration { get; set; }
		float Variance { get; set; }

		void Update(float s);
	}

	public interface IEyes
	{
		bool Blink { get; set; }
		bool Saccade { get; set; }
		Vector3 TargetPosition { get; }

		void LookAt(Vector3 p);
		void LookAtNothing();

		void Update(float s);
	}

	public interface ISpeaker
	{
		void Say(string s);
	}

	public interface IClothing
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
