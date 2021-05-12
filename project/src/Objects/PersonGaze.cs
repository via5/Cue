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
						Vector3.Rotate(random_.Position, RandomLookAt.Ref(person_).Direction));
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

		public const float Delay = 1;

		private Person person_;
		private float e_ = Delay;
		private Vector3 pos_ = Vector3.Zero;
		private IObject avoid_ = null;
		private FrustumInfo[] frustums_ = new FrustumInfo[FrustumCount];
		private Box avoidBox_ = Box.Zero;
		private RandomLookAtRenderer render_ = null;

		public RandomLookAt(Person p)
		{
			person_ = p;

			var main = new Frustum(Near, Far);
			var fs = main.Split(XCount, YCount);

			for (int i = 0; i < fs.Length; ++i)
				frustums_[i] = new FrustumInfo(p, fs[i]);

			//render_ = new RandomLookAtRenderer(this);
		}

		public static BodyPart Ref(Person p)
		{
			return p.Body.Get(BodyParts.Chest);
		}

		public Person Person
		{
			get { return person_; }
		}

		public FrustumInfo GetFrustum(int i)
		{
			return frustums_[i];
		}

		public Box AvoidBox
		{
			get { return avoidBox_; }
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

			render_?.Update(s);

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
			pos_ = frustums_[sel].frustum.RandomPoint();
		}

		private int CheckAvoid()
		{
			if (avoid_ == null)
				return frustums_.Length;

			UpdateAvoidBox();

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

		private void UpdateAvoidBox()
		{
			var selfHead = person_.Body.Head;
			var avoidP = avoid_ as Person;

			if (avoidP == null)
			{
				avoidBox_ = new Box(
					avoid_.EyeInterest - selfHead.Position,
					new Vector3(0.2f, 0.2f, 0.2f));
			}
			else
			{
				var q = Ref(person_).Direction;

				var avoidHeadU =
					avoidP.Body.Head.Position -
					selfHead.Position +
					new Vector3(0, 0.2f, 0);

				var avoidHipU =
					avoidP.Body.Get(BodyParts.Hips).Position -
					selfHead.Position;

				var avoidHead = Vector3.RotateInv(avoidHeadU, q);
				var avoidHip = Vector3.RotateInv(avoidHipU, q);

				avoidBox_ = new Box(
					avoidHip + (avoidHead - avoidHip) / 2,
					new Vector3(0.5f, (avoidHead - avoidHip).Y, 0.5f));
			}
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


	class RandomLookAtRenderer
	{
		private RandomLookAt r_;
		private FrustumRender[] frustums_;
		private W.IGraphic avoid_ = null;

		public RandomLookAtRenderer(RandomLookAt r)
		{
			r_ = r;
			CreateFrustums();
			CreateAvoid();
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
				r_.Person.Body.Head.Position +
				Vector3.Rotate(r_.AvoidBox.center, RandomLookAt.Ref(r_.Person).Direction);

			avoid_.Size = r_.AvoidBox.size;
		}

		private void CreateFrustums()
		{
			frustums_ = new FrustumRender[RandomLookAt.FrustumCount];
			for (int i=0; i<frustums_.Length; ++i)
				frustums_[i] = new FrustumRender(r_.Person, r_.GetFrustum(i).frustum);
		}

		private void CreateAvoid()
		{
			avoid_ = Cue.Instance.Sys.CreateBoxGraphic(
				"RandomLookAt.AvoidRender",
				Vector3.Zero, Vector3.Zero, new Color(0, 0, 1, 0.1f));

			avoid_.Collision = false;
			avoid_.Visible = true;
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
				Vector3.Rotate(frustum_.NearCenter(), RandomLookAt.Ref(person_).Direction);

			near_.Direction = RandomLookAt.Ref(person_).Direction;


			far_.Position =
				person_.Body.Head.Position +
				Vector3.Rotate(frustum_.FarCenter(), RandomLookAt.Ref(person_).Direction);

			far_.Direction = RandomLookAt.Ref(person_).Direction;
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
