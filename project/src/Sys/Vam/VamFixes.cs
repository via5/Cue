using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Cue.Sys.Vam
{
	class VamFixes
	{
		public static void Run(Atom a)
		{
			DisableFreezeWhenGrabbed(a);
			DisableAutoExpressions(a);
			EnableAudioJawDriving(a);
			FixTriggers(a);

			SetStrongerDamping(a, "hipControl");
			SetStrongerDamping(a, "chestControl");
			SetStrongerDamping(a, "headControl");
			SetStrongerDamping(a, "lFootControl");
			SetStrongerDamping(a, "rFootControl");
			SetStrongerDamping(a, "lKneeControl");
			SetStrongerDamping(a, "rKneeControl");
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

		private static void SetStrongerDamping(Atom a, string cn)
		{
			var c = Cue.Instance.VamSys.FindController(a, cn);
			if (c == null)
			{
				Cue.LogError($"SetStrongerSpring: controller '{cn}' not found in {a.uid}");
				return;
			}

			c.RBHoldPositionDamper = 100;
			c.RBHoldRotationDamper = 10;
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
