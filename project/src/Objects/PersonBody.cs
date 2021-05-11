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


	class RandomLookAt
	{
		private const float Delay = 1;

		private Person person_;
		private float e_ = Delay;
		private Vector3 pos_ = Vector3.Zero;
		private IObject avoid_ = null;

		public RandomLookAt(Person p)
		{
			person_ = p;
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

			return false;
		}

		private UnityEngine.Plane P(Vector3 a, Vector3 b, Vector3 c)
		{
			return new UnityEngine.Plane(
				W.VamU.ToUnity(a), W.VamU.ToUnity(b), W.VamU.ToUnity(c)).flipped;
		}


		class Frustum
		{
			public Vector3 nearTL;
			public Vector3 nearTR;
			public Vector3 nearBL;
			public Vector3 nearBR;

			public Vector3 farTL;
			public Vector3 farTR;
			public Vector3 farBL;
			public Vector3 farBR;

			public Plane[] planes;
			public bool avoid = false;

			public Vector3 Random()
			{
				var nearWidth = nearTR.X - nearTL.Y;
				var nearHeight = nearBL.Y - nearTL.Y;
				var farWidth = farTR.X - farTL.Y;
				var farHeight = farBL.Y - farTL.Y;


				var nearPoint = new Vector3(
					U.RandomFloat(0, nearWidth) - nearWidth / 2,
					U.RandomFloat(0, nearHeight) - nearHeight / 2,
					nearTL.Z);

				var farPoint = new Vector3(
					U.RandomFloat(0, farWidth) - farWidth / 2,
					U.RandomFloat(0, farHeight) - farHeight / 2,
					farTL.Z);

				var d = Vector3.Distance(nearPoint, farPoint);
				var rd = U.RandomFloat(0, d);

				return nearPoint + (farPoint - nearPoint).Normalized * rd;
			}
		}

		private Frustum MakeFrustum(
			float totalNearWidth, float totalNearHeight, float nearDistance,
			float totalFarWidth, float totalFarHeight, float farDistance,
			int x, int y, int xCount, int yCount)
		{
			var nearWidth = totalNearWidth / xCount;
			var nearHeight = totalNearHeight / yCount;
			var nearOffset = new Vector3(
				-totalNearWidth / 2 + x * nearWidth,
				totalNearHeight / 2 - y * nearHeight,
				nearDistance);

			var farWidth = totalFarWidth / xCount;
			var farHeight = totalFarHeight / yCount;
			var farOffset = new Vector3(
				-totalFarWidth / 2 + x * farWidth,
				totalFarHeight / 2 - y * farHeight,
				farDistance);

			var f = new Frustum();

			f.nearTL = nearOffset;
			f.nearTR = nearOffset + new Vector3(nearWidth, 0, 0);
			f.nearBL = nearOffset + new Vector3(0, -nearHeight, 0);
			f.nearBR = nearOffset + new Vector3(nearWidth, -nearHeight, 0);

			f.farTL = farOffset;
			f.farTR = farOffset + new Vector3(farWidth, 0, 0);
			f.farBL = farOffset + new Vector3(0, -farHeight, 0);
			f.farBR = farOffset + new Vector3(farWidth, -farHeight, 0);

			f.planes = new UnityEngine.Plane[]
			{
				P(f.farTL,  f.nearTL, f.nearBL),  // left
				P(f.nearTR, f.farTR,  f.farBR),   // right
				P(f.nearBL, f.nearBR, f.farBR),   // down
				P(f.nearTL, f.farTL,  f.farTR),   // up
				P(f.nearTL, f.nearTR, f.nearBR),  // near
				P(f.farTR,  f.farTL,  f.farBL)    // far
			};

			return f;
		}

		private void NextPosition()
		{
			float nearWidth = 2;
			float nearHeight = 1;
			float nearDistance = 0.1f;

			float farWidth = 4;
			float farHeight = 2;
			float farDistance = 10;

			var fs = new Frustum[9];

			for (int x = 0; x < 3; ++x)
			{
				for (int y = 0; y < 3; ++y)
				{
					fs[y * 3 + x] = MakeFrustum(
						nearWidth, nearHeight, nearDistance,
						farWidth, farHeight, farDistance,
						x, y, 3, 3);
				}
			}



			string log = "";
			int availableCount = 9;

			if (avoid_ != null)
			{
				availableCount = 0;

				var selfHead = person_.Body.Head;
				var avoidPos = avoid_.EyeInterest - selfHead.Position;
				var avoidBox = new Bounds(
					W.VamU.ToUnity(avoidPos),
					new UnityEngine.Vector3(0.15f, 0.15f));

				log += $"av={avoidBox} ";

				for (int i = 0; i < 9; ++i)
				{
					//Cue.LogInfo($"{fs[i].nearTL} {fs[i].nearTR} {fs[i].nearBL} {fs[i].nearBR}");
					//Cue.LogInfo($"{fs[i].farTL} {fs[i].farTR} {fs[i].farBL} {fs[i].farBR}");

					//for (int p = 0; p < 6; ++p)
					//	Cue.LogInfo(fs[i].planes[p].ToString());

					if (GeometryUtility.TestPlanesAABB(fs[i].planes, avoidBox))
					{
						log += $"{i}=N ";
						fs[i].avoid = true;
					}
					else
					{
						log += $"{i}=Y ";
						++availableCount;
					}
				}
			}

			if (availableCount == 0)
			{
				//Cue.LogInfo(log);
				Cue.LogError("nowhere to look");
				return;
			}

			log += $"n={availableCount} ";


			int fi = U.RandomInt(0, availableCount - 1);
			log += $"fi={fi} ";

			var f = fs[fi];

			pos_ = f.Random();
			log += $"p={pos_}";

			//Cue.LogInfo(log);


			//var far =
			//	selfHead.Position +
			//	Vector3.Rotate(new Vector3(0, 0, 2), selfHead.Bearing);


			//pos_ = new Vector3(
			//	U.RandomFloat(-1.0f, 1.0f),
			//	U.RandomFloat(-1.0f, 1.0f),
			//	1);
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
