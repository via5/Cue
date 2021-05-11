using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cue
{
	class BodyParts
	{
		public const int Head = 0;

		public const int Lips = 1;
		public const int Mouth = 2;
		public const int LeftBreast = 3;
		public const int RightBreast = 4;
		public const int Labia = 5;
		public const int Vagina = 6;
		public const int DeepVagina = 7;
		public const int DeeperVagina = 8;
		public const int Anus = 9;

		public const int Chest = 10;
		public const int Belly = 11;
		public const int Hips = 12;
		public const int LeftGlute = 13;
		public const int RightGlute = 14;

		public const int LeftShoulder = 15;
		public const int LeftArm = 16;
		public const int LeftForearm = 17;
		public const int LeftHand = 18;

		public const int RightShoulder = 19;
		public const int RightArm = 20;
		public const int RightForearm = 21;
		public const int RightHand = 22;

		public const int LeftThigh = 23;
		public const int LeftShin = 24;
		public const int LeftFoot = 25;

		public const int RightThigh = 26;
		public const int RightShin = 27;
		public const int RightFoot = 28;

		public const int Count = 29;


		private static string[] names_ = new string[]
		{
			"head", "lips", "mouth", "leftbreast", "rightbreast",
			"labia", "vagina", "deepvagina", "deepervagina", "anus",

			"chest", "belly", "hips", "leftglute", "rightglute",

			"leftshoulder", "leftarm", "leftforearm", "lefthand",
			"rightshoulder", "rightarm", "rightforearm", "righthand",

			"leftthigh", "leftshin", "leftfoot",
			"rightthigh", "rightshin", "rightfoot"
		};

		public static string ToString(int t)
		{
			if (t >= 0 && t < names_.Length)
				return names_[t];

			return $"?{t}";
		}
	}


	class BodyPart
	{
		private Person person_;
		private int type_;
		private W.IBodyPart part_;
		private bool close_ = false;

		public BodyPart(Person p, int type, W.IBodyPart part)
		{
			person_ = p;
			type_ = type;
			part_ = part;
		}

		public Person Person
		{
			get { return person_; }
		}

		public W.IBodyPart Sys
		{
			get { return part_; }
		}

		public bool Exists
		{
			get { return (part_ != null); }
		}

		public string Name
		{
			get { return BodyParts.ToString(type_); }
		}

		public int Type
		{
			get { return type_; }
		}

		public bool Triggering
		{
			get { return part_?.Triggering ?? false; }
		}

		public bool Close
		{
			get { return close_; }
			set { close_ = value; }
		}

		public Vector3 Position
		{
			get { return part_?.Position ?? Vector3.Zero; }
		}

		public Vector3 Direction
		{
			get { return part_?.Direction ?? Vector3.Zero; }
		}

		public float Bearing
		{
			get { return Vector3.Bearing(Direction); }
		}
	}


	class Body
	{
		private Person person_;
		private readonly BodyPart[] all_;
		private bool handsClose_;
		private float timeSinceClose_ = 0;

		public Body(Person p)
		{
			person_ = p;

			var parts = p.Atom.GetBodyParts();
			var all = new List<BodyPart>();

			for (int i = 0; i < BodyParts.Count; ++i)
			{
				if (parts[i] != null && parts[i].Type != i)
					Cue.LogError($"mismatched body part type {parts[i].Type} {i}");

				all.Add(new BodyPart(person_, i, parts[i]));
			}

			all_ = all.ToArray();
		}

		public BodyPart[] Parts
		{
			get { return all_; }
		}

		public bool PlayerIsClose
		{
			get
			{
				return handsClose_ || (timeSinceClose_ < 2);
			}
		}

		public BodyPart Get(int type)
		{
			if (type < 0 || type >= all_.Length)
			{
				Cue.LogError($"bad part type {type}");
				return null;
			}

			return all_[type];
		}

		public void Update(float s)
		{
			var wasClose = handsClose_;
			handsClose_ = false;

			var leftHand = Cue.Instance.InteractiveLeftHandPosition;
			var rightHand = Cue.Instance.InteractiveRightHandPosition;

			for (int i = 0; i < all_.Length; ++i)
			{
				var p = all_[i];
				if (p == null)
					continue;

				var leftD = Vector3.Distance(p.Position, leftHand);
				var rightD = Vector3.Distance(p.Position, rightHand);

				p.Close = (leftD < 0.2f) || (rightD < 0.2f);
				handsClose_ = handsClose_ || p.Close;
			}

			if (wasClose && !handsClose_)
				timeSinceClose_ = 0;
			else if (!handsClose_)
				timeSinceClose_ += s;
		}

		// convenience
		public BodyPart Lips { get { return Get(BodyParts.Lips); } }
		public BodyPart Head { get { return Get(BodyParts.Head); } }
	}


	class Excitement
	{
		private Person person_;
		private float[] parts_ = new float[BodyParts.Count];
		private float decay_ = 1;

		private float excitement_ = 0;
		private float forcedExcitement_ = -1;

		public Excitement(Person p)
		{
			person_ = p;
		}

		public float Value
		{
			get
			{
				if (forcedExcitement_ >= 0)
					return forcedExcitement_;
				else
					return excitement_;
			}

			set
			{
				excitement_ = U.Clamp(value, 0, 1);
			}
		}

		public void ForceValue(float s)
		{
			forcedExcitement_ = s;
		}


		public void Update(float s)
		{
			for (int i=0; i<BodyParts.Count; ++i)
				parts_[i] = Check(s, person_.Body.Get(i), parts_[i]);

			if (excitement_ >= 1)
			{
				person_.Orgasmer.Orgasm();
				excitement_ = 0;
			}
		}

		private float Check(float s, BodyPart p, float v)
		{
			if (p?.Triggering ?? false)
				return 1;
			else
				return Math.Max(v - s * decay_, 0);
		}

		public float Genitals
		{
			get
			{
				return Math.Min(1,
					parts_[BodyParts.Labia] * 0.005f +
					parts_[BodyParts.Vagina] * 0.01f +
					parts_[BodyParts.DeepVagina] * 0.2f +
					parts_[BodyParts.DeeperVagina] * 1);
			}
		}

		public float Mouth
		{
			get
			{
				return
					parts_[BodyParts.Lips] * 0.1f +
					parts_[BodyParts.Mouth] * 0.9f;
			}
		}

		public float Breasts
		{
			get
			{
				return
					parts_[BodyParts.LeftBreast] * 0.5f +
					parts_[BodyParts.RightBreast] * 0.5f;
			}
		}
	}


	class FrustumRender
	{
		private Person person_;
		private Frustum frustum_;
		private Color color_ = Color.Zero;
		private UnityEngine.GameObject near_ = null;
		private UnityEngine.GameObject far_ = null;
		private UnityEngine.Material mat_ = null;

		public FrustumRender(Person p, Frustum f)
		{
			person_ = p;
			frustum_ = f;
		}

		public void Update(float s)
		{
			if (near_ == null)
			{
				near_ = Create();
				//far_ = Create();
			}

			near_.transform.position = W.VamU.ToUnity(
				person_.Body.Head.Position +
				frustum_.NearCenter());

			near_.transform.localScale = W.VamU.ToUnity(
				frustum_.NearSize());

			//far_.transform.position = W.VamU.ToUnity(
			//	person_.Body.Head.Position +
			//	frustum_.FarCenter());
			//
			//far_.transform.localScale = W.VamU.ToUnity(
			//	frustum_.FarSize());
		}

		public Color Color
		{
			set
			{
				color_ = value;
				if (mat_ != null)
					mat_.color = W.VamU.ToUnity(value);
			}
		}

		private UnityEngine.GameObject Create()
		{
			var o = UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cube);
			o.transform.SetParent(Cue.Instance.VamSys.RootTransform);

			foreach (var c in o.GetComponents<UnityEngine.Collider>())
				UnityEngine.Object.Destroy(c);

			mat_ = new UnityEngine.Material(UnityEngine.Shader.Find("Battlehub/RTGizmos/Handles"));
			mat_.color = W.VamU.ToUnity(color_);

			o.GetComponent<UnityEngine.Renderer>().material = mat_;

			return o;
		}
	}


	class RandomLookAt
	{
		class FrustumInfo
		{
			public Frustum frustum;
			public bool avoid;
			public FrustumRender render;

			public FrustumInfo(Person p, Frustum f)
			{
				frustum = f;
				avoid = false;
				render = new FrustumRender(p, f);
			}
		}

		private Vector3 Near = new Vector3(2, 1, 0.1f);
		private Vector3 Far = new Vector3(4, 2, 2);

		private const int XCount = 5;
		private const int YCount = 5;
		private const int FrustumCount = XCount * YCount;

		private const float Delay = 1;

		private Person person_;
		private float e_ = Delay;
		private Vector3 pos_ = Vector3.Zero;
		private IObject avoid_ = null;
		private FrustumInfo[] frustums_ = new FrustumInfo[FrustumCount];
		private UnityEngine.GameObject avoidRender_ = null;

		public RandomLookAt(Person p)
		{
			person_ = p;

			var main = new Frustum(Near, Far);
			var fs = main.Split(XCount, YCount);

			for (int i = 0; i < fs.Length; ++i)
				frustums_[i] = new FrustumInfo(p, fs[i]);
		}

		private void CreateAvoidRender()
		{
			avoidRender_ = UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cube);
			avoidRender_.transform.SetParent(Cue.Instance.VamSys.RootTransform);

			foreach (var c in avoidRender_.GetComponents<UnityEngine.Collider>())
				UnityEngine.Object.Destroy(c);

			var mat = new UnityEngine.Material(UnityEngine.Shader.Find("Battlehub/RTGizmos/Handles"));
			mat.color = W.VamU.ToUnity(new Color(0, 0, 1, 0.1f));

			avoidRender_.GetComponent<UnityEngine.Renderer>().material = mat;
		}

		public IObject Avoid
		{
			get { return avoid_; }
			set { avoid_ = value; }
		}

		public Vector3 Position
		{
			get { return pos_; }
		}

		public bool Update(float s)
		{
			e_ += s;

			if (e_ >= Delay)
			{
				NextPosition();
				e_ = 0;
				return true;
			}

			for (int i = 0; i < frustums_.Length; ++i)
				frustums_[i].render.Update(s);

			return false;
		}

		private void NextPosition()
		{
			int av = CheckAvoid();
			if (av == 0)
			{
				Cue.LogError("nowhere to look at");
				return;
			}

			var sel = RandomAvailableFrustum(av);
			pos_ = sel.Random();

			for (int i = 0; i < frustums_.Length; ++i)
			{
				if (frustums_[i].avoid)
					frustums_[i].render.Color = new Color(1, 0, 0, 0.1f);
				else
					frustums_[i].render.Color = new Color(0, 1, 0, 0.1f);
			}
		}

		private int CheckAvoid()
		{
			if (avoid_ == null)
			{
				for (int i = 0; i < frustums_.Length; ++i)
					frustums_[i].avoid = false;

				return frustums_.Length;
			}

			int av = 0;

			var selfHead = person_.Body.Head;

			var avoidP = avoid_ as Person;
			Box avoidBox;

			if (avoidP != null)
			{
				var avoidHead = avoidP.Body.Head.Position - selfHead.Position + new Vector3(0, 0.2f, 0);
				var avoidHip = avoidP.Body.Get(BodyParts.Hips).Position - selfHead.Position;
				avoidBox = new Box(
					avoidHip + (avoidHead - avoidHip) / 2,
					new Vector3(0.5f, (avoidHead - avoidHip).Y, 0.2f));
			}
			else
			{
				avoidBox = new Box(
					avoid_.EyeInterest - selfHead.Position,
					new Vector3(0.2f, 0.2f, 0.2f));
			}

			if (avoidRender_ == null)
				CreateAvoidRender();

			avoidRender_.transform.position = W.VamU.ToUnity(avoidBox.center + selfHead.Position);
			avoidRender_.transform.localScale = W.VamU.ToUnity(avoidBox.size);


			string s = $"{avoidBox} ";

			for (int i = 0; i < frustums_.Length; ++i)
			{
				if (i == 12)
				{
					for (int p=0; p< frustums_[i].frustum.planes.Length; ++p)
						Cue.LogInfo($"{frustums_[i].frustum.planes[p]}");
				}


				if (frustums_[i].frustum.TestPlanesAABB(avoidBox))
				{
					frustums_[i].avoid = true;
					s += "N";
				}
				else
				{
					frustums_[i].avoid = false;
					s += "Y";
					++av;
				}

				if (i > 0 && ((i + 1) % XCount) == 0)
					s += " ";
			}

			Cue.LogInfo(s);

			return av;
		}

		private Frustum RandomAvailableFrustum(int av)
		{
			int fi = U.RandomInt(0, av - 1);

			for (int i = 0; i < frustums_.Length; ++i)
			{
				if (frustums_[i].avoid)
					continue;

				if (fi == 0)
					return frustums_[i].frustum;

				--fi;
			}

			Cue.LogError($"RandomAvailableFrustum: fi={fi} av={av} l={frustums_.Length}");
			return Frustum.Zero;
		}
	}


	class Gaze
	{
		private Person person_;
		private IEyes eyes_;
		private IGazer gazer_;
		private bool interested_ = false;
		private RandomLookAt random_;
		private bool randomActive_ = false;

		public Gaze(Person p)
		{
			person_ = p;
			random_ = new RandomLookAt(p);
			eyes_ = Integration.CreateEyes(p);
			gazer_ = Integration.CreateGazer(p);
		}

		public IEyes Eyes { get { return eyes_; } }
		public IGazer Gazer { get { return gazer_; } }

		public bool HasInterestingTarget
		{
			get { return interested_; }
		}

		public void Update(float s)
		{
			if (randomActive_)
			{
				if (random_.Update(s))
				{
					eyes_.LookAt(
						person_.Body.Head.Position +
						Vector3.Rotate(random_.Position, person_.Bearing));
				}
			}

			eyes_.Update(s);
			gazer_.Update(s);
		}

		public void LookAtDefault()
		{
			if (person_ == Cue.Instance.Player)
				LookAtNothing();
			else if (Cue.Instance.Player == null)
				LookAtCamera();
			else
				LookAt(Cue.Instance.Player);

			interested_ = false;
			randomActive_ = false;
		}

		public void LookAtCamera()
		{
			person_.Log.Info("looking at camera");
			eyes_.LookAtCamera();
			interested_ = false;
			randomActive_ = false;
		}

		public void LookAt(IObject o, bool gaze = true)
		{
			person_.Log.Info($"looking at {o} gaze={gaze}");
			eyes_.LookAt(o);
			gazer_.Enabled = gaze;
			interested_ = true;
			randomActive_ = false;
		}

		public void LookAt(Vector3 p, bool gaze = true)
		{
			person_.Log.Info($"looking at {p} gaze={gaze}");
			eyes_.LookAt(p);
			gazer_.Enabled = gaze;
			interested_ = false;
			randomActive_ = false;
		}

		public void LookAtRandom(bool gaze = true)
		{
			person_.Log.Info($"looking at random gaze={gaze}");
			randomActive_ = true;
			random_.Avoid = null;
			gazer_.Enabled = gaze;
		}

		public void Avoid(IObject o, bool gaze = true)
		{
			person_.Log.Info($"looking at random, avoiding {o}, gaze={gaze}");
			randomActive_ = true;
			random_.Avoid = o;
			gazer_.Enabled = gaze;
		}

		public void LookAtNothing()
		{
			person_.Log.Info("looking at nothing");
			eyes_.LookAtNothing();
			gazer_.Enabled = false;
			interested_ = false;
			randomActive_ = false;
		}

		public void LookInFront()
		{
			person_.Log.Info("looking in front");
			eyes_.LookInFront();
			gazer_.Enabled = false;
			interested_ = false;
			randomActive_ = false;
		}
	}
}
