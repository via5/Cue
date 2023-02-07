using System.Text;

namespace Cue
{
	public class GazeTargetPicker
	{
		public class FrustumInfo
		{
			public Frustum frustum;
			public bool avoid;
			public bool reluctant;
			public bool selected;
			public float weight;

			public FrustumInfo(Person p, Frustum f, float w)
			{
				frustum = f;
				avoid = false;
				reluctant = false;
				selected = false;
				weight = w;
			}
		}

		public struct ObjectBox
		{
			public Box box;
			public bool reluctant;
			public bool avoid;
		}

		private Vector3 Near = new Vector3(2, 2, 0.1f);
		private Vector3 Far = new Vector3(10, 10, 2);

		public const int XCount = 5;
		public const int YCount = 5;
		public const int FrustumCount = XCount * YCount;

		private const float UpdateGeoInterval = 1;
		private const float CheckCanLookInterval = 0.5f;
		private const float AvoidInterval = 5;

		private readonly float[] FrustumWeights = new float[XCount * YCount]
		{
			0.1f, 0.1f, 0.1f, 0.1f, 0.1f,
			0.1f, 0.1f, 0.1f, 0.1f, 0.1f,
			0.5f, 2.0f, 2.0f, 2.0f, 0.5f,
			0.1f, 0.1f, 0.1f, 0.1f, 0.1f,
			0.1f, 0.1f, 0.1f, 0.1f, 0.1f,
		};

		private Person person_;
		private Logger log_;
		private FrustumInfo[] frustums_ = new FrustumInfo[FrustumCount];
		private ObjectBox[] objectBoxes_ = new ObjectBox[0];
		private Duration delay_ = new Duration();
		private IGazeLookat[] targets_ = new IGazeLookat[0];
		private bool[] targetFailed_ = new bool[0];
		private IGazeLookat currentTarget_ = null;
		private bool emergency_ = false;
		private readonly StringBuilder lastString_ = new StringBuilder();
		private readonly StringBuilder avoidString_ = new StringBuilder();
		private float timeSinceLastAvoid_ = 0;
		private float updateGeoElapsed_ = UpdateGeoInterval;
		private float canLookElapsed_ = CheckCanLookInterval;
		private bool forceNewTarget_ = false;

		// temp target
		private IGazeLookat tempTarget_ = null;
		private float tempTargetTime_ = 0;
		private float tempTargetElapsed_ = 0;

		public GazeTargetPicker(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.AI, person_, "gaze.picker");

			var main = new Frustum(Near, Far);
			var fs = main.Split(XCount, YCount);

			for (int i = 0; i < fs.Length; ++i)
				frustums_[i] = new FrustumInfo(p, fs[i], FrustumWeights[i]);

			OnPersonalityChanged();
			person_.PersonalityChanged += OnPersonalityChanged;
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

		public ObjectBox[] ObjectBoxes
		{
			get { return objectBoxes_; }
		}

		public IGazeLookat CurrentTarget
		{
			get { return tempTarget_ ?? currentTarget_; }
		}

		public void SetTemporaryTarget(IGazeLookat target, float time)
		{
			tempTarget_ = target;
			tempTargetTime_ = time;
			tempTargetElapsed_ = 0;
		}

		public bool HasTarget
		{
			get { return (CurrentTarget != null); }
		}

		public bool IsTargetTemporary
		{
			get { return (tempTarget_ != null); }
		}

		public float TemporaryTargetTime
		{
			get { return tempTargetTime_; }
		}

		public float TemporaryTargetElapsed
		{
			get { return tempTargetElapsed_; }
		}

		public Duration NextInterval
		{
			get { return delay_; }
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
				var p = chest.Position + chest.Rotation.Rotate(new Vector3(0, 0, -0.05f));
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
			targetFailed_ = new bool[t.Length];
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
					if (f != null && (f.avoid || f.reluctant))
						return false;
				}
				else
				{
					var f = FindFrustum(t.Position);
					if (f != null && f.avoid)
						return false;

					if (t.Object != null)
					{
						if (Person.Gaze.Targets.ShouldAvoid(t.Object))
							return false;
					}
				}
			}

			if (!FrontPlane.PointInFront(t.Position))
				return false;

			return true;
		}

		public void AvoidNow()
		{
			canLookElapsed_ = CheckCanLookInterval;
			timeSinceLastAvoid_ = AvoidInterval;
		}

		public void ForceNewTarget()
		{
			forceNewTarget_ = true;
		}

		public bool Update(float s)
		{
			if (tempTarget_ != null)
			{
				tempTargetElapsed_ += s;
				if (tempTargetElapsed_ >= tempTargetTime_)
				{
					tempTarget_ = null;
					tempTargetTime_ = 0;
					tempTargetElapsed_ = 0;
				}
			}

			timeSinceLastAvoid_ += s;

			// so it's displayed in the ui for more than a frame
			if (timeSinceLastAvoid_ > 2)
				avoidString_.Length = 0;

			delay_.Update(s, 0);

			updateGeoElapsed_ += s;
			if (updateGeoElapsed_ >= UpdateGeoInterval)
			{
				updateGeoElapsed_ = 0;

				Instrumentation.Start(I.PickerGeo);
				{
					UpdateAvoidBoxes();
					UpdateFrustums();
				}
				Instrumentation.End();
			}

			canLookElapsed_ += s;

			bool needsTarget = false;

			if (delay_.Finished || !HasTarget || forceNewTarget_)
			{
				needsTarget = true;
				forceNewTarget_ = false;
				updateGeoElapsed_ = UpdateGeoInterval;

				// don't allow avoidance after a new target is picked naturally
				// so the gaze doesn't change immediately
				timeSinceLastAvoid_ = 0;
			}
			else if (HasTarget)
			{
				if (canLookElapsed_ >= CheckCanLookInterval)
				{
					canLookElapsed_ = 0;

					Instrumentation.Start(I.PickerCanLook);
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
								delay_.Reset(0);
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
					Instrumentation.End();
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

		public void EmergencyStarted()
		{
			emergency_ = true;
			delay_.Reset(0);
			NextTarget();
		}

		public void EmergencyEnded()
		{
			emergency_ = false;
		}

		public void NextTarget()
		{
			for (int i = 0; i < targetFailed_.Length; ++i)
				targetFailed_[i] = false;

			lastString_.Length = 0;

			float total = 0;
			for (int i = 0; i < targets_.Length; ++i)
			{
				if (targets_[i].Exclusive)
				{
					lastString_.Append(" target=#");
					lastString_.Append(i);
					lastString_.Append(" (emergency)");

					currentTarget_ = targets_[i];
					return;
				}

				total += targets_[i].Weight;
			}

			lastString_.Append("tw=");
			lastString_.Append(total);

			for (int i = 0; i < 10; ++i)
			{
				var r = U.RandomFloat(0, total);
				lastString_.Append(" r=");
				lastString_.Append(r);

				for (int j = 0; j < targets_.Length; ++j)
				{
					if (targetFailed_[j])
						continue;

					if (r < targets_[j].Weight)
					{
						//log_.Verbose($"trying {targets_[j]}");

						if (targets_[j].Next())
						{
							if (CanLookAtTarget(targets_[j]))
							{
								lastString_.Append(" target=#");
								lastString_.Append(j);

								currentTarget_ = targets_[j];

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

						targetFailed_[j] = true;

						break;
					}

					r -= targets_[j].Weight;
				}

				total = 0;
				for (int j = 0; j < targets_.Length; ++j)
				{
					if (!targetFailed_[j])
						total += targets_[j].Weight;
				}
			}

			lastString_.Append(" NONE");
			currentTarget_ = person_.Gaze.Targets.RandomPoint;
		}

		private void OnPersonalityChanged()
		{
			delay_ = person_.Personality.GetDuration(PS.GazeRandomInterval).Clone();
		}

		private void UpdateFrustums()
		{
			Box currentBox = CurrentTargetAABox;

			for (int i = 0; i < frustums_.Length; ++i)
			{
				var fi = frustums_[i];
				fi.avoid = false;
				fi.reluctant = false;

				for (int j = 0; j < objectBoxes_.Length; ++j)
				{
					if (fi.frustum.TestPlanesAABB(objectBoxes_[j].box))
					{
						fi.avoid = objectBoxes_[j].avoid;
						fi.reluctant = objectBoxes_[j].reluctant;
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
			int count = 0;
			var everything = Cue.Instance.Everything;

			for (int i = 0; i < Cue.Instance.Everything.Count; ++i)
			{
				var o = everything[i];
				if (o == person_)
					continue;

				if (person_.Gaze.Targets.IsReluctant(o) ||
					person_.Gaze.Targets.ShouldAvoid(o))
				{
					++count;
				}
			}

			if (count != objectBoxes_.Length)
				objectBoxes_ = new ObjectBox[count];

			int boxIndex = 0;
			for (int i = 0; i < everything.Count; ++i)
			{
				var o = everything[i];
				if (o == person_)
					continue;

				bool reluctant = person_.Gaze.Targets.IsReluctant(o);
				bool avoid = person_.Gaze.Targets.ShouldAvoid(o);

				if (reluctant || avoid)
				{
					if (o is Person)
						objectBoxes_[boxIndex].box = CreatePersonAvoidBox(o as Person);
					else
						objectBoxes_[boxIndex].box = CreateObjectAvoidBox(o as BasicObject);

					objectBoxes_[boxIndex].reluctant = reluctant;
					objectBoxes_[boxIndex].avoid = avoid;

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

			var b = avoidP.Body.GetUpperBodyBox();
			b.center = q.RotateInv(b.center - selfRef.Position);

			return b;
		}

		public Frustum RandomAvailableFrustum()
		{
			int av = 0;
			float totalWeight = 0;

			for (int i = 0; i < frustums_.Length; ++i)
			{
				if (!frustums_[i].avoid)
				{
					++av;
					totalWeight += frustums_[i].weight;
				}
			}


			float or = U.RandomFloat(0, totalWeight);
			float r = or;

			for (int i = 0; i < frustums_.Length; ++i)
			{
				if (frustums_[i].avoid)
					continue;

				if (r <= frustums_[i].weight)
				{
					//Cue.LogError($"tw={totalWeight} r={r} picked {i}");
					return frustums_[i].frustum;
				}

				r -= frustums_[i].weight;
			}

			//Cue.LogError($"no frustum, tw={totalWeight} r was {or}");
			return Frustum.Zero;
		}
	}
}
