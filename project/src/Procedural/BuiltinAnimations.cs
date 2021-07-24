using System.Collections.Generic;

namespace Cue.Proc
{
	class BuiltinAnimations
	{
		public static List<Animation> Get()
		{
			var list = new List<Animation>();

			list.Add(Stand(PersonState.Walking));
			list.Add(Stand(PersonState.Standing));
			list.Add(Sex());

			return list;
		}

		private static Animation Stand(int from)
		{
			var a = new ProcAnimation("backToNeutral");

			var s = new ElapsedSync(1);

			a.AddTarget(new Controller("headControl", new Vector3(0, 1.6f, 0), new Vector3(0, 0, 0), s.Clone()));
			a.AddTarget(new Controller("chestControl", new Vector3(0, 1.4f, 0), new Vector3(20, 0, 0), s.Clone()));
			a.AddTarget(new Controller("hipControl", new Vector3(0, 1.1f, 0), new Vector3(340, 10, 0), s.Clone()));
			a.AddTarget(new Controller("lHandControl", new Vector3(-0.2f, 0.9f, 0), new Vector3(0, 10, 90), s.Clone()));
			a.AddTarget(new Controller("rHandControl", new Vector3(0.2f, 0.9f, 0), new Vector3(0, 0, 270), s.Clone()));
			a.AddTarget(new Controller("lFootControl", new Vector3(-0.1f, 0, 0), new Vector3(20, 10, 0), s.Clone()));
			a.AddTarget(new Controller("rFootControl", new Vector3(0.1f, 0, -0.1f), new Vector3(20, 10, 0), s.Clone()));

			return new Animation(
				Animation.TransitionType,
				from, PersonState.Standing,
				PersonState.None, Sexes.Any, a);
		}

		private static Animation Sex()
		{
			var a = new SexProcAnimation();

			return new Animation(
				Animation.SexType,
				PersonState.None, PersonState.None,
				PersonState.None, Sexes.Any, a);
		}
	}


	class SexProcAnimation : ProcAnimation
	{
		private const float DirectionCheckInterval = 1;

		private float hipForceMin_ = 300;
		private float hipForceMax_ = 1000;
		private float hipTorqueMin_ = 0;
		private float hipTorqueMax_ = -20;
		private float chestTorqueMin_ = -20;
		private float chestTorqueMax_ = -40;
		private float headTorqueMin_ = 0;
		private float headTorqueMax_ = -10;
		private float durationMin_ = 1;
		private float durationMax_ = 0.1f;
		private float durationWin_ = 0.15f;
		private float durationInterval_ = 10;
		private Force[] forces_ = null;

		public SexProcAnimation()
			: base("procSex", false)
		{
			var g = new ConcurrentTargetGroup(
				"g", new Duration(), new Duration(), true,
				new SlidingDurationSync(
					new SlidingDuration(
						durationMin_, durationMax_,
						durationInterval_, durationInterval_,
						durationWin_, new CubicOutEasing()),
					new SlidingDuration(
						durationMin_, durationMax_,
						durationInterval_, durationInterval_,
						durationWin_, new CubicOutEasing()),
					new Duration(0, 0), new Duration(0, 0),
					SlidingDurationSync.Loop | SlidingDurationSync.ResetBetween));


			g.AddTarget(new Force(
				Force.AbsoluteForce, BodyParts.Hips, "hip",
				new SlidingMovement(
					Vector3.Zero, Vector3.Zero,
					0, 0, new Vector3(0, 0, 0), new LinearEasing()),
				new LinearEasing(), new ParentTargetSync(),
				new LinearEasing(), new LinearEasing()));

			g.AddTarget(new Force(
				Force.RelativeTorque, BodyParts.Hips, "hip",
				new SlidingMovement(
					new Vector3(hipTorqueMin_, 0, 0),
					new Vector3(hipTorqueMax_, 0, 0),
					0, 0, new Vector3(0, 0, 0), new LinearEasing()),
				new LinearEasing(), new ParentTargetSync(),
				new LinearEasing(), new LinearEasing()));

			g.AddTarget(new Force(
				Force.RelativeTorque, BodyParts.Chest, "chest",
				new SlidingMovement(
					new Vector3(chestTorqueMin_, 0, 0),
					new Vector3(chestTorqueMax_, 0, 0),
					0, 0, new Vector3(0, 0, 0), new LinearEasing()),
				new LinearEasing(), new ParentTargetSync(),
				new LinearEasing(), new LinearEasing()));

			g.AddTarget(new Force(
				Force.RelativeTorque, BodyParts.Head, "head",
				new SlidingMovement(
					new Vector3(headTorqueMin_, 0, 0),
					new Vector3(headTorqueMax_, 0, 0),
					0, 0, new Vector3(0, 0, 0), new LinearEasing()),
				new LinearEasing(), new ParentTargetSync(),
				new LinearEasing(), new LinearEasing()));

			AddTarget(g);
		}

		public override ProcAnimation Clone()
		{
			var a = new SexProcAnimation();
			a.CopyFrom(this);
			return a;
		}

		public override void Start(Person p)
		{
			base.Start(p);

			if (forces_ == null)
			{
				var list = new List<Force>();
				GatherForces(list, Targets);
				forces_ = list.ToArray();
			}

			UpdateForces(true);
		}

		private void UpdateForces(bool force=false)
		{
			for (int i=0; i<forces_.Length; ++i)
			{
				var f = forces_[i];

				if (f.Done || force)
					UpdateForce(f);
			}
		}

		private void UpdateForce(Force f)
		{
			var hips = person_.Body.Get(BodyParts.Hips);
			var genBase = person_.Body.Get(BodyParts.Genitals);
			var d = (genBase.Position - hips.Position).Normalized;

			f.Movement.SetRange(d * hipForceMin_, d * hipForceMax_);
		}

		private void GatherForces(List<Force> list, List<ITarget> targets)
		{
			for (int i = 0; i < targets.Count; ++i)
			{
				var t = targets[i];

				if (t is ITargetGroup)
				{
					GatherForces(list, ((ITargetGroup)t).Targets);
				}
				else if (t is Force)
				{
					var f = (Force)t;

					if (f.Type == Force.AbsoluteForce)
					{
						f.BeforeNextAction = () => UpdateForce(f);
						list.Add(f);
					}
				}
			}
		}
	}
}
