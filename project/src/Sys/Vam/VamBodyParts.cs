using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cue.Sys.Vam
{
	public abstract class VamBodyPart : IBodyPart
	{
		private IAtom atom_;
		private BodyPartType type_;
		private VamColliderRegion[] colliders_ = null;
		private List<GrabInfo> grabCache_ = new List<GrabInfo>();
		private Logger log_ = null;

		public BodyPartType Type { get { return type_; } }
		public virtual bool Exists { get { return true; } }
		public virtual bool IsPhysical { get { return true; } }

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

		private List<VamDebugRenderer.IDebugRender> renderers_ = null;
		private List<VamDebugRenderer.IDebugRender> distanceRenderers_ = null;


		protected VamBodyPart(IAtom a, BodyPartType t, string[] colliders = null)
		{
			atom_ = a;
			type_ = t;

			if (colliders != null)
			{
				var cs = new List<VamColliderRegion>();
				foreach (var cn in colliders)
				{
					var c = U.FindCollider((a as VamAtom).Atom, cn);
					if (c == null)
					{
						Log.Error($"{a.ID}: collider {cn} not found");
						continue;
					}

					cs.Add(new VamColliderRegion(this, c));
				}

				colliders_ = cs.ToArray();
			}
		}

		public Logger Log
		{
			get
			{
				if (log_ == null)
					log_ = new Logger(Logger.Sys, atom_, ToString());

				return log_;
			}
		}

		public IAtom Atom
		{
			get { return atom_; }
		}

		public VamAtom VamAtom
		{
			get { return atom_ as VamAtom; }
		}

		public bool Render
		{
			get
			{
				return (renderers_ != null && renderers_.Count > 0);
			}

			set
			{
				if (value)
				{
					AddDebugRenderers();
				}
				else
				{
					if (renderers_ != null)
					{
						foreach (var r in renderers_)
							Cue.Instance.VamSys.DebugRenderer.RemoveRender(r);

						renderers_.Clear();
					}

					if (distanceRenderers_ != null)
					{
						foreach (var r in distanceRenderers_)
							Cue.Instance.VamSys.DebugRenderer.RemoveRender(r);

						distanceRenderers_.Clear();
					}
				}
			}
		}

		protected virtual void AddDebugRenderers()
		{
			AddDebugRenderer(Cue.Instance.VamSys.DebugRenderer.AddRender(this));

			var cs = GetRegions();
			if (cs != null)
			{
				foreach (var c in cs)
				{
					var cc = c as VamColliderRegion;
					if (cc != null)
						AddDebugRenderer(Cue.Instance.VamSys.DebugRenderer.AddRender(cc.Collider));
				}
			}
		}

		protected void AddDebugRenderer(VamDebugRenderer.IDebugRender r)
		{
			if (renderers_ == null)
				renderers_ = new List<VamDebugRenderer.IDebugRender>();

			renderers_.Add(r);
		}

		protected void AddDistanceRenderer(Vector3 from, Vector3 to)
		{
			if (distanceRenderers_ == null)
				distanceRenderers_ = new List<VamDebugRenderer.IDebugRender>();

			distanceRenderers_.Add(
				Cue.Instance.VamSys.DebugRenderer.AddRender(from, to));
		}

		public virtual IBodyPartRegion Link
		{
			get { return Cue.Instance.VamSys.Linker.GetLink(this); }
		}

		public virtual bool IsLinked
		{
			get { return Cue.Instance.VamSys.Linker.IsLinked(this); }
		}

		public virtual void LinkTo(IBodyPartRegion other)
		{
			Cue.Instance.VamSys.Linker.Add(this, other as VamBodyPartRegion);
		}

		public void Unlink()
		{
			Cue.Instance.VamSys.Linker.Remove(this);
		}

		public virtual bool IsLinkedTo(IBodyPart other)
		{
			var o = other as VamBodyPart;
			if (o == null)
				return false;

			if (Controller == null || o.Rigidbody == null)
				return false;

			if (Cue.Instance.VamSys.Linker.IsLinkedTo(this, other as VamBodyPart))
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

		public void SetOn()
		{
			var fc = Controller;
			if (fc == null)
				return;

			fc.SelectLinkToRigidbody(null);
			fc.currentPositionState = FreeControllerV3.PositionState.On;
			fc.currentRotationState = FreeControllerV3.RotationState.On;
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

		public float DistanceToSurface(Vector3 pos, bool debug = false)
		{
			return DistanceToSurface(null, debug, pos, true);
		}

		public float DistanceToSurface(IBodyPart other, bool debug)
		{
			return DistanceToSurface(other, debug, Vector3.Zero, false);
		}

		private UnityEngine.Vector3 ClosestPoint(VamBodyPartRegion c, Vector3 p)
		{
			var cc = (c as VamColliderRegion);
			Cue.Assert(cc != null);

			return Physics.ClosestPoint(
				U.ToUnity(p), cc.Collider,
				cc.Collider.transform.position, cc.Collider.transform.rotation);
		}

		public BodyPartRegionInfo ClosestBodyPartRegion(Vector3 pos)
		{
			return ClosestBodyPartRegion(null, false, pos, true);
		}

		public BodyPartRegionInfo ClosestBodyPartRegion(IBodyPart other, bool debug, Vector3 forceOtherPos, bool doForceOtherPos)
		{
			if (distanceRenderers_ != null)
			{
				foreach (var d in distanceRenderers_)
					Cue.Instance.VamSys.DebugRenderer.RemoveRender(d);

				distanceRenderers_.Clear();
			}

			var thisColliders = GetRegions();
			var otherColliders = (other as VamBodyPart)?.GetRegions();

			var thisPos = Position;
			var thisPosU = U.ToUnity(thisPos);
			var otherPos = (doForceOtherPos ? forceOtherPos : (other?.Position ?? Vector3.Zero));
			var otherPosU = U.ToUnity(otherPos);

			bool thisCollidersValid = (thisColliders != null && thisColliders.Length > 0);
			bool otherCollidersValid = (otherColliders != null && otherColliders.Length > 0);


			if (thisCollidersValid && otherCollidersValid)
			{
				float closest = float.MaxValue;

				VamBodyPartRegion thisDebug = null;
				VamBodyPartRegion otherDebug = null;

				try
				{
					for (int i = 0; i < thisColliders.Length; ++i)
					{
						for (int j = 0; j < otherColliders.Length; ++j)
						{
							var thisPoint = ClosestPoint(
								thisColliders[i],
								otherColliders[j].Position);

							var otherPoint = ClosestPoint(
								otherColliders[j],
								thisColliders[i].Position);

							var d = UnityEngine.Vector3.Distance(thisPoint, otherPoint);

							if (d < closest)
							{
								thisDebug = thisColliders[i];
								otherDebug = otherColliders[j];
								closest = d;
							}
						}
					}

					var dp = Vector3.Distance(thisPos, otherPos);

					if (dp < closest)
					{
						if (debug)
							Log.Error($"both valid, closest is position, {thisPos} {otherPos} {dp}");

						closest = dp;
					}
					else
					{
						if (debug)
						{
							Log.Error(
								$"both valid, " +
								$"closest is {thisDebug.FullName} " +
								$"{otherDebug.FullName} " +
								$"{closest}");
						}
					}
				}
				catch (Exception e)
				{
					Log.Error($"ex1 t={thisColliders} o={otherColliders?.Length}");
					Log.Error(e.ToString());
				}

				return new BodyPartRegionInfo(otherDebug, closest);
			}
			else if (thisCollidersValid)
			{
				float closest = float.MaxValue;
				VamBodyPartRegion c = null;

				try
				{
					for (int i = 0; i < thisColliders.Length; ++i)
					{
						var p = ClosestPoint(thisColliders[i], otherPos);
						var d = Vector3.Distance(otherPos, U.FromUnity(p));

						if (Render)
							AddDistanceRenderer(otherPos, U.FromUnity(p));

						if (d < closest)
						{
							c = thisColliders[i];
							closest = d;
						}
					}

					if (debug)
					{
						Log.Error(
							$"this valid, " +
							$"closest is {other} at {otherPos}, " +
							$"this is {this} {c.FullName} " +
							$"at {c.Position}, d={closest}");
					}
				}
				catch (Exception e)
				{
					Log.Error("ex2");
					Log.Error(e.ToString());
				}


				return new BodyPartRegionInfo(c, closest);
			}
			else if (otherCollidersValid)
			{
				float closest = float.MaxValue;
				VamBodyPartRegion c = null;

				try
				{
					for (int i = 0; i < otherColliders.Length; ++i)
					{
						var p = ClosestPoint(otherColliders[i], thisPos);
						var d = Vector3.Distance(thisPos, U.FromUnity(p));

						if (d < closest)
						{
							c = otherColliders[i];
							closest = d;
						}
					}

					if (debug)
					{
						Log.Error(
							$"other valid, closest is {thisPos} " +
							$"{c.FullName} {closest}");
					}
				}
				catch (Exception e)
				{
					Log.Error("ex3");
					Log.Error(e.ToString());
				}

				return new BodyPartRegionInfo(c, closest);
			}
			else
			{
				return BodyPartRegionInfo.None;
			}
		}

		public float DistanceToSurface(IBodyPart other, bool debug, Vector3 forceOtherPos, bool doForceOtherPos)
		{
			var r = ClosestBodyPartRegion(other, debug, forceOtherPos, doForceOtherPos);

			if (r.region != null)
				return r.distance;

			var thisPos = Position;
			var otherPos = (doForceOtherPos ? forceOtherPos : (other?.Position ?? Vector3.Zero));

			float d = Vector3.Distance(thisPos, otherPos);
			if (debug)
				Log.Error($"neither valid, {thisPos} {otherPos} {d}");

			return d;
		}

		public abstract bool ContainsTransform(Transform t, bool debug);

		protected virtual VamBodyPartRegion[] GetRegions()
		{
			return colliders_;
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


	class NullBodyPart : VamBodyPart
	{
		public NullBodyPart(VamAtom a, BodyPartType type)
			: base(a, type)
		{
		}

		public override bool Exists { get { return false; } }

		public override Vector3 ControlPosition { get; set; }
		public override Quaternion ControlRotation { get; set; }
		public override Vector3 Position { get; }
		public override Quaternion Rotation { get; }

		public override bool ContainsTransform(Transform t, bool debug)
		{
			if (debug)
				Log.Error($"{BodyPartType.ToString(Type)}: {t.name} not found");

			return false;
		}

		public override string ToString()
		{
			return "";
		}
	}
}
