using SimpleJSON;
using System;
using System.Collections.Generic;

namespace Cue.Sys
{
	public class ObjectParameters
	{
		private JSONClass ps_ = null;

		public ObjectParameters(JSONClass o)
		{
			ps_ = o ?? new JSONClass();
		}

		public JSONClass Object
		{
			get { return ps_; }
		}
	}


	public class FileInfo
	{
		public string path;
		public string origin;

		public FileInfo(string path, string origin)
		{
			this.path = path;
			this.origin = origin;
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
		void Init();
		void FixedUpdate(float s);
		void Update(float s);
		void LateUpdate(float s);
		void OnPluginState(bool b);
		void OnReady(Action f);
		string ReadFileIntoString(string path);
		string GetResourcePath(string path);
		List<FileInfo> GetFiles(string path, string pattern);
		void SaveFileDialog(string ext, Action<string> f);
		void LoadFileDialog(string ext, Action<string> f);
		JSONNode ReadJSON(string path);
		void WriteJSON(string path, JSONNode content);
		void HardReset();
		void ReloadPlugin();
		void OpenScriptUI();
		void SetMenuVisible(bool b);
		bool IsPlayMode { get; }
		bool IsVR { get; }
		bool HasUI { get; }
		float DeltaTime { get; }
		float RealtimeSinceStartup { get; }
		string Fps { get; }
		bool ForceDevMode { get; }
		int RandomInt(int first, int last);
		float RandomFloat(float first, float last);
		IObjectCreator CreateObjectCreator(string name, string type, JSONClass opts, ObjectParameters ps);
		IGraphic CreateBoxGraphic(string name, Box box, Color c);
		IGraphic CreateBoxGraphic(string name, Vector3 pos, Vector3 size, Color c);
		IGraphic CreateSphereGraphic(string name, Vector3 pos, float radius, Color c);
		IGraphic CreateCapsuleGraphic(string name, Color c);
		ILiveSaver CreateLiveSaver();
		IActionTrigger CreateActionTrigger();
		IActionTrigger LoadActionTrigger(JSONNode n);
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
		HoveredInfo GetMouseHovered();

		List<Pair<string, string>> Debug();
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
		public const int NoneType = 0;
		public const int PersonType = 1;
		public const int ToyType = 2;


		private int type_;

		private int personIndex_;
		private BodyPartType bodyPart_;
		private float mag_;
		private bool forced_;

		private IAtom externalAtom_;

		public static TriggerInfo FromExternal(
			int type, IAtom atom, float v, bool forced=false)
		{
			return new TriggerInfo(type, -1, BP.None, v, forced, atom);
		}

		public static TriggerInfo FromPerson(
			int sourcePersonIndex, BodyPartType sourceBodyPart, float mag,
			bool forced = false)
		{
			return new TriggerInfo(
				PersonType, sourcePersonIndex, sourceBodyPart,
				mag, forced, null);
		}

		private TriggerInfo(
			int type, int sourcePersonIndex, BodyPartType sourceBodyPart,
			float mag, bool forced, IAtom externalAtom)
		{
			type_ = type;
			personIndex_ = sourcePersonIndex;
			bodyPart_ = sourceBodyPart;
			mag_ = mag;
			forced_ = forced;
			externalAtom_ = externalAtom;
		}

		public int Type
		{
			get { return type_; }
		}

		public int PersonIndex
		{
			get { return personIndex_; }
		}

		public BodyPartType BodyPart
		{
			get { return bodyPart_; }
		}

		public float Magnitude
		{
			get { return mag_; }
		}

		public bool SameAs(TriggerInfo other)
		{
			if (type_ != other.type_)
				return false;

			switch (type_)
			{
				case PersonType:
				{
					return
						(personIndex_ == other.personIndex_) &&
						(bodyPart_ == other.bodyPart_);
				}

				case ToyType:
				case NoneType:
				default:
				{
					return (externalAtom_ == other.externalAtom_);
				}
			}
		}

		public TriggerInfo MergeFrom(TriggerInfo other)
		{
			return new TriggerInfo(
				type_, personIndex_, bodyPart_,
				Math.Max(mag_, other.mag_),
				forced_, externalAtom_);
		}

		public bool Is(int personIndex, BodyPartType bodyPart)
		{
			return (personIndex_ == personIndex) && (bodyPart_ == bodyPart);
		}

		public override string ToString()
		{
			switch (type_)
			{
				case PersonType:
				{
					var p = Cue.Instance.AllPersons[personIndex_];
					var bp = p.Body.Get(bodyPart_);

					string s = $"{p.ID}.{bp.Name}:{mag_:0.00}";

					if (forced_)
						s += "(forced)";

					return s;
				}

				case ToyType:
				{
					string s;

					if (externalAtom_ != null)
						s = $"{externalAtom_.ID}:{mag_:0.00}";
					else
						s = "?";

					if (forced_)
						s += "(forced)";

					return s;
				}

				case NoneType:
				default:
				{
					if (forced_)
						return "forced";
					else
						return "?";
				}
			}
		}
	}

	public struct GrabInfo
	{
		public int personIndex;
		public BodyPartType sourcePartIndex;

		public GrabInfo(int personIndex, BodyPartType sourcePartIndex)
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


			if (sourcePartIndex == BP.None)
				s += "?";
			else
				s += BodyPartType.ToString(sourcePartIndex);

			return s;
		}
	}

	public interface IBodyPartRegion
	{
		IBodyPart BodyPart { get; }
	}

	public struct BodyPartRegionInfo
	{
		public IBodyPartRegion region;
		public float distance;

		public BodyPartRegionInfo(IBodyPartRegion r, float d)
		{
			region = r;
			distance = d;
		}

		public static BodyPartRegionInfo None
		{
			get { return new BodyPartRegionInfo(null, float.MaxValue); }
		}
	}

	public interface IBodyPart
	{
		IAtom Atom { get; }
		BodyPartType Type { get; }
		bool Exists { get; }
		bool IsPhysical { get; }
		bool Render { get; set; }
		TriggerInfo[] GetTriggers();
		GrabInfo[] GetGrabs();
		bool CanGrab { get; }
		bool Grabbed { get; }
		Vector3 ControlPosition { get; set; }
		Quaternion ControlRotation { get; set; }
		Vector3 Position { get; }
		Quaternion Rotation { get; }

		IBodyPartRegion Link { get; }
		bool IsLinked { get; }
		void LinkTo(IBodyPartRegion other);
		void Unlink();
		bool IsLinkedTo(IBodyPart other);

		float DistanceToSurface(IBodyPart other, bool debug = false);
		float DistanceToSurface(Vector3 pos, bool debug = false);
		BodyPartRegionInfo ClosestBodyPartRegion(Vector3 pos);

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
		bool Valid { get; }
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
		public const int SexReceiver = 1;
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
		bool AutoBlink { get; set; }

		Vector3 Position { get; set; }
		Quaternion Rotation { get; set; }

		string Warning { get; }

		void Init();
		void Destroy();

		IBody Body { get; }
		IHair Hair { get; }

		IMorph GetMorph(string id, float eyesClosed);

		void SetPose(Pose p);
		void SetParentLink(IBodyPart bp);
		void SetBodyDamping(int e);
		void SetCollidersForKiss(bool b, IAtom other);

		void OnPluginState(bool b);

		void Update(float s);
		void LateUpdate(float s);

		string DebugString();
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

	public interface IActionTrigger
	{
		string Name { get; set; }
		void Edit(Action onDone = null);
		void Fire();
		JSONNode ToJSON();
	}
}
