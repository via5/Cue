using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cue.Sys.Vam
{
	class StraponBodyPart : TriggerBodyPart
	{
		private IObject dildo_ = null;
		private Collider anchor_ = null;
		private Collider[] colliders_ = null;

		private float postCreateElapsed_ = 0;
		private bool postCreate_ = false;

		public StraponBodyPart(VamAtom a)
			: base(a, BP.Penis)
		{
			if (Cue.Instance.Sys.GetAtom(DildoID) != null)
				Set(true);
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

		public void Set(bool b)
		{
			if (Exists == b)
				return;

			if (b)
			{
				if (dildo_ == null)
					Create();
				else
					dildo_.Visible = true;
			}
			else
			{
				if (dildo_ != null)
					dildo_.Visible = false;
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
				oc.Create(Atom, StraponID, (o) =>
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

			oc.Create(Atom, DildoID, (o) =>
			{
				if (o == null)
				{
					Log.Error("failed to create dildo");
					return;
				}

				SetDildo(o);
			});
		}

		private void SetDildo(IObject a)
		{
			if (a == null)
			{
				Log.Info($"removing dildo");
				dildo_ = null;
				Init(null, null, null, null);
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

				DoInit();
				SetEnabled(true);
			}
		}

		protected override Collider[] GetColliders()
		{
			return colliders_;
		}

		private void SetEnabled(bool b)
		{
			Enabled = b;
			SetClothingActive(b);
		}

		public void LateUpdate(float s)
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
						DoInit();

					SetEnabled(true);
				}

				if (!Enabled || anchor_ == null)
					return;

				var labia = (Atom.Body as VamBody)?.GetPart(BP.Labia);

				var q = Quaternion.Zero;
				if (labia != null)
					q = labia.Rotation;

				var v = U.FromUnity(anchor_.transform.position);
				v.Y += 0.01f;
				v.Z -= 0.01f;

				dildo_.Position = v;
				dildo_.Rotation = q;

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
			catch (Exception)
			{
				// dildo can get deleted at any time
				Log.Error($"looks like dildo got deleted");
				SetDildo(null);
			}
		}

		public override string ToString()
		{
			if (!Exists)
				return "";

			return $"dildo (" + base.ToString() + ")";
		}

		private void DoInit()
		{
			dildo_.Atom.Collisions = false;
			dildo_.Atom.Scale = Atom.Scale;

			postCreate_ = true;
			postCreateElapsed_ = 0;

			var collidersString = dildo_.GetParameter("colliders");
			if (collidersString == "")
			{
				Log.Warning("dildo is missing collidersString parameter");
			}
			else
			{
				var colliderNames = collidersString.Split(';');

				var cs = new List<Collider>();
				foreach (var cn in colliderNames)
				{
					var c = U.FindCollider(
						(dildo_.Atom as VamAtom).Atom, cn.Trim());

					if (c == null)
					{
						Log.Error($"{Atom.ID}: dildo collider {cn} not found");
						continue;
					}

					cs.Add(c);
				}

				colliders_ = cs.ToArray();

				var names = new List<string>();
				foreach (var c in colliders_)
					names.Add(c.name);

				Log.Info($"dildo colliders: {string.Join(", ", names.ToArray())}");
			}


			var anchorName = dildo_.GetParameter("anchor");
			if (anchorName == "")
			{
				Log.Warning("dildo is missing anchor parameter");
			}
			else
			{
				anchor_ = U.FindCollider(VamAtom.Atom, anchorName);

				if (anchor_ == null)
					Log.Error($"dildo anchor {anchor_} not found in {Atom.ID}");
				else
					Log.Info($"dildo anchor: {anchor_}");
			}


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

			Init(h, d.mainController, d.transform, null);
		}
	}
}
