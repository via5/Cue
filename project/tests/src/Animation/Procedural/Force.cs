using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cue.Proc.Tests
{
	class TestBodyPart : Sys.IBodyPart
	{
		public Vector3 force = Vector3.Zero;
		public bool canApply = true;

		public Sys.IAtom Atom { get; }
		public BodyPartType Type { get; }
		public bool Exists { get; }
		public bool IsPhysical { get; }
		public bool CanTrigger { get; }
		public bool CanGrab { get; }
		public bool Grabbed { get; }
		public Vector3 ControlPosition { get; set; }
		public Quaternion ControlRotation { get; set; }
		public Vector3 Position { get; }
		public Quaternion Rotation { get; }
		public bool Linked { get; }
		public bool Render { get; set; }

		public void AddForce(Vector3 v)
		{
			force = v;
		}

		public void AddRelativeForce(Vector3 v)
		{
			throw new System.NotImplementedException();
		}

		public void AddRelativeTorque(Vector3 v)
		{
			throw new System.NotImplementedException();
		}

		public void AddTorque(Vector3 v)
		{
			throw new System.NotImplementedException();
		}

		public bool CanApplyForce()
		{
			return canApply;
		}

		public float DistanceToSurface(Sys.IBodyPart other, bool debug = false)
		{
			throw new System.NotImplementedException();
		}

		public float DistanceToSurface(Vector3 pos, bool debug = false)
		{
			throw new System.NotImplementedException();
		}

		public Sys.GrabInfo[] GetGrabs()
		{
			throw new System.NotImplementedException();
		}

		public Sys.TriggerInfo[] GetTriggers()
		{
			throw new System.NotImplementedException();
		}

		public bool IsLinkedTo(Sys.IBodyPart other)
		{
			throw new System.NotImplementedException();
		}

		public void LinkTo(Sys.IBodyPart other)
		{
			throw new System.NotImplementedException();
		}
	}


	[TestClass]
	public class Init
	{
		[AssemblyInitialize()]
		public static void MyTestInitialize(TestContext testContext)
		{
			new CueMain();
		}
	}


	[TestClass]
	public class ForceTests
	{
		[TestMethod]
		public void CanApplyForce()
		{
			var tbp = new TestBodyPart();
			var bp = new BodyPart(null, BP.Head, tbp);

			var g = new RootTargetGroup(new DurationSync(
				new Duration(2), null, null, null,
				DurationSync.Loop | DurationSync.ResetBetween));

			var f = new Force(
				"", Force.AbsoluteForce, bp,
				new Vector3(4, 4, 4), new Vector3(4, 4, 4),
				null, Vector3.Zero,
				new ParentTargetSync(),
				new LinearEasing());

			g.AddTarget(f);


			g.Start(null, new AnimationContext(null));
			Assert.AreEqual(new Vector3(0, 0, 0), tbp.force);

			// normal loops
			TestLoops(tbp, g);


			// disable
			tbp.canApply = false;


			// will go up for one more frame because the sync duration increases
			// and the force then remembers the current magnitude
			g.FixedUpdate(1);
			Assert.AreEqual(new Vector3(2, 2, 2), tbp.force);

			// then back to 0 over one second
			g.FixedUpdate(0.5f);
			Assert.AreEqual(new Vector3(1, 1, 1), tbp.force);

			g.FixedUpdate(0.5f);
			Assert.AreEqual(new Vector3(0, 0, 0), tbp.force);

			// then stay at 0 forever
			g.FixedUpdate(1);
			Assert.AreEqual(new Vector3(0, 0, 0), tbp.force);
			g.FixedUpdate(1);
			Assert.AreEqual(new Vector3(0, 0, 0), tbp.force);
			g.FixedUpdate(1);
			Assert.AreEqual(new Vector3(0, 0, 0), tbp.force);


			// based on all the updates above, sync is now at half magnitude
			// (1s, duration is 2s) and going up
			Assert.AreEqual(0.5f, g.Sync.Magnitude);


			// enable
			tbp.canApply = true;


			// there's no catching up, the force will immediately get back to
			// what it's supposed to have

			g.FixedUpdate(1);
			Assert.AreEqual(1.0f, g.Sync.Magnitude);
			Assert.AreEqual(new Vector3(4, 4, 4), tbp.force);

			g.FixedUpdate(1);
			Assert.AreEqual(0.5f, g.Sync.Magnitude);
			Assert.AreEqual(new Vector3(2, 2, 2), tbp.force);

			g.FixedUpdate(1);
			Assert.AreEqual(1, g.Sync.Magnitude);
			Assert.AreEqual(new Vector3(0, 0, 0), tbp.force);

			// normal loops again
			TestLoops(tbp, g);
		}

		private void TestLoops(TestBodyPart tbp, ITarget g)
		{
			for (int i = 0; i < 10; ++i)
				TestOneLoop(tbp, g);
		}

		private void TestOneLoop(TestBodyPart tbp, ITarget g)
		{
			Assert.AreEqual(new Vector3(0, 0, 0), tbp.force);

			g.FixedUpdate(1);
			Assert.AreEqual(new Vector3(2, 2, 2), tbp.force);

			g.FixedUpdate(1);
			Assert.AreEqual(new Vector3(4, 4, 4), tbp.force);

			g.FixedUpdate(1);
			Assert.AreEqual(new Vector3(2, 2, 2), tbp.force);

			g.FixedUpdate(1);
			Assert.AreEqual(new Vector3(0, 0, 0), tbp.force);
		}
	}
}
