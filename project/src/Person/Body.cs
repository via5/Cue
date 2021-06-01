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

		// all
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


		private static int[] breasts_ = new int[] { LeftBreast, RightBreast };
		public static int[] BreastParts
		{
			get { return breasts_; }
		}

		private static int[] genitals_ = new int[] { Labia, Vagina, DeepVagina, DeeperVagina };
		public static int[] GenitalParts
		{
			get { return genitals_; }
		}

		private static int[] personalSpace_ = new[]{
			LeftHand, RightHand, Head, Chest, LeftBreast, RightBreast,
			Hips, Genitals, Labia, Vagina, DeepVagina, DeeperVagina,
			LeftFoot, RightFoot};
		public static int[] PersonalSpaceParts
		{
			get { return personalSpace_; }
		}


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

		public static int FromString(string s)
		{
			for (int i = 0; i < names_.Length; ++i)
			{
				if (names_[i] == s)
					return i;
			}

			return None;
		}

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
		private bool forceBusy_ = false;

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

		public bool CanTrigger
		{
			get { return part_?.CanTrigger ?? false; }
		}

		public float Trigger
		{
			get { return part_?.Trigger ?? 0; }
		}

		public bool Grabbed
		{
			get { return part_?.Grabbed ?? false; }
		}

		public bool TriggeredBy(BodyPart other)
		{
			if (!Exists || !other.Exists)
				return false;

			if (Trigger == 0)
				return false;

			return CloseToImpl(other);
		}

		public bool CloseTo(BodyPart other)
		{
			if (!Exists || !other.Exists)
				return false;

			return CloseToImpl(other);
		}

		private bool CloseToImpl(BodyPart other)
		{
			var d = Vector3.Distance(Position, other.Position);
			return (d < 0.2f);
		}

		public void ForceBusy(bool b)
		{
			forceBusy_ = b;
		}

		public bool Busy
		{
			get
			{
				if (forceBusy_)
					return true;

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

		public Vector3 ControlPosition
		{
			get
			{
				return part_?.ControlPosition ?? Vector3.Zero;

			}

			set
			{
				if (part_ != null)
					part_.ControlPosition = value;
			}
		}

		public Quaternion ControlRotation
		{
			get
			{
				return part_?.ControlRotation ?? Quaternion.Zero;
			}

			set
			{
				if (part_ != null)
					part_.ControlRotation = value;
			}
		}

		public Vector3 Position
		{
			get
			{
				return part_?.Position ?? Vector3.Zero;

			}
		}

		public Quaternion Rotation
		{
			get
			{
				return part_?.Rotation ?? Quaternion.Zero;
			}
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


	class Hair
	{
		private Person person_;
		private DampedFloat loose_;

		public Hair(Person p)
		{
			person_ = p;
			loose_ = new DampedFloat(x => person_.Atom.Hair.Loose = x);
		}

		public float Loose
		{
			get { return loose_.Target; }
			set { loose_.Target = value; }
		}

		public DampedFloat DampedLoose
		{
			get { return loose_; }
		}

		public void Update(float s)
		{
			loose_.Update(s);
		}
	}


	class Bone
	{
		private string name_;
		private W.IBone sys_;

		public Bone(string name, W.IBone b)
		{
			name_ = name;
			sys_ = b;
		}

		public bool Exists
		{
			get { return (sys_ != null); }
		}

		public string Name
		{
			get { return name_; }
		}

		public Vector3 Position
		{
			get
			{
				if (sys_ == null)
					return Vector3.Zero;
				else
					return sys_.Position;
			}
		}

		public Quaternion Rotation
		{
			get
			{
				if (sys_ == null)
					return Quaternion.Zero;
				else
					return sys_.Rotation;
			}
		}
	}


	class Morph
	{
		private W.IMorph m_;

		public Morph(W.IMorph m)
		{
			m_ = m;
		}

		public string Name
		{
			get { return m_.Name; }
		}

		public float Value
		{
			get { return m_.Value; }
			set { m_.Value = value; }
		}

		public float DefaultValue
		{
			get { return m_.DefaultValue; }
		}

		public void Reset()
		{
			m_.Reset();
		}
	}


	class Finger
	{
		private Hand hand_;
		private string name_;
		private Bone[] bones_;

		public Finger(Hand h, string name, W.IBone[] bones)
		{
			hand_ = h;
			name_ = name;
			bones_ = new Bone[3];

			bones_[0] = new Bone("proximal", bones[0]);
			bones_[1] = new Bone("intermediate", bones[1]);
			bones_[2] = new Bone("distal", bones[2]);
		}

		public string Name
		{
			get { return name_; }
		}

		public Bone[] Bones
		{
			get { return bones_; }
		}

		// closest to palm
		//
		public Bone Proximal
		{
			get { return bones_[0]; }
		}

		// middle
		//
		public Bone Intermediate
		{
			get { return bones_[1]; }
		}

		// closest to tip
		//
		public Bone Distal
		{
			get { return bones_[2]; }
		}
	}


	class Hand
	{
		private Person person_;
		private string name_;
		private Finger[] fingers_;
		private Morph fist_;
		private Morph inOut_;

		public Hand(Person p, string name, W.Hand h)
		{
			person_ = p;
			name_ = name;

			fingers_ = new Finger[5];
			fingers_[0] = new Finger(this, "thumb", h.bones[0]);
			fingers_[1] = new Finger(this, "index", h.bones[1]);
			fingers_[2] = new Finger(this, "middle", h.bones[2]);
			fingers_[3] = new Finger(this, "ring", h.bones[3]);
			fingers_[4] = new Finger(this, "little", h.bones[4]);

			fist_ = new Morph(h.fist);
			inOut_ = new Morph(h.inOut);
		}

		public string Name
		{
			get { return name_; }
		}

		public Finger[] Fingers
		{
			get { return fingers_; }
		}

		public Finger Thumb
		{
			get { return fingers_[0]; }
		}

		public Finger Index
		{
			get { return fingers_[1]; }
		}

		public Finger Middle
		{
			get { return fingers_[2]; }
		}

		public Finger Ring
		{
			get { return fingers_[3]; }
		}

		public Finger Little
		{
			get { return fingers_[4]; }
		}

		public float Fist
		{
			get { return fist_.Value; }
			set { fist_.Value = value; }
		}

		public float InOut
		{
			get { return inOut_.Value; }
			set { inOut_.Value = value; }
		}
	}


	class Body
	{
		public const int CloseDelay = 2;

		private Person person_;
		private readonly BodyPart[] all_;
		private Hand leftHand_, rightHand_;
		private DampedFloat sweat_, flush_;

		public Body(Person p)
		{
			person_ = p;

			sweat_ = new DampedFloat(x => person_.Atom.Body.Sweat = x);
			flush_ = new DampedFloat(x =>
				person_.Atom.Body.LerpColor(
					Color.Red, x * person_.Physiology.MaxFlush));

			var parts = p.Atom.Body.GetBodyParts();
			var all = new List<BodyPart>();

			for (int i = 0; i < BodyParts.Count; ++i)
			{
				if (parts[i] != null && parts[i].Type != i)
					Cue.LogError($"mismatched body part type {parts[i].Type} {i}");

				all.Add(new BodyPart(person_, i, parts[i]));
			}

			all_ = all.ToArray();

			leftHand_ = new Hand(p, "left", p.Atom.Body.GetLeftHand());
			rightHand_ = new Hand(p, "right", p.Atom.Body.GetRightHand());
		}

		public void Init()
		{
		}

		public BodyPart[] Parts
		{
			get { return all_; }
		}

		public bool InsidePersonalSpace(Person other)
		{
			var checkParts = BodyParts.PersonalSpaceParts;

			for (int i = 0; i < checkParts.Length; ++i)
			{
				var a = person_.Body.Get(checkParts[i]);

				for (int j = 0; j < checkParts.Length; ++j)
				{
					var b = other.Body.Get(checkParts[j]);
					if (a.CloseTo(b))
						return true;
				}
			}

			return false;
		}

		public bool Groped()
		{
			var parts = new int[]
			{
				BodyParts.Chest, BodyParts.Genitals
			};


			for (int i = 0; i < Cue.Instance.Persons.Count; ++i)
			{
				if (GropedBy(Cue.Instance.Persons[i], parts))
					return true;
			}

			return false;
		}

		public bool Penetrated()
		{
			for (int i = 0; i < Cue.Instance.Persons.Count; ++i)
			{
				if (PenetratedBy(Cue.Instance.Persons[i]))
					return true;
			}

			return false;
		}

		public bool GropedBy(Person p, int triggerBodyPart)
		{
			return GropedBy(p, new int[] { triggerBodyPart });
		}

		public bool GropedBy(Person p, int[] triggerBodyParts)
		{
			if (p == person_)
				return false;

			var checkParts = new int[]
			{
				BodyParts.Head, BodyParts.LeftHand, BodyParts.RightHand,
				BodyParts.LeftFoot, BodyParts.RightFoot
			};

			return CheckParts(p, triggerBodyParts, checkParts);
		}

		public bool PenetratedBy(Person p)
		{
			if (p == person_)
				return false;

			var triggerParts = new int[]
			{
				BodyParts.Vagina,
				BodyParts.DeepVagina,
				BodyParts.DeeperVagina,
				BodyParts.Anus
			};

			var checkParts = new int[]
			{
				BodyParts.Genitals
			};

			return CheckParts(p, triggerParts, checkParts);
		}

		private bool CheckParts(Person by, int[] triggerParts, int[] checkParts)
		{
			for (int i = 0; i < triggerParts.Length; ++i)
			{
				var triggerPart = Get(triggerParts[i]);

				for (int j = 0; j < checkParts.Length; ++j)
				{
					var byPart = by.Body.Get(checkParts[j]);

					if (triggerPart.CanTrigger)
					{
						if (triggerPart.TriggeredBy(byPart))
							return true;
					}
					else
					{
						if (triggerPart.CloseTo(byPart))
							return true;
					}
				}
			}

			return false;
		}

		public float Sweat
		{
			get { return sweat_.Target; }
			set { sweat_.Target = value; }
		}

		public DampedFloat DampedSweat
		{
			get { return sweat_; }
		}

		public float Flush
		{
			get { return flush_.Target; }
			set { flush_.Target = value; }
		}

		public DampedFloat DampedFlush
		{
			get { return flush_; }
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

		public Hand LeftHand
		{
			get { return leftHand_; }
		}

		public Hand RightHand
		{
			get { return rightHand_; }
		}

		public Box TopBox
		{
			get
			{
				var q = Get(BodyParts.Chest).Rotation;

				var avoidHeadU = Get(BodyParts.Head).Position + new Vector3(0, 0.2f, 0);
				var avoidHipU = Get(BodyParts.Hips).Position;

				var avoidHead = q.RotateInv(avoidHeadU);
				var avoidHip = q.RotateInv(avoidHipU);

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

			sweat_.Update(s);
			flush_.Update(s);
		}
	}
}
