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

		private Person person_;
		private Logger log_;
		private FrustumInfo[] frustums_ = new FrustumInfo[FrustumCount];
		private Box avoidBox_ = Box.Zero;
		private RandomTargetGeneratorRenderer render_ = null;
		private Duration delay_ = new Duration();
		private IGazeLookat[] targets_ = new IGazeLookat[0];
		private int currentTarget_ = -1;
		private string lastString_ = "";

		public GazeTargetPicker(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.AI, person_, "GazeTargetPicker");

			var main = new Frustum(Near, Far);
			var fs = main.Split(XCount, YCount);

			for (int i = 0; i < fs.Length; ++i)
				frustums_[i] = new FrustumInfo(p, fs[i]);
		}

		public bool Render
		{
			set
			{
				if (value)
				{
					if (render_ == null)
						render_ = new RandomTargetGeneratorRenderer(this);

					render_.Visible = value;
				}
				else
				{
					if (render_ != null)
						render_.Visible = false;
				}
			}
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

		public string LastString
		{
			get { return lastString_; }
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

		public IGazeLookat CurrentTarget
		{
			get { return HasTarget ? targets_[currentTarget_] : null; }
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

		public void SetTargets(IGazeLookat[] t)
		{
			targets_ = t;
		}

		public IGazeLookat[] Targets
		{
			get { return targets_; }
		}

		public bool CanLookAtPoint(Vector3 p)
		{
			var f = FindFrustum(p);
			return f == null || !f.avoid;
		}

		public bool Update(float s)
		{
			bool needsTarget = false;

			UpdateAvoidBox();
			UpdateFrustums();
			delay_.Update(s);

			if (delay_.Finished || !HasTarget)
			{
				needsTarget = true;
			}
			else if (HasTarget)
			{
				if (!CanLookAtPoint(targets_[currentTarget_].Position))
				{
					needsTarget = true;
				}
			}

			render_?.Update(s);

			return needsTarget;
		}

		public override string ToString()
		{
			if (HasTarget)
				return $"j={currentTarget_} {targets_[currentTarget_]}";
			else
				return "no target";
		}

		public void ForceNextTarget()
		{
			delay_.Reset();
			NextTarget();
		}

		public void NextTarget()
		{
			delay_.SetRange(person_.Personality.LookAtRandomInterval);
			lastString_ = "";

			float total = 0;
			for (int i = 0; i < targets_.Length; ++i)
				total += targets_[i].Weight;

			lastString_ += $"tw={total}";

			for (int i = 0; i < 10; ++i)
			{
				var r = U.RandomFloat(0, total);
				lastString_ += $" r={r}";

				for (int j = 0; j < targets_.Length; ++j)
				{
					if (r < targets_[j].Weight)
					{
						log_.Verbose($"trying {targets_[j]}");

						if (targets_[j].Next())
						{
							if (CanLookAtPoint(targets_[j].Position))
							{
								lastString_ += $" target=#{j}";
								log_.Verbose($"picked {targets_[j]}");
								currentTarget_ = j;
								return;
							}
						}

						lastString_ += $" {j}=X";
						break;
					}

					r -= targets_[j].Weight;
				}
			}

			lastString_ += $" NONE";
		}

		private void UpdateFrustums()
		{
			Box currentBox = CurrentTargetAABox;

			for (int i = 0; i < frustums_.Length; ++i)
			{
				var fi = frustums_[i];

				fi.avoid = fi.frustum.TestPlanesAABB(avoidBox_);

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
			var selfRef = person_.Body.Get(BodyParts.Eyes);
			var rp = p - selfRef.Position;
			var aaP = q.RotateInv(rp);

			return new Box(aaP, new Vector3(0.01f, 0.01f, 0.01f));
		}

		private bool UpdateAvoidBox()
		{
			// todo: must support multiple avoid boxes
			IObject avoidO = null;

			for (int i = 0; i < Cue.Instance.Everything.Count; ++i)
			{
				if (person_.Gaze.Targets.ShouldAvoid(Cue.Instance.Everything[i]))
				{
					avoidO = Cue.Instance.Everything[i];
					break;
				}
			}

			if (avoidO == null)
				return false;

			var selfRef = person_.Body.Get(BodyParts.Eyes);
			var avoidP = avoidO as Person;

			if (avoidP == null)
			{
				avoidBox_ = new Box(
					avoidO.EyeInterest - selfRef.Position,
					new Vector3(0.2f, 0.2f, 0.2f));
			}
			else
			{
				var avoidRef = avoidP.Body.Get(BodyParts.Eyes);

				var q = ReferencePart.Rotation;

				var avoidHeadU =
					avoidRef.Position -
					selfRef.Position +
					new Vector3(0, 0.2f, 0);

				var avoidHipU =
					avoidP.Body.Get(BodyParts.Hips).Position -
					selfRef.Position;

				var avoidHead = q.RotateInv(avoidHeadU);
				var avoidHip = q.RotateInv(avoidHipU);

				avoidBox_ = new Box(
					avoidHip + (avoidHead - avoidHip) / 2,
					new Vector3(0.5f, (avoidHead - avoidHip).Y, 0.5f));
			}

			return true;
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


	class RandomTargetGeneratorRenderer
	{
		private GazeTargetPicker r_;
		private FrustumRenderer[] frustums_ = new FrustumRenderer[0];
		private Sys.IGraphic avoid_ = null;
		private Sys.IGraphic lookAt_ = null;
		private bool visible_ = false;

		public RandomTargetGeneratorRenderer(GazeTargetPicker r)
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

					if (lookAt_ == null)
						CreateLookAt();
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
					frustums_[i].Color = new Color(1, 1, 1, 0.3f);
				else
					frustums_[i].Color = new Color(0, 1, 0, 0.1f);

				frustums_[i].Update(s);
			}

			avoid_.Position =
				r_.Person.Body.Get(BodyParts.Eyes).Position +
				r_.ReferencePart.Rotation.Rotate(r_.AvoidBox.center);

			avoid_.Size = r_.AvoidBox.size;


			if (r_.HasTarget)
			{
				var b = r_.CurrentTargetAABox;

				lookAt_.Visible = true;

				lookAt_.Position =
					r_.Person.Body.Get(BodyParts.Eyes).Position +
					r_.ReferencePart.Rotation.Rotate(b.center);

				lookAt_.Size = b.size;
			}
			else
			{
				lookAt_.Visible = false;
			}
		}

		private void CreateFrustums()
		{
			frustums_ = new FrustumRenderer[GazeTargetPicker.FrustumCount];
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
				Vector3.Zero, Vector3.Zero, new Color(1, 0, 0, 0.1f));

			avoid_.Visible = visible_;
		}

		private void CreateLookAt()
		{
			lookAt_ = Cue.Instance.Sys.CreateBoxGraphic(
				"RandomTargetGenerator.LookAt",
				Vector3.Zero, Vector3.Zero, new Color(0, 0, 1, 0.1f));

			lookAt_.Visible = visible_;
		}
	}
}
