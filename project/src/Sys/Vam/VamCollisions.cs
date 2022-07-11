using System;
using UnityEngine;

namespace Cue.Sys.Vam
{
	class CueCollisionHandler : MonoBehaviour
	{
		private VamBodyPart bp_ = null;
		private Person person_ = null;
		private Rigidbody rb_ = null;
		private Collider collider_ = null;

		public static CueCollisionHandler AddToCollider(Collider c, VamBodyPart bp)
		{
			var rb = FindRigidbody(c);
			if (rb == null)
			{
				Cue.LogError($"{U.QualifiedName(c)} has no rigidbody");
				return null;
			}

			var ch = rb.gameObject.AddComponent<CueCollisionHandler>();
			if (ch == null)
			{
				Cue.LogError($"failed to add handler to collider {U.QualifiedName(c)}");
				return null;
			}

			ch.bp_ = bp;
			if (ch.bp_ != null)
			{
				ch.person_ = Cue.Instance.PersonForAtom(ch.bp_.Atom);

				if (ch.person_ == null)
					Cue.LogErrorST($"CueCollisionHandler: bp atom {ch.bp_.Atom} not found for {bp} ({U.QualifiedName(c)})");
			}

			ch.rb_ = rb;
			ch.collider_ = c;

			return ch;
		}

		public static void RemoveAll(Transform t)
		{
			foreach (var cm in t.GetComponentsInChildren<Component>())
			{
				if (cm != null && cm.ToString().Contains("CueCollisionHandler"))
					UnityEngine.Object.Destroy(cm);
			}
		}

		public static void RemoveFromCollider(Collider c)
		{
			foreach (var cm in c.GetComponentsInChildren<Component>())
			{
				if (cm != null && cm.ToString().Contains("CueCollisionHandler"))
					UnityEngine.Object.Destroy(cm);
			}
		}

		private static Rigidbody FindRigidbody(Collider c)
		{
			var rb = c.GetComponent<Rigidbody>();
			if (rb != null)
				return rb;

			return c.attachedRigidbody;
		}

		public void OnCollisionStay(Collision c)
		{
			Instrumentation.Start(I.Collisions);
			{
				DoOnCollisionStay(c);
			}
			Instrumentation.End();
		}

		private void DoOnCollisionStay(Collision c)
		{
			try
			{
				if (!isActiveAndEnabled || bp_ == null ||
					CueMain.Instance.Sys.Paused ||
					!CueMain.Instance.PluginEnabled)
				{
					return;
				}

				if (!CollisionWithThis(c))
					return;

				float mag = Math.Max(0.01f, c.relativeVelocity.magnitude);

				var sourcePart = Cue.Instance.VamSys
					.BodyPartForTransform(c.rigidbody.transform) as VamBodyPart;

				if (sourcePart == null)
					DoExternalCollision(c, mag);
				else
					DoPersonCollision(sourcePart, mag);
			}
			catch (PluginGone)
			{
			}
			catch (Exception e)
			{
				CueMain.OnException(e);
			}
		}

		private bool CollisionWithThis(Collision c)
		{
			// there can be multiple instances of this script on the same
			// rigidbody if the colliders were children of a rigidbody instead
			// of a rigidbody themselves, like all the head colliders are
			// children of the head rigidbody
			//
			// `collider_` is the actual collider this script was added for
			// (like the left ear, for example)
			//
			// `contacts` contains all the colliders that are children of this
			// rigidbody that collided with `c.collider`, so make sure this
			// script is the correct one

			for (int i = 0; i < c.contacts.Length; ++i)
			{
				if (c.contacts[i].thisCollider == collider_)
					return true;
			}

			// this is for another CueCollisionHandler
			return false;
		}

		private void DoExternalCollision(Collision c, float mag)
		{
			var a = U.AtomForCollider(c.collider);

			if (VamBodyPart.IgnoreTrigger(a, null, bp_.Atom as VamAtom, bp_))
				return;

			//Cue.LogError(
			//	$"{bp_} " +
			//	$"{U.QualifiedName(c.collider)}");

			bp_.AddExternalCollision(a, mag);
		}

		private void DoPersonCollision(VamBodyPart sourcePart, float mag)
		{
			if (VamBodyPart.IgnoreTrigger(
					sourcePart.Atom as VamAtom, sourcePart,
					bp_.Atom as VamAtom, bp_))
			{
				return;
			}

			var sourcePerson = Cue.Instance.PersonForAtom(sourcePart.Atom);

			var sourceIndex = sourcePerson?.PersonIndex ?? -1;
			if (sourceIndex < 0)
			{
				Cue.LogError($"no source index");
				return;
			}

			var targetPart = bp_;
			if (targetPart == null)
			{
				Cue.LogError($"no target part");
				return;
			}

			var targetIndex = person_.PersonIndex;

			bp_.AddPersonCollision(sourceIndex, sourcePart.Type, mag);
			sourcePart.AddPersonCollision(targetIndex, targetPart.Type, mag);
		}

		public override string ToString()
		{
			return $"CueCollisionHandler bp={bp_} c={U.QualifiedName(collider_)}";
		}
	}
}
