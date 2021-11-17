using System;
using System.Collections.Generic;

namespace Cue
{
	public class Source
	{
		private bool active_ = false;
		private readonly int personIndex_;
		private readonly bool[] bodyParts_ = new bool[BP.Count];
		private readonly float[] bodyPartsElapsed_ = new float[BP.Count];
		private bool unknown_ = false;
		private float unknownElapsed_ = 0;

		public Source(int personIndex)
		{
			personIndex_ = personIndex;
		}

		public bool Active
		{
			get { return active_; }
		}

		public bool[] BodyParts
		{
			get { return bodyParts_; }
		}

		public bool Unknown
		{
			get { return unknown_; }
		}

		public void Decay(float s)
		{
			active_ = false;

			for (int i = 0; i < bodyPartsElapsed_.Length; ++i)
				DoDecay(s, ref bodyParts_[i], ref bodyPartsElapsed_[i]);

			DoDecay(s, ref unknown_, ref unknownElapsed_);
		}

		private void DoDecay(float s, ref bool value, ref float elapsed)
		{
			// todo, decay speed
			elapsed = Math.Max(0, elapsed - (s / 2));
			value = (elapsed  > 0);

			if (value)
				active_ = true;
		}

		public void Set(int bodyPart)
		{
			active_ = true;

			if (bodyPart == BP.None)
			{
				unknownElapsed_ = 1.0f;
				unknown_ = true;
			}
			else
			{
				bodyPartsElapsed_[bodyPart] = 1.0f;
				bodyParts_[bodyPart] = true;
			}
		}

		public override string ToString()
		{
			if (personIndex_ == -1)
				return $"external";
			else
				return $"{Cue.Instance.GetPerson(personIndex_)}";
		}
	}


	public class ErogenousZone
	{
		private const float UpdateInterval = 0.5f;

		private Person person_;
		private int[] bodyParts_;
		private Source[] sources_;
		private bool active_ = false;
		private float elapsed_ = 0;

		public ErogenousZone(Person p, int[] bodyParts)
		{
			person_ = p;
			bodyParts_ = bodyParts;

			// include an external one at the end
			sources_ = new Source[Cue.Instance.ActivePersons.Length + 1];

			for (int i = 0; i < Cue.Instance.ActivePersons.Length; ++i)
				sources_[i] = new Source(i);

			sources_[sources_.Length - 1] = new Source(-1);
		}

		public bool Active
		{
			get { return active_; }
		}

		public Source[] Sources
		{
			get { return sources_; }
		}

		private Source External
		{
			get { return sources_[sources_.Length - 1]; }
		}

		public void Update(float s)
		{
			Decay(s);

			elapsed_ += s;
			if (elapsed_ >= UpdateInterval)
			{
				elapsed_ = 0;

				for (int i = 0; i < bodyParts_.Length; ++i)
				{
					var bp = bodyParts_[i];
					var ts = person_.Body.Get(bp).GetTriggers();

					if (ts != null)
						CheckTriggers(ts);
				}
			}
		}

		private void Decay(float s)
		{
			active_ = false;

			for (int i = 0; i < sources_.Length;++i)
			{
				sources_[i].Decay(s);
				if (sources_[i].Active)
					active_ = true;
			}
		}

		private void CheckTriggers(Sys.TriggerInfo[] ts)
		{
			for (int i = 0; i < ts.Length; ++i)
			{
				var t = ts[i];

				if (t.IsPerson())
					sources_[t.personIndex].Set(t.sourcePartIndex);
				else
					External.Set(-1);

				active_ = true;
			}
		}
	}


	public class PersonStatus
	{
		public struct PartResult
		{
			public int ownBodyPart;
			public int byBodyPart;
			public int byObjectIndex;

			public PartResult(int ownBodyPart, int byObjectIndex, int byBodyPart)
			{
				this.ownBodyPart = ownBodyPart;
				this.byObjectIndex = byObjectIndex;
				this.byBodyPart = byBodyPart;
			}

			public static PartResult None
			{
				get { return new PartResult(-1, -1, -1); }
			}

			public bool Valid
			{
				get { return (ownBodyPart != -1); }
			}

			public override string ToString()
			{
				string s = "";

				s +=
					$"{BP.ToString(ownBodyPart)} by " +
					$"{Cue.Instance.GetObject(byObjectIndex)?.ID ?? "?"}" +
					$"." +
					$"{BP.ToString(byBodyPart)}";

				return s;
			}

			public static implicit operator bool(PartResult pr)
			{
				return pr.Valid;
			}
		}


		private readonly Person person_;
		private readonly Body body_;
		private ErogenousZone vaginal_ = null;
		private ErogenousZone mouth_ = null;
		private ErogenousZone breasts_ = null;
		private ErogenousZone genitals_ = null;

		public PersonStatus(Person p)
		{
			person_ = p;
			body_ = p.Body;
		}

		public void Init()
		{
			vaginal_ = new ErogenousZone(person_, new int[]
			{
				BP.Vagina, BP.DeepVagina, BP.DeeperVagina
			});

			mouth_ = new ErogenousZone(person_, new int[]
			{
				BP.Lips, BP.Mouth
			});

			breasts_ = new ErogenousZone(person_, new int[]
			{
				BP.LeftBreast, BP.RightBreast
			});

			genitals_ = new ErogenousZone(person_, new int[]
			{
				BP.Labia
			});
		}

		public void Update(float s)
		{
			vaginal_.Update(s);
			mouth_.Update(s);
			breasts_.Update(s);
			genitals_.Update(s);
		}


		public ErogenousZone VaginalPenetration { get { return vaginal_; } }
		public ErogenousZone Mouth { get { return mouth_; } }
		public ErogenousZone Breasts { get { return breasts_; } }
		public ErogenousZone Genitals { get { return genitals_; } }

		public bool AnyInsidePersonalSpace()
		{
			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (p == person_)
					continue;

				if (InsidePersonalSpace(p))
					return true;
			}

			return false;
		}

		public bool InsidePersonalSpace(Person other)
		{
			var checkParts = BodyParts.PersonalSpaceParts;

			for (int i = 0; i < checkParts.Length; ++i)
			{
				var a = body_.Get(checkParts[i]);

				for (int j = 0; j < checkParts.Length; ++j)
				{
					var b = other.Body.Get(checkParts[j]);
					if (a.CloseTo(b))
						return true;
				}
			}

			return false;
		}

		public bool InteractingWith(Person other)
		{
			if (InsidePersonalSpace(other) || PenetratedBy(other) || GropedBy(other))
				return true;

			// special case for unpossessed, because it's just the camera and
			// the mouse pointer grab is not handled
			if (!other.Body.Exists && other.IsPlayer)
			{
				for (int i = 0; i < BP.Count; ++i)
				{
					if (body_.Get(i).GrabbedByPlayer)
						return true;
				}
			}

			return false;
		}

		public bool Groped()
		{
			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (GropedBy(p, BodyParts.GropedParts))
					return true;
			}

			return false;
		}

		public Person PenetratedBy()
		{
			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (PenetratedBy(p))
					return p;
			}

			return null;
		}

		public bool Penetrated()
		{
			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (PenetratedBy(p))
					return true;
			}

			if (body_.Get(BP.Vagina).Triggered)
				return true;

			return false;
		}

		public bool Penetrating()
		{
			if (!body_.HasPenis)
				return false;

			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (p.Status.PenetratedBy(person_))
					return true;
			}

			return false;
		}

		public PartResult GropedByAny(int triggerBodyPart)
		{
			return GropedByAny(new int[] { triggerBodyPart });
		}

		public PartResult GropedByAny(int[] triggerBodyParts)
		{
			foreach (var p in Cue.Instance.ActivePersons)
			{
				var pr = GropedBy(p, triggerBodyParts);
				if (pr.Valid)
					return pr;
			}

			return PartResult.None;
		}

		public PartResult GropedBy(Person p)
		{
			return GropedBy(p, BodyParts.GropedParts);
		}

		public PartResult GropedBy(Person p, int triggerBodyPart)
		{
			return GropedBy(p, new int[] { triggerBodyPart });
		}

		public PartResult GropedBy(Person p, int[] triggerBodyParts)
		{
			if (p == person_)
				return PartResult.None;

			return CheckParts(p, triggerBodyParts, BodyParts.GropedByParts);
		}

		public bool PenetratedBy(Person p)
		{
			if (p == person_)
				return false;

			return CheckParts(
				p, BodyParts.PenetratedParts, BodyParts.PenetratedByParts);
		}

		private PartResult CheckParts(Person by, int[] triggerParts, int[] checkParts)
		{
			for (int i = 0; i < triggerParts.Length; ++i)
			{
				var triggerPart = body_.Get(triggerParts[i]);

				for (int j = 0; j < checkParts.Length; ++j)
				{
					var byPart = by.Body.Get(checkParts[j]);

					if (triggerPart.CanTrigger)
					{
						var pr = TriggeredBy(triggerPart, byPart);
						if (pr.Valid)
							return pr;
					}
					else
					{
						if (triggerPart.CloseTo(byPart))
						{
							return new PartResult(
								triggerPart.Type, by.ObjectIndex, byPart.Type);
						}
					}
				}
			}

			return PartResult.None;
		}

		private PartResult TriggeredBy(BodyPart p, BodyPart by)
		{
			if (!p.Exists || !by.Exists)
				return PartResult.None;

			var ts = p.GetTriggers();

			if (ts != null)
			{
				for (int i = 0; i < ts.Length; ++i)
				{
					if (ts[i].sourcePartIndex >= 0)
					{
						var pp = Cue.Instance.GetPerson(ts[i].personIndex);
						var bp = pp.Body.Get(ts[i].sourcePartIndex);

						if (bp == by)
						{
							return new PartResult(
								p.Type,
								pp.ObjectIndex, ts[i].sourcePartIndex);
						}
					}
				}
			}

			return PartResult.None;
		}
	}
}
