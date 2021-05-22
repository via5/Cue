namespace Cue
{
	interface IGazeLookat
	{
		float Weight { get; set; }
		bool HasPosition { get; }
		Vector3 Position { get; }

		bool Next();
		void Update(Person p, float s);
	}


	abstract class BasicGazeLookat : IGazeLookat
	{
		private float weight_ = 0;

		protected BasicGazeLookat(float w = 0)
		{
			weight_ = w;
		}

		public float Weight
		{
			get { return weight_; }
			set { weight_ = value; }
		}

		public abstract bool HasPosition { get; }
		public abstract Vector3 Position { get; }

		public virtual bool Next()
		{
			// no-op
			return true;
		}

		public virtual void Update(Person p, float s)
		{
			// no-op
		}
	}


	class LookatNothing : BasicGazeLookat
	{
		public LookatNothing(Person p)
		{
		}

		public override bool HasPosition
		{
			get { return false; }
		}

		public override Vector3 Position
		{
			get { return Vector3.Zero; }
		}

		public override string ToString()
		{
			return "nothing";
		}
	}


	class LookatFront : BasicGazeLookat
	{
		private Vector3 pos_ = Vector3.Zero;

		public LookatFront(Person p)
		{
		}

		public override bool HasPosition
		{
			get { return true; }
		}

		public override Vector3 Position
		{
			get { return pos_; }
		}

		public override void Update(Person p, float s)
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


	class LookatObject : BasicGazeLookat
	{
		private IObject object_ = null;

		public LookatObject(Person p, IObject o, float weight)
			: base(weight)
		{
			object_ = o;
		}

		public override bool HasPosition
		{
			get { return (object_ != null); }
		}

		public override Vector3 Position
		{
			get { return object_.EyeInterest; }
		}

		public IObject Object
		{
			get { return object_; }
		}

		public override string ToString()
		{
			if (object_ == null)
				return "object (null)";
			else
				return $"object {object_}";
		}
	}


	class LookatPart : BasicGazeLookat
	{
		private Person person_;
		private BodyPart bodyPart_;

		public LookatPart(Person p, int bodyPart)
		{
			person_ = p;
			bodyPart_ = p.Body.Get(bodyPart);
		}

		public BodyPart BodyPart
		{
			get { return bodyPart_; }
		}

		public override bool HasPosition
		{
			get { return (bodyPart_ != null); }
		}

		public override Vector3 Position
		{
			get { return bodyPart_.Position; }
		}

		public override string ToString()
		{
			if (bodyPart_ == null)
				return "bodypart (null)";
			else
				return $"bodypart {bodyPart_.Person.ID} {bodyPart_}";
		}
	}


	class LookatPosition : BasicGazeLookat
	{
		private Vector3 pos_;

		public LookatPosition(Person p, Vector3 pos)
		{
			pos_ = pos;
		}

		public override bool HasPosition
		{
			get { return true; }
		}

		public override Vector3 Position
		{
			get { return pos_; }
		}

		public override string ToString()
		{
			return $"position {pos_}";
		}
	}


	class LookatRandomPoint : BasicGazeLookat
	{
		private Person person_;
		private Vector3 pos_ = Vector3.Zero;
		private bool hasPos_ = false;

		public LookatRandomPoint(Person p)
		{
			person_ = p;
		}

		public override bool HasPosition
		{
			get { return hasPos_; }
		}

		public override Vector3 Position
		{
			get { return pos_; }
		}

		public override bool Next()
		{
			var f = person_.Gaze.Generator.RandomAvailableFrustum();
			if (f.Empty)
			{
				person_.Log.Verbose($"lookat random: no available frustrums");
				return false;
			}

			var rp = f.RandomPoint();

			pos_ =
				person_.Body.Get(BodyParts.Eyes).Position +
				Vector3.Rotate(rp, person_.Body.Get(BodyParts.Chest).Direction);

			hasPos_ = true;

			return true;
		}

		public override string ToString()
		{
			if (hasPos_)
				return $"random {pos_}";
			else
				return "random (none)";
		}
	}
}
