using System.Collections.Generic;
using UnityEngine;

namespace Cue
{
	class ProceduralAnimations
	{
		public static List<Animation> Get()
		{
			var list = new List<Animation>();

			list.Add(Stand(PersonState.Walking));
			list.Add(Stand(PersonState.Standing));

			return list;
		}

		private static Animation Stand(int from)
		{
			var a = new ProceduralAnimation("stand");

			var neutral = a.AddStep();
			neutral.AddController("headControl", new Vector3(0, 1.6f, 0), new Vector3(0, 0, 0));
			neutral.AddController("chestControl", new Vector3(0, 1.4f, 0), new Vector3(20, 0, 0));
			neutral.AddController("hipControl", new Vector3(0, 1.1f, 0), new Vector3(340, 10, 0));
			neutral.AddController("lHandControl", new Vector3(-0.2f, 0.9f, 0), new Vector3(0, 10, 90));
			neutral.AddController("rHandControl", new Vector3(0.2f, 0.9f, 0), new Vector3(0, 0, 270));
			neutral.AddController("lFootControl", new Vector3(-0.1f, 0, 0), new Vector3(20, 10, 0));
			neutral.AddController("rFootControl", new Vector3(0.1f, 0, -0.1f), new Vector3(20, 10, 0));

			return new Animation(
				Animation.TransitionType,
				from, PersonState.Standing,
				PersonState.None, Sexes.Any, a);
		}
	}


	class ProceduralPlayer : IPlayer
	{
		private Person person_;
		private ProceduralAnimation proto_ = null;
		private ProceduralAnimation anim_ = null;

		public ProceduralPlayer(Person p)
		{
			person_ = p;
		}

		public bool Playing
		{
			get
			{
				return (anim_ != null);
			}
		}

		public bool Play(IAnimation a, int flags)
		{
			if (proto_ == a || anim_ == a)
				return true;

			anim_ = null;
			proto_ = (a as ProceduralAnimation);
			if (proto_ == null)
				return false;

			person_.Atom.SetDefaultControls("playing proc anim");

			anim_ = proto_.Clone();
			anim_.Start(person_);

			return true;
		}

		public void Stop(bool rewind)
		{
			anim_ = null;
			proto_ = null;
		}

		public void FixedUpdate(float s)
		{
			if (anim_ != null)
				anim_.FixedUpdate(s);
		}

		public void Update(float s)
		{
			if (anim_ != null)
			{
				anim_.Update(s);
				if (anim_.Done)
					Stop(false);
			}
		}

		public override string ToString()
		{
			return "Procedural: " + (anim_ == null ? "(none)" : anim_.ToString());
		}
	}



	class ProceduralStep
	{
		class Controller
		{
			private readonly string name_;
			private readonly UnityEngine.Vector3 pos_;
			private readonly UnityEngine.Vector3 rot_;
			private FreeControllerV3 fc_ = null;
			private UnityEngine.Vector3 startPos_;
			private UnityEngine.Vector3 endPos_;
			private Quaternion startRot_;
			private Quaternion endRot_;
			private float elapsed_ = 0;
			private bool done_ = false;

			public Controller(string name, Vector3 pos, Vector3 rot)
			{
				name_ = name;
				pos_ = W.VamU.ToUnity(pos);
				rot_ = W.VamU.ToUnity(rot);
			}

			public Controller Clone()
			{
				return new Controller(
					name_,
					W.VamU.FromUnity(pos_),
					W.VamU.FromUnity(rot_));
			}

			public bool Done
			{
				get { return done_; }
			}

			public void Start(Person p)
			{
				fc_ = Cue.Instance.VamSys.FindController(p.VamAtom.Atom, name_);
				if (fc_ == null)
				{
					Cue.LogError($"ProceduralStep: controller {name_} not found in {p}");
					return;
				}

				done_ = false;
				elapsed_ = 0;
				startPos_ = fc_.transform.localPosition;
				endPos_ = pos_ + new UnityEngine.Vector3(0, p.Clothing.HeelsHeight, 0);
				startRot_ = fc_.transform.localRotation;

				if (name_ == "lFootControl" || name_ == "rFootControl")
				{
					endRot_ = Quaternion.Euler(
						rot_ + new UnityEngine.Vector3(p.Clothing.HeelsAngle, 0, 0));
				}
				else
				{
					endRot_ = Quaternion.Euler(rot_);
				}
			}

			public void Reset()
			{
			}

			public static UnityEngine.Vector3 Bezier2(
				UnityEngine.Vector3 s,
				UnityEngine.Vector3 p,
				UnityEngine.Vector3 e,
				float t)
			{
				float rt = 1 - t;
				return rt * rt * s + 2 * rt * t * p + t * t * e;
			}

			public void Update(float s)
			{
				if (fc_ == null)
				{
					done_ = true;
					return;
				}

				elapsed_ += s;

				float t = U.Clamp(elapsed_, 0, 1);

				var mid = startPos_ + (endPos_ - startPos_) / 2 + new UnityEngine.Vector3(0, 0.3f, 0);

				if (name_ == "lFootControl" || name_ == "rFootControl")
				{
					fc_.transform.localPosition =
						Bezier2(startPos_, mid, endPos_, t);
				}
				else
				{
					fc_.transform.localPosition =
						UnityEngine.Vector3.Lerp(startPos_, endPos_, t);
				}

				fc_.transform.localRotation =
					Quaternion.Lerp(startRot_, endRot_, t);

				if (t >= 1)
					done_ = true;
			}
		}


		private readonly List<Controller> cs_ = new List<Controller>();
		private bool done_ = false;

		public ProceduralStep Clone()
		{
			var s = new ProceduralStep();

			foreach (var c in cs_)
				s.cs_.Add(c.Clone());

			return s;
		}

		public bool Done
		{
			get { return done_; }
		}

		public void Start(Person p)
		{
			done_ = false;
			for (int i = 0; i < cs_.Count; ++i)
				cs_[i].Start(p);
		}

		public void AddController(string name, Vector3 pos, Vector3 rot)
		{
			cs_.Add(new Controller(name, pos, rot));
		}

		public void Reset()
		{
			for (int i = 0; i < cs_.Count; ++i)
				cs_[i].Reset();
		}

		public void Update(float s)
		{
			for (int i = 0; i < cs_.Count; ++i)
			{
				cs_[i].Update(s);
				done_ = done_ || cs_[i].Done;
			}
		}
	}


	class ProceduralAnimation : IAnimation
	{
		class Part
		{
			private Rigidbody rb_ = null;
			private Vector3 force_;
			private float time_;
			private float elapsed_ = 0;
			private bool fwd_ = true;

			public Part(Rigidbody rb, Vector3 f, float t)
			{
				rb_ = rb;
				force_ = f;
				time_ = t;
			}

			public void Reset()
			{
				elapsed_ = 0;
				fwd_ = true;
			}

			public void Update(float s)
			{
				elapsed_ += s;
				if (elapsed_ >= time_)
				{
					elapsed_ = 0;
					fwd_ = !fwd_;
				}

				float p = (fwd_ ? (elapsed_ / time_) : ((time_ - elapsed_) / time_));
				var f = force_ * p;

				rb_.AddForce(W.VamU.ToUnity(f));
			}

			public override string ToString()
			{
				return rb_.name;
			}
		}


		//private Person person_ = null;
		private readonly string name_;
		private readonly List<ProceduralStep> steps_ = new List<ProceduralStep>();
		//private readonly List<Part> parts_ = new List<Part>();

		public ProceduralAnimation( string name)
		{
			name_ = name;
		}

		public ProceduralAnimation Clone()
		{
			var a = new ProceduralAnimation(name_);

			foreach (var s in steps_)
				a.steps_.Add(s.Clone());

			return a;
		}

		public bool Done
		{
			get { return steps_[0].Done; }
		}

		public ProceduralStep AddStep()
		{
			var s = new ProceduralStep();
			steps_.Add(s);
			return s;
		}

		public void Start(Person p)
		{
			for (int i = 0; i < steps_.Count; ++i)
				steps_[i].Start(p);
		}

		//public void Add(string rbId, Vector3 f, float time)
		//{
		//	var rb = Cue.Instance.VamSys.FindRigidbody(
		//		((W.VamAtom)person_.Atom).Atom, rbId);
		//
		//	parts_.Add(new Part(rb, f, time));
		//}

		public void Reset()
		{
			for (int i = 0; i < steps_.Count; ++i)
				steps_[i].Reset();

			//for (int i = 0; i < parts_.Count; ++i)
			//	parts_[i].Reset();
		}

		public void Update(float s)
		{
			steps_[0].Update(s);

			//for (int i = 0; i < parts_.Count; ++i)
			//	parts_[i].Update(s);
		}

		public void FixedUpdate(float s)
		{
		}

		public override string ToString()
		{
			string s = name_;

			//if (parts_.Count == 0)
			//	s += " (empty)";
			//else
			//	s += " (" + parts_[0].ToString() + ")";

			return s;
		}
	}


	class Duration
	{
		private float min_, max_;
		private float current_ = 0;
		private float elapsed_ = 0;
		private bool finished_ = false;

		public Duration(float min, float max)
		{
			min_ = min;
			max_ = max;
			Next();
		}

		public bool Finished
		{
			get { return finished_; }
		}

		public bool Enabled
		{
			get { return min_ > 0 && max_ > 0; }
		}

		public float Progress
		{
			get { return U.Clamp(elapsed_ / current_, 0, 1); }
		}

		public void Reset()
		{
			elapsed_ = 0;
			finished_ = false;
			Next();
		}

		public void Update(float s)
		{
			if (finished_)
			{
				finished_ = false;
				elapsed_ = 0;
			}

			elapsed_ += s;
			if (elapsed_ > current_)
			{
				finished_ = true;
				Next();
			}
		}

		public override string ToString()
		{
			return $"{current_:0.##}/({min_:0.##},{max_:0.##})";
		}

		private void Next()
		{
			current_ = U.RandomFloat(min_, max_);
		}
	}
}
