using System.Collections.Generic;

namespace Cue
{
	class GazeTargets
	{
		private Person person_;

		private LookatPart[,] persons_ = new LookatPart[0, 0];
		private LookatFront front_;
		private LookatNothing nothing_;
		private LookatRandomPoint random_;
		private LookatObject[] objects_ = new LookatObject[0];
		private IGazeLookat[] all_ = new IGazeLookat[0];

		public GazeTargets(Person p)
		{
			person_ = p;
			front_ = new LookatFront(p);
			nothing_ = new LookatNothing(p);
			random_ = new LookatRandomPoint(p);
		}

		public void Init()
		{
			persons_ = new LookatPart[
				Cue.Instance.Persons.Count, BodyParts.Count];

			for (int pi = 0; pi < Cue.Instance.Persons.Count; ++pi)
			{
				var p = Cue.Instance.Persons[pi];

				for (int bi = 0; bi < BodyParts.Count; ++bi)
					persons_[pi, bi] = new LookatPart(p, bi);
			}

			objects_ = new LookatObject[Cue.Instance.AllObjects.Count];
			for (int oi = 0; oi < objects_.Length; ++oi)
				objects_[oi] = new LookatObject(person_, Cue.Instance.AllObjects[oi], 0);

			all_ = GetAll();
		}

		public IGazeLookat[] All
		{
			get { return all_; }
		}

		private IGazeLookat[] GetAll()
		{
			var all = new IGazeLookat[
				persons_.Length +
				1 +  // front
				1 +  // nothing
				1 +  // random
				objects_.Length];

			int i = 0;

			for (int pi = 0; pi < persons_.GetLength(0); ++pi)
			{
				for (int bi = 0; bi < persons_.GetLength(1); ++bi)
					all[i++] = persons_[pi, bi];
			}

			all[i++] = front_;
			all[i++] = nothing_;
			all[i++] = random_;

			for (int oi = 0; oi < objects_.Length; ++oi)
				all[i++] = objects_[oi];

			return all;
		}

		public void Clear()
		{
			for (int i = 0; i < all_.Length; ++i)
				all_[i].Weight = 0;
		}

		public void SetWeight(Person p, int bodyPart, float w)
		{
			persons_[p.PersonIndex, bodyPart].Weight = w;
		}

		public void SetRandomWeight(float w)
		{
			random_.Weight = w;
		}

		public void SetObjectWeight(IObject o, float w)
		{
			for (int i = 0; i < objects_.Length; ++i)
			{
				if (objects_[i].Object == o)
				{
					objects_[i].Weight = w;
					return;
				}
			}

			Cue.LogError($"SetObjectWeight: {o} not in list");
		}

		public void SetFrontWeight(float w)
		{
			front_.Weight = w;
		}
	}


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
			var f = person_.Gaze.Picker.RandomAvailableFrustum();
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
