namespace Cue
{
	interface IGazeLookat
	{
		bool HasPosition { get; }
		Vector3 Position { get; }
		bool EnableGaze { get; }
		void Update(Person p, float s);
	}


	class LookatNothing : IGazeLookat
	{
		public LookatNothing(Person p)
		{
		}

		public bool HasPosition
		{
			get { return false; }
		}

		public Vector3 Position
		{
			get { return Vector3.Zero; }
		}

		public bool EnableGaze
		{
			get { return false; }
		}

		public void Update(Person p, float s)
		{
			// no-op
		}

		public override string ToString()
		{
			return "nothing";
		}
	}


	class LookatFront : IGazeLookat
	{
		private Vector3 pos_ = Vector3.Zero;

		public LookatFront(Person p)
		{
		}

		public bool HasPosition
		{
			get { return true; }
		}

		public Vector3 Position
		{
			get { return pos_; }
		}

		public bool EnableGaze
		{
			get { return true; }
		}

		public void Update(Person p, float s)
		{
			pos_ =
				p.Body.Get(BodyParts.Eyes).Position +
				Vector3.Rotate(new Vector3(0, 0, 1), p.Bearing);
		}

		public override string ToString()
		{
			return "front";
		}
	}


	class LookatObject : IGazeLookat
	{
		private IObject object_ = null;
		private bool gaze_;

		public LookatObject(Person p, IObject o, bool gaze)
		{
			object_ = o;
			gaze_ = gaze;
		}

		public bool HasPosition
		{
			get { return (object_ != null); }
		}

		public Vector3 Position
		{
			get { return object_.EyeInterest; }
		}

		public bool EnableGaze
		{
			get { return gaze_; }
		}

		public void Update(Person p, float s)
		{
			// no-op
		}

		public void Set(IObject o, bool gaze)
		{
			object_ = o;
			gaze_ = gaze;
		}

		public override string ToString()
		{
			if (object_ == null)
				return "object (null)";
			else
				return $"object {object_}";
		}
	}


	class LookatPosition : IGazeLookat
	{
		private Vector3 pos_;

		public LookatPosition(Person p, Vector3 pos)
		{
			pos_ = pos;
		}

		public bool HasPosition
		{
			get { return true; }
		}

		public Vector3 Position
		{
			get { return pos_; }
		}

		public bool EnableGaze
		{
			get { return true; }
		}

		public void Update(Person p, float s)
		{
			// no-op
		}

		public override string ToString()
		{
			return $"position {pos_}";
		}
	}


	abstract class BasicLookatRandom : IGazeLookat
	{
		protected RandomTargetGenerator random_ = null;
		private bool gaze_ = true;

		protected BasicLookatRandom(Person p, bool gaze)
		{
			random_ = new RandomTargetGenerator(p);
			gaze_ = gaze;
		}

		public bool HasPosition
		{
			get { return random_?.HasTarget ?? false; }
		}

		public Vector3 Position
		{
			get { return random_?.Position ?? Vector3.Zero; }
		}

		public bool EnableGaze
		{
			get { return gaze_; }
			set { gaze_ = value; }
		}

		public void Update(Person p, float s)
		{
			random_.Update(s);
		}
	}


	class LookatParts : BasicLookatRandom
	{
		public LookatParts(Person p, Person target, int[] bodyParts, bool gaze)
			: base(p, gaze)
		{
			random_.SetTargets(new IRandomTarget[]
			{
				new BodyPartsTarget(target, bodyParts)
			});
		}

		public void Set(Person target, int[] parts, bool gaze)
		{
			(random_.Targets[0] as BodyPartsTarget).Set(target, parts);
			EnableGaze = gaze;
		}

		public override string ToString()
		{
			return $"random parts {random_}";
		}
	}


	class LookatRandom : BasicLookatRandom
	{
		public LookatRandom(Person p, bool gaze)
			: base(p, gaze)
		{
			random_.SetTargets(new IRandomTarget[]
			{
				new RandomPointTarget(),
				new SexTarget(),
				new BodyPartsTarget(null, new int[]
				{
					BodyParts.LeftBreast,
					BodyParts.RightBreast,
					BodyParts.Pectorals,
					BodyParts.Genitals
				}),
				new EyeContactTarget()
			});
		}

		public override string ToString()
		{
			return $"random {random_}";
		}
	}
}
