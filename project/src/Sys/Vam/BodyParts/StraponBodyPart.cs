﻿using SimpleJSON;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cue.Sys.Vam
{
	class StraponBodyPart : ColliderBodyPart
	{
		private static readonly string[] IgnoreTransforms = new string[]
		{
			"belly", "hips", "leftThigh", "rightThigh", "leftGlute", "rightGlute"
		};

		private IObject dildo_ = null;
		private Collider anchor_ = null;
		private CapsuleCollider extremity_ = null;
		private CapsuleCollider anchorCC_ = null;
		private SphereCollider anchorSC_ = null;
		private Transform parent_ = null;
		private Transform child_ = null;

		private float postCreateElapsed_ = 0;
		private bool postCreate_ = false;
		private bool enabled_ = false;

		private Vector3 positionOffset_ = Vector3.Zero;
		private Quaternion rotationOffset_ = Quaternion.Identity;


		public StraponBodyPart(VamAtom a)
			: base(a, BP.Penis)
		{
			if (Cue.Instance.Sys.GetAtom(DildoID) != null)
			{
				Log.Verbose($"dildo '{DildoID}' found");
				Set(true);
			}
			else
			{
				Log.Verbose($"dildo '{DildoID}' doesn't exist");
			}
		}

		private string DildoID
		{
			get { return $"Dildo#{Atom.ID}"; }
		}

		private string StraponID
		{
			get { return $"Strapon#{Atom.ID}"; }
		}

		public IObject Dildo
		{
			get { return dildo_; }
		}

		public override bool IsPhysical
		{
			get { return Cue.Instance.Options.StraponPhysical; }
		}

		public override bool Exists
		{
			get { return (base.Exists && enabled_ && dildo_ != null); }
		}

		public override Vector3 ControlPosition
		{
			get { return dildo_.Atom.Position; }
			set { Log.Error("cannot move colliders"); }
		}

		public override Quaternion ControlRotation
		{
			get { return dildo_.Atom.Rotation; }
			set { Log.Error("cannot rotate colliders"); }
		}

		public override Vector3 Extremity
		{
			get
			{
				if (extremity_ == null || dildo_ == null)
					return base.Extremity;

				var size = (extremity_.height / 2 + 0.005f) * dildo_.Atom.Scale;
				var fwd = extremity_.transform.up * size;
				var pos = extremity_.transform.position + extremity_.center + fwd;

				return U.FromUnity(pos);
			}
		}

		private bool Enabled
		{
			get { return enabled_; }
			set { enabled_ = value; }
		}

		public void Set(bool b)
		{
			if (Exists == b)
			{
				Log.Verbose($"Set: {Exists} == {b}");
				return;
			}

			if (b)
			{
				if (dildo_ != null)
				{
					try
					{
						dildo_.Visible = true;
					}
					catch (Exception)
					{
						// dead
						dildo_ = null;
						Log.Verbose($"dildo '{DildoID}' is dead");
					}
				}

				if (dildo_ == null)
					Create();
			}
			else
			{
				if (dildo_ != null)
				{
					try
					{
						dildo_.Visible = false;
					}
					catch (Exception)
					{
						// dead
						dildo_ = null;
					}
				}
			}
		}

		private void Create()
		{
			Log.Info("creating strapon");
			AddDildo();
		}

		private void SetClothingActive(bool b)
		{
			var oc = Resources.Objects.Get("strapon");

			if (oc == null)
			{
				Log.Error("no strapon object creator");
				return;
			}

			if (b)
			{
				oc.Create(Atom, StraponID, (o, e) =>
				{
					Log.Error("strapon created");
				});
			}
			else
			{
				oc.Destroy(Atom, StraponID);
			}
		}

		private void AddDildo()
		{
			Log.Info("creating dildo");

			var oc = Resources.Objects.Get("dildo");
			if (oc == null)
			{
				Log.Error("no dildo object creator");
				return;
			}

			oc.Create(Atom, DildoID, (o, e) =>
			{
				if (o == null)
				{
					Log.Error("failed to create dildo");
					return;
				}

				SetDildo(o, e);
			});
		}

		private void SetDildo(IObject a, bool existing)
		{
			if (a == null)
			{
				Log.Info($"removing dildo");
				dildo_ = null;
				Set(null, null);
				SetEnabled(false);
			}
			else
			{
				Log.Info($"setting dildo to {a.ID}" + (a.Visible ? "" : " (but off)"));
				dildo_ = a;

				if (!dildo_.Visible)
				{
					SetEnabled(false);
					return;
				}

				SetEnabled(true);
				DoInit(existing);
			}
		}

		private void SetEnabled(bool b)
		{
			Enabled = b;
			SetClothingActive(b);
		}

		public override void LateUpdate(float s)
		{
			if (dildo_ == null)
				return;

			try
			{
				if (!dildo_.Visible && Enabled)
				{
					Log.Info($"dildo {dildo_.ID} turned off");
					SetEnabled(false);
				}
				else if (dildo_.Visible && !Enabled)
				{
					Log.Info($"dildo {dildo_.ID} turned on");

					if (Rigidbody == null)
						DoInit(true);

					SetEnabled(true);
				}

				if (!Enabled || anchor_ == null)
					return;

				if (anchorCC_ != null)
					SetFromCapsule();
				else if (anchorSC_ != null)
					SetFromSphere();
				else
					SetFromCollider();

				if (postCreate_)
				{
					postCreateElapsed_ += s;
					if (postCreateElapsed_ > 2)
					{
						postCreate_ = false;
						dildo_.Atom.Collisions = true;
					}
				}
			}
			catch (Exception e)
			{
				// dildo can get deleted at any time
				Log.Error($"looks like dildo got deleted: {e}");
				SetDildo(null, false);
			}
		}

		private void SetFromCapsule()
		{
			parent_.position = anchorCC_.transform.position;
			parent_.rotation = anchorCC_.transform.rotation;

			if (anchorCC_.direction == 0)
				child_.localRotation = UnityEngine.Quaternion.AngleAxis(90, UnityEngine.Vector3.forward);
			else if (anchorCC_.direction == 2)
				child_.localRotation = UnityEngine.Quaternion.AngleAxis(90, UnityEngine.Vector3.right);

			child_.localPosition = anchorCC_.center;

			dildo_.Position = U.FromUnity(child_.position) + positionOffset_ + new Vector3(0, 0, anchorCC_.radius);
			dildo_.Rotation = U.FromUnity(child_.rotation) * rotationOffset_;
		}

		private void SetFromSphere()
		{
			parent_.position = anchorSC_.transform.position;
			parent_.rotation = anchorSC_.transform.rotation;

			child_.localRotation = UnityEngine.Quaternion.identity;
			child_.localPosition = anchorSC_.center;

			dildo_.Position = U.FromUnity(child_.position) + positionOffset_ + new Vector3(0, 0, anchorSC_.radius);
			dildo_.Rotation = U.FromUnity(child_.rotation) * rotationOffset_;
		}

		private void SetFromCollider()
		{
			dildo_.Position = U.FromUnity(anchor_.transform.position) + positionOffset_;
			dildo_.Rotation = U.FromUnity(anchor_.transform.rotation) * rotationOffset_;
		}

		public override string ToString()
		{
			if (!base.Exists)
				return "dildo (non existent)";
			else if (!enabled_)
				return "dildo (disabled)";
			else
				return $"dildo (" + base.ToString() + ")";
		}

		protected override void AddDebugRenderers()
		{
			base.AddDebugRenderers();

			if (Exists)
				AddDebugRenderer(Cue.Instance.VamSys.DebugRenderer.AddRender(anchor_));
		}

		protected override bool DoContainsTransform(Transform t, bool debug)
		{
			var d = (dildo_?.Atom as VamAtom)?.Atom;
			if (d == null)
				return false;

			if (d.transform == t)
			{
				if (debug)
					Log.Error($"found {t.name}");

				return true;
			}
			else
			{
				if (debug)
					Log.Error($"{t.name} is not {d.transform.name}");

				return false;
			}
		}

		private void DoInit(bool existing)
		{
			dildo_.Atom.Collisions = false;

			if (!existing)
				dildo_.Atom.Scale = Atom.Scale;

			postCreate_ = true;
			postCreateElapsed_ = 0;

			var cs = new List<Collider>();

			var colliderNames = dildo_.Parameters.Object["colliders"]?.AsArray;
			if (colliderNames == null)
			{
				Log.Warning("dildo missing colliders");
			}
			else
			{
				foreach (JSONNode cn in colliderNames)
				{
					// cache has to be cleared because the dildo might have been
					// loaded as hidden, which doesn't find the colliders, for
					// whatever reason
					var c = (dildo_.Atom as VamAtom).FindCollider(cn.Value.Trim(), true);

					if (c == null)
					{
						Log.Error($"dildo collider {cn.Value} not found in {dildo_.Atom.ID}");
						continue;
					}

					cs.Add(c);
				}

				var names = new List<string>();
				foreach (var c in cs)
					names.Add(U.QualifiedName(c));

				Log.Info($"dildo colliders: {string.Join(", ", names.ToArray())}");
			}


			var anchorName = dildo_.Parameters.Object["anchor"].Value.Trim();
			if (string.IsNullOrEmpty(anchorName))
			{
				Log.Warning("dildo is missing anchor parameter");
			}
			else
			{
				anchor_ = (VamAtom as VamAtom).FindCollider(anchorName);
				anchorCC_ = anchor_ as CapsuleCollider;
				anchorSC_ = anchor_ as SphereCollider;

				if (anchor_ == null)
					Log.Error($"dildo anchor {anchorName} not found in {Atom.ID}");
				else
					Log.Info($"dildo anchor: {U.QualifiedName(anchor_)}");
			}


			var extremityName = dildo_.Parameters.Object["extremity"].Value.Trim();
			if (string.IsNullOrEmpty(extremityName))
			{
				Log.Warning("dildo is missing extremity parameter");
			}
			else
			{
				var c = (dildo_.Atom as VamAtom).FindCollider(extremityName);

				if (c == null)
				{
					Log.Error($"dildo extremity {extremityName} not found in {dildo_.Atom.ID}");
				}
				else
				{
					extremity_ = c as CapsuleCollider;

					if (extremity_ == null)
						Log.Error($"dildo extremity {extremityName} is not a capsule collider");
					else
						Log.Info($"dildo extremity: {U.QualifiedName(extremity_)}");
				}
			}


			positionOffset_ = Vector3.FromJSON(dildo_.Parameters.Object, "positionOffset");
			rotationOffset_ = Quaternion.FromJSON(dildo_.Parameters.Object, "rotationOffset");

			var d = (dildo_.Atom as VamAtom).Atom;

			var ct = d.GetComponentInChildren<CollisionTrigger>();
			if (ct == null)
			{
				Log.Error($"{d.uid} has no collision trigger");
				return;
			}

			var oldTriggerEnabled = ct.triggerEnabled;
			ct.triggerEnabled = true;

			var h = d.GetComponentInChildren<CollisionTriggerEventHandler>();
			if (h == null)
			{
				Log.Error($"{d.uid} has no collision trigger handler");
				ct.triggerEnabled = oldTriggerEnabled;
				return;
			}


			Log.Verbose($"removing vamfixes handlers from {d}");
			CueCollisionHandler.RemoveAll(d.transform);

			Log.Verbose($"adding handlers on {d}");
			foreach (var c in cs)
			{
				Log.Verbose($"{U.QualifiedName(c)}");
				CueCollisionHandler.AddToCollider(c, this);
			}

			Set(cs.ToArray(), d.mainController, IgnoreTransforms);

			if (parent_ == null)
			{
				parent_ = new GameObject().transform;
				parent_.SetParent(Cue.Instance.VamSys.RootTransform, false);

				child_ = new GameObject().transform;
				child_.SetParent(parent_, false);
			}

			foreach (var fc in d.freeControllers)
				fc.interactableInPlayMode = false;
		}
	}
}
