using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cue.Sys.Vam
{
	abstract class VamBodyPart : IBodyPart
	{
		private IAtom atom_;
		private int type_;
		private List<GrabInfo> grabCache_ = new List<GrabInfo>();

		protected VamBodyPart(IAtom a, int t)
		{
			atom_ = a;
			type_ = t;
		}

		public IAtom Atom
		{
			get { return atom_; }
		}

		public VamAtom VamAtom
		{
			get { return atom_ as VamAtom; }
		}

		public int Type { get { return type_; } }
		public virtual bool Exists { get { return true; } }

		public virtual Transform Transform { get { return null; } }
		public virtual Rigidbody Rigidbody { get { return null; } }
		public virtual FreeControllerV3 Controller { get { return null; } }

		public virtual bool CanTrigger { get { return false; } }
		public virtual TriggerInfo[] GetTriggers() { return null; }

		public virtual bool CanGrab { get { return false; } }
		public virtual bool Grabbed { get { return false; } }

		public abstract Vector3 ControlPosition { get; set; }
		public abstract Quaternion ControlRotation { get; set; }
		public abstract Vector3 Position { get; }
		public abstract Quaternion Rotation { get; }

		public virtual bool Linked
		{
			get
			{
				return
					Controller?.linkToRB != null &&
					Controller.currentPositionState == FreeControllerV3.PositionState.ParentLink &&
					Controller.currentRotationState == FreeControllerV3.RotationState.ParentLink;
			}
		}

		public virtual void LinkTo(IBodyPart other)
		{
			if (Controller == null)
			{
				Cue.LogError($"cannot link {this} to {other}, no controller");
				return;
			}

			if (other == null)
			{
				SetOn(Controller);
			}
			else
			{
				var o = other as VamBodyPart;
				if (o?.Rigidbody == null)
				{
					Cue.LogError($"cannot link {this} to {other}, not an rb");
				}
				else
				{
					if (o.Controller?.linkToRB == Rigidbody)
					{
						Cue.LogError(
							$"cannot link {this} to {other}, would be " +
							$"reciprocal");
					}
					else
					{
						SetParentLink(Controller, o.Rigidbody);
					}
				}
			}
		}

		public virtual bool IsLinkedTo(IBodyPart other)
		{
			var o = other as VamBodyPart;
			if (o == null)
				return false;

			if (Controller == null || o.Rigidbody == null)
				return false;

			if (Controller.linkToRB == o.Rigidbody)
				return true;

			// todo: should probably use BodyPartForTransform()

			if (other.Atom.Possessed || other.Atom is VamCameraAtom)
			{
				var sys = Cue.Instance.VamSys;

				if (o.Type == BP.LeftHand)
				{
					return sys.IsVRHand(Controller.linkToRB?.transform, BP.LeftHand);
				}
				else if (o.Type == BP.RightHand)
				{
					return
						sys.IsVRHand(Controller.linkToRB?.transform, BP.RightHand) ||
						(Controller.linkToRB == SuperController.singleton.mouseGrab.GetComponent<Rigidbody>());
				}
			}

			return false;
		}

		public virtual GrabInfo[] GetGrabs()
		{
			if (!Grabbed)
				return null;

			var rb = Controller?.linkToRB;
			if (rb == null)
				return null;

			grabCache_.Clear();

			var bp = Cue.Instance.VamSys.BodyPartForTransform(rb.transform);
			if (bp == null)
			{
				grabCache_.Add(GrabInfo.None);
			}
			else
			{
				grabCache_.Add(new GrabInfo(
					Cue.Instance.PersonForAtom(bp.Atom).PersonIndex,
					bp.Type));
			}

			return grabCache_.ToArray();
		}

		private void SetParentLink(FreeControllerV3 fc, Rigidbody rb)
		{
			fc.SelectLinkToRigidbody(
				rb, FreeControllerV3.SelectLinkState.PositionAndRotation);
		}

		private void SetOn(FreeControllerV3 fc)
		{
			fc.SelectLinkToRigidbody(null);
			fc.currentPositionState = FreeControllerV3.PositionState.On;
			fc.currentRotationState = FreeControllerV3.RotationState.On;
		}

		public float DistanceToSurface(IBodyPart other, bool debug)
		{
			var thisColliders = GetColliders();
			var otherColliders = (other as VamBodyPart).GetColliders();

			var thisPos = Position;
			var thisPosU = U.ToUnity(thisPos);
			var otherPos = other.Position;
			var otherPosU = U.ToUnity(otherPos);

			bool thisCollidersValid = (thisColliders != null && thisColliders.Length > 0);
			bool otherCollidersValid = (otherColliders != null && otherColliders.Length > 0);


			if (thisCollidersValid && otherCollidersValid)
			{
				float closest = float.MaxValue;

				Collider thisDebug = null;
				Collider otherDebug = null;

				try
				{
					for (int i = 0; i < thisColliders.Length; ++i)
					{
						for (int j = 0; j < otherColliders.Length; ++j)
						{
							var thisPoint = thisColliders[i].ClosestPointOnBounds(
								otherColliders[j].transform.position);

							var otherPoint = otherColliders[j].ClosestPointOnBounds(
								thisColliders[i].transform.position);

							var d = UnityEngine.Vector3.Distance(thisPoint, otherPoint);

							if (d < closest)
							{
								if (debug)
								{
									thisDebug = thisColliders[i];
									otherDebug = otherColliders[j];
								}

								closest = d;
							}
						}
					}

					var dp = Vector3.Distance(thisPos, otherPos);

					if (dp < closest)
					{
						if (debug)
							Cue.LogError($"both valid, closest is position, {thisPos} {otherPos} {dp}");

						closest = dp;
					}
					else
					{
						if (debug)
						{
							Cue.LogError($"both valid, closest is {thisDebug} {otherDebug} {closest}");
						}
					}
				}
				catch (Exception)
				{
					Cue.LogError($"t={thisColliders} o={otherColliders?.Length}");
				}

				return closest;
			}
			else if (thisCollidersValid)
			{
				float closest = float.MaxValue;
				Collider c = null;

				try
				{
					for (int i = 0; i < thisColliders.Length; ++i)
					{
						var p = thisColliders[i].ClosestPointOnBounds(otherPosU);
						var d = Vector3.Distance(otherPos, U.FromUnity(p));

						if (d < closest)
						{
							if (debug)
								c = thisColliders[i];

							closest = d;
						}
					}

					if (debug)
					{
						Cue.LogError(
							$"this valid, " +
							$"closest is {other} at {otherPos}, " +
							$"this is {this} {c.transform.parent.name}.{c.name} " +
							$"at {c.transform.position}, d={closest}");
					}
				}
				catch (Exception)
				{
					Cue.LogError("2");
				}


				return closest;
			}
			else if (otherCollidersValid)
			{
				float closest = float.MaxValue;
				Collider c = null;

				try
				{
					for (int i = 0; i < otherColliders.Length; ++i)
					{
						var p = otherColliders[i].ClosestPointOnBounds(thisPosU);
						var d = Vector3.Distance(thisPos, U.FromUnity(p));

						if (d < closest)
						{
							if (debug)
								c = otherColliders[i];

							closest = d;
						}
					}

					if (debug)
					{
						Cue.LogError(
							$"other valid, closest is {thisPos} " +
							$"{c.transform.parent.name}.{c.name} {closest}");
					}
				}
				catch (Exception)
				{
					Cue.LogError("1");
				}

				return closest;
			}
			else
			{
				float d = Vector3.Distance(thisPos, other.Position);

				if (debug)
					Cue.LogError($"neither valid, {thisPos} {other.Position} {d}");

				return d;
			}
		}

		protected virtual Collider[] GetColliders()
		{
			return null;
		}

		public virtual bool CanApplyForce()
		{
			return true;
		}

		public virtual void AddRelativeForce(Vector3 v)
		{
			// no-op
		}

		public virtual void AddRelativeTorque(Vector3 v)
		{
			// no-op
		}

		public virtual void AddForce(Vector3 v)
		{
			// no-op
		}

		public virtual void AddTorque(Vector3 v)
		{
			// no-op
		}
	}
}
