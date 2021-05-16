using System.Collections.Generic;

namespace Cue
{
	class RandomTargetGenerator
	{
		public class FrustumInfo
		{
			public Frustum frustum;
			public bool avoid;
			public bool selected;

			public FrustumInfo(Person p, Frustum f)
			{
				frustum = f;
				avoid = false;
				selected = false;
			}
		}

		private Vector3 Near = new Vector3(2, 1, 0.1f);
		private Vector3 Far = new Vector3(6, 3, 2);

		public const int XCount = 5;
		public const int YCount = 5;
		public const int FrustumCount = XCount * YCount;

		private Person person_;
		private Logger log_;
		private FrustumInfo[] frustums_ = new FrustumInfo[FrustumCount];
		private Box avoidBox_ = Box.Zero;
		private RandomTargetGeneratorRenderer render_ = null;
		private Duration delay_ = new Duration(0, 0);
		private IRandomTarget[] targets_ = new IRandomTarget[0];
		private int currentTarget_ = -1;

		public RandomTargetGenerator(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.AI, person_, "RandomLookAt");

			var main = new Frustum(Near, Far);
			var fs = main.Split(XCount, YCount);

			for (int i = 0; i < fs.Length; ++i)
				frustums_[i] = new FrustumInfo(p, fs[i]);

			//render_ = new RandomTargetGeneratorRenderer(this);
		}

		public BodyPart ReferencePart
		{
			get { return person_.Body.Get(BodyParts.Chest); }
		}

		public Person Person
		{
			get { return person_; }
		}

		public Logger Log
		{
			get { return log_; }
		}

		public FrustumInfo GetFrustum(int i)
		{
			return frustums_[i];
		}

		public Box AvoidBox
		{
			get { return avoidBox_; }
		}

		public bool HasTarget
		{
			get { return currentTarget_ >= 0 && currentTarget_ < targets_.Length; }
		}

		public Vector3 Position
		{
			get
			{
				if (HasTarget)
					return targets_[currentTarget_].Position;
				else
					return Vector3.Zero;
			}
		}

		public void SetTargets(IRandomTarget[] t)
		{
			targets_ = t;
		}

		public IRandomTarget[] Targets
		{
			get { return targets_; }
		}

		public bool CanLookAt(Vector3 p)
		{
			var box = new Box(p, new Vector3(0.01f, 0.01f, 0.01f));

			for (int i = 0; i < frustums_.Length; ++i)
			{
				if (frustums_[i].avoid)
				{
					if (frustums_[i].frustum.TestPlanesAABB(box))
						return false;
				}
			}

			return true;
		}

		public void Reset()
		{
			currentTarget_ = -1;

			for (int i = 0; i < targets_.Length; ++i)
				targets_[i].Reset();
		}

		public bool Update(float s)
		{
			delay_.SetRange(person_.Personality.LookAtRandomInterval);
			//delay_.SetRange(1, 1);
			delay_.Update(s);

			if (delay_.Finished || !HasTarget)
			{
				NextTarget();
				return true;
			}
			else if (HasTarget)
			{
				if (!CanLookAt(targets_[currentTarget_].Position))
				{
					NextTarget();
					return true;
				}
			}

			render_?.Update(s);

			return false;
		}

		public override string ToString()
		{
			if (HasTarget)
				return targets_[currentTarget_].ToString();
			else
				return "no target";
		}

		private void NextTarget()
		{
			ResetFrustums();
			UpdateFrustums();


			float[] weights = new float[targets_.Length];
			float total = 0;

			for (int i = 0; i < targets_.Length; ++i)
			{
				weights[i] = person_.Personality.GazeRandomTargetWeight(
					targets_[i].Type);

				total += weights[i];
			}


			for (int i = 0; i < 10; ++i)
			{
				var r = U.RandomFloat(0, total);
				for (int j = 0; j < weights.Length; ++j)
				{
					if (r < weights[j])
					{
						log_.Verbose($"trying {(object)targets_[j]}");

						if (targets_[j].NextPosition(this))
						{
							log_.Verbose($"picked {targets_[j]}");
							currentTarget_ = j;
							return;
						}

						break;
					}

					r -= weights[j];
				}
			}

			log_.Error($"no valid target");
		}

		private void ResetFrustums()
		{
			for (int i = 0; i < frustums_.Length; ++i)
			{
				frustums_[i].avoid = false;
				frustums_[i].selected = false;
			}
		}

		private int UpdateFrustums()
		{
			if (person_.Gaze.Avoid == null)
				return frustums_.Length;

			UpdateAvoidBox();

			int av = 0;

			for (int i = 0; i < frustums_.Length; ++i)
			{
				if (frustums_[i].frustum.TestPlanesAABB(avoidBox_))
				{
					frustums_[i].avoid = true;
				}
				else
				{
					frustums_[i].avoid = false;
					++av;
				}
			}

			return av;
		}

		private void UpdateAvoidBox()
		{
			var selfRef = person_.Body.Get(BodyParts.Eyes);
			var avoidP = person_.Gaze.Avoid as Person;

			if (avoidP == null)
			{
				avoidBox_ = new Box(
					person_.Gaze.Avoid.EyeInterest - selfRef.Position,
					new Vector3(0.2f, 0.2f, 0.2f));
			}
			else
			{
				var avoidRef = avoidP.Body.Get(BodyParts.Eyes);

				var q = ReferencePart.Direction;

				var avoidHeadU =
					avoidRef.Position -
					selfRef.Position +
					new Vector3(0, 0.2f, 0);

				var avoidHipU =
					avoidP.Body.Get(BodyParts.Hips).Position -
					selfRef.Position;

				var avoidHead = Vector3.RotateInv(avoidHeadU, q);
				var avoidHip = Vector3.RotateInv(avoidHipU, q);

				avoidBox_ = new Box(
					avoidHip + (avoidHead - avoidHip) / 2,
					new Vector3(0.5f, (avoidHead - avoidHip).Y, 0.5f));
			}
		}

		public Frustum RandomAvailableFrustum()
		{
			int av = 0;

			for (int i = 0; i < frustums_.Length; ++i)
			{
				if (!frustums_[i].avoid)
					++av;
			}


			int fi = U.RandomInt(0, av - 1);

			for (int i = 0; i < frustums_.Length; ++i)
			{
				if (frustums_[i].avoid)
					continue;

				if (fi == 0)
					return frustums_[i].frustum;

				--fi;
			}

			return Frustum.Zero;
		}
	}


	static class RandomTargetTypes
	{
		public const int Sex = 1;
		public const int Body = 2;
		public const int Eyes = 3;
		public const int Random = 4;
	}


	interface IRandomTarget
	{
		int Type { get; }
		Vector3 Position { get; }

		void Reset();
		bool NextPosition(RandomTargetGenerator r);
	}


	abstract class RandomBodyPartTarget : IRandomTarget
	{
		private BodyPart part_ = null;

		public RandomBodyPartTarget()
		{
		}

		public abstract int Type { get; }

		public Vector3 Position
		{
			get { return part_?.Position ?? Vector3.Zero; }
		}

		public virtual void Reset()
		{
		}

		public abstract bool NextPosition(RandomTargetGenerator r);

		protected bool CheckTargets(RandomTargetGenerator r, List<BodyPart> parts)
		{
			if (parts.Count == 0)
			{
				r.Log.Verbose("no parts found");
				return false;
			}

			U.Shuffle(parts);

			for (int i = 0; i < parts.Count; ++i)
			{
				if (r.CanLookAt(parts[i].Position))
				{
					part_ = parts[i];
					r.Log.Verbose($"using {parts[i]}");
					return true;
				}
				else
				{
					r.Log.Verbose($"can't look at {parts[i]}");
				}
			}

			r.Log.Verbose($"all parts failed");
			return false;
		}

		public override string ToString()
		{
			return part_?.ToString() ?? "(null)";
		}
	}


	class SexTarget : RandomBodyPartTarget
	{
		public SexTarget()
		{
		}

		public override int Type
		{
			get { return RandomTargetTypes.Sex; }
		}

		public override bool NextPosition(RandomTargetGenerator r)
		{
			var self = r.Person;
			var parts = new List<BodyPart>();

			for (int i = 0; i < Cue.Instance.Persons.Count; ++i)
			{
				var p = Cue.Instance.Persons[i];
				if (p == self || p == r.Person.Gaze.Avoid)
					continue;

				if (p.Kisser.Active)
				{
					var bp = p.Body.Get(BodyParts.Eyes);
					if (bp != null && bp.Exists)
					{
						r.Log.Verbose($" - kiss {p}");
						parts.Add(bp);
					}
				}
				else if (p.Blowjob.Active)
				{
					var bp = p.Body.Get(BodyParts.Eyes);
					if (bp != null && bp.Exists)
					{
						r.Log.Verbose($" - bj {p}");
						parts.Add(bp);
					}
				}
				else if (p.Handjob.Active)
				{
					var bp = p.Handjob?.Target?.Body?.Get(BodyParts.Genitals);
					if (bp != null && bp.Exists)
					{
						r.Log.Verbose($" - hj {p}");
						parts.Add(bp);
					}
				}
			}

			return CheckTargets(r, parts);
		}

		public override string ToString()
		{
			return "sex " + base.ToString();
		}
	}


	class BodyPartsTarget : RandomBodyPartTarget
	{
		private Person target_ = null;
		private int[] types_;

		public BodyPartsTarget(Person target, int[] types)
		{
			target_ = target;
			types_ = types;
		}

		public override int Type
		{
			get { return RandomTargetTypes.Body; }
		}

		public Person Target
		{
			get { return target_; }
			set { target_ = value; }
		}

		public void Set(Person target, int[] types)
		{
			target_ = target;
			types_ = types;
		}

		public override bool NextPosition(RandomTargetGenerator r)
		{
			var parts = new List<BodyPart>();

			if (target_ == null)
			{
				var self = r.Person;
				for (int i = 0; i < Cue.Instance.Persons.Count; ++i)
				{
					var p = Cue.Instance.Persons[i];
					if (p == self || p == r.Person.Gaze.Avoid)
						continue;

					for (int j = 0; j < types_.Length; ++j)
					{
						var bp = p.Body.Get(types_[j]);
						if (bp != null && bp.Exists)
						{
							r.Log.Verbose($" - {bp}");
							parts.Add(bp);
						}
					}
				}
			}
			else
			{
				for (int j = 0; j < types_.Length; ++j)
				{
					var bp = target_.Body.Get(types_[j]);
					if (bp != null && bp.Exists)
					{
						r.Log.Verbose($" - {bp}");
						parts.Add(bp);
					}
				}
			}

			return CheckTargets(r, parts);
		}

		public override string ToString()
		{
			return $"bodypart " + base.ToString();
		}
	}


	class EyeContactTarget : RandomBodyPartTarget
	{
		public EyeContactTarget()
		{
		}

		public override int Type
		{
			get { return RandomTargetTypes.Eyes; }
		}

		public override bool NextPosition(RandomTargetGenerator r)
		{
			var self = r.Person;
			var parts = new List<BodyPart>();

			for (int i = 0; i < Cue.Instance.Persons.Count; ++i)
			{
				var p = Cue.Instance.Persons[i];
				if (p == self || p == r.Person.Gaze.Avoid)
					continue;

				var bp = p.Body.Get(BodyParts.Eyes);
				if (bp != null && bp.Exists)
				{
					r.Log.Verbose($" - {bp}");
					parts.Add(bp);
				}
			}

			return CheckTargets(r, parts);
		}

		public override string ToString()
		{
			return $"eyecontact " + base.ToString();
		}
	}


	class RandomPointTarget : IRandomTarget
	{
		private Vector3 pos_ = Vector3.Zero;


		public int Type
		{
			get { return RandomTargetTypes.Random; }
		}

		public Vector3 Position
		{
			get { return pos_; }
		}

		public float GetWeight(Person p)
		{
			return 1;
		}

		public void Reset()
		{
		}

		public bool NextPosition(RandomTargetGenerator r)
		{
			var f = r.RandomAvailableFrustum();
			if (f.Empty)
			{
				r.Log.Verbose($"no available frustrums");
				return false;
			}

			var rp = f.RandomPoint();

			pos_ =
				r.Person.Body.Get(BodyParts.Eyes).Position +
				Vector3.Rotate(rp, r.Person.Body.Get(BodyParts.Chest).Direction);

			return true;
		}

		public override string ToString()
		{
			return $"point {pos_}";
		}
	}


	class RandomTargetGeneratorRenderer
	{
		private RandomTargetGenerator r_;
		private FrustumRenderer[] frustums_ = new FrustumRenderer[0];
		private W.IGraphic avoid_ = null;
		private bool visible_ = false;

		public RandomTargetGeneratorRenderer(RandomTargetGenerator r)
		{
			r_ = r;
		}

		public bool Visible
		{
			get
			{
				return visible_;
			}

			set
			{
				visible_ = value;

				if (value)
				{
					if (frustums_.Length == 0)
						CreateFrustums();

					if (avoid_ == null)
						CreateAvoid();
				}

				for (int i = 0; i < frustums_.Length; ++i)
					frustums_[i].Visible = value;

				if (avoid_ != null)
					avoid_.Visible = value;
			}
		}

		public void Update(float s)
		{
			for (int i = 0; i < frustums_.Length; ++i)
			{
				var fi = r_.GetFrustum(i);

				if (fi.avoid)
					frustums_[i].Color = new Color(1, 0, 0, 0.1f);
				else if (fi.selected)
					frustums_[i].Color = new Color(1, 1, 1, 0.1f);
				else
					frustums_[i].Color = new Color(0, 1, 0, 0.1f);

				frustums_[i].Update(s);
			}

			avoid_.Position =
				r_.Person.Body.Get(BodyParts.Eyes).Position +
				Vector3.Rotate(r_.AvoidBox.center, r_.ReferencePart.Direction);

			avoid_.Size = r_.AvoidBox.size;
		}

		private void CreateFrustums()
		{
			frustums_ = new FrustumRenderer[RandomTargetGenerator.FrustumCount];
			for (int i = 0; i < frustums_.Length; ++i)
			{
				frustums_[i] = new FrustumRenderer(
					r_.Person, r_.GetFrustum(i).frustum,
					BodyParts.Head, BodyParts.Chest);

				frustums_[i].Visible = visible_;
			}
		}

		private void CreateAvoid()
		{
			avoid_ = Cue.Instance.Sys.CreateBoxGraphic(
				"RandomTargetGenerator.AvoidRender",
				Vector3.Zero, Vector3.Zero, new Color(0, 0, 1, 0.1f));

			avoid_.Collision = false;
			avoid_.Visible = visible_;
		}
	}
}
