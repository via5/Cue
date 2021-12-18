using System;
using UnityEngine;

namespace Cue.Sys.Vam
{
	class VamFixes
	{
		public static void Run()
		{
			FixKnownAtoms();
		}

		private static void FixKnownAtoms()
		{
			foreach (var a in SuperController.singleton.GetAtoms())
				FixKnownAtom(a);
		}

		public static void FixKnownAtom(Atom a)
		{
			if (!a.on)
				return;

			if (a.type == "Dildo")
			{
				FixDildo(a);
			}
			else if (IsCUA(a, "Cigarette.unity"))
			{
				FixCigarette(a);
			}
		}

		private static bool IsCUA(Atom a, string assetName)
		{
			if (a.type != "CustomUnityAsset")
				return false;

			var cua = a.GetComponentInChildren<CustomUnityAssetLoader>();
			if (cua == null)
				return false;

			var asset = a.GetStorableByID("asset");
			if (asset == null)
				return false;

			var name = asset.GetStringChooserJSONParam("assetName");
			if (asset == null)
				return false;

			return (name.val == assetName);
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
			if (ct == null)
			{
				SuperController.LogError($"dildo {a.uid} has no CollisionTrigger");
				return;
			}

			// there's no CollisionTriggerEventHandler without this
			ct.triggerEnabled = true;

			var o = a.transform
				?.Find("reParentObject")
				?.Find("object");

			if (o == null)
			{
				SuperController.LogError($"dildo {a.uid} has no object");
				return;
			}

			var d1 = o
				?.Find("rescaleObject")
				?.Find("quick_test_subdiv1_correct")
				?.Find("dildo1");

			if (d1 == null)
			{
				SuperController.LogError($"dildo {a.uid} has no dildo1");
				return;
			}

			var root = d1.Find("root");
			var h = o.GetComponent<CollisionTriggerEventHandler>();

			if (h == null)
			{
				SuperController.LogError($"dildo {a.uid} has no CollisionTriggerEventHandler");
				return;
			}

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

		private static void FixCigarette(Atom a)
		{
			U.ForEachChildRecursive(a.transform, (c) =>
			{
				if (c.name.StartsWith("Cylinder") || c.name.StartsWith("Particle"))
				{
					var v = c.localPosition;
					v.z = 0;
					c.localPosition = v;
				}
			});
		}

		public static void Run(Atom a)
		{
			DisableFreezeWhenGrabbed(a);
			DisableAutoExpressions(a);
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

		private static void SetEyes(Atom a)
		{
			var v = Parameters.GetFloat(
				a, "Eyes", "maxLeft");

			if (v != null)
				v.val = 35.0f;
		}
	}
}
