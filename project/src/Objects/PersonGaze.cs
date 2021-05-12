namespace Cue
{
	class Gaze
	{
		private const int LookatNothing = 0;
		private const int LookatFront = 1;
		private const int LookatCamera = 2;
		private const int LookatObject = 3;
		private const int LookatRandom = 4;
		private const int LookatPosition = 5;

		private const float AfterHeadTouchDuration = 2;

		private Person person_;
		private IEyes eyes_;
		private IGazer gazer_;
		private RandomLookAt random_;
		private IObject object_ = null;
		private int lookat_ = LookatNothing;
		private RandomRange gazeDuration_ = new RandomRange(0, 0);
		private float afterHeadTouchElapsed_ = AfterHeadTouchDuration + 1;
		private bool randomInhibited_ = false;

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
			get { return (lookat_ == LookatObject); }
		}

		public void Update(float s)
		{
			switch (lookat_)
			{
				case LookatNothing:
				case LookatPosition:
				{
					// nothing to do
					break;
				}

				case LookatFront:
				{
					eyes_.LookAt(
						person_.Body.Head?.Position ?? Vector3.Zero +
						Vector3.Rotate(new Vector3(0, 0, 1), person_.Bearing));

					break;
				}

				case LookatCamera:
				{
					eyes_.LookAt(Cue.Instance.Sys.Camera);
					break;
				}

				case LookatObject:
				{
					if (object_ != null)
						eyes_.LookAt(object_.EyeInterest);

					break;
				}

				case LookatRandom:
				{
					if (person_.Body.Head.Close)
					{
						if (!randomInhibited_)
						{
							afterHeadTouchElapsed_ = 0;
							person_.Log.Info("head touched, inhibiting random gaze");
							randomInhibited_ = true;
						}
					}
					else
					{
						if (randomInhibited_ && afterHeadTouchElapsed_ <= 0)
							person_.Log.Info("head cleared, waiting for random gaze");

						afterHeadTouchElapsed_ += s;

						if (afterHeadTouchElapsed_ > AfterHeadTouchDuration)
						{
							if (randomInhibited_)
							{
								person_.Log.Info("head cleared, resuming random gaze");
								randomInhibited_ = false;
							}

							if (random_.Update(s))
							{
								gazeDuration_.SetRange(
									person_.Personality.LookAtRandomGazeDuration);

								gazer_.Duration = gazeDuration_.Next();

								eyes_.LookAt(
									person_.Body.Head.Position +
									Vector3.Rotate(random_.Position, RandomLookAt.Ref(person_).Direction));
							}
						}
					}

					break;
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
		}

		public void LookAtCamera()
		{
			person_.Log.Info("looking at camera");
			SetLookat(LookatCamera);
		}

		public void LookAt(IObject o, bool gaze = true)
		{
			person_.Log.Info($"looking at {o} gaze={gaze}");
			object_ = o;
			gazer_.Enabled = gaze;
			SetLookat(LookatObject);
		}

		public void LookAt(Vector3 p, bool gaze = true)
		{
			person_.Log.Info($"looking at {p} gaze={gaze}");
			eyes_.LookAt(p);
			gazer_.Enabled = gaze;
			SetLookat(LookatPosition);
		}

		public void LookAtRandom(bool gaze = true)
		{
			person_.Log.Info($"looking at random gaze={gaze}");
			random_.Avoid = null;
			gazer_.Enabled = gaze;
			SetLookat(LookatRandom);
		}

		public void Avoid(IObject o, bool gaze = true)
		{
			person_.Log.Info($"looking at random, avoiding {o}, gaze={gaze}");
			random_.Avoid = o;
			random_.Reset();
			gazer_.Enabled = gaze;
			SetLookat(LookatRandom);
		}

		public void LookAtNothing()
		{
			person_.Log.Info("looking at nothing");
			eyes_.LookAtNothing();
			gazer_.Enabled = false;
			SetLookat(LookatNothing);
		}

		public void LookInFront()
		{
			person_.Log.Info("looking in front");
			gazer_.Enabled = false;
			SetLookat(LookatFront);
		}

		private void SetLookat(int i)
		{
			if (lookat_ != i)
			{
				if (lookat_ == LookatRandom)
					gazer_.Duration = person_.Personality.GazeDuration;

				lookat_ = i;
				randomInhibited_ = false;
			}
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

		private Person person_;
		private Vector3 pos_ = Vector3.Zero;
		private IObject avoid_ = null;
		private FrustumInfo[] frustums_ = new FrustumInfo[FrustumCount];
		private Box avoidBox_ = Box.Zero;
		private RandomLookAtRenderer render_ = null;
		private Duration delay_ = new Duration(0, 0);
		private bool forceNext_ = false;

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

		public void Reset()
		{
			forceNext_ = true;
		}

		public bool Update(float s)
		{
			var force = forceNext_;
			forceNext_ = false;

			delay_.SetRange(person_.Personality.LookAtRandomInterval);
			delay_.Update(s);

			if (delay_.Finished || force)
			{
				NextPosition();
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
		private FrustumRender[] frustums_ = new FrustumRender[0];
		private W.IGraphic avoid_ = null;
		private bool visible_ = false;

		public RandomLookAtRenderer(RandomLookAt r)
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
				r_.Person.Body.Head.Position +
				Vector3.Rotate(r_.AvoidBox.center, RandomLookAt.Ref(r_.Person).Direction);

			avoid_.Size = r_.AvoidBox.size;
		}

		private void CreateFrustums()
		{
			frustums_ = new FrustumRender[RandomLookAt.FrustumCount];
			for (int i = 0; i < frustums_.Length; ++i)
			{
				frustums_[i] = new FrustumRender(r_.Person, r_.GetFrustum(i).frustum);
				frustums_[i].Visible = visible_;
			}
		}

		private void CreateAvoid()
		{
			avoid_ = Cue.Instance.Sys.CreateBoxGraphic(
				"RandomLookAt.AvoidRender",
				Vector3.Zero, Vector3.Zero, new Color(0, 0, 1, 0.1f));

			avoid_.Collision = false;
			avoid_.Visible = visible_;
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

		public bool Visible
		{
			set
			{
				if (near_ != null)
					near_.Visible = value;

				if (far_ != null)
					far_.Visible = value;
			}
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
