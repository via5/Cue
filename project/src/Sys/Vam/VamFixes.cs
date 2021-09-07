﻿using System;
using System.Collections.Generic;
using System.Text;
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
				if (a.type == "Dildo")
					FixDildo(a);
			}
		}

		private static void FixDildo(Atom a)
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

			var h = a.GetComponentInChildren<CollisionTriggerEventHandler>();
			var d1 = a.transform
				.Find("reParentObject")
				.Find("object")
				.Find("rescaleObject")
				.Find("quick_test_subdiv1_correct")
				.Find("dildo1");

			foreach (var rb in d1.GetComponentsInChildren<Rigidbody>())
			{
				var old = rb.gameObject.GetComponent<CollisionTriggerEventHandler>();
				if (old == null)
				{
					var cc = rb.gameObject.AddComponent<CollisionTriggerEventHandler>();

					// forward to main handler
					cc.collisionTrigger = h.collisionTrigger;
					cc.collidingWithDictionary = h.collidingWithDictionary;
					cc.collidingWithButFailedVelocityTestDictionary = h.collidingWithButFailedVelocityTestDictionary;
					cc.collidingWith = h.collidingWith;
				}
			}

			h.Reset();
		}

		public static void Run(Atom a)
		{
			DisableFreezeWhenGrabbed(a);
			DisableAutoExpressions(a);
			EnableAudioJawDriving(a);
			FixTriggers(a);
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
			var b = Cue.Instance.VamSys.GetBoolParameter(
				a, "AutoExpressions", "enabled");

			if (b != null)
				b.val = false;
		}

		private static void EnableAudioJawDriving(Atom a)
		{
			var p = Cue.Instance.VamSys.GetBoolParameter(
				a, "JawControl", "driveXRotationFromAudioSource");

			if (p != null)
				p.val = true;

			var v = Cue.Instance.VamSys.GetFloatParameter(
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
	}
}
