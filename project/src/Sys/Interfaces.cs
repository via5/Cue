using SimpleJSON;
using System;
using System.Collections;
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
		JSONClass GetConfig();
		IAtom GetAtom(string id);
		List<IAtom> GetAtoms(bool alsoOff=false);
		IAtom ContainingAtom { get; }
		INav Nav { get; }
		IInput Input { get; }
		Vector3 CameraPosition { get; }
		Vector3 InteractiveLeftHandPosition { get; }
		Vector3 InteractiveRightHandPosition { get; }
		bool Paused { get; }
		void Update(float s);
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
		IObjectCreator CreateObjectCreator(string name, string type, JSONClass opts);
		VUI.Root CreateHud(Vector3 offset, Point pos, Size size);
		VUI.Root CreateAttached(bool left, Vector3 offset, Point pos, Size size);
		VUI.Root Create2D(float topOffset, Size size);
		VUI.Root CreateScriptUI();
		IGraphic CreateBoxGraphic(string name, Box box, Color c);
		IGraphic CreateBoxGraphic(string name, Vector3 pos, Vector3 size, Color c);
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

		void Update(float s);
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

	struct TriggerInfo
	{
		public int personIndex;
		public int sourcePartIndex;
		public float value;

		public TriggerInfo(int pi, int spi, float v)
		{
			personIndex = pi;
			sourcePartIndex = spi;
			value = v;
		}

		public override string ToString()
		{
			if (personIndex == -1 || sourcePartIndex == -1)
				return "?";

			var p = Cue.Instance.AllPersons[personIndex];
			var bp = p.Body.Get(sourcePartIndex);

			if (value == 1)
				return $"{p.ID}.{bp.Name}";
			else
				return $"{p.ID}.{bp.Name}/{value:0.0}";
		}
	}

	interface IBodyPart
	{
		int Type { get; }
		bool CanTrigger { get; }
		TriggerInfo[] GetTriggers();
		bool CanGrab { get; }
		bool Grabbed { get; }
		Vector3 ControlPosition { get; set; }
		Quaternion ControlRotation { get; set; }
		Vector3 Position { get; }
		Quaternion Rotation { get; }
		void AddRelativeForce(Vector3 v);
		void AddRelativeTorque(Vector3 v);
	}

	interface IBone
	{
		Vector3 Position { get; }
		Quaternion Rotation { get; }
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

	interface IHair
	{
		float Loose { set; }
	}

	interface IMorph
	{
		string Name { get; }
		float Value { get; set; }
		float DefaultValue { get; }
		void Reset();
	}

	struct Hand
	{
		public IMorph fist, inOut;
		public IBone[][] bones;
	}

	interface IBody
	{
		IBodyPart[] GetBodyParts();
		Hand GetLeftHand();
		Hand GetRightHand();
		float Sweat { set; }
		void LerpColor(Color c, float f);
	}

	interface IAtom
	{
		string ID { get; }
		bool Visible { get; set; }
		bool IsPerson { get; }
		int Sex { get; }
		bool Teleporting { get; }
		bool Possessed { get; }
		bool Selected { get; }

		bool Collisions { get; set; }
		bool Physics { get; set; }
		bool Hidden { get; set; }
		float Scale { get; set; }

		Vector3 Position { get; set; }
		Quaternion Rotation { get; set; }

		void Init();
		void Destroy();

		IClothing Clothing { get; }
		IBody Body { get; }
		IHair Hair { get; }

		IMorph GetMorph(string id);

		void SetDefaultControls(string why);
		void SetParentLink(IBodyPart bp);

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
		Quaternion Rotation { get; set; }
		Vector3 Size { get; set; }
		Color Color { get; set; }
		bool Collision { get; set; }
		void Destroy();
	}
}
