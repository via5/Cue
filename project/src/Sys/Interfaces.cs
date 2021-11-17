using SimpleJSON;
using System;
using System.Collections.Generic;

namespace Cue.Sys
{
	public class ObjectParameters
	{
		private Dictionary<string, string> map_ = null;

		public ObjectParameters(JSONClass o)
		{
			var keys = o?.Keys?.ToList();

			if (keys != null && keys.Count > 0)
			{
				map_ = new Dictionary<string, string>();

				for (int i = 0; i < keys.Count; ++i)
					map_.Add(keys[i], o[keys[i]].Value);
			}
		}

		public string Get(string key)
		{
			if (map_ != null)
			{
				string v;
				if (map_.TryGetValue(key, out v))
					return v;
			}

			return "";
		}
	}


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

	public interface ISys
	{
		void ClearLog();
		void LogLines(string s, int level);
		IAtom GetAtom(string id);
		List<IAtom> GetAtoms(bool alsoOff=false);
		IAtom ContainingAtom { get; }
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
		List<string> GetFilenames(string path, string pattern);
		void HardReset();
		void ReloadPlugin();
		void OpenScriptUI();
		bool IsPlayMode { get; }
		bool IsVR { get; }
		bool HasUI { get; }
		float DeltaTime { get; }
		float RealtimeSinceStartup { get; }
		string Fps { get; }
		int RandomInt(int first, int last);
		float RandomFloat(float first, float last);
		IObjectCreator CreateObjectCreator(string name, string type, JSONClass opts, ObjectParameters ps);
		IGraphic CreateBoxGraphic(string name, Box box, Color c);
		IGraphic CreateBoxGraphic(string name, Vector3 pos, Vector3 size, Color c);
		IGraphic CreateSphereGraphic(string name, Vector3 pos, float radius, Color c);
		IGraphic CreateCapsuleGraphic(string name, Color c);
		ILiveSaver CreateLiveSaver();
	}


	public interface ILiveSaver
	{
		JSONClass Load();
		void Save(JSONClass o);
	}


	public interface IObjectCreator
	{
		string Name { get; }
		void Create(Sys.IAtom user, string id, Action<IObject> callback);
		void Destroy(Sys.IAtom user, string id);
	}


	public struct HoveredInfo
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

	public interface IInput
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

		bool MenuUp { get; }
		bool MenuDown { get; }
		bool MenuLeft { get; }
		bool MenuRight { get; }
		bool MenuSelect { get; }

		void Update(float s);
		HoveredInfo GetLeftHovered();
		HoveredInfo GetRightHovered();
		HoveredInfo GetMouseHovered();

		string DebugString();
		string VRInfo();
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

	public struct TriggerInfo
	{
		public int personIndex;
		public int sourcePartIndex;
		public float value;
		public bool forced;

		public TriggerInfo(int sourcePersonIndex, int sourceBodyPart, float v, bool forced = false)
		{
			personIndex = sourcePersonIndex;
			sourcePartIndex = sourceBodyPart;
			value = v;
			this.forced = forced;
		}

		public static TriggerInfo None
		{
			get { return new TriggerInfo(-1, BP.None, 1.0f); }
		}

		public bool IsPerson()
		{
			return (personIndex != -1 && sourcePartIndex != -1);
		}

		public override string ToString()
		{
			if (personIndex == -1 || sourcePartIndex == -1)
			{
				if (forced)
					return "forced";
				else
					return "?";
			}
			else
			{
				var p = Cue.Instance.AllPersons[personIndex];
				var bp = p.Body.Get(sourcePartIndex);

				string s = $"{p.ID}.{bp.Name}";

				if (forced)
					s += "(forced)";

				return s;
			}
		}
	}

	public struct GrabInfo
	{
		public int personIndex;
		public int sourcePartIndex;

		public GrabInfo(int personIndex, int sourcePartIndex)
		{
			this.personIndex = personIndex;
			this.sourcePartIndex = sourcePartIndex;
		}

		public static GrabInfo None
		{
			get { return new GrabInfo(-1, BP.None); }
		}

		public override string ToString()
		{
			string s = "";
			Person p = null;

			if (personIndex == -1)
			{
				s += "?";
			}
			else
			{
				p = Cue.Instance.AllPersons[personIndex];
				s += p.ID;
			}

			s += ".";


			if (sourcePartIndex == -1)
				s += "?";
			else
				s += BP.ToString(sourcePartIndex);

			return s;
		}
	}

	public interface IBodyPart
	{
		IAtom Atom { get; }
		int Type { get; }
		bool Exists { get; }
		bool CanTrigger { get; }
		TriggerInfo[] GetTriggers();
		GrabInfo[] GetGrabs();
		bool CanGrab { get; }
		bool Grabbed { get; }
		Vector3 ControlPosition { get; set; }
		Quaternion ControlRotation { get; set; }
		Vector3 Position { get; }
		Quaternion Rotation { get; }
		bool Linked { get; }
		void LinkTo(IBodyPart other);
		bool IsLinkedTo(IBodyPart other);
		float DistanceToSurface(IBodyPart other, bool debug = false);

		bool CanApplyForce();
		void AddRelativeForce(Vector3 v);
		void AddRelativeTorque(Vector3 v);
		void AddForce(Vector3 v);
		void AddTorque(Vector3 v);
	}

	public interface IBone
	{
		Vector3 Position { get; }
		Quaternion Rotation { get; }
	}

	public interface IHair
	{
		float Loose { get; set; }
	}

	public interface IMorph
	{
		string Name { get; }
		float Value { get; set; }
		bool LimiterEnabled { set; }
		float DefaultValue { get; }
		void Reset();
	}

	public struct Hand
	{
		public IMorph fist, inOut;
		public IBone[][] bones;
	}

	public interface IBody
	{
		IBodyPart[] GetBodyParts();
		Hand GetLeftHand();
		Hand GetRightHand();
		bool Exists { get; }
		float Sweat { get; set; }
		float Flush { get; set; }
		bool Strapon { get; set; }
	}

	static class BodyDamping
	{
		public const int Normal = 0;
		public const int Sex = 1;
	}

	public interface IAtom
	{
		string ID { get; }
		bool Visible { get; set; }
		bool IsPerson { get; }
		bool IsMale { get; }
		bool Possessed { get; }
		bool Selected { get; }
		bool Grabbed { get; }

		bool Collisions { get; set; }
		bool Physics { get; set; }
		bool Hidden { get; set; }
		float Scale { get; set; }

		Vector3 Position { get; set; }
		Quaternion Rotation { get; set; }

		void Init();
		void Destroy();

		IBody Body { get; }
		IHair Hair { get; }

		IMorph GetMorph(string id);

		void SetDefaultControls(string why);
		void SetParentLink(IBodyPart bp);
		void SetBodyDamping(int e);

		void OnPluginState(bool b);

		void Update(float s);
		void LateUpdate(float s);
	}

	public interface IGraphic
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
