namespace Cue
{
	public class Bone
	{
		private string name_;
		private Sys.IBone sys_;

		public Bone(string name, Sys.IBone b)
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

	public class Finger
	{
		private Hand hand_;
		private string name_;
		private Bone[] bones_;

		public Finger(Hand h, string name, Sys.IBone[] bones)
		{
			hand_ = h;
			name_ = name;
			bones_ = new Bone[3];

			bones_[0] = new Bone("proximal", bones?[0]);
			bones_[1] = new Bone("intermediate", bones?[1]);
			bones_[2] = new Bone("distal", bones?[2]);
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


	public class Hand
	{
		private Person person_;
		private string name_;
		private int bodyPart_;
		private Finger[] fingers_;
		private Morph fist_;
		private Morph inOut_;

		public Hand(Person p, string name, Sys.Hand h, int bodyPart)
		{
			person_ = p;
			name_ = name;
			bodyPart_ = bodyPart;

			fingers_ = new Finger[5];
			fingers_[0] = new Finger(this, "thumb", h.bones?[0]);
			fingers_[1] = new Finger(this, "index", h.bones?[1]);
			fingers_[2] = new Finger(this, "middle", h.bones?[2]);
			fingers_[3] = new Finger(this, "ring", h.bones?[3]);
			fingers_[4] = new Finger(this, "little", h.bones?[4]);

			fist_ = new Morph(person_, h.fist, bodyPart_);
			inOut_ = new Morph(person_, h.inOut, bodyPart_);
		}

		public string Name
		{
			get { return name_; }
		}

		public int BodyPart
		{
			get { return bodyPart_; }
		}

		public Person Person
		{
			get { return person_; }
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
}
