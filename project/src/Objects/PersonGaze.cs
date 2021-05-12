namespace Cue
{
	class Gaze
	{
		private Person person_;
		private IEyes eyes_;
		private IGazer gazer_;
		private bool interested_ = false;
		private RandomLookAt random_;
		private bool randomActive_ = false;

		public Gaze(Person p)
		{
			person_ = p;
			random_ = new RandomLookAt(p);
			eyes_ = Integration.CreateEyes(p);
			gazer_ = Integration.CreateGazer(p);
		}

		public IEyes Eyes { get { return eyes_; } }
		public IGazer Gazer { get { return gazer_; } }

		public bool HasInterestingTarget
		{
			get { return interested_; }
		}

		public void Update(float s)
		{
			if (randomActive_)
			{
				if (random_.Update(s))
				{
					eyes_.LookAt(
						person_.Body.Head.Position +
						Vector3.Rotate(random_.Position, person_.Bearing));
				}
			}

			eyes_.Update(s);
			gazer_.Update(s);
		}

		public void LookAtDefault()
		{
			if (person_ == Cue.Instance.Player)
				LookAtNothing();
			else if (Cue.Instance.Player == null)
				LookAtCamera();
			else
				LookAt(Cue.Instance.Player);

			interested_ = false;
			randomActive_ = false;
		}

		public void LookAtCamera()
		{
			person_.Log.Info("looking at camera");
			eyes_.LookAtCamera();
			interested_ = false;
			randomActive_ = false;
		}

		public void LookAt(IObject o, bool gaze = true)
		{
			person_.Log.Info($"looking at {o} gaze={gaze}");
			eyes_.LookAt(o);
			gazer_.Enabled = gaze;
			interested_ = true;
			randomActive_ = false;
		}

		public void LookAt(Vector3 p, bool gaze = true)
		{
			person_.Log.Info($"looking at {p} gaze={gaze}");
			eyes_.LookAt(p);
			gazer_.Enabled = gaze;
			interested_ = false;
			randomActive_ = false;
		}

		public void LookAtRandom(bool gaze = true)
		{
			person_.Log.Info($"looking at random gaze={gaze}");
			randomActive_ = true;
			random_.Avoid = null;
			gazer_.Enabled = gaze;
		}

		public void Avoid(IObject o, bool gaze = true)
		{
			person_.Log.Info($"looking at random, avoiding {o}, gaze={gaze}");
			randomActive_ = true;
			random_.Avoid = o;
			gazer_.Enabled = gaze;
		}

		public void LookAtNothing()
		{
			person_.Log.Info("looking at nothing");
			eyes_.LookAtNothing();
			gazer_.Enabled = false;
			interested_ = false;
			randomActive_ = false;
		}

		public void LookInFront()
		{
			person_.Log.Info("looking in front");
			eyes_.LookInFront();
			gazer_.Enabled = false;
			interested_ = false;
			randomActive_ = false;
		}
	}


	class RandomLookAt
	{
		class FrustumInfo
		{
			public Frustum frustum;
			public bool avoid;
			public bool selected;
			public FrustumRender render;

			public FrustumInfo(Person p, Frustum f)
			{
				frustum = f;
				avoid = false;
				selected = false;
				render = new FrustumRender(p, f);
			}
		}

		private Vector3 Near = new Vector3(2, 1, 0.1f);
		private Vector3 Far = new Vector3(4, 2, 2);

		private const int XCount = 5;
		private const int YCount = 5;
		private const int FrustumCount = XCount * YCount;

		private const float Delay = 1;

		private Person person_;
		private float e_ = Delay;
		private Vector3 pos_ = Vector3.Zero;
		private IObject avoid_ = null;
		private FrustumInfo[] frustums_ = new FrustumInfo[FrustumCount];
		private W.IGraphic avoidRender_ = null;

		public RandomLookAt(Person p)
		{
			person_ = p;

			var main = new Frustum(Near, Far);
			var fs = main.Split(XCount, YCount);

			for (int i = 0; i < fs.Length; ++i)
				frustums_[i] = new FrustumInfo(p, fs[i]);
		}

		private void CreateAvoidRender()
		{
			avoidRender_ = Cue.Instance.Sys.CreateBoxGraphic(
				"RandomLookAt.AvoidRender",
				Vector3.Zero, Vector3.Zero, new Color(0, 0, 1, 0.1f));

			avoidRender_.Collision = false;
			avoidRender_.Visible = true;
		}

		public IObject Avoid
		{
			get { return avoid_; }
			set { avoid_ = value; }
		}

		public Vector3 Position
		{
			get { return pos_; }
		}

		public bool Update(float s)
		{
			e_ += s;

			if (e_ >= Delay)
			{
				NextPosition();
				e_ = 0;
				return true;
			}

			for (int i = 0; i < frustums_.Length; ++i)
				frustums_[i].render.Update(s);

			return false;
		}

		private void NextPosition()
		{
			for (int i = 0; i < frustums_.Length; ++i)
			{
				frustums_[i].avoid = false;
				frustums_[i].selected = false;
			}

			int av = CheckAvoid();
			if (av == 0)
			{
				Cue.LogError("nowhere to look at");
				return;
			}

			var sel = RandomAvailableFrustum(av);
			frustums_[sel].selected = true;
			pos_ = frustums_[sel].frustum.Random();

			for (int i = 0; i < frustums_.Length; ++i)
			{
				if (frustums_[i].avoid)
					frustums_[i].render.Color = new Color(1, 0, 0, 0.1f);
				else if (frustums_[i].selected)
					frustums_[i].render.Color = new Color(1, 1, 1, 0.1f);
				else
					frustums_[i].render.Color = new Color(0, 1, 0, 0.1f);
			}
		}

		private int CheckAvoid()
		{
			if (avoid_ == null)
				return frustums_.Length;

			int av = 0;

			var selfHead = person_.Body.Head;

			var avoidP = avoid_ as Person;
			Box avoidBox;

			if (avoidP != null)
			{
				var q = person_.Body.Get(BodyParts.Chest).Direction;

				var avoidHeadU =
					avoidP.Body.Head.Position -
					selfHead.Position +
					new Vector3(0, 0.2f, 0);

				var avoidHipU =
					avoidP.Body.Get(BodyParts.Hips).Position -
					selfHead.Position;

				var avoidHead = Vector3.RotateInv(avoidHeadU, q);
				var avoidHip = Vector3.RotateInv(avoidHipU, q);

				avoidBox = new Box(
					avoidHip + (avoidHead - avoidHip) / 2,
					new Vector3(0.5f, (avoidHead - avoidHip).Y, 0.5f));
			}
			else
			{
				avoidBox = new Box(
					avoid_.EyeInterest - selfHead.Position,
					new Vector3(0.2f, 0.2f, 0.2f));
			}

			if (avoidRender_ == null)
				CreateAvoidRender();

			avoidRender_.Position =
				selfHead.Position +
				Vector3.Rotate(avoidBox.center, person_.Body.Get(BodyParts.Chest).Direction);

			avoidRender_.Size = avoidBox.size;

			for (int i = 0; i < frustums_.Length; ++i)
			{
				if (frustums_[i].frustum.TestPlanesAABB(avoidBox))
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

		private int RandomAvailableFrustum(int av)
		{
			int fi = U.RandomInt(0, av - 1);

			for (int i = 0; i < frustums_.Length; ++i)
			{
				if (frustums_[i].avoid)
					continue;

				if (fi == 0)
					return i;

				--fi;
			}

			Cue.LogError($"RandomAvailableFrustum: fi={fi} av={av} l={frustums_.Length}");
			return 0;
		}
	}


	class FrustumRender
	{
		private Person person_;
		private Frustum frustum_;
		private Color color_ = Color.Zero;
		private W.IGraphic near_ = null;
		private W.IGraphic far_ = null;

		public FrustumRender(Person p, Frustum f)
		{
			person_ = p;
			frustum_ = f;
		}

		public void Update(float s)
		{
			if (near_ == null)
			{
				near_ = Create(frustum_.NearSize());
				far_ = Create(frustum_.FarSize());
			}

			near_.Position =
				person_.Body.Head.Position +
				Vector3.Rotate(frustum_.NearCenter(), person_.Body.Get(BodyParts.Chest).Direction);

			near_.Direction = person_.Body.Get(BodyParts.Chest).Direction;


			far_.Position =
				person_.Body.Head.Position +
				Vector3.Rotate(frustum_.FarCenter(), person_.Body.Get(BodyParts.Chest).Direction);

			far_.Direction = person_.Body.Get(BodyParts.Chest).Direction;
		}

		public Color Color
		{
			set
			{
				color_ = value;

				if (near_ != null)
					near_.Color = color_;

				if (far_ != null)
					far_.Color = color_;
			}
		}

		private W.IGraphic Create(Vector3 size)
		{
			var g = Cue.Instance.Sys.CreateBoxGraphic(
				"FrustumRender.near", Vector3.Zero, size, Color.Zero);

			g.Collision = false;
			g.Color = color_;
			g.Visible = true;

			return g;
		}
	}
}
