using System;
using UnityEngine;

namespace Cue.Sys.Vam
{
	class CueCollisionHandler : MonoBehaviour
	{
		private VamSys sys_ = null;
		private VamBodyPart bp_ = null;
		private Person person_ = null;
		private Rigidbody rb_ = null;
		private Collider collider_ = null;
		private bool active_ = false;
		private bool first_ = true;

		public static CueCollisionHandler AddToCollider(Collider c, VamBodyPart bp)
		{
			var rb = FindRigidbody(c);
			if (rb == null)
			{
				Logger.Global.Error($"{U.QualifiedName(c)} has no rigidbody");
				return null;
			}

			var ch = rb.gameObject.AddComponent<CueCollisionHandler>();
			if (ch == null)
			{
				Logger.Global.Error($"failed to add handler to collider {U.QualifiedName(c)}");
				return null;
			}

			ch.bp_ = bp;
			if (ch.bp_ != null)
			{
				ch.person_ = Cue.Instance.PersonForAtom(ch.bp_.Atom);

				if (ch.person_ == null)
					Logger.Global.ErrorST($"CueCollisionHandler: bp atom {ch.bp_.Atom} not found for {bp} ({U.QualifiedName(c)})");
			}

			ch.sys_ = Cue.Instance.VamSys;
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

		public void OnCollisionEnter(Collision c)
		{
			Instrumentation.Start(I.Collisions);
			{
				Instrumentation.Start(I.ColWithThis);
				{
					active_ = CollisionWithThis(c);
				}
				Instrumentation.End();
			}
			Instrumentation.End();
		}

		public void OnCollisionExit(Collision c)
		{
			Instrumentation.Start(I.Collisions);
			{
				Instrumentation.Start(I.ColWithThis);
				{
					if (CollisionWithThis(c))
						active_ = false;
				}
				Instrumentation.End();
			}
			Instrumentation.End();
		}

		public void OnCollisionStay(Collision c)
		{
			Instrumentation.Start(I.Collisions);
			{
				if (first_)
				{
					first_ = false;
					active_ = CollisionWithThis(c);
				}
				else
				{
					DoOnCollisionStay(c);
				}
			}
			Instrumentation.End();
		}

		private void DoOnCollisionStay(Collision c)
		{
			try
			{
				if (!active_ ||
					bp_ == null ||
					CueMain.Instance.Sys.Paused ||
					!CueMain.Instance.PluginEnabled)
				{
					return;
				}


				bool withThis;

				Instrumentation.Start(I.ColWithThis);
				{
					withThis = CollisionWithThis(c);
				}
				Instrumentation.End();

				if (!withThis)
					return;


				float mag;
				VamBodyPart sourcePart;

				Instrumentation.Start(I.ColGetBP);
				{
					mag = Math.Max(0.01f, c.relativeVelocity.magnitude);
					sourcePart = sys_.BodyPartForTransform(c.rigidbody.transform);
				}
				Instrumentation.End();


				if (sourcePart == null)
				{
					Instrumentation.Start(I.ColExternal);
					{
						DoExternalCollision(c, mag);
					}
					Instrumentation.End();
				}
				else
				{
					Instrumentation.Start(I.ColPerson);
					{
						DoPersonCollision(sourcePart, mag);
					}
					Instrumentation.End();
				}
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

			var cs = c.contacts;

			for (int i = 0; i < cs.Length; ++i)
			{
				if (cs[i].thisCollider == collider_)
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
				Logger.Global.Error($"no source index");
				return;
			}

			var targetPart = bp_;
			if (targetPart == null)
			{
				Logger.Global.Error($"no target part");
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
