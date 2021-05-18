using System;
using System.Collections.Generic;

namespace Cue
{
	class BodyParts
	{
		public const int None = -1;

		public const int Head = 0;
		public const int Lips = 1;
		public const int Mouth = 2;

		// female
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

		public const int Eyes = 29;
		public const int Genitals = 30;

		// male
		public const int Pectorals = 31;

		public const int Count = 32;


		private static string[] names_ = new string[]
		{
			"head", "lips", "mouth", "leftbreast", "rightbreast",
			"labia", "vagina", "deepvagina", "deepervagina", "anus",

			"chest", "belly", "hips", "leftglute", "rightglute",

			"leftshoulder", "leftarm", "leftforearm", "lefthand",
			"rightshoulder", "rightarm", "rightforearm", "righthand",

			"leftthigh", "leftshin", "leftfoot",
			"rightthigh", "rightshin", "rightfoot",

			"eyes", "genitals", "pectorals"
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

		public W.VamBodyPart VamSys
		{
			get { return part_ as W.VamBodyPart; }
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

		public float Trigger
		{
			get { return part_?.Trigger ?? 0; }
		}

		public bool Grabbed
		{
			get { return part_?.Grabbed ?? false; }
		}

		public bool Close
		{
			get { return close_; }
			set { close_ = value; }
		}

		public bool Busy
		{
			get
			{
				// todo
				switch (type_)
				{
					case BodyParts.Head:
					case BodyParts.Lips:
					case BodyParts.Mouth:
					{
						return person_.Kisser.Active || person_.Blowjob.Active;
					}

					case BodyParts.LeftArm:
					case BodyParts.LeftForearm:
					case BodyParts.LeftHand:
					{
						return
							person_.Handjob.Active &&
							person_.Handjob.LeftUsed;
					}

					case BodyParts.RightArm:
					case BodyParts.RightForearm:
					case BodyParts.RightHand:
					{
						return
							person_.Handjob.Active &&
							person_.Handjob.RightUsed;
					}
				}

				return false;
			}
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

		public override string ToString()
		{
			string s = "";

			if (part_ == null)
				s += "null";
			else
				s += part_.ToString();

			s += $" ({BodyParts.ToString(type_)})";

			return s;
		}
	}


	class Body
	{
		public const int CloseDelay = 2;

		private Person person_;
		private readonly BodyPart[] all_;
		private bool handsClose_;
		private float timeSinceClose_ = CloseDelay + 1;
		private float currentSweat_ = 0;
		private float targetSweat_ = 0;

		public Body(Person p)
		{
			person_ = p;

			var parts = p.Atom.Body.GetBodyParts();
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
				return handsClose_ || (timeSinceClose_ < CloseDelay);
			}
		}

		public bool PlayerIsCloseDelayed
		{
			get
			{
				return !handsClose_ && (timeSinceClose_ < CloseDelay);
			}
		}

		public float Sweat
		{
			get { return targetSweat_; }
			set { targetSweat_ = value; }
		}

		public float CurrentSweat
		{
			get { return currentSweat_; }
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

		public Box TopBox
		{
			get
			{
				var q = Get(BodyParts.Chest).Direction;

				var avoidHeadU = Get(BodyParts.Head).Position + new Vector3(0, 0.2f, 0);
				var avoidHipU = Get(BodyParts.Hips).Position;

				var avoidHead = Vector3.RotateInv(avoidHeadU, q);
				var avoidHip = Vector3.RotateInv(avoidHipU, q);

				return new Box(
					avoidHip + (avoidHead - avoidHip) / 2,
					new Vector3(0.5f, (avoidHead - avoidHip).Y, 0.5f));
			}
		}

		public Box FullBox
		{
			get
			{
				var avoidHead = Get(BodyParts.Head).Position + new Vector3(0, 0.2f, 0);
				var avoidFeet = person_.Position;

				var b = new Box(
					avoidFeet + (avoidHead - avoidFeet) / 2,
					new Vector3(0.5f, (avoidHead - avoidFeet).Y, 0.35f));

				b.center.Y -= b.size.Y / 2;

				return b;
			}
		}

		public Frustum ClosenessFrustum
		{
			get
			{
				var f = new Frustum(
					new Vector3(0.45f, 2, 0.3f),
					new Vector3(0.2f, 2, 0.4f));

				return f;
			}
		}

		public void Update(float s)
		{
			if (person_.Atom.Teleporting)
				return;

			var wasClose = handsClose_;
			handsClose_ = false;

			var leftHand = Cue.Instance.InteractiveLeftHandPosition;
			var rightHand = Cue.Instance.InteractiveRightHandPosition;
			var head = Cue.Instance.Player?.Body?.Get(BodyParts.Head)?.Position ?? Vector3.Zero;

			for (int i = 0; i < all_.Length; ++i)
			{
				var p = all_[i];
				if (p == null)
					continue;

				var leftD = Vector3.Distance(p.Position, leftHand);
				var rightD = Vector3.Distance(p.Position, rightHand);

				var headD = float.MaxValue;
				if (Cue.Instance.Player != null)
					headD = Vector3.Distance(p.Position, head);

				p.Close = (leftD < 0.2f) || (rightD < 0.2f) || (headD < 0.2f);
				handsClose_ = handsClose_ || p.Close;
			}

			if (wasClose && !handsClose_)
				timeSinceClose_ = 0;
			else if (!handsClose_)
				timeSinceClose_ += s;

			UpdateSweat(s);

			if (person_.Atom.Body != null)
				person_.Atom.Body.Sweat = currentSweat_;
		}

		private void UpdateSweat(float s)
		{
			if (targetSweat_ > currentSweat_)
				currentSweat_ = U.Clamp(currentSweat_ + s / 20, 0, targetSweat_);
			else
				currentSweat_ = U.Clamp(currentSweat_ - s / 40, targetSweat_, 1);
		}
	}


	class Excitement
	{
		private Person person_;
		private float[] parts_ = new float[BodyParts.Count];
		private float decay_ = 1;

		private float flatExcitement_ = 0;
		private float forcedExcitement_ = -1;
		private float mouthRate_ = 0;
		private float breastsRate_ = 0;
		private float genitalsRate_ = 0;
		private float penetrationRate_ = 0;
		private float totalRate_ = 0;
		private float max_ = 0;
		private bool postOrgasm_ = false;
		private float postOrgasmElapsed_ = 0;

		private IEasing easing_ = new CubicOutEasing();


		public Excitement(Person p)
		{
			person_ = p;
		}

		public string StateString
		{
			get
			{
				if (postOrgasm_)
					return "post orgasm";
				else
					return "none";
			}
		}

		public float Value
		{
			get
			{
				if (forcedExcitement_ >= 0)
					return forcedExcitement_;
				else
					return easing_.Magnitude(flatExcitement_);
			}
		}

		public void ForceValue(float s)
		{
			forcedExcitement_ = s;
		}

		public void Update(float s)
		{
			var ss = person_.Personality.Sensitivity;

			if (postOrgasm_)
			{
				postOrgasmElapsed_ += s;
				if (postOrgasmElapsed_ < ss.DelayPostOrgasm)
					return;

				postOrgasm_ = false;
				postOrgasmElapsed_ = 0;
			}

			UpdateParts(s);
			UpdateRates(s);
			UpdateMax(s);
			UpdateValue(s);
			Apply(s);
		}

		private void UpdateParts(float s)
		{
			for (int i = 0; i < BodyParts.Count; ++i)
			{
				var t = person_.Body.Get(i).Trigger;

				if (t > 0)
					parts_[i] = t;
				else
					parts_[i] = Math.Max(parts_[i] - s * decay_, 0);
			}
		}

		private void UpdateRates(float s)
		{
			var ss = person_.Personality.Sensitivity;

			totalRate_ = 0;

			if (flatExcitement_ < ss.MouthMax)
				mouthRate_ = Mouth * ss.MouthRate * s;
			else
				mouthRate_ = 0;

			if (flatExcitement_ < ss.BreastsMax)
				breastsRate_ = Breasts * ss.BreastsRate * s;
			else
				breastsRate_ = 0;

			if (flatExcitement_ < ss.GenitalsMax)
				genitalsRate_ = Genitals * ss.GenitalsRate * s;
			else
				genitalsRate_ = 0;

			if (flatExcitement_ < ss.PenetrationMax)
				penetrationRate_ = Penetration * ss.PenetrationRate * s;
			else
				penetrationRate_ = 0;


			totalRate_ += mouthRate_ + breastsRate_ + genitalsRate_ + penetrationRate_;
			if (totalRate_ == 0)
				totalRate_ = ss.DecayPerSecond * s;
		}

		private void UpdateMax(float s)
		{
			var ss = person_.Personality.Sensitivity;

			max_ = 0;

			if (Mouth > 0)
				max_ = Math.Max(max_, ss.MouthMax);

			if (Breasts > 0)
				max_ = Math.Max(max_, ss.BreastsMax);

			if (Genitals > 0)
				max_ = Math.Max(max_, ss.GenitalsMax);

			if (Penetration > 0)
				max_ = Math.Max(max_, ss.PenetrationMax);
		}

		private void UpdateValue(float s)
		{
			var ss = person_.Personality.Sensitivity;

			if (flatExcitement_ > max_)
			{
				flatExcitement_ =
					Math.Max(flatExcitement_ + ss.DecayPerSecond * s, max_);
			}
			else
			{
				flatExcitement_ =
					U.Clamp(flatExcitement_ + totalRate_, 0, max_);
			}
		}

		private void Apply(float s)
		{
			var ss = person_.Personality.Sensitivity;

			person_.Breathing.Intensity = Value;
			person_.Body.Sweat = Value;
			person_.Expression.Set(Expressions.Pleasure, Value);

			if (Value >= 1)
			{
				person_.Orgasmer.Orgasm();
				flatExcitement_ = ss.ExcitementPostOrgasm;
				postOrgasm_ = true;
				postOrgasmElapsed_ = 0;
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

		public float MouthRate
		{
			get { return mouthRate_; }
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

		public float BreastsRate
		{
			get { return breastsRate_; }
		}

		public float Genitals
		{
			get
			{
				return Math.Min(1,
					parts_[BodyParts.Labia]);
			}
		}

		public float GenitalsRate
		{
			get { return genitalsRate_; }
		}

		public float Penetration
		{
			get
			{
				return Math.Min(1,
					parts_[BodyParts.Vagina] * 0.3f +
					parts_[BodyParts.DeepVagina] * 1 +
					parts_[BodyParts.DeeperVagina] * 1);
			}
		}

		public float PenetrationRate
		{
			get { return penetrationRate_; }
		}

		public float Rate
		{
			get { return totalRate_; }
		}

		public float Max
		{
			get { return max_; }
		}

		public override string ToString()
		{
			string s =
				$"{Value:0.000000} " +
				$"(flat {flatExcitement_:0.000000}, max {max_:0.000000})";

			if (forcedExcitement_ >= 0)
				s += $" forced {forcedExcitement_:0.000000})";

			return s;
		}
	}
}
