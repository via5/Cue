using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cue.Sys.Vam
{
	class VamBone : IBone
	{
		private Rigidbody parent_;
		private DAZBone bone_;
		private ConfigurableJoint joint_;

		public VamBone(VamBody body, Rigidbody parent, DAZBone b)
		{
			parent_ = parent;
			bone_ = b;
			joint_ = b.GetComponent<ConfigurableJoint>();

			if (joint_ == null)
				body.Log.Error($"bone '{b.name}' has no configurable joint");
		}

		public Transform Transform
		{
			get { return bone_.transform; }
		}

		public Vector3 Position
		{
			get { return U.FromUnity(bone_.transform.position); }
		}

		public Quaternion Rotation
		{
			get
			{
				var rb = bone_.GetComponent<Rigidbody>();
				return U.FromUnity(bone_.transform.rotation);
			}
		}

		public override string ToString()
		{
			return $"{bone_.name}";
		}
	}


	abstract class VamBasicBody : IBody
	{
		private VamAtom atom_;

		protected VamBasicBody(VamAtom a)
		{
			atom_ = a;
		}

		public VamAtom Atom
		{
			get { return atom_; }
		}

		public Logger Log
		{
			get { return atom_.Log; }
		}

		public virtual bool Exists { get { return true; } }
		public abstract float Sweat { get; }
		public abstract float Flush { get; }
		public abstract bool Strapon { get; set; }

		public abstract void SetSweat(float f, float multiplier);
		public abstract void SetFlush(float f, float multiplier, Color baseColor);

		public abstract IBodyPart[] GetBodyParts();
		public abstract Hand GetLeftHand();
		public abstract Hand GetRightHand();
		public abstract void Debug(DebugLines d);

		public abstract VamBodyPart BodyPartForTransform(
			Transform t, Transform stop, bool debug);
	}


	class VamBody : VamBasicBody
	{
		struct FlushInfo
		{
			public const float MaxDistanceToWhite = 1.5f;
			public const float MaxSaturation = 0.35f;

			public HSVColor hsv;
			public float distanceToWhite;
			public float rawWhite, rawSaturation, white, saturation, p;
		}

		private StraponBodyPart strapon_ = null;
		private FloatParameter gloss_ = null;
		private ColorParameter color_ = null;
		private Color initialColor_;
		private IBodyPart[] parts_;
		private Hand leftHand_, rightHand_;

		private float sweat_ = 0;
		private float sweatMultiplier_ = 1.0f;
		private IEasing sweatEasing_ = new CubicInEasing();
		private bool glossEnabled_ = false;

		private float flush_ = 0;
		private float flushMultiplier_ = 1.0f;
		private Color flushBaseColor_ = Color.Red;
		private IEasing flushEasing_ = new SineInEasing();
		private bool colorEnabled_ = false;
		private IEasing skinWhiteEasing_ = new QuadOutEasing();
		private IEasing skinSaturationEasing_ = new QuartOutEasing();

		private string[] mouthColliderNames_ = null;
		private Collider[] mouthColliders_ = null;

		private string[] tongueColliderNames_ = null;
		private Collider[] tongueColliders_ = null;

		public VamBody(VamAtom a)
			: base(a)
		{
			CueCollisionHandler.RemoveAll(Atom.Atom.transform);

			gloss_ = new FloatParameter(a, "skin", "Gloss");
			if (!gloss_.Check(true))
				Log.Error("no skin gloss parameter");

			color_ = new ColorParameter(a, "skin", "Skin Color");
			if (!color_.Check(true))
				Log.Error("no skin color parameter");

			initialColor_ = color_.Value;

			var ld = new VamBodyLoader(Atom);

			ld.Load();

			parts_ = ld.Parts;
			leftHand_ = ld.LeftHand;
			rightHand_ = ld.RightHand;
			strapon_ = parts_[BP.Penis.Int] as StraponBodyPart;
			mouthColliderNames_ = ld.MouthColliders;
			tongueColliderNames_ = ld.TongueColliders;

			// too slow to do it on demand
			GetKissColliders();

			Cue.Instance.Options.Changed += CheckOptions;
			CheckOptions();
		}

		public override void Debug(DebugLines d)
		{
			var fi = MakeFlushInfo();

			d.Add($"gloss     {gloss_}");
			d.Add($"color     {color_}");
			d.Add($"initColor {initialColor_}");
			d.Add($"flush:");
			d.Add($"  - baseColor       {flushBaseColor_}");
			d.Add($"  - hsv             H={fi.hsv.H:0.00} S={fi.hsv.S:0.00} V={fi.hsv.V:0.00} (max S {FlushInfo.MaxSaturation:0.00})");
			d.Add($"  - distanceToWhite {fi.distanceToWhite:0.000} (max {FlushInfo.MaxDistanceToWhite})");
			d.Add($"  - raw             white={fi.rawWhite:0.000} saturation={fi.rawSaturation}");
			d.Add($"  - eased           white={fi.white:0.000} saturation={fi.saturation}");
			d.Add($"  - p               {fi.p:0.000}");
		}

		public Color CurrentColor
		{
			get { return color_.Value; }
		}

		public override IBodyPart[] GetBodyParts()
		{
			return parts_;
		}

		public void LateUpdate(float s)
		{
			strapon_?.LateUpdate(s);
		}

		public IBodyPart GetPart(BodyPartType i)
		{
			return parts_[i.Int];
		}

		public override VamBodyPart BodyPartForTransform(
			Transform t, Transform stop, bool debug)
		{
			// see VamSys.BodyPartForTransform()

			var check = t;
			while (check != null)
			{
				if (debug)
					Log.Error($"{this}: looking for {t.name}, stop={stop.name}");

				for (int i = 0; i < parts_.Length; ++i)
				{
					var vp = parts_[i] as VamBodyPart;
					if (vp == null)
						continue;

					// check this transform and all of its parents to see if they
					// match any body part

					if (vp.ContainsTransform(check, debug))
					{
						if (debug)
							Log.Error($"found {t.name}, is {check.name} in {vp}");

						return vp;
					}
				}

				if (check == stop)
				{
					if (debug)
						Log.Error($"{t.name} not found, reached stop");

					break;
				}

				if (debug)
					Log.Error($"{check.name} not found, checking parent {check.parent.name}");

				check = check.parent;
			}

			return null;
		}

		public void SetCollidersForKiss(bool ignoreCollision, VamBody other)
		{
			GetKissColliders();
			other.GetKissColliders();

			SetIgnoreCollision(ignoreCollision, mouthColliders_, other.mouthColliders_);
			SetIgnoreCollision(ignoreCollision, tongueColliders_, other.tongueColliders_);
		}

		private void SetIgnoreCollision(bool ignoreCollision, Collider[] acs, Collider[] bcs)
		{
			for (int i = 0; i < acs.Length; ++i)
			{
				var a = acs[i];
				if (a == null)
					continue;

				for (int j = 0; j < bcs.Length; ++j)
				{
					var b = bcs[j];
					if (b == null)
						continue;

					//Log.Info($"ignore collision {a} {b}");
					Physics.IgnoreCollision(a, b, ignoreCollision);
				}
			}
		}

		private void GetKissColliders()
		{
			if (mouthColliders_ == null)
			{
				var list = new List<Collider>();

				for (int i = 0; i < mouthColliderNames_.Length; ++i)
				{
					var c = Atom.FindCollider(mouthColliderNames_[i]);
					if (c == null)
						Log.Error($"mouth colliders: {mouthColliderNames_[i]} not found");

					list.Add(c);
				}

				Log.Verbose($"{list.Count} mouth colliders");
				mouthColliders_ = list.ToArray();
			}

			if (tongueColliders_ == null)
			{
				var list = new List<Collider>();

				for (int i = 0; i < tongueColliderNames_.Length; ++i)
				{
					var c = Atom.FindCollider(tongueColliderNames_[i]);
					if (c == null)
						Log.Error($"tongue colliders: {tongueColliderNames_[i]} not found");

					list.Add(c);
				}

				Log.Verbose($"{list.Count} tongue colliders");
				tongueColliders_ = list.ToArray();
			}
		}

		public override bool Strapon
		{
			get { return strapon_?.Exists ?? false; }
			set { strapon_?.Set(value); }
		}

		public override Hand GetLeftHand()
		{
			return leftHand_;
		}

		public override Hand GetRightHand()
		{
			return rightHand_;
		}

		public void Init()
		{
			foreach (var p in parts_)
				(p as VamBodyPart).Init();
		}

		public void OnPluginState(bool b)
		{
			if (b)
			{
				initialColor_ = color_.Value;
				SetSweat(sweat_, sweatMultiplier_);
				SetFlush(flush_, flushMultiplier_, flushBaseColor_);
			}
			else
			{
				Reset();
			}

			foreach (var p in parts_)
				(p as VamBodyPart).OnPluginState(b);
		}

		public override float Sweat
		{
			get { return sweat_; }
		}

		public override float Flush
		{
			get { return flush_; }
		}


		public override void SetSweat(float f, float multiplier)
		{
			if (sweat_ != f || sweatMultiplier_ != multiplier)
			{
				sweat_ = f;
				sweatMultiplier_ = multiplier;

				SetSweatInternal(f * multiplier);
			}
		}

		private void SetSweatInternal(float v)
		{
			var p = gloss_.Parameter;
			if (p != null)
			{
				float def = p.defaultVal;
				float range = (p.max - def);

				SetGloss(def + sweatEasing_.Magnitude(v) * range);
			}
		}


		public override void SetFlush(float f, float multiplier, Color baseColor)
		{
			if (flush_ != f || flushMultiplier_ != multiplier || flushBaseColor_ != baseColor)
			{
				flush_ = f;
				flushMultiplier_ = multiplier;
				flushBaseColor_ = baseColor;

				SetFlushInternal(f, multiplier, baseColor);
			}
		}

		private void SetFlushInternal(float f, float multiplier, Color baseColor)
		{
			var fi = MakeFlushInfo();
			LerpColor(baseColor, f * fi.p, multiplier);
		}

		private FlushInfo MakeFlushInfo()
		{
			var i = new FlushInfo();

			{
				i.hsv = U.ToHSV(initialColor_);
				i.distanceToWhite = Color.Distance(Color.White, initialColor_);

				i.rawWhite = U.Clamp(FlushInfo.MaxDistanceToWhite - i.distanceToWhite, 0, 1);
				i.rawSaturation = U.Clamp(1 - (i.hsv.S / FlushInfo.MaxSaturation), 0, 1);

				i.white = skinWhiteEasing_.Magnitude(i.rawWhite);
				i.saturation = skinSaturationEasing_.Magnitude(i.rawSaturation);

				i.p = Math.Min(i.white, i.saturation);
			}

			return i;
		}

		private void LerpColor(Color target, float f, float multiplier)
		{
			var p = color_.Parameter;

			if (p != null)
			{
				var c = Color.Lerp(
					initialColor_, target,
					flushEasing_.Magnitude(f) * multiplier);

				var cd = Color.Distance(c, U.FromHSV(p.val));

				// changing body colours seem to allocate memory to update
				// textures, avoid for small changes
				if (cd >= 0.02f)
					SetColor(U.ToHSV(c));
			}
		}

		private void Reset()
		{
			if (gloss_.Parameter != null)
				SetGloss(gloss_.Parameter.defaultVal);

			if (color_.Parameter != null)
				SetColor(U.ToHSV(initialColor_));
		}

		private void SetGloss(float f)
		{
			if (glossEnabled_)
				gloss_.Parameter.val = f;
		}

		private void SetColor(HSVColor c)
		{
			if (colorEnabled_)
				color_.Parameter.val = c;
		}

		private void CheckOptions()
		{
			if (glossEnabled_ != Cue.Instance.Options.SkinGloss)
			{
				glossEnabled_ = Cue.Instance.Options.SkinGloss;

				if (Cue.Instance.Options.SkinGloss)
					SetSweat(sweat_, sweatMultiplier_);
				else if (gloss_.Parameter != null)
					gloss_.Parameter.val = gloss_.Parameter.defaultVal;
			}


			if (colorEnabled_ != Cue.Instance.Options.SkinColor)
			{
				colorEnabled_ = Cue.Instance.Options.SkinColor;

				if (colorEnabled_)
					SetFlush(flush_, flushMultiplier_, flushBaseColor_);
				else if (color_.Parameter != null)
					color_.Parameter.val = U.ToHSV(initialColor_);
			}
		}
	}
}
