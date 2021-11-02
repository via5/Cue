using System.Text;

namespace Cue
{
	class GazeTargetPicker
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

		private Vector3 Near = new Vector3(2, 2, 0.1f);
		private Vector3 Far = new Vector3(10, 10, 2);

		public const int XCount = 5;
		public const int YCount = 5;
		public const int FrustumCount = XCount * YCount;
		private const float AvoidInterval = 5;

		private Person person_;
		private Logger log_;
		private FrustumInfo[] frustums_ = new FrustumInfo[FrustumCount];
		private Box[] avoidBoxes_ = new Box[0];
		private Duration delay_ = new Duration();
		private IGazeLookat[] targets_ = new IGazeLookat[0];
		private IGazeLookat currentTarget_ = null;
		private bool currentTargetReluctant_ = false;
		private IGazeLookat forcedTarget_ = null;
		private bool emergency_ = false;
		private readonly StringBuilder lastString_ = new StringBuilder();
		private readonly StringBuilder avoidString_ = new StringBuilder();
		private float timeSinceLastAvoid_ = 0;

		public GazeTargetPicker(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.AI, person_, "GazeTargetPicker");

			var main = new Frustum(Near, Far);
			var fs = main.Split(XCount, YCount);

			for (int i = 0; i < fs.Length; ++i)
				frustums_[i] = new FrustumInfo(p, fs[i]);
		}

		public BodyPart ReferencePart
		{
			get { return person_.Body.Get(BP.Chest); }
		}

		public Person Person
		{
			get { return person_; }
		}

		public Logger Log
		{
			get { return log_; }
		}

		public string LastString
		{
			get { return lastString_.ToString(); }
		}

		public string AvoidString
		{
			get { return avoidString_.ToString(); }
		}

		public FrustumInfo GetFrustum(int i)
		{
			return frustums_[i];
		}

		public Box[] AvoidBoxes
		{
			get { return avoidBoxes_; }
		}

		public IGazeLookat CurrentTarget
		{
			get { return forcedTarget_ ?? currentTarget_; }
		}

		public IGazeLookat ForcedTarget
		{
			get { return forcedTarget_; }
			set { forcedTarget_ = value; }
		}

		public bool CurrentTargetReluctant
		{
			get { return currentTargetReluctant_; }
		}

		public bool HasTarget
		{
			get { return (CurrentTarget != null); }
		}

		public float TimeBeforeNext
		{
			get { return delay_.Remaining; }
		}

		public Box CurrentTargetAABox
		{
			get
			{
				if (HasTarget)
					return CreateBoxForFrustum(CurrentTarget.Position);
				else
					return Box.Zero;
			}
		}

		public Plane FrontPlane
		{
			get
			{
				var chest = person_.Body.Get(BP.Chest);
				var p = chest.Position + chest.Rotation.Rotate(new Vector3(0, 0, -0.3f));
				return new Plane(p, chest.Rotation.Rotate(new Vector3(0, 0, 1)));
			}
		}

		public Vector3 Position
		{
			get { return CurrentTarget?.Position ?? Vector3.Zero; }
		}

		public void SetTargets(IGazeLookat[] t)
		{
			targets_ = t;
		}

		public IGazeLookat[] Targets
		{
			get { return targets_; }
		}

		public bool CanLookAtTarget(IGazeLookat t)
		{
			// don't use avoidance for emergencies
			if (!emergency_)
			{
				if (t.Weight < 0)
				{
					return false;
				}
				else if (t.Idling)
				{
					var f = FindFrustum(t.Position);
					if (f != null && f.avoid)
						return false;
				}
			}

			if (!FrontPlane.PointInFront(t.Position))
				return false;

			return true;
		}

		public bool Update(float s)
		{
			timeSinceLastAvoid_ += s;

			// so it's displayed in the ui for more than a frame
			if (timeSinceLastAvoid_ > 2)
				avoidString_.Length = 0;

			bool needsTarget = false;

			UpdateAvoidBoxes();
			UpdateFrustums();
			delay_.Update(s);

			if (delay_.Finished || !HasTarget)
			{
				needsTarget = true;

				// don't allow avoidance after a new target is picked naturally
				// so the gaze doesn't change immediately
				timeSinceLastAvoid_ = 0;
			}
			else if (HasTarget)
			{
				if (!CanLookAtTarget(CurrentTarget))
				{
					// can't look at the current point anymore, must pick a new
					// target
					//
					// the problem is that after a new target is found, the head
					// will rotate towards it, along with the chest, which
					// changes the gaze frustum positions
					//
					// if the player's eyes is located just right, a frustum
					// that was valid when initially picked might become
					// forbidden after the chest rotation
					//
					// when this happens, new targets are constantly picked
					// because frustums always become forbidden after gazing
					// has ended
					//
					// one fix would be to check if the frustum is allowed, but
					// with the final chest rotation instead of the current one;
					// that would require predicting the final rotation of the
					// chest after the head has finished rotating towards the
					// new target, which is mostly impossible
					//
					// instead, the avoid interval makes sure there's some delay
					// before the next target so the head doesn't constantly
					// move around; it's also arguably more natural, since the
					// character isn't just immediately avoiding gaze like a
					// whack-a-mole

					avoidString_.Length = 0;

					if (timeSinceLastAvoid_ > AvoidInterval)
					{
						needsTarget = true;
						delay_.Reset();
						timeSinceLastAvoid_ = 0;
						avoidString_.Append("avoiding");
					}
					else
					{
						var left = AvoidInterval - timeSinceLastAvoid_;
						avoidString_.AppendFormat("will avoid in {0:F2}s", left);
					}
				}
			}

			return needsTarget;
		}

		public override string ToString()
		{
			if (HasTarget)
				return $"t={CurrentTarget}";
			else
				return "no target";
		}

		public void ForceNextTarget(bool emergency)
		{
			emergency_ = emergency;
			delay_.Reset();
			NextTarget();
		}

		public void NextTarget()
		{
			delay_.SetRange(person_.Personality.LookAtRandomInterval);
			lastString_.Length = 0;

			float total = 0;
			for (int i = 0; i < targets_.Length; ++i)
				total += targets_[i].Weight;

			lastString_.Append("tw=");
			lastString_.Append(total);

			for (int i = 0; i < 10; ++i)
			{
				var r = U.RandomFloat(0, total);
				lastString_.Append(" r=");
				lastString_.Append(r);

				for (int j = 0; j < targets_.Length; ++j)
				{
					if (r < targets_[j].Weight)
					{
						//log_.Verbose($"trying {targets_[j]}");

						if (targets_[j].Next())
						{
							if (CanLookAtTarget(targets_[j]))
							{
								lastString_.Append(" target=#");
								lastString_.Append(j);

								//log_.Verbose($"picked {targets_[j]}");
								currentTarget_ = targets_[j];

								currentTargetReluctant_ =
									(targets_[j].Object != null) &&
									person_.Gaze.Targets.ShouldAvoid(targets_[j].Object);

								return;
							}
							else
							{
								targets_[j].SetFailed("can't look at this point");
							}
						}
						else
						{
							targets_[j].SetFailed("next");
						}

						lastString_.Append(" ");
						lastString_.Append(j);
						lastString_.Append("=X");

						break;
					}

					r -= targets_[j].Weight;
				}
			}

			lastString_.Append(" NONE");
		}

		private void UpdateFrustums()
		{
			Box currentBox = CurrentTargetAABox;

			for (int i = 0; i < frustums_.Length; ++i)
			{
				var fi = frustums_[i];
				fi.avoid = false;

				for (int j = 0; j < avoidBoxes_.Length; ++j)
				{
					if (fi.frustum.TestPlanesAABB(avoidBoxes_[j]))
					{
						fi.avoid = true;
						break;
					}
				}

				fi.selected = (
					HasTarget &&
					fi.frustum.TestPlanesAABB(currentBox));
			}
		}

		private FrustumInfo FindFrustum(Vector3 p)
		{
			var box = CreateBoxForFrustum(p);

			for (int i = 0; i < frustums_.Length; ++i)
			{
				if (frustums_[i].frustum.TestPlanesAABB(box))
					return frustums_[i];
			}

			return null;
		}

		private Box CreateBoxForFrustum(Vector3 p)
		{
			var q = ReferencePart.Rotation;
			var selfRef = person_.Body.Get(BP.Eyes);
			var rp = p - selfRef.Position;
			var aaP = q.RotateInv(rp);

			return new Box(aaP, new Vector3(0.01f, 0.01f, 0.01f));
		}

		private void UpdateAvoidBoxes()
		{
			int avoidCount = 0;

			for (int i = 0; i < Cue.Instance.Everything.Count; ++i)
			{
				if (person_.Gaze.Targets.ShouldAvoid(Cue.Instance.Everything[i]))
					++avoidCount;
			}

			if (avoidCount != avoidBoxes_.Length)
				avoidBoxes_ = new Box[avoidCount];

			int boxIndex = 0;
			for (int i = 0; i < Cue.Instance.Everything.Count; ++i)
			{
				var o = Cue.Instance.Everything[i];

				if (person_.Gaze.Targets.ShouldAvoid(o))
				{
					if (o is Person)
						avoidBoxes_[boxIndex] = CreatePersonAvoidBox(o as Person);
					else
						avoidBoxes_[boxIndex] = CreateObjectAvoidBox(o as BasicObject);

					++boxIndex;
				}
			}
		}

		private Box CreateObjectAvoidBox(BasicObject avoidO)
		{
			var q = ReferencePart.Rotation;
			var selfRef = person_.Body.Get(BP.Eyes);
			var rp = avoidO.EyeInterest - selfRef.Position;
			var aaP = q.RotateInv(rp);

			return new Box(aaP, new Vector3(0.2f, 0.2f, 0.2f));
		}

		private Box CreatePersonAvoidBox(Person avoidP)
		{
			var selfRef = person_.Body.Get(BP.Eyes);
			var q = ReferencePart.Rotation;

			var b = avoidP.Body.TopBox;
			b.center = q.RotateInv(b.center - selfRef.Position);

			return b;
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
}
