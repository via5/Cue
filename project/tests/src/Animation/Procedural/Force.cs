using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cue.Proc.Tests
{
	class TestBodyPart : Sys.IBodyPart
	{
		public Vector3 force = Vector3.Zero;
		public bool canApply = true;

		public Sys.IAtom Atom { get; }
		public int Type { get; }
		public bool Exists { get; }
		public bool CanTrigger { get; }
		public bool CanGrab { get; }
		public bool Grabbed { get; }
		public Vector3 ControlPosition { get; set; }
		public Quaternion ControlRotation { get; set; }
		public Vector3 Position { get; }
		public Quaternion Rotation { get; }
		public bool Linked { get; }

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
	public class UnitTest1
	{
		[TestMethod]
		public void TestMethod1()
		{
			var cue = new CueMain();

			var tbp = new TestBodyPart();
			var bp = new BodyPart(null, BP.Head, tbp);

			var m = new Force(
				"", Force.AbsoluteForce, bp,
				new Vector3(4, 4, 4), new Vector3(4, 4, 4),
				null, Vector3.Zero,
				new DurationSync(
					new Duration(2), null, null, null,
					DurationSync.Loop | DurationSync.ResetBetween),
				new LinearEasing());

			m.Start(null, new AnimationContext(null));
			Assert.AreEqual(new Vector3(0, 0, 0), tbp.force);

			m.FixedUpdate(1);
			Assert.AreEqual(new Vector3(2, 2, 2), tbp.force);

			m.FixedUpdate(1);
			Assert.AreEqual(new Vector3(4, 4, 4), tbp.force);

			m.FixedUpdate(1);
			Assert.AreEqual(new Vector3(2, 2, 2), tbp.force);

			m.FixedUpdate(1);
			Assert.AreEqual(new Vector3(0, 0, 0), tbp.force);

			m.FixedUpdate(1);
			Assert.AreEqual(new Vector3(2, 2, 2), tbp.force);

			tbp.canApply = false;

			m.FixedUpdate(1);
			Assert.AreEqual(new Vector3(4, 4, 4), tbp.force);

			m.FixedUpdate(0.5f);
			Assert.AreEqual(new Vector3(2, 2, 2), tbp.force);

			m.FixedUpdate(0.5f);
			Assert.AreEqual(new Vector3(0, 0, 0), tbp.force);

			m.FixedUpdate(1);
			Assert.AreEqual(new Vector3(0, 0, 0), tbp.force);

			m.FixedUpdate(1);
			Assert.AreEqual(new Vector3(0, 0, 0), tbp.force);

			tbp.canApply = true;

			m.FixedUpdate(1);
			Assert.AreEqual(new Vector3(0, 0, 0), tbp.force);

			m.FixedUpdate(1);
			Assert.AreEqual(new Vector3(2, 2, 2), tbp.force);

			m.FixedUpdate(1);
			Assert.AreEqual(new Vector3(4, 4, 4), tbp.force);

			m.FixedUpdate(1);
			Assert.AreEqual(new Vector3(2, 2, 2), tbp.force);

			m.FixedUpdate(1);
			Assert.AreEqual(new Vector3(0, 0, 0), tbp.force);

			m.FixedUpdate(1);
			Assert.AreEqual(new Vector3(2, 2, 2), tbp.force);
		}
	}
}
