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

		public static IVoice CreateVoice(string provider, JSONClass options)
		{
			if (provider == "macgruber")
				return new MacGruber.Voice(options);
			else if (provider == "vammoan")
				return new VamMoan.Voice(options);
			else if (provider == "none")
				return new NoVoice(options);
			else
				return null;
		}

		public static IHoming CreateHoming(Person p)
		{
			return new ToumeiHitsuji.DiviningRod(p);
		}
	}

	public interface IVoice
	{
		void Load(JSONClass o, bool inherited);
		void Init(Person p);
		void Destroy();
		IVoice Clone();

		void Update(float s);

		void SetMoaning(float v);
		void SetBreathing();
		void SetSilent();
		void SetOrgasm();
		void SetKissing();
		void SetBJ(float v);

		void Debug(DebugLines debug);

		string Name { get; }
		bool Muted { set; }
		bool MouthEnabled { get; set; }
		bool ChestEnabled { get; set; }

		string Warning { get; }
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
		void OnPluginState(bool b);
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

	public interface ISmoke
	{
		Vector3 Position { get; set; }
		Quaternion Rotation { get; set; }
		float Opacity { get; set; }
		void Destroy();
	}

	public interface IHoming
	{
		bool Genitals { get; set; }
		bool Mouth { get; set; }
		bool LeftHand { get; set; }
		bool RightHand { get; set; }
		string Warning { get; }

		void Init();
		void Update(float s);
	}

	public interface IPossesser
	{
		bool Possessed { get; }
	}
}
