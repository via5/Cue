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
		private Duration delay_ = new Duration();
		private IGazeLookat[] targets_ = new IGazeLookat[0];
		private int currentTarget_ = -1;
		private string lastString_ = "";

		public GazeTargetPicker(Person p)
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

		public bool CanLookAt(IGazeLookat t)
		{
			if (t.Weight == 0)
				return false;

			var p = t.Position;

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

		public bool Update(float s)
		{
			bool changed = false;

			delay_.Update(s);

			if (delay_.Finished || !HasTarget)
			{
				NextTarget();
				changed = true;
			}
			else if (HasTarget)
			{
				if (!CanLookAt(targets_[currentTarget_]))
				{
					NextTarget();
					changed = true;
				}
			}

			render_?.Update(s);

			return changed;
		}

		public override string ToString()
		{
			if (HasTarget)
				return $"j={currentTarget_} {targets_[currentTarget_]}";
			else
				return "no target";
		}

		private void NextTarget()
		{
			delay_.SetRange(person_.Personality.LookAtRandomInterval);
			//delay_ = new Duration(1, 1);

			lastString_ = "";

			ResetFrustums();
			UpdateFrustums();


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
							lastString_ += $" j={j} t={targets_[j]}";
							log_.Verbose($"picked {targets_[j]}");
							currentTarget_ = j;
							return;
						}
						else
						{
							lastString_ += $" {j}=X";
						}

						break;
					}

					r -= targets_[j].Weight;
				}
			}

			lastString_ += $" NONE";
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
			if (!UpdateAvoidBox())
				return frustums_.Length;

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

		private bool UpdateAvoidBox()
		{
			// todo: must support multiple avoid boxes
			IObject avoidO = null;

			for (int i = 0; i < Cue.Instance.AllObjects.Count; ++i)
			{
				if (person_.Gaze.ShouldAvoid(Cue.Instance.AllObjects[i]))
				{
					avoidO = Cue.Instance.AllObjects[i];
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
		private W.IGraphic avoid_ = null;
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
				r_.ReferencePart.Rotation.Rotate(r_.AvoidBox.center);

			avoid_.Size = r_.AvoidBox.size;
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
				Vector3.Zero, Vector3.Zero, new Color(0, 0, 1, 0.1f));

			avoid_.Collision = false;
			avoid_.Visible = visible_;
		}
	}
}
