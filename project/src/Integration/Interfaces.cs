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
			return new MacGruber.Gaze(p);
		}

		public static IClothing CreateClothing(Person p)
		{
			return new ClothingManager(p);
		}

		public static ISmoke CreateSmoke(string id, bool existsOnly = false)
		{
			return VamSmoke.Create(id, existsOnly);
		}

		public static IBreather CreateBreather(string provider, JSONClass options)
		{
			if (provider == "macgruber")
				return new MacGruber.Breather(options);
			else if (provider == "vammoan")
				return new VamMoan.Breather(options);
			else
				return null;
		}

		public static IOrgasmer CreateOrgasmer(string provider, JSONClass options)
		{
			if (provider == "macgruber")
				return new MacGruber.Orgasmer(options);
			else
				return null;
		}
	}

	public interface IOrgasmer
	{
		void Init(Person p);
		void Destroy();
		IOrgasmer Clone();

		void ForcePitch(float f);
		float ForcedPitch { get; }

		void Orgasm();
	}

	public interface IBreather
	{
		void Init(Person p);
		void Destroy();
		IBreather Clone();

		void ForcePitch(float f);
		float ForcedPitch { get; }

		bool MouthEnabled { get; set; }
		float Intensity { get; set; }
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
