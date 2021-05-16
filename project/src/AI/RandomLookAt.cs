using System.Collections.Generic;

namespace Cue
{
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
		bool NextPosition(RandomLookAt r);
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

		public abstract bool NextPosition(RandomLookAt r);

		protected bool CheckTargets(RandomLookAt r, List<BodyPart> parts)
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

		public override bool NextPosition(RandomLookAt r)
		{
			var self = r.Person;
			var parts = new List<BodyPart>();

			for (int i = 0; i < Cue.Instance.Persons.Count; ++i)
			{
				var p = Cue.Instance.Persons[i];
				if (p == self)
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
		private int[] types_;

		public BodyPartsTarget(int[] types)
		{
			types_ = types;
		}

		public override int Type
		{
			get { return RandomTargetTypes.Body; }
		}

		public override bool NextPosition(RandomLookAt r)
		{
			var self = r.Person;
			var parts = new List<BodyPart>();

			for (int i = 0; i < Cue.Instance.Persons.Count; ++i)
			{
				var p = Cue.Instance.Persons[i];
				if (p == self)
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

		public override bool NextPosition(RandomLookAt r)
		{
			var self = r.Person;
			var parts = new List<BodyPart>();

			for (int i = 0; i < Cue.Instance.Persons.Count; ++i)
			{
				var p = Cue.Instance.Persons[i];
				if (p == self)
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
			return $"eyes " + base.ToString();
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

		public bool NextPosition(RandomLookAt r)
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
}
