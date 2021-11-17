namespace Cue
{
	public class GazeRender
	{
		private Person person_;
		private RandomTargetGeneratorRenderer frustums_ = null;
		private FrontPlane fp_ = null;

		public GazeRender(Person p)
		{
			person_ = p;
		}

		public bool Frustums
		{
			get
			{
				if (frustums_ == null)
					return false;
				else
					return frustums_.Enabled;
			}

			set
			{
				if (value)
				{
					if (frustums_ == null)
						frustums_ = new RandomTargetGeneratorRenderer(person_);

					frustums_.Enabled = true;
				}
				else
				{
					if (frustums_ != null)
						frustums_.Enabled = false;
				}
			}
		}

		public bool FrontPlane
		{
			get
			{
				if (fp_ == null)
					return false;
				else
					return fp_.Enabled;
			}

			set
			{
				if (value)
				{
					if (fp_ == null)
						fp_ = new FrontPlane(person_);

					fp_.Enabled = true;
				}
				else
				{
					if (fp_ != null)
						fp_.Enabled = false;
				}
			}
		}

		public void Update(float s)
		{
			frustums_?.Update(s);
			fp_?.Update(s);
		}
	}


	class FrontPlane
	{
		private Person person_;
		private Sys.IGraphic plane_ = null;
		private bool enabled_ = false;

		public FrontPlane(Person p)
		{
			person_ = p;
		}

		public bool Enabled
		{
			get
			{
				return enabled_;
			}

			set
			{
				if (value)
				{
					if (plane_ == null)
					{
						plane_ = Cue.Instance.Sys.CreateBoxGraphic(
							"Gaze.Render.FrontPlane",
							Vector3.Zero, new Vector3(5, 5, 0.01f),
							new Color(0, 0, 1, 0.1f));
					}
				}
				else
				{
					if (plane_ != null)
						plane_.Visible = false;
				}

				enabled_ = value;
			}
		}

		public void Update(float s)
		{
			if (!enabled_)
				return;

			var p = person_.Gaze.Picker.FrontPlane;
			plane_.Position = p.Point;
			plane_.Rotation = p.Rotation;
		}
	}


	class RandomTargetGeneratorRenderer
	{
		private Person person_;
		private GazeTargetPicker r_;
		private FrustumRenderer[] frustums_ = new FrustumRenderer[0];
		private Sys.IGraphic[] avoid_ = new Sys.IGraphic[0];
		private Sys.IGraphic lookAt_ = null;
		private bool enabled_ = false;

		public RandomTargetGeneratorRenderer(Person p)
		{
			person_ = p;
			r_ = person_.Gaze.Picker;
		}

		public bool Enabled
		{
			get
			{
				return enabled_;
			}

			set
			{
				enabled_ = value;

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
			if (!enabled_)
				return;

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
					r_.Person.Body.Get(BP.Eyes).Position +
					r_.ReferencePart.Rotation.Rotate(boxes[i].center);

				avoid_[i].Size = boxes[i].size;
				avoid_[i].Visible = enabled_;
			}
		}

		private void UpdateTargetBox()
		{
			if (r_.HasTarget)
			{
				var b = r_.CurrentTargetAABox;

				lookAt_.Visible = true;

				lookAt_.Position =
					r_.Person.Body.Get(BP.Eyes).Position +
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
					BP.Head, BP.Chest);

				frustums_[i].Visible = enabled_;
			}
		}

		private Sys.IGraphic CreateAvoid()
		{
			var g = Cue.Instance.Sys.CreateBoxGraphic(
				"Gaze.Render.Random.Avoid",
				Vector3.Zero, Vector3.Zero, new Color(1, 0, 0, 0.1f));

			g.Visible = enabled_;

			return g;
		}

		private void CreateLookAt()
		{
			lookAt_ = Cue.Instance.Sys.CreateBoxGraphic(
				"Gaze.Render.Random.LookAt",
				Vector3.Zero, Vector3.Zero, new Color(0, 0, 1, 0.1f));

			lookAt_.Visible = enabled_;
		}
	}
}
