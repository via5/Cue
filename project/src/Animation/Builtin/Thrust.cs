using System.Collections.Generic;

namespace Cue.Proc
{
	abstract class BasicThrustProcAnimation : BasicProcAnimation
	{
		protected struct Config
		{
			public float durationMin;
			public float durationMax;
			public float durationWin;
			public float durationInterval;
			public bool checkDirection;
			public float hipBackForceMaxDuration;
		}

		protected struct ForceConfig
		{
			public float hipForceMin;
			public float hipForceMax;
			public float hipForceWin;
			public Vector3 hipTorqueMin;
			public Vector3 hipTorqueMax;
			public Vector3 hipTorqueWin;
		}

		private struct Render
		{
			public Sys.IGraphic bp, targetBp;
			public Sys.ILineGraphic dir;

			public void Destroy()
			{
				if (bp != null)
				{
					bp.Destroy();
					bp = null;
				}

				if (targetBp != null)
				{
					targetBp.Destroy();
					targetBp = null;
				}

				if (dir != null)
				{
					dir.Destroy();
					dir = null;
				}
			}
		}

		private const float DirectionChangeMaxDistance = 0.01f;
		private const float ForceFarDistance = 0.07f;
		private const float ForceCloseDistance = 0.04f;
		private const float ForceChangeMaxAmount = 0.02f;

		private float chestTorqueMin_ = -10;
		private float chestTorqueMax_ = -45;
		private float chestTorqueWin_ = 40;
		private float chestForceMin_ = -150;
		private float chestForceMax_ = 150;
		private float headTorqueMin_ = 0;
		private float headTorqueMax_ = -10;
		private float headTorqueWin_ = 5;
		private float headForceMin_ = -20;
		private float headForceMax_ = 20;
		private Force hipForce_ = null;
		private Force hipTorque_ = null;

		private float lastForceFactor_ = 0;
		private Vector3 lastDir_ = Vector3.Zero;
		private Person receiver_ = null;
		private Config config_;
		private ForceConfig fconfig_;
		private Force[] forces_ = new Force[0];

		private Render render_ = new Render();

		private IEasing downEasing_ = new SinusoidalEasing();
		private IEasing[] upEasings_ = new IEasing[]
		{
			new SinusoidalEasing(),
			new CubicInEasing(),
			new QuintInEasing()
		};

		private Duration easingChange_ = new Duration(0, 5);

		private IEasing UpEasing()
		{
			return upEasings_[0];
		}

		private IEasing DownEasing()
		{
			return downEasing_;
		}


		protected BasicThrustProcAnimation(string name, Config c)
			: base(name, false, ScaleMultiplier)
		{
			config_ = c;

			RootGroup.Sync = new DurationSync(
				new Duration(
					config_.durationMin, config_.durationMax,
					config_.durationInterval, config_.durationInterval,
					config_.durationWin, new CubicOutEasing()),
				null, null, null,
				DurationSync.Loop | DurationSync.ResetBetween);

			RootGroup.Sync.Slaps = true;

			RootGroup.AddTarget(new Force(
				"hipForce", Force.AbsoluteForce, BP.Hips,
				Vector3.Zero, Vector3.Zero, config_.durationInterval, Vector3.Zero,
				new ParentTargetSync(), Force.ApplyOnSource,
				UpEasing(), DownEasing()));

			RootGroup.AddTarget(new Force(
				"hipTorque", Force.RelativeTorque, BP.Hips,
				Vector3.Zero, Vector3.Zero, config_.durationInterval, Vector3.Zero,
				new ParentTargetSync(), Force.ApplyOnSource,
				UpEasing(), DownEasing()));

			RootGroup.AddTarget(new Force(
				"", Force.RelativeTorque, BP.Chest,
				new Vector3(chestTorqueMin_, 0, 0),
				new Vector3(chestTorqueMax_, 0, 0),
				config_.durationInterval, new Vector3(chestTorqueWin_, 0, 0),
				new ParentTargetSync(), Force.ApplyOnSource,
				UpEasing(), DownEasing()));

			RootGroup.AddTarget(new Force(
				"", Force.RelativeTorque, BP.Head,
				new Vector3(headTorqueMin_, 0, 0),
				new Vector3(headTorqueMax_, 0, 0),
				config_.durationInterval, new Vector3(headTorqueWin_, 0, 0),
				new ParentTargetSync(), Force.ApplyOnSource,
				UpEasing(), DownEasing()));


			RootGroup.AddTarget(new Force(
				"", Force.RelativeForce, BP.Chest,
				new Vector3(0, 0, chestForceMin_), new Vector3(0, 0, chestForceMax_), null, Vector3.Zero,
				new DurationSync(
					new Duration(0.5f, 3), null,
					new Duration(0, 3), null,
					DurationSync.Loop),
				Force.ApplyOnSource,
				UpEasing(), DownEasing()));


			RootGroup.AddTarget(new Force(
				"", Force.RelativeForce, BP.Head,
				new Vector3(0, 0, headForceMin_), new Vector3(0, 0, headForceMax_),
				null, Vector3.Zero,
				new DurationSync(
					new Duration(0.5f, 3), null,
					new Duration(0, 3), null,
					DurationSync.Loop),
				Force.ApplyOnSource,
				UpEasing(), DownEasing()));

			// target forces

			RootGroup.AddTarget(new Force(
				"", Force.RelativeTorque, BP.Head,
				new Vector3(headTorqueMin_ * 0.5f, 0, 0),
				new Vector3(headTorqueMax_ * 0.5f, 0, 0),
				config_.durationInterval, new Vector3(headTorqueWin_, 0, 0),
				new ParentTargetSync(), Force.ApplyOnTarget,
				UpEasing(), DownEasing()));

			RootGroup.AddTarget(new Force(
				"", Force.RelativeTorque, BP.Chest,
				new Vector3(chestTorqueMin_ * 0.75f, 0, 0),
				new Vector3(chestTorqueMax_ * 0.75f, 0, 0),
				config_.durationInterval, new Vector3(chestTorqueWin_, 0, 0),
				new ParentTargetSync(), Force.ApplyOnTarget,
				UpEasing(), DownEasing()));
		}

		protected Person Receiver
		{
			get { return receiver_; }
		}

		public override bool Start(Person p, AnimationContext cx)
		{
			receiver_ = cx.ps as Person;

			SetEnergySource(receiver_);
			RootGroup.Sync.SlapTargets = new Person[] { p, receiver_ };

			hipForce_ = FindTarget("hipForce") as Force;
			if (hipForce_ == null)
			{
				Log.Error("hipForce not found");
			}
			else
			{
				hipForce_.BeforeNextAction = () => UpdateForce(hipForce_);
			}

			if (!DoStart())
				return false;

			hipTorque_ = FindTarget("hipTorque") as Force;
			if (hipTorque_ == null)
			{
				Log.Error("hipTorque not found");
			}
			else
			{
				fconfig_ = GetForceConfig(p, receiver_);

				hipTorque_.SetRange(
					fconfig_.hipTorqueMin,
					fconfig_.hipTorqueMax,
					fconfig_.hipTorqueWin);
			}


			var list = new List<Force>();
			foreach (var t in RootGroup.Targets)
			{
				var f = t as Force;
				if (f != null)
					list.Add(f);
			}

			forces_ = list.ToArray();

			if (!base.Start(p, cx))
				return false;

			SetAsMainSync(receiver_);
			Reset();
			UpdateForces(true);

			return true;
		}

		public override void Update(float s)
		{
			base.Update(s);
			CheckEasings(s);
			CheckDebug();
		}

		private void CheckEasings(float s)
		{
			easingChange_.Update(s, 1.0f);

			if (easingChange_.Finished)
			{
				if (upEasings_.Length > 0)
				{
					int e = U.RandomInt(0, upEasings_.Length - 1);

					for (int i = 0; i < forces_.Length; ++i)
						forces_[i].SetEasings(upEasings_[e], downEasing_);
				}
			}
		}

		public override void Stopped()
		{
			base.Stopped();
			render_.Destroy();
		}

		private void CheckDebug()
		{
			if (DebugRender)
			{
				if (render_.bp == null)
				{
					render_.bp = Cue.Instance.Sys.CreateBoxGraphic(
						"thrustBp",
						new Box(Vector3.Zero, new Vector3(0.01f, 0.01f, 0.01f)),
						new Color(0, 0, 1, 0.2f));
				}

				if (render_.targetBp == null)
				{
					render_.targetBp = Cue.Instance.Sys.CreateBoxGraphic(
						"thrustTargetBp",
						new Box(Vector3.Zero, new Vector3(0.01f, 0.01f, 0.01f)),
						new Color(0, 0, 1, 0.2f));
				}

				if (render_.dir == null)
				{
					render_.dir = Cue.Instance.Sys.CreateLineGraphic(
						"thrustDir",
						Vector3.Zero, Vector3.Zero,
						new Color(1, 0, 0, 0.2f));
				}

				var thisBP = GetThisBP();
				render_.bp.Position = thisBP.Position;
				render_.bp.Rotation = thisBP.Rotation;
				render_.bp.Visible = true;

				if (receiver_ == null)
				{
					render_.targetBp.Visible = false;
					render_.dir.Visible = false;
				}
				else
				{
					var targetBP = GetTargetBP();
					render_.targetBp.Position = targetBP.Position;
					render_.targetBp.Rotation = targetBP.Rotation;
					render_.targetBp.Visible = true;

					render_.dir.SetDirection(thisBP.Position, lastDir_, 0.1f);
					render_.dir.Visible = true;
				}
			}
			else
			{
				if (render_.bp != null)
					render_.bp.Visible = false;

				if (render_.targetBp != null)
					render_.targetBp.Visible = false;

				if (render_.dir != null)
					render_.dir.Visible = false;
			}
		}

		protected abstract ForceConfig GetForceConfig(Person self, Person receiver);
		protected abstract bool DoStart();

		private void UpdateForces(bool alwaysUpdate = false)
		{
			if (hipForce_ != null)
			{
				if (hipForce_.Done || alwaysUpdate)
					UpdateForce(hipForce_);
			}
		}

		private BodyPart GetThisBP()
		{
			// use anus instead of genitals
			//
			// problems start happening at high excitement because the genitals
			// can be stuck pretty far forwards, sometimes even _behind_ the
			// target genitals, which breaks the direction
			//
			// the anus is positioned behind the genitals and its relationship
			// with the genitals is pretty good

			return Person.Body.Get(BP.Anus);
		}

		private BodyPart GetTargetBP()
		{
			return receiver_.Body.Get(receiver_.Body.GenitalsBodyPart);
		}

		// gets the direction between the genitals so the forces go that way
		//
		// changes in direction need to be dampened because if the hips are
		// to the side of the target, they'll come down at an angle and
		// bounce back in the opposite direction
		//
		// this would reverse the direction for the next thrust, which would
		// just compound the direction changes infinitely
		//
		// dampening the direction changes eventually centers the movement
		//
		private Vector3 GetDirection()
		{
			Vector3 currentDir;

			if (config_.checkDirection && receiver_ != null)
			{
				var thisBP = GetThisBP();
				var targetBP = GetTargetBP();

				// direction between genitals
				currentDir = (targetBP.Position - thisBP.Position).Normalized;
			}
			else
			{
				var rot = Person.Body.Get(BP.Hips).Rotation;
				currentDir = rot.Rotate(new Vector3(0, 0, 1)).Normalized;
			}

			Vector3 dir;

			if (lastDir_ == Vector3.Zero)
			{
				dir = currentDir;
			}
			else
			{
				dir = Vector3.MoveTowards(
					lastDir_, currentDir, DirectionChangeMaxDistance);
			}

			lastDir_ = dir;

			return dir;
		}

		// gets a [0, 1] factor that multiplies the maximum force applied, based
		// on the distance between genitals
		//
		// this avoids forces that are too large when the genitals are close
		//
		// changes in forces need to be dampened because the hips don't always
		// have time to fully move back up before the distance is checked, which
		// would constantly alternate between different force factors
		//
		private float GetForceFactor()
		{
			if (receiver_ == null)
				return 0.7f;

			float scaleMin = 0.65f;
			float scaleMax = 1.0f;
			float scaleRange = scaleMax - scaleMin;

			float scale = U.Clamp(receiver_.Body.Scale, scaleMin, scaleMax);
			float scaleF = (scale - scaleMin) / scaleRange;


			float minForce = 0.8f;
			float maxForce = 1.0f;
			float forceRange = maxForce - minForce;

			lastForceFactor_ = minForce + scaleF * forceRange;

			return lastForceFactor_;
		}

		private void UpdateForce(Force f)
		{
			var p = GetForceFactor();
			var dir = GetDirection();

			float fmin = fconfig_.hipForceMin * p;
			float fmax = fconfig_.hipForceMax * p;

			f.SetRangeWithDirection(fmin, fmax, fconfig_.hipForceWin, dir);
			hipForce_.Backforce = HipBackForce;
		}

		private bool HipBackForce
		{
			get
			{
				if (config_.hipBackForceMaxDuration > 0)
				{
					var sync = RootGroup?.Sync;
					if (sync != null)
					{
						float t = sync.CurrentDurationTime;
						if (t < config_.hipBackForceMaxDuration)
							return true;
					}
				}

				return false;
			}
		}

		public override string ToDetailedString()
		{
			return
				base.ToDetailedString() + "\n" +
				$"ff={lastForceFactor_} dir={lastDir_}";
		}
	}


	class ThrustProcAnimation : BasicThrustProcAnimation
	{
		public ThrustProcAnimation()
			: base("cueThrust", GetConfig())
		{
		}

		public override BuiltinAnimation Clone()
		{
			var a = new ThrustProcAnimation();
			a.CopyFrom(this);
			return a;
		}

		protected override bool DoStart()
		{
			if (Receiver == null)
			{
				Log.Error("no receiver");
				return false;
			}

			return true;
		}

		protected override ForceConfig GetForceConfig(Person self, Person receiver)
		{
			var c = new ForceConfig();

			c.hipForceMin = 300;
			c.hipForceMax = 1250;
			c.hipForceWin = 300;

			if (self.Body.HasPenis)
			{
				c.hipTorqueMin = new Vector3(-50, 0, 0);
				c.hipTorqueMax = new Vector3(-150, 0, 0);
				c.hipTorqueWin = new Vector3(50, 0, 0);
			}
			else
			{
				c.hipTorqueMin = new Vector3(0, 0, 0);
				c.hipTorqueMax = new Vector3(-30, 0, 0);
				c.hipTorqueWin = new Vector3(10, 0, 0);
			}

			return c;
		}

		private static Config GetConfig()
		{
			var c = new Config();

			c.durationMin = 1.0f;
			c.durationMax = 0.09f;
			c.durationWin = 0.06f;
			c.durationInterval = 10;
			c.checkDirection = true;
			c.hipBackForceMaxDuration = 0;

			return c;
		}
	}


	class TribProcAnimation : BasicThrustProcAnimation
	{
		public TribProcAnimation()
			: base("cueTrib", GetConfig())
		{
		}

		public override BuiltinAnimation Clone()
		{
			var a = new TribProcAnimation();
			a.CopyFrom(this);
			return a;
		}

		protected override bool DoStart()
		{
			return true;
		}

		protected override ForceConfig GetForceConfig(Person self, Person receiver)
		{
			var c = new ForceConfig();

			c.hipForceMin = 200;
			c.hipForceMax = 400;
			c.hipForceWin = 50;

			c.hipTorqueMin = new Vector3(-20, 0, 0);
			c.hipTorqueMax = new Vector3(-150, 0, 0);
			c.hipTorqueWin = new Vector3(20, 0, 0);

			return c;
		}

		private static Config GetConfig()
		{
			var c = new Config();

			c.durationMin = 1.0f;
			c.durationMax = 0.12f;
			c.durationWin = 0.12f;
			c.durationInterval = 10;
			c.checkDirection = false;
			c.hipBackForceMaxDuration = 0.15f;

			return c;
		}
	}
}
