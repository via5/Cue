using System.Collections.Generic;

namespace Cue
{
	// all the possible targets in the scene for a particular person
	//
	class GazeTargets
	{
		struct AvoidInfo
		{
			public bool avoid;
			public string why;

			public void Clear()
			{
				avoid = false;
				why = "";
			}
		}

		private Person person_;

		// per body part, per person
		private LookatPart[,] bodyParts_ = new LookatPart[0, 0];

		// used mostly when moving
		private LookatFront front_;

		// no-op
		private LookatNothing nothing_;

		// picks a random point
		private LookatRandomPoint random_;

		// per object
		private LookatObject[] objects_ = new LookatObject[0];

		// point above head
		private LookatAbove above_;

		// all of the above in one array
		private IGazeLookat[] all_ = new IGazeLookat[0];

		// an avoidance flag per object, indexed per ObjectIndex
		private AvoidInfo[] avoid_ = new AvoidInfo[0];


		public GazeTargets(Person p)
		{
			person_ = p;
			front_ = new LookatFront(p);
			nothing_ = new LookatNothing(p);
			random_ = new LookatRandomPoint(p);
			above_ = new LookatAbove(p);
		}

		public IGazeLookat LookatAbove
		{
			get { return above_; }
		}

		public void Init()
		{
			bodyParts_ = new LookatPart[
				Cue.Instance.AllPersons.Count, BP.Count];

			for (int pi = 0; pi < Cue.Instance.AllPersons.Count; ++pi)
			{
				var p = Cue.Instance.AllPersons[pi];

				for (int bi = 0; bi < BP.Count; ++bi)
					bodyParts_[pi, bi] = new LookatPart(p, bi);
			}

			objects_ = new LookatObject[Cue.Instance.Everything.Count];
			for (int oi = 0; oi < objects_.Length; ++oi)
				objects_[oi] = new LookatObject(person_, Cue.Instance.Everything[oi]);

			all_ = GetAll();
			avoid_ = new AvoidInfo[Cue.Instance.Everything.Count];
		}

		public IGazeLookat[] All
		{
			get { return all_; }
		}

		private IGazeLookat[] GetAll()
		{
			var all = new IGazeLookat[
				bodyParts_.Length +
				1 +  // front
				1 +  // nothing
				1 +  // random
				1 +  // above
				objects_.Length];

			int i = 0;

			for (int pi = 0; pi < bodyParts_.GetLength(0); ++pi)
			{
				for (int bi = 0; bi < bodyParts_.GetLength(1); ++bi)
					all[i++] = bodyParts_[pi, bi];
			}

			all[i++] = front_;
			all[i++] = nothing_;
			all[i++] = random_;
			all[i++] = above_;

			for (int oi = 0; oi < objects_.Length; ++oi)
				all[i++] = objects_[oi];

			return all;
		}

		public void Clear()
		{
			for (int i = 0; i < all_.Length; ++i)
				all_[i].Clear();

			for (int i = 0; i < avoid_.Length; ++i)
				avoid_[i].Clear();
		}

		public bool ShouldAvoid(IObject o)
		{
			return avoid_[o.ObjectIndex].avoid;
		}

		public void SetShouldAvoid(IObject o, bool b, string why)
		{
			avoid_[o.ObjectIndex].avoid = b;
			avoid_[o.ObjectIndex].why = why;
		}

		public void SetWeightIfZero(Person p, int bodyPart, float w, string why)
		{
			if (bodyParts_[p.PersonIndex, bodyPart].Weight == 0)
				bodyParts_[p.PersonIndex, bodyPart].SetWeight(w, why);
		}

		public void SetWeight(Person p, int bodyPart, float w, string why)
		{
			bodyParts_[p.PersonIndex, bodyPart].SetWeight(w, why);
		}

		public void SetRandomWeight(float w, string why)
		{
			random_.SetWeight(w, why);
		}

		public void SetRandomWeightIfZero(float w, string why)
		{
			if (random_.Weight == 0)
				random_.SetWeight(w, why);
		}

		public void SetAboveWeight(float w, string why)
		{
			above_.SetWeight(w, why);
		}

		public void SetObjectWeight(IObject o, float w, string why)
		{
			for (int i = 0; i < objects_.Length; ++i)
			{
				if (objects_[i].Object == o)
				{
					objects_[i].SetWeight(w, why);
					return;
				}
			}

			Cue.LogError($"SetObjectWeight: {o} not in list");
		}

		public void SetFrontWeight(float w, string why)
		{
			front_.SetWeight(w, why);
		}

		public List<string> GetAllAvoidForDebug()
		{
			var list = new List<string>();

			for (int i = 0; i < avoid_.Length; ++i)
			{
				if (avoid_[i].avoid)
					list.Add($"{Cue.Instance.Everything[i]}, {avoid_[i].why}");
			}

			return list;
		}
	}


	interface IGazeLookat
	{
		float Weight { get; }
		string Why { get; }
		string Failure { get; }
		bool WasSet { get; }

		bool HasPosition { get; }
		Vector3 Position { get; }
		float Variance { get; }

		void Clear();
		void SetWeight(float f, string why);
		void SetFailed(string why);
		bool Next();
		void Update(Person p, float s);
	}


	abstract class BasicGazeLookat : IGazeLookat
	{
		private float weight_ = 0;
		private string why_, failure_;
		private bool set_ = false;

		protected BasicGazeLookat()
		{
		}

		public float Weight
		{
			get { return weight_; }
		}

		public string Why
		{
			get { return why_; }
		}

		public string Failure
		{
			get { return failure_; }
		}

		public bool WasSet
		{
			get { return set_; }
		}

		public void Clear()
		{
			weight_ = 0;
			why_ = "";
			failure_ = "";
			set_ = false;
		}

		public void SetFailed(string why)
		{
			failure_ = why;
		}

		public void SetWeight(float f, string why)
		{
			weight_ = f;
			why_ = why;
			set_ = true;
		}

		public abstract bool HasPosition { get; }
		public abstract Vector3 Position { get; }

		public virtual float Variance
		{
			get { return 1; }
		}

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
			return "look at nothing";
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
				p.Body.Get(BP.Eyes).Position +
				p.Rotation.Rotate(new Vector3(0, 0, 1));
		}

		public override string ToString()
		{
			return "look in front";
		}
	}


	class LookatObject : BasicGazeLookat
	{
		private IObject object_ = null;

		public LookatObject(Person p, IObject o)
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


	class LookatAbove : BasicGazeLookat
	{
		private Person person_;

		public LookatAbove(Person p)
		{
			person_ = p;
		}

		public override bool HasPosition
		{
			get { return true; }
		}

		public override Vector3 Position
		{
			get
			{
				var h = person_.Body.Get(BP.Head);
				var c = person_.Body.Get(BP.Chest);
				var d = new Vector3(0, 0.5f, 0.05f);
				var p = h.Position + c.Rotation.Rotate(d);

				return p;
			}
		}

		public override float Variance
		{
			get { return 0; }
		}

		public override string ToString()
		{
			return $"look above";
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
			return $"look at position {pos_}";
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
			var eyes = person_.Body.Get(BP.Eyes);
			var chest = person_.Body.Get(BP.Chest);

			pos_ = eyes.Position + chest.Rotation.Rotate(rp);
			hasPos_ = true;

			return true;
		}

		public override string ToString()
		{
			if (hasPos_)
				return $"random point {pos_}";
			else
				return "random point (none)";
		}
	}
}
