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
		private Dictionary<Transform, int> partMap_ = new Dictionary<Transform, int>();

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
		public abstract float Sweat { get; set; }
		public abstract float Flush { get; set; }
		public abstract bool Strapon { get; set; }

		public abstract IBodyPart[] GetBodyParts();
		public abstract Hand GetLeftHand();
		public abstract Hand GetRightHand();


		public virtual IBodyPart BodyPartForTransformCached(Transform t)
		{
			int bp;
			if (partMap_.TryGetValue(t, out bp))
				return GetBodyParts()[bp];

			return null;
		}

		public abstract IBodyPart BodyPartForTransform(Transform t, Transform stop);

		protected void AddBodyPartCache(Transform t, int bodyPart)
		{
			partMap_.Add(t, bodyPart);
		}
	}


	class VamBody : VamBasicBody
	{
		private StraponBodyPart strapon_ = null;
		private FloatParameter gloss_ = null;
		private ColorParameter color_ = null;
		private Color initialColor_;
		private IBodyPart[] parts_;
		private Hand leftHand_, rightHand_;

		private float sweat_ = 0;
		private IEasing sweatEasing_ = new CubicInEasing();

		private float flush_ = 0;
		private IEasing flushEasing_ = new SineInEasing();

		public VamBody(VamAtom a)
			: base(a)
		{
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
			strapon_ = parts_[BP.Penis] as StraponBodyPart;
		}

		public override IBodyPart[] GetBodyParts()
		{
			return parts_;
		}

		public void LateUpdate(float s)
		{
			strapon_?.LateUpdate(s);
		}

		public IBodyPart GetPart(int i)
		{
			return parts_[i];
		}

		public override IBodyPart BodyPartForTransform(Transform t, Transform stop)
		{
			// see VamSys.BodyPartForTransform()

			var check = t;
			while (check != null)
			{
				for (int i = 0; i < parts_.Length; ++i)
				{
					var vp = parts_[i] as VamBodyPart;
					if (vp == null)
						continue;

					// check this transform and all of its parents to see if they
					// match any body part

					if (vp.ContainsTransform(check))
					{
						AddBodyPartCache(t, i);
						return vp;
					}
				}

				if (check == stop)
					break;

				check = check.parent;
			}

			return null;
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


		public void OnPluginState(bool b)
		{
			if (!b)
				Reset();
		}

		public override float Sweat
		{
			get
			{
				return sweat_;
			}

			set
			{
				sweat_ = value;

				var p = gloss_.Parameter;
				if (p != null)
				{
					float def = p.defaultVal;
					float range = (p.max - def);

					p.val = def + sweatEasing_.Magnitude(sweat_) * range;
				}
			}
		}

		public override float Flush
		{
			get
			{
				return flush_;
			}

			set
			{
				flush_ = value;
				LerpColor(Color.Red, flush_);
			}
		}

		private void LerpColor(Color target, float f)
		{
			var p = color_.Parameter;

			if (p != null)
			{
				var c = Color.Lerp(
					initialColor_, target, flushEasing_.Magnitude(f) * 0.07f);

				var cd = Color.Distance(c, U.FromHSV(p.val));

				// changing body colours seem to allocate memory to update
				// textures, avoid for small changes
				if (cd >= 0.02f)
				{
					p.val = U.ToHSV(c);
				}
			}
		}

		private void Reset()
		{
			if (gloss_.Parameter != null)
				gloss_.Parameter.val = gloss_.Parameter.defaultVal;

			if (color_.Parameter != null)
				color_.Parameter.val = U.ToHSV(initialColor_);
		}
	}
}
