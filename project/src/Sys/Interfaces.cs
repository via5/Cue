using System;
using System.Collections.Generic;

namespace Cue.W
{
	class LogLevels
	{
		public const int Error = 0;
		public const int Warning = 1;
		public const int Info = 2;
		public const int Verbose = 3;

		public static string ToShortString(int i)
		{
			switch (i)
			{
				case Error: return "E";
				case Warning: return "W";
				case Info: return "I";
				case Verbose: return "V";
				default: return $"?{i}";
			}
		}
	}

	interface ISys
	{
		void ClearLog();
		void Log(string s, int level);
		IAtom GetAtom(string id);
		List<IAtom> GetAtoms(bool alsoOff=false);
		IAtom ContainingAtom { get; }
		INav Nav { get; }
		IInput Input { get; }
		Vector3 Camera { get; }
		bool Paused { get; }
		void OnPluginState(bool b);
		void OnReady(Action f);
		string ReadFileIntoString(string path);
		string GetResourcePath(string path);
		void HardReset();
		void ReloadPlugin();
		bool IsPlayMode { get; }
		bool IsVR { get; }
		float DeltaTime { get; }
		float RealtimeSinceStartup { get; }
		int RandomInt(int first, int last);
		float RandomFloat(float first, float last);
		VUI.Root CreateHud(Vector3 offset, Point pos, Size size);
		VUI.Root CreateAttached(bool left, Vector3 offset, Point pos, Size size);
		VUI.Root Create2D(float topOffset, Size size);
		VUI.Root CreateScriptUI();
		IGraphic CreateBoxGraphic(string name, Vector3 pos, Color c);
		IGraphic CreateSphereGraphic(string name, Vector3 pos, float radius, Color c);
	}

	struct HoveredInfo
	{
		public IObject o;
		public Vector3 pos;
		public bool hit;

		public HoveredInfo(IObject o, Vector3 pos, bool hit)
		{
			this.o = o;
			this.pos = pos;
			this.hit = hit;
		}

		public static HoveredInfo None
		{
			get
			{
				return new HoveredInfo(null, Vector3.Zero, false);
			}
		}
	}

	interface IInput
	{
		bool HardReset { get; }
		bool ReloadPlugin { get; }

		bool ShowLeftMenu { get; }
		bool LeftAction { get; }
		bool ShowRightMenu { get; }
		bool RightAction { get; }

		bool Select { get; }
		bool Action { get; }
		bool ToggleControls { get; }

		void Update();
		HoveredInfo GetLeftHovered();
		HoveredInfo GetRightHovered();
		HoveredInfo GetMouseHovered();
	}

	class NavStates
	{
		public const int None = 0;
		public const int Calculating = 1;
		public const int Moving = 2;
		public const int TurningLeft = 3;
		public const int TurningRight = 4;

		public static string ToString(int state)
		{
			switch (state)
			{
				case None:
					return "(none)";

				case Calculating:
					return "calculating";

				case Moving:
					return "moving";

				case TurningLeft:
					return "turning-left";

				case TurningRight:
					return "turning-right";

				default:
					return $"?{state}";
			}
		}
	}

	interface IBodyPart
	{
		int Type { get; }
		bool Triggering { get; }
		Vector3 Position { get; }
		Vector3 Direction { get; }
	}

	interface IClothing
	{
		float HeelsAngle { get; }
		float HeelsHeight { get; }
		bool GenitalsVisible { get; set; }
		bool BreastsVisible { get; set; }
		void Init();
		void OnPluginState(bool b);
		void Dump();
	}

	interface IAtom
	{
		string ID { get; }
		bool IsPerson { get; }
		int Sex { get; }
		bool Teleporting { get; }
		bool Possessed { get; }
		bool Selected { get; }

		Vector3 Position { get; set; }
		Vector3 Direction { get; set; }

		void Init();

		List<IBodyPart> GetBodyParts();
		IClothing Clothing { get; }
		void SetDefaultControls(string why);

		void OnPluginState(bool b);

		void Update(float s);
		void TeleportTo(Vector3 p, float bearing);

		bool NavEnabled { get; set; }
		bool NavPaused { get; set; }
		int NavState { get; }
		void NavTo(Vector3 v, float bearing, float stoppingDistance);
		void NavStop(string why);
	}

	interface INav
	{
		void Update();
		List<Vector3> Calculate(Vector3 from, Vector3 to);
		bool Render { get; set; }
	}

	interface IGraphic
	{
		bool Visible { get; set; }
		Vector3 Position { get; set; }
		Vector3 Direction { get; set; }
		Color Color { get; set; }
		bool Collision { get; set; }
		void Destroy();
	}
}
