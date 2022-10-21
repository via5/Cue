using System.Collections.Generic;

namespace Cue
{
	// all the possible targets in the scene for a particular person
	//
	public class GazeTargets
	{
		public const float ExclusiveWeight = -1;

		struct ObjectInfo
		{
			private bool reluctant_;
			private bool avoid_;
			private float weight_;
			private string whyReluctant_;
			private string whyAvoid_;

			public bool Reluctant
			{
				get { return reluctant_; }
			}

			public bool Avoid
			{
				get { return avoid_; }
			}

			public float Weight
			{
				get { return weight_; }
			}

			public string WhyReluctant
			{
				get { return whyReluctant_; }
			}

			public string WhyAvoid
			{
				get { return whyAvoid_; }
			}

			public void SetReluctant(bool reluctant, float weight, string why)
			{
				reluctant_ = reluctant;
				weight_ = weight;
				whyReluctant_ = why;
			}

			public void SetAvoid(bool avoid, string why)
			{
				avoid_ = avoid;
				whyAvoid_ = why;
			}

			public void Clear()
			{
				reluctant_ = false;
				avoid_ = false;
				weight_ = 0;
				whyReluctant_ = "";
				whyAvoid_ = "";
			}
		}

		private Person person_;

		// per body part, per person
		private LookatPart[,] bodyParts_ = new LookatPart[0, 0];

		// picks a random point
		private LookatRandomPoint random_;

		// per object
		private LookatObject[] objects_ = new LookatObject[0];

		// point above head
		private LookatAbove above_;

		// point in front
		private LookatFront front_;

		// all of the above in one array
		private IGazeLookat[] all_ = new IGazeLookat[0];

		// an avoidance/reluctance flag per object, indexed per ObjectIndex
		private ObjectInfo[] infos_ = new ObjectInfo[0];

		// temporary avoid
		private IObject tempAvoid_ = null;
		private float tempAvoidTime_ = 0;
		private float tempAvoidElapsed_ = 0;


		public GazeTargets(Person p)
		{
			person_ = p;
			random_ = new LookatRandomPoint(p);
			above_ = new LookatAbove(p);
			front_ = new LookatFront(p);
		}

		public Logger Log
		{
			get { return person_.Gaze.Log; }
		}

		public IGazeLookat LookatAbove
		{
			get { return above_; }
		}

		public IGazeLookat LookatFront
		{
			get { return front_; }
		}

		public IGazeLookat RandomPoint
		{
			get { return random_; }
		}

		public void Init()
		{
			bodyParts_ = new LookatPart[
				Cue.Instance.AllPersons.Count, BP.Count];

			for (int pi = 0; pi < Cue.Instance.AllPersons.Count; ++pi)
			{
				var p = Cue.Instance.AllPersons[pi];

				foreach (BodyPartType bi in BodyPartType.Values)
					bodyParts_[pi, bi.Int] = new LookatPart(person_, p.Body.Get(bi));
			}

			objects_ = new LookatObject[Cue.Instance.Everything.Count];
			for (int oi = 0; oi < objects_.Length; ++oi)
				objects_[oi] = new LookatObject(person_, Cue.Instance.Everything[oi]);

			all_ = GetAll();
			infos_ = new ObjectInfo[Cue.Instance.Everything.Count];
		}

		public IGazeLookat[] All
		{
			get { return all_; }
		}

		public IGazeLookat GetEyes(int personIndex)
		{
			return bodyParts_[personIndex, BP.Eyes.Int];
		}

		private IGazeLookat[] GetAll()
		{
			var all = new IGazeLookat[
				bodyParts_.Length +
				1 +  // random
				1 +  // above
				1 +  // front
				objects_.Length];

			int i = 0;

			for (int pi = 0; pi < bodyParts_.GetLength(0); ++pi)
			{
				for (int bi = 0; bi < bodyParts_.GetLength(1); ++bi)
					all[i++] = bodyParts_[pi, bi];
			}

			all[i++] = random_;
			all[i++] = above_;
			all[i++] = front_;

			for (int oi = 0; oi < objects_.Length; ++oi)
				all[i++] = objects_[oi];

			return all;
		}

		public void Clear()
		{
			for (int i = 0; i < all_.Length; ++i)
				all_[i].Clear();

			for (int i = 0; i < infos_.Length; ++i)
				infos_[i].Clear();
		}

		public void Update(float s)
		{
			if (tempAvoid_ != null)
			{
				tempAvoidElapsed_ += s;
				if (tempAvoidElapsed_ >= tempAvoidTime_)
				{
					tempAvoid_ = null;
					tempAvoidTime_ = 0;
					tempAvoidElapsed_ = 0;
				}
			}
		}

		public void SetTemporaryAvoid(IObject p, float time)
		{
			tempAvoid_ = p;
			tempAvoidTime_ = time;
			tempAvoidElapsed_ = 0;
		}

		public IObject TemporaryAvoidTarget
		{
			get { return tempAvoid_; }
		}

		public float TemporaryAvoidTime
		{
			get { return tempAvoidTime_; }
		}

		public float TemporaryAvoidElapsed
		{
			get { return tempAvoidElapsed_; }
		}

		public bool IsReluctant(IObject o)
		{
			return infos_[o.ObjectIndex].Reluctant;
		}

		public bool ShouldAvoid(IObject o)
		{
			if (tempAvoid_ == o)
				return true;

			return infos_[o.ObjectIndex].Avoid;
		}

		public void SetReluctant(IObject o, bool b, float weight, string why)
		{
			infos_[o.ObjectIndex].SetReluctant(b, weight, why);

			if (o is Person && weight > 0)
				SetWeightIfZero(o as Person, BP.Eyes, weight, why + " (from reluctant)");
		}

		//public void SetShouldAvoid(IObject o, bool b, float weight, string why)
		//{
		//	avoid_[o.ObjectIndex].Set(b, weight, why);
		//
		//	if (o is Person && weight > 0)
		//		SetWeightIfZero(o as Person, BP.Eyes, weight, why + " (from avoid)");
		//}

		public void SetWeightIfZero(Person p, BodyPartType bodyPart, float w, string why)
		{
			if (bodyParts_[p.PersonIndex, bodyPart.Int].Weight == 0)
				bodyParts_[p.PersonIndex, bodyPart.Int].SetWeight(w, why);
		}

		public void SetWeight(Person p, BodyPartType bodyPart, float w, string why)
		{
			bodyParts_[p.PersonIndex, bodyPart.Int].SetWeight(w, why);
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

		public void SetFrontWeight(float w, string why)
		{
			front_.SetWeight(w, why);
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

			Log.Error($"SetObjectWeight: {o} not in list");
		}

		public List<string> GetAllInfosForDebug()
		{
			var list = new List<string>();

			for (int i = 0; i < infos_.Length; ++i)
			{
				string s = "";

				if (infos_[i].Reluctant)
					s += $"reluctant w={infos_[i].Weight} ({infos_[i].WhyReluctant})";

				if (infos_[i].Avoid)
				{
					if (s != "")
						s += ", ";

					s += $"avoid ({infos_[i].WhyAvoid})";
				}

				if (s != "")
					s = $"{Cue.Instance.Everything[i]} {s}";

				if (s != "")
					list.Add(s);
			}

			return list;
		}
	}


	public interface IGazeLookat
	{
		bool Exclusive { get; }
		float Weight { get; }
		string Why { get; }
		string Failure { get; }
		bool WasSet { get; }

		IObject Object { get; }
		bool Idling { get; }

		bool HasPosition { get; }
		Vector3 Position { get; }
		float Variance { get; }

		bool Reluctant { get; }

		void Clear();
		void SetWeight(float f, string why);
		void SetFailed(string why);
		bool Next();
	}


	abstract class BasicGazeLookat : IGazeLookat
	{
		protected Person person_;
		private float weight_ = 0;
		private string why_, failure_;
		private bool set_ = false;

		protected BasicGazeLookat(Person p)
		{
			person_ = p;
		}

		public Logger Log
		{
			get { return person_.Gaze.Log; }
		}

		public bool Exclusive
		{
			get { return (weight_ < 0); }
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

		public virtual bool Idling
		{
			get { return false; }
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

		public virtual IObject Object
		{
			get { return null; }
		}

		public abstract bool HasPosition { get; }
		public abstract Vector3 Position { get; }

		public virtual float Variance
		{
			get { return 1; }
		}

		public virtual bool Reluctant
		{
			get
			{
				if (Object != null)
					return person_.Gaze.Targets.IsReluctant(Object);

				return false;
			}
		}

		public virtual bool Next()
		{
			// no-op
			return true;
		}
	}


	class LookatObject : BasicGazeLookat
	{
		private IObject object_ = null;

		public LookatObject(Person p, IObject o)
			: base(p)
		{
			object_ = o;
		}

		public override IObject Object
		{
			get { return object_; }
		}

		public override bool HasPosition
		{
			get { return (object_ != null); }
		}

		public override Vector3 Position
		{
			get { return object_.EyeInterest; }
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
		private BodyPart bodyPart_;

		public LookatPart(Person p, BodyPart bp)
			: base(p)
		{
			bodyPart_ = bp;
		}

		public override IObject Object
		{
			get { return bodyPart_.Person; }
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
				return $"bodypart {bodyPart_.Person.ID}.{bodyPart_.Name}";
		}
	}


	class LookatAbove : BasicGazeLookat
	{
		public LookatAbove(Person p)
			: base(p)
		{
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
			: base(p)
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


	class LookatFront : BasicGazeLookat
	{
		public LookatFront(Person p)
			: base(p)
		{
		}

		public override bool HasPosition
		{
			get { return true; }
		}

		public override Vector3 Position
		{
			get
			{
				var h =  person_.Body.Get(BP.Head);
				var p = h.Position + h.Rotation.Rotate(new Vector3(0, 0, 1));
				return p;
			}
		}

		public override string ToString()
		{
			return $"look at front {Position}";
		}
	}


	class LookatRandomPoint : BasicGazeLookat
	{
		private Vector3 pos_ = Vector3.Zero;
		private bool hasPos_ = false;

		public LookatRandomPoint(Person p)
			: base(p)
		{
		}

		public override bool Idling
		{
			get { return true; }
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
				Log.Verbose($"lookat random: no available frustrums");
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
