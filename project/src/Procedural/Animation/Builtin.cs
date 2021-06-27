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

			a.AddTarget(new Controller("headControl", new Vector3(0, 1.6f, 0), new Vector3(0, 0, 0)));
			a.AddTarget(new Controller("chestControl", new Vector3(0, 1.4f, 0), new Vector3(20, 0, 0)));
			a.AddTarget(new Controller("hipControl", new Vector3(0, 1.1f, 0), new Vector3(340, 10, 0)));
			a.AddTarget(new Controller("lHandControl", new Vector3(-0.2f, 0.9f, 0), new Vector3(0, 10, 90)));
			a.AddTarget(new Controller("rHandControl", new Vector3(0.2f, 0.9f, 0), new Vector3(0, 0, 270)));
			a.AddTarget(new Controller("lFootControl", new Vector3(-0.1f, 0, 0), new Vector3(20, 10, 0)));
			a.AddTarget(new Controller("rFootControl", new Vector3(0.1f, 0, -0.1f), new Vector3(20, 10, 0)));

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

		private float elapsed_ = 0;
		private float hipForce_ = 500;
		private float hipTorque_ = -30;
		private Force[] forces_ = null;

		public SexProcAnimation()
			: base("procSex", false)
		{
			var g = new ConcurrentTargetGroup("g", new Duration(), new Duration(), true);

			g.AddTarget(new Force(
				Force.AbsoluteForce, BodyParts.Hips, "hip",
				new SlidingMovement(
					Vector3.Zero, Vector3.Zero,
					0, 0, new Vector3(0, 0, 0), new LinearEasing()),
				new LinearEasing(),
				new SlidingDuration(1, 1, 1, 1, 0, new LinearEasing()),
				new SlidingDuration(1, 1, 1, 1, 0, new LinearEasing()),
				new Duration(0, 0), new Duration(0, 0), new LinearEasing(), new LinearEasing(),
				Force.Loop | Force.ResetBetween));

			g.AddTarget(new Force(
				Force.RelativeTorque, BodyParts.Hips, "hip",
				new SlidingMovement(
					new Vector3(hipTorque_, 0, 0), new Vector3(hipTorque_, 0, 0),
					0, 0, new Vector3(0, 0, 0), new LinearEasing()),
				new LinearEasing(),
				new SlidingDuration(1, 1, 1, 1, 0, new LinearEasing()),
				new SlidingDuration(1, 1, 1, 1, 0, new LinearEasing()),
				new Duration(0, 0), new Duration(0, 0), new LinearEasing(), new LinearEasing(),
				Force.Loop | Force.ResetBetween));

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
			elapsed_ = DirectionCheckInterval + 1;

			if (forces_ == null)
			{
				var list = new List<Force>();
				GatherForces(list, Targets);
				forces_ = list.ToArray();
			}

			UpdateForces(true);
		}

		public override void Reset()
		{
			base.Reset();
			elapsed_ = DirectionCheckInterval + 1;
		}

		public override void FixedUpdate(float s)
		{
			//UpdateForces();
			base.FixedUpdate(s);
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
			Cue.LogInfo($"{d}");

			f.Movement.SetRange(d * hipForce_, d * hipForce_);
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
