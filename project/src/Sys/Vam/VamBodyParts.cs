using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cue.Sys.Vam
{
	public abstract class VamBodyPart : IBodyPart
	{
		private VamBasicAtom atom_;
		private BodyPartType type_;
		private VamColliderRegion[] regions_ = null;
		private List<GrabInfo> grabCache_ = new List<GrabInfo>();
		private Logger log_ = null;
		private CueCollisionHandler[] handlers_ = null;
		private float[,] collisions_ = null;
		private BodyPartType[] ignoreBodyParts_ = new BodyPartType[0];

		private VamAtom toyAtom_ = null;
		private float toyCollision_ = 0;

		private VamAtom externalAtom_ = null;
		private float externalCollision_ = 0;

		public BodyPartType Type { get { return type_; } }
		public virtual bool Exists { get { return true; } }
		public virtual bool IsPhysical { get { return true; } }

		public virtual Rigidbody Rigidbody { get { return null; } }
		public virtual FreeControllerV3 Controller { get { return null; } }

		public virtual bool CanGrab { get { return false; } }
		public virtual bool Grabbed { get { return false; } }

		public abstract Vector3 ControlPosition { get; set; }
		public abstract Quaternion ControlRotation { get; set; }
		public abstract Vector3 Position { get; }
		public virtual Vector3 Center { get { return Position; } }
		public abstract Quaternion Rotation { get; }
		public virtual Quaternion CenterRotation { get { return Rotation; } }

		private List<VamDebugRenderer.IDebugRender> renderers_ = null;
		private List<VamDebugRenderer.IDebugRender> distanceRenderers_ = null;

		private List<TriggerInfo> triggerCache_ = new List<TriggerInfo>();
		private TriggerInfo[] triggers_ = null;


		protected VamBodyPart(VamBasicAtom a, BodyPartType t)
		{
			atom_ = a;
			type_ = t;
		}

		protected VamBodyPart(VamBasicAtom a, BodyPartType t, Collider[] colliders, string[] ignoreBodyParts)
			: this(a, t)
		{
			if (colliders != null)
			{
				var cs = new List<VamColliderRegion>();
				foreach (var c in colliders)
					cs.Add(new VamColliderRegion(this, c));

				regions_ = cs.ToArray();
			}

			if (ignoreBodyParts == null)
				ignoreBodyParts_ = new BodyPartType[0];
			else
				ignoreBodyParts_ = MakeIgnoreBodyParts(ignoreBodyParts);

		}

		protected VamBodyPart(VamBasicAtom a, BodyPartType t, string[] colliders, string[] ignoreBodyParts)
			: this(a, t)
		{
			if (colliders != null)
			{
				var cs = new List<VamColliderRegion>();
				foreach (var cn in colliders)
				{
					var c = (a as VamAtom).FindCollider(cn);
					if (c == null)
					{
						Log.Error($"{a.ID}: collider {cn} not found");
						continue;
					}

					cs.Add(new VamColliderRegion(this, c));
				}

				regions_ = cs.ToArray();
			}

			if (ignoreBodyParts == null)
				ignoreBodyParts_ = new BodyPartType[0];
			else
				ignoreBodyParts_ = MakeIgnoreBodyParts(ignoreBodyParts);

		}

		protected void Set(Collider[] colliders, string[] ignoreBodyParts)
		{
			if (regions_ != null)
			{
				foreach (var c in regions_)
					CueCollisionHandler.RemoveFromCollider(c.Collider);

				handlers_ = null;
				regions_ = null;
			}

			if (colliders != null)
			{
				var cs = new List<VamColliderRegion>();
				foreach (var c in colliders)
					cs.Add(new VamColliderRegion(this, c));

				regions_ = cs.ToArray();
			}

			if (ignoreBodyParts == null)
				ignoreBodyParts_ = new BodyPartType[0];
			else
				ignoreBodyParts_ = MakeIgnoreBodyParts(ignoreBodyParts);


			SetColliders();
		}

		private BodyPartType[] MakeIgnoreBodyParts(string[] ignoreBodyParts)
		{
			var list = new List<BodyPartType>();

			foreach (var s in ignoreBodyParts)
			{
				var bp = BodyPartType.FromString(s);
				if (bp == BP.None)
				{
					Log.Error($"ignore body parts: bad part '{s}'");
					continue;
				}

				list.Add(bp);
			}

			return list.ToArray();
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

		public VamBasicAtom VamAtom
		{
			get { return atom_; }
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

		public virtual void Init()
		{
			collisions_ = new float[Cue.Instance.ActivePersons.Length, BP.Count];
			SetColliders();
		}

		private void SetColliders()
		{
			var hs = new List<CueCollisionHandler>();

			if (regions_ != null)
			{
				foreach (var c in regions_)
				{
					var h = CueCollisionHandler.AddToCollider(c.Collider, this);
					if (h != null)
						hs.Add(h);
				}

				if (hs.Count > 0)
					handlers_ = hs.ToArray();
			}
		}

		public void OnPluginState(bool b)
		{
			if (handlers_ != null)
			{
				foreach (var h in handlers_)
				{
					try
					{
						h.enabled = b;
					}
					catch (Exception)
					{
						// eat it, happens on reload
					}
				}
			}
		}

		protected virtual void AddDebugRenderers()
		{
			AddDebugRenderer(Cue.Instance.VamSys.DebugRenderer.AddPositionRender(this));

			if (Position != Center)
				AddDebugRenderer(Cue.Instance.VamSys.DebugRenderer.AddCenterRender(this));

			if (regions_ != null)
			{
				foreach (var c in regions_)
				{
					var cc = c as VamColliderRegion;
					if (cc?.Collider != null)
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
			if (other.Atom.Possessed || other.Atom is VamCameraAtom || other.Atom == Cue.Instance.Player?.Atom)
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


		public static bool IgnoreTrigger(
			VamAtom sourceAtom, VamBodyPart sourcePart,
			VamAtom targetAtom, VamBodyPart targetPart)
		{
			if (sourceAtom == null || targetAtom == null)
				return true;

			if (sourceAtom.IsPerson && targetAtom.IsPerson)
			{
				if (sourceAtom == targetAtom && sourcePart == targetPart)
				{
					// self collision
					return true;
				}

				if (sourcePart != null && targetPart != null)
				{
					return
						targetPart.DoIgnoreTrigger(sourceAtom, sourcePart) ||
						sourcePart.DoIgnoreTrigger(targetAtom, targetPart);
				}
			}
			else
			{
				return
					sourceAtom.Atom.category == "Environments" ||
					sourceAtom.Atom.category == "Floors And Walls" ||
					sourceAtom.Atom.category == "Furniture" ||
					sourceAtom.Atom.category == "Props" ||
					sourceAtom.Atom.category == "None";  // cua
			}

			return false;
		}

		private bool DoIgnoreTrigger(IAtom sourceAtom, VamBodyPart sourcePart)
		{
			// self collision
			if (sourceAtom != null && sourceAtom == Atom)
			{
				if (sourcePart.Type == BP.Penis)
				{
					// probably the dildo touching genitals, ignore
					return true;
				}
				else
				{
					if (Type == BP.Penis && sourcePart.Type == BP.Hips)
					{
						// probably the dildo touching genitals, ignore
						return true;
					}
				}

				for (int i = 0; i < ignoreBodyParts_.Length; ++i)
				{
					if (ignoreBodyParts_[i] == sourcePart.Type)
						return true;
				}
			}

			return false;
		}

		public TriggerInfo[] GetTriggers()
		{
			TriggerInfo[] r;

			Instrumentation.Start(I.Triggers);
			{
				r = DoGetTriggers();
			}
			Instrumentation.End();

			return r;
		}

		private TriggerInfo[] DoGetTriggers()
		{
			if (!Exists)
				return null;

			//if (triggerCache_ != null)
			{
				triggers_ = null;
				triggerCache_.Clear();
			}

			UpdateTriggers();

			for (int i = 0; i < collisions_.GetLength(0); ++i)
			{
				foreach (var b in BodyPartType.Values)
				{
					if (collisions_[i, b.Int] > 0)
					{
						triggerCache_.Add(TriggerInfo.FromPerson(
							i, b, collisions_[i, b.Int]));
					}
				}
			}

			if (toyCollision_ > 0)
				triggerCache_.Add(TriggerInfo.FromExternal(TriggerInfo.ToyType, toyAtom_, toyCollision_));

			if (externalCollision_ > 0)
				triggerCache_.Add(TriggerInfo.FromExternal(TriggerInfo.NoneType, externalAtom_, externalCollision_));

			triggers_ = triggerCache_.ToArray();
			ClearCollisions();

			return triggers_;
		}

		protected virtual void UpdateTriggers()
		{
			// no-op
		}

		public void AddPersonCollision(int sourcePersonIndex, BodyPartType sourceBodyPart, float f)
		{
			collisions_[sourcePersonIndex, sourceBodyPart.Int] = Math.Max(
				collisions_[sourcePersonIndex, sourceBodyPart.Int], f);
		}

		public void AddExternalCollision(VamAtom a, float f)
		{
			if (a != null && a.Atom.category == "Toys")
			{
				toyCollision_ = Math.Max(toyCollision_, f);
				toyAtom_ = a;
			}
			else
			{
				externalCollision_ = Math.Max(externalCollision_, f);
				externalAtom_ = a;
			}
		}

		private void ClearCollisions()
		{
			toyAtom_ = null;
			toyCollision_ = 0;
			externalAtom_ = null;
			externalCollision_ = 0;

			for (int i = 0; i < Cue.Instance.ActivePersons.Length; ++i)
			{
				for (int j = 0; j < BP.Count; ++j)
					collisions_[i, j] = 0;
			}
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
			if (Render)
				debug = true;

			if (distanceRenderers_ != null)
			{
				foreach (var d in distanceRenderers_)
					Cue.Instance.VamSys.DebugRenderer.RemoveRender(d);

				distanceRenderers_.Clear();
			}

			var thisColliders = regions_;
			var otherColliders = (other as VamBodyPart)?.regions_;

			var thisPos = Position;
			var thisPosU = U.ToUnity(thisPos);
			var otherPos = (doForceOtherPos ? forceOtherPos : (other?.Position ?? Vector3.Zero));
			var otherPosU = U.ToUnity(otherPos);

			bool thisCollidersValid = (thisColliders != null && thisColliders.Length > 0);
			bool otherCollidersValid = (otherColliders != null && otherColliders.Length > 0);


			if (thisCollidersValid && otherCollidersValid)
			{
				float closest = float.MaxValue;

				VamBodyPartRegion thisRegion = null;
				VamBodyPartRegion otherRegion = null;

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
								thisRegion = thisColliders[i];
								otherRegion = otherColliders[j];
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
								$"closest is {thisRegion.FullName} " +
								$"{otherRegion.FullName} " +
								$"{closest}");
						}
					}
				}
				catch (Exception e)
				{
					Log.Error($"ex1 t={thisColliders} o={otherColliders?.Length}");
					Log.Error(e.ToString());
				}

				return new BodyPartRegionInfo(otherRegion, closest);
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

		public bool ContainsTransform(Transform t, bool debug)
		{
			if (regions_ != null)
			{
				for (int i = 0; i < regions_.Length; ++i)
				{
					if (regions_[i].Collider.transform == t)
					{
						if (debug)
							Log.Error($"{t.name} is collider #{i}");

						return true;
					}
					else
					{
						if (debug)
							Log.Error($"{t.name} not collider {regions_[i].Collider.transform.name}");
					}
				}
			}

			return DoContainsTransform(t, debug);
		}

		protected abstract bool DoContainsTransform(Transform t, bool debug);

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

		public override string ToString()
		{
			return $"{Atom.ID}.{BodyPartType.ToString(Type)}";
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

		protected override bool DoContainsTransform(Transform t, bool debug)
		{
			if (debug)
				Log.Error($"{BodyPartType.ToString(Type)}: {t.name} not found");

			return false;
		}

		public string ToDetailedString()
		{
			return "";
		}
	}
}
