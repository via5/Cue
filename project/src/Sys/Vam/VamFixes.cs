using System;
using UnityEngine;

namespace Cue.Sys.Vam
{
	class VamFixes
	{
		public static void Run()
		{
			FixDildos();
		}

		private static void FixDildos()
		{
			foreach (var a in SuperController.singleton.GetAtoms())
			{
				if (a.on && a.type == "Dildo")
					FixDildo(a);
			}
		}

		public static void FixDildo(Atom a)
		{
			// looks like the CollisionTriggerEventHandler on the main object
			// never receives the OnCollision* callbacks, but there's a bunch
			// of colliders in subobjects
			//
			// this adds a CollisionTriggerEventHandler to every rigidbody and
			// changes the various references to point to the main handler

			var ct = a.GetComponentInChildren<CollisionTrigger>();

			// there's no CollisionTriggerEventHandler without this
			ct.triggerEnabled = true;

			var o = a.transform
				.Find("reParentObject")
				.Find("object");

			var d1 = o
				.Find("rescaleObject")
				.Find("quick_test_subdiv1_correct")
				.Find("dildo1");

			var root = d1.Find("root");
			var h = o.GetComponent<CollisionTriggerEventHandler>();

			// rb isn't set
			h.thisRigidbody = o.GetComponent<Rigidbody>();

			foreach (var rb in d1.GetComponentsInChildren<Rigidbody>())
			{
				var cc = rb.gameObject.GetComponent<CollisionTriggerEventHandler>();
				if (cc == null)
					cc = rb.gameObject.AddComponent<CollisionTriggerEventHandler>();

				// forward to main handler
				cc.collisionTrigger = h.collisionTrigger;
				cc.collidingWithDictionary = h.collidingWithDictionary;
				cc.collidingWithButFailedVelocityTestDictionary = h.collidingWithButFailedVelocityTestDictionary;
				cc.collidingWith = h.collidingWith;
			}

			h.Reset();
		}

		public static void Run(Atom a)
		{
			DisableFreezeWhenGrabbed(a);
			DisableAutoExpressions(a);
			EnableAudioJawDriving(a);
			FixTriggers(a);
			SetEyes(a);
		}

		private static void DisableFreezeWhenGrabbed(Atom a)
		{
			// setting grabFreezePhysics on the atom or
			// freezeAtomPhysicsWhenGrabbed on controllers doesn't
			// update the toggle in the ui, the param has to be set
			// manually
			foreach (var fc in a.freeControllers)
			{
				try
				{
					var b = fc.GetBoolJSONParam("freezeAtomPhysicsWhenGrabbed");
					if (b != null)
						b.val = false;
				}
				catch (Exception)
				{
					// happens sometimes, not sure why
				}
			}
		}

		private static void DisableAutoExpressions(Atom a)
		{
			var b = Parameters.GetBool(a, "AutoExpressions", "enabled");
			if (b != null)
				b.val = false;
		}

		private static void EnableAudioJawDriving(Atom a)
		{
			var p = Parameters.GetBool(
				a, "JawControl", "driveXRotationFromAudioSource");

			if (p != null)
				p.val = true;

			var v = Parameters.GetFloat(
				a, "JawControl", "driveXRotationFromAudioSourceMultiplier");

			v.val = v.max;
		}

		private static void FixTriggers(Atom a)
		{
			// not sure that's necessary, the trigger seems updated on reload

			//var c = Cue.Instance.VamSys.FindCollider(
			//	a, "AutoColliderAutoCollidersFaceCentral2Hard");
			//
			//if (c == null)
			//{
			//	Cue.LogError($"FixTriggers: lip collider not found in {a.uid}");
			//	return;
			//}
			//
			//var o = Cue.Instance.VamSys.FindChildRecursive(a.transform, "LipTrigger")
			//	?.GetComponentInChildren<CollisionTriggerEventHandler>();
			//
			//if (o == null)
			//{
			//	Cue.LogError($"FixTriggers: lip trigger not found in {a.uid}");
			//	return;
			//}
			//
			//var t = o.collisionTrigger.transform;
			//
			//Cue.LogError($"FixTriggers: moving lip trigger from {t.position} to {c.bounds.center} for {a.uid}");
			////t.position = c.bounds.center;
		}

		private static void SetEyes(Atom a)
		{
			var v = Parameters.GetFloat(
				a, "Eyes", "maxLeft");

			if (v != null)
				v.val = 35.0f;
		}
	}
}
