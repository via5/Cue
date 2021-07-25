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
		private Box[] avoidBoxes_ = new Box[0];
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

		public Box[] AvoidBoxes
		{
			get { return avoidBoxes_; }
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

			UpdateAvoidBoxes();
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
			var selfRef = person_.Body.Get(BodyParts.Eyes);
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
			var selfRef = person_.Body.Get(BodyParts.Eyes);
			var rp = avoidO.EyeInterest - selfRef.Position;
			var aaP = q.RotateInv(rp);

			return new Box(aaP, new Vector3(0.2f, 0.2f, 0.2f));
		}

		private Box CreatePersonAvoidBox(Person avoidP)
		{
			var selfRef = person_.Body.Get(BodyParts.Eyes);
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


	class RandomTargetGeneratorRenderer
	{
		private GazeTargetPicker r_;
		private FrustumRenderer[] frustums_ = new FrustumRenderer[0];
		private Sys.IGraphic[] avoid_ = new Sys.IGraphic[0];
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

					if (lookAt_ == null)
						CreateLookAt();
				}

				for (int i = 0; i < frustums_.Length; ++i)
					frustums_[i].Visible = value;
			}
		}

		public void Update(float s)
		{
			UpdateFrustums(s);
			UpdateAvoidBoxes();
			UpdateTargetBox();
		}

		private void UpdateFrustums(float s)
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
		}

		private void UpdateAvoidBoxes()
		{
			var boxes = r_.AvoidBoxes;

			if (boxes.Length != avoid_.Length)
			{
				for (int i = 0; i < avoid_.Length; ++i)
					avoid_[i].Destroy();

				avoid_ = new Sys.IGraphic[boxes.Length];

				for (int i = 0; i < boxes.Length; ++i)
					avoid_[i] = CreateAvoid();
			}


			for (int i = 0; i < boxes.Length; ++i)
			{
				avoid_[i].Position =
					r_.Person.Body.Get(BodyParts.Eyes).Position +
					r_.ReferencePart.Rotation.Rotate(boxes[i].center);

				avoid_[i].Size = boxes[i].size;
				avoid_[i].Visible = visible_;
			}
		}

		private void UpdateTargetBox()
		{
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

		private Sys.IGraphic CreateAvoid()
		{
			var g = Cue.Instance.Sys.CreateBoxGraphic(
				"RandomTargetGenerator.AvoidRender",
				Vector3.Zero, Vector3.Zero, new Color(1, 0, 0, 0.1f));

			g.Visible = visible_;

			return g;
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
