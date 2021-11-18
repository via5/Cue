using System;

namespace Cue
{
	public class Source
	{
		class Part
		{
			public int bodyPart;
			public bool active = false;
			public float elapsed = 0;
			public bool ignored = false;
			public int targetBodyPart = BP.None;

			public Part(int i)
			{
				bodyPart = i;
			}
		}

		private int totalActive_ = 0;
		private int validActive_ = 0;
		private readonly int personIndex_;
		private readonly Part[] parts_ = new Part[BP.Count + 1];

		public Source(int personIndex)
		{
			personIndex_ = personIndex;

			for (int i = 0; i < parts_.Length; ++i)
				parts_[i] = new Part(i);
		}

		public int ActiveCount
		{
			get { return validActive_; }
		}

		public int StrictlyActiveCount
		{
			get { return totalActive_; }
		}

		private Part GetPart(int bodyPart)
		{
			if (bodyPart == BP.None)
				return parts_[parts_.Length - 1];
			else
				return parts_[bodyPart];
		}

		public bool IsActive(int bodyPart)
		{
			return IsStrictlyActive(bodyPart) && !IsIgnored(bodyPart);
		}

		public bool IsStrictlyActive(int bodyPart)
		{
			return GetPart(bodyPart).active;
		}

		public bool IsIgnored(int bodyPart)
		{
			return GetPart(bodyPart).ignored;
		}

		public int TargetBodyPart(int bodyPart)
		{
			return GetPart(bodyPart).targetBodyPart;
		}

		public bool IsAnyActiveForTarget(int targetBodyPart)
		{
			for (int i = 0; i < parts_.Length; ++i)
			{
				if (IsActive(i) && parts_[i].targetBodyPart == targetBodyPart)
					return true;
			}

			return false;
		}

		public void IgnoreTarget(int targetBodyPart)
		{
			for (int i = 0; i < parts_.Length; ++i)
			{
				if (IsActive(i) && parts_[i].targetBodyPart == targetBodyPart)
					Ignore(i);
			}
		}

		public void Decay(float s)
		{
			totalActive_ = 0;
			validActive_ = 0;

			for (int i = 0; i < parts_.Length; ++i)
			{
				DoDecay(s, parts_[i]);
				parts_[i].ignored = false;
 			}
		}

		private void DoDecay(float s, Part part)
		{
			// todo, decay speed
			part.elapsed = Math.Max(0, part.elapsed - (s / 2));
			part.active = (part.elapsed  > 0);

			if (part.active)
			{
				++totalActive_;
				++validActive_;
			}
		}

		public void Set(int sourceBodyPart, int targetBodyPart)
		{
			var p = GetPart(sourceBodyPart);

			p.elapsed = 1.0f;
			p.ignored = false;
			p.targetBodyPart = targetBodyPart;

			if (!p.active)
			{
				p.active = true;
				++totalActive_;
				++validActive_;
			}
		}

		public void Ignore(int bodyPart)
		{
			var p = GetPart(bodyPart);

			if (!p.ignored)
			{
				p.ignored = true;
				if (p.active)
					--validActive_;
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
		public struct Part
		{
			public int target;
			public int source;

			public Part(int target, int source = BP.None)
			{
				this.source = source;
				this.target = target;
			}
		}

		private const float UpdateInterval = 0.5f;

		private Person person_;
		private Part[] parts_;
		private Source[] sources_;
		private int activeSources_ = 0;
		private float elapsed_ = 0;

		public ErogenousZone(Person p, Part[] bodyParts)
		{
			person_ = p;
			parts_ = bodyParts;

			// include an external one at the end
			sources_ = new Source[Cue.Instance.ActivePersons.Length + 1];

			for (int i = 0; i < Cue.Instance.ActivePersons.Length; ++i)
				sources_[i] = new Source(i);

			sources_[sources_.Length - 1] = new Source(-1);
		}

		public bool Active
		{
			get { return (activeSources_ > 0); }
		}

		public int ActiveSources
		{
			get { return activeSources_; }
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

				for (int i = 0; i < parts_.Length; ++i)
				{
					var bp = parts_[i];
					var ts = person_.Body.Get(bp.target).GetTriggers();

					if (ts != null)
						CheckTriggers(ts, bp.target, bp.source);
				}

				activeSources_ = 0;
				for (int i = 0; i < sources_.Length; ++i)
				{
					if (sources_[i].ActiveCount > 0)
						++activeSources_;
				}
			}
		}

		public void IgnoreParts(ErogenousZone penZone, int targetBodyPart)
		{
			for (int i = 0; i < penZone.Sources.Length; ++i)
			{
				if (penZone.Sources[i].IsAnyActiveForTarget(targetBodyPart))
				{
					bool wasActive = (sources_[i].ActiveCount > 0);
					sources_[i].IgnoreTarget(targetBodyPart);

					if (wasActive && sources_[i].ActiveCount == 0)
						--activeSources_;
				}
			}
		}

		private void Decay(float s)
		{
			activeSources_ = 0;

			for (int i = 0; i < sources_.Length;++i)
			{
				sources_[i].Decay(s);
				if (sources_[i].ActiveCount > 0)
					++activeSources_;
			}
		}

		private void CheckTriggers(
			Sys.TriggerInfo[] ts, int targetBodyPart, int sourceCheck)
		{
			for (int i = 0; i < ts.Length; ++i)
			{
				var t = ts[i];

				if (sourceCheck == BP.None || sourceCheck == t.sourcePartIndex)
				{
					if (t.IsPerson())
						sources_[t.personIndex].Set(t.sourcePartIndex, targetBodyPart);
					else
						External.Set(-1, targetBodyPart);
				}
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
		private ErogenousZone penetration_ = null;
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
			penetration_ = new ErogenousZone(person_, new ErogenousZone.Part[]
			{
				new ErogenousZone.Part(BP.Labia, BP.Penis),
				new ErogenousZone.Part(BP.Vagina),
				new ErogenousZone.Part(BP.DeepVagina),
				new ErogenousZone.Part(BP.DeeperVagina),
				new ErogenousZone.Part(BP.Penis, BP.Labia),
				new ErogenousZone.Part(BP.Penis, BP.DeepVagina),
				new ErogenousZone.Part(BP.Penis, BP.DeeperVagina),
			});

			mouth_ = new ErogenousZone(person_, new ErogenousZone.Part[]
			{
				new ErogenousZone.Part(BP.Lips),
				new ErogenousZone.Part(BP.Mouth)
			});

			breasts_ = new ErogenousZone(person_, new ErogenousZone.Part[]
			{
				new ErogenousZone.Part(BP.LeftBreast),
				new ErogenousZone.Part(BP.RightBreast)
			});

			genitals_ = new ErogenousZone(person_, new ErogenousZone.Part[]
			{
				new ErogenousZone.Part(BP.Labia),
				new ErogenousZone.Part(BP.Penis)
			});
		}

		public void Update(float s)
		{
			penetration_.Update(s);
			mouth_.Update(s);
			breasts_.Update(s);
			genitals_.Update(s);


			int[] ignore = new int[]
			{
				BP.Penis, BP.Labia, BP.Vagina, BP.DeepVagina, BP.DeeperVagina
			};

			for (int i = 0; i < ignore.Length; ++i)
				genitals_.IgnoreParts(penetration_, ignore[i]);
		}


		public ErogenousZone Penetration { get { return penetration_; } }
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
