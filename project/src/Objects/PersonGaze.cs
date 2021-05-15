using System.Collections.Generic;

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
					if (person_.Body.Head.Grabbed)
					{
						if (Cue.Instance.Player != null)
							eyes_.LookAt(Cue.Instance.Player.EyeInterest);
					}

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
								eyes_.LookAt(random_.Position);
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

		public override string ToString()
		{
			switch (lookat_)
			{
				case LookatNothing:
					return "nothing";

				case LookatFront:
					return "front";

				case LookatCamera:
					return "camera";

				case LookatObject:
					return $"object {object_.ID} {object_.EyeInterest}";

				case LookatRandom:
					return random_.ToString();

				case LookatPosition:
					return $"pos {eyes_.Position}";

				default:
					return $"?{lookat_}";
			}
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


	interface IRandomTarget
	{
		Vector3 Position { get; }
		void Reset();
		bool NextPosition(RandomLookAt r);
	}


	class BodyPartTarget : IRandomTarget
	{
		private BodyPart part_;

		public BodyPartTarget(BodyPart p)
		{
			part_ = p;
		}

		public Vector3 Position
		{
			get { return part_.Position; }
		}

		public void Reset()
		{
		}

		public bool NextPosition(RandomLookAt r)
		{
			return r.CanLookAt(part_.Position);
		}

		public override string ToString()
		{
			return $"bodypart {part_}";
		}
	}


	class RandomPointTarget : IRandomTarget
	{
		private Vector3 pos_ = Vector3.Zero;

		public Vector3 Position
		{
			get { return pos_; }
		}

		public void Reset()
		{
		}

		public bool NextPosition(RandomLookAt r)
		{
			var f = r.RandomAvailableFrustum();
			if (f.Empty)
				return false;

			var rp = f.RandomPoint();

			pos_ =
				r.Person.Body.Head.Position +
				Vector3.Rotate(rp, r.Person.Body.Get(BodyParts.Chest).Direction);

			return true;
		}

		public override string ToString()
		{
			return $"point {pos_}";
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

		private int[] interestingBodyParts_ = new int[]
		{
			BodyParts.Head, BodyParts.Hips, BodyParts.Chest
		};

		private Person person_;
		private IObject avoid_ = null;
		private FrustumInfo[] frustums_ = new FrustumInfo[FrustumCount];
		private Box avoidBox_ = Box.Zero;
		private RandomLookAtRenderer render_ = null;
		private Duration delay_ = new Duration(0, 0);
		private List<IRandomTarget> targets_ = new List<IRandomTarget>();
		private ShuffledIndex currentTarget_;

		public RandomLookAt(Person p)
		{
			person_ = p;

			var main = new Frustum(Near, Far);
			var fs = main.Split(XCount, YCount);

			for (int i = 0; i < fs.Length; ++i)
				frustums_[i] = new FrustumInfo(p, fs[i]);

			currentTarget_ = new ShuffledIndex(targets_);

			//render_ = new RandomLookAtRenderer(this);
		}

		public BodyPart ReferencePart
		{
			get { return person_.Body.Get(BodyParts.Chest); }
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
			get
			{
				if (currentTarget_.HasIndex)
					return targets_[currentTarget_.Index].Position;
				else
					return Vector3.Zero;
			}
		}

		public bool CanLookAt(Vector3 p)
		{
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

		public void Reset()
		{
			currentTarget_.Reset();

			for (int i = 0; i < targets_.Count; ++i)
				targets_[i].Reset();
		}

		public bool Update(float s)
		{
			if (targets_.Count == 0)
				GetTargets();

			delay_.SetRange(person_.Personality.LookAtRandomInterval);
			//delay_.SetRange(1, 1);
			delay_.Update(s);

			if (delay_.Finished || !currentTarget_.HasIndex)
			{
				NextTarget();
				return true;
			}
			else if (currentTarget_.HasIndex)
			{
				if (!CanLookAt(targets_[currentTarget_.Index].Position))
				{
					NextTarget();
					return true;
				}
			}

			render_?.Update(s);

			return false;
		}

		public override string ToString()
		{
			string s = "random: ";

			if (currentTarget_.HasIndex)
				s += targets_[currentTarget_.Index].ToString();
			else
				s += "no target";

			if (avoid_ != null)
				s += $", avoid {avoid_}";

			return s;
		}

		private void GetTargets()
		{
			targets_.Clear();

			targets_.Add(new RandomPointTarget());

			for (int i = 0; i < Cue.Instance.Persons.Count; ++i)
			{
				if (Cue.Instance.Persons[i] == person_)
					continue;

				for (int ii = 0; ii < interestingBodyParts_.Length; ++ii)
				{
					targets_.Add(new BodyPartTarget(
						Cue.Instance.Persons[i].Body.Get(interestingBodyParts_[ii])));
				}
			}
		}

		private void NextTarget()
		{
			ResetFrustums();
			UpdateFrustums();

			currentTarget_.Next((i) =>
			{
				if (!targets_[i].NextPosition(this))
					return false;

				return true;
			});
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
				var q = ReferencePart.Direction;

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


	class RandomLookAtRenderer
	{
		private RandomLookAt r_;
		private FrustumRenderer[] frustums_ = new FrustumRenderer[0];
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
				Vector3.Rotate(r_.AvoidBox.center, r_.ReferencePart.Direction);

			avoid_.Size = r_.AvoidBox.size;
		}

		private void CreateFrustums()
		{
			frustums_ = new FrustumRenderer[RandomLookAt.FrustumCount];
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
				"RandomLookAt.AvoidRender",
				Vector3.Zero, Vector3.Zero, new Color(0, 0, 1, 0.1f));

			avoid_.Collision = false;
			avoid_.Visible = visible_;
		}
	}


	class FrustumRenderer
	{
		private Person person_;
		private Frustum frustum_;
		private Color color_ = Color.Zero;
		private W.IGraphic near_ = null;
		private W.IGraphic far_ = null;
		private int offset_, rot_;

		public FrustumRenderer(Person p, Frustum f, int offsetBodyPart, int rotationBodyPart)
		{
			person_ = p;
			frustum_ = f;
			offset_ = offsetBodyPart;
			rot_ = rotationBodyPart;
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
				Cue.LogInfo($"{frustum_.nearTL} {frustum_.nearTR} {frustum_.nearBL} {frustum_.nearBR}");
			}

			near_.Position =
				RefOffset +
				Vector3.Rotate(frustum_.NearCenter(), RefDirection);

			near_.Direction = RefDirection;


			far_.Position =
				RefOffset +
				Vector3.Rotate(frustum_.FarCenter(), RefDirection);

			far_.Direction = RefDirection;
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

		private Vector3 RefOffset
		{
			get
			{
				if (offset_ == BodyParts.None)
					return person_.Position;
				else
					return person_.Body.Get(offset_).Position;
			}
		}

		private Vector3 RefDirection
		{
			get
			{
				if (rot_ == BodyParts.None)
					return person_.Direction;
				else
					return person_.Body.Get(rot_).Direction;
			}
		}
	}
}
