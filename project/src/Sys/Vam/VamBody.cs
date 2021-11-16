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
		private readonly StraponBodyPart strapon_;
		private FloatParameter gloss_ = null;
		private ColorParameter color_ = null;
		private Color initialColor_;
		private DAZBone hipBone_ = null;
		private IBodyPart[] parts_;

		private float sweat_ = 0;
		private IEasing sweatEasing_ = new CubicInEasing();

		private float flush_ = 0;
		private IEasing flushEasing_ = new SineInEasing();

		public VamBody(VamAtom a)
			: base(a)
		{
			strapon_ = new StraponBodyPart(a);

			gloss_ = new FloatParameter(a, "skin", "Gloss");
			if (!gloss_.Check(true))
				Log.Error("no skin gloss parameter");

			color_ = new ColorParameter(a, "skin", "Skin Color");
			if (!color_.Check(true))
				Log.Error("no skin color parameter");

			initialColor_ = color_.Value;
			parts_ = CreateBodyParts();
		}

		public override IBodyPart[] GetBodyParts()
		{
			return parts_;
		}

		public void LateUpdate(float s)
		{
			strapon_.LateUpdate(s);
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
			get { return strapon_.Exists; }
			set { strapon_.Set(value); }
		}

		private IBodyPart[] CreateBodyParts()
		{
			var map = new Dictionary<int, IBodyPart>();

			Action<int, IBodyPart> add = (type, p) =>
			{
				map[type] = p;
			};

			// some colliders can fire the labia/vagina triggers even though
			// they don't really make sense, especially for larger body parts;
			// ignore them
			var genitalsIgnore = new string[]
			{
				// happens for crossed legs
				"lThigh", "rThigh",

				// happens for larger bellies
				"FemaleAutoCollidersabdomen",
				"FemaleAutoColliderschest"
			};



			// head
			//
			add(BP.Head, GetRigidbody(
				BP.Head, new string[] {
					"HeadHard1Hard", "HeadHard10Hard", "FaceCentral1Hard",
					"TongueColliders/_Collider1",
					"lowerJawStandardColliders/_ColliderL1b",
					"HeadBack1Hard",
					"FaceHardLeft4Hard",
					"FaceHardRight4Hard",
					"neck/StandardColliders/_Collider1l",
					"neck/StandardColliders/_Collider1r",
					"neck/StandardColliders/_ColliderB2",
					"neck/StandardColliders/_Collider4r",
					"neck/StandardColliders/_Collider4l",
					"HeadLeftEarHard", "HeadRightEarHard"
				}, "headControl", "head"));

			add(BP.Lips, GetTrigger(
				BP.Lips, "", "LipTrigger"));

			add(BP.Mouth, GetTrigger(
				BP.Mouth, "", "MouthTrigger"));


			// breasts
			//
			add(BP.LeftBreast, GetTrigger(
				BP.LeftBreast, "lNippleControl", "lNippleTrigger", "",
				new string[] { "lShldr" }));

			add(BP.RightBreast, GetTrigger(
				BP.RightBreast, "rNippleControl", "rNippleTrigger", "",
				new string[] { "rShldr" }));


			// genitals
			//
			add(BP.Labia, GetTrigger(
				BP.Labia, "", "LabiaTrigger", "",
				genitalsIgnore, new string[] { "pelvisF1/pelvisF1Joint" }));

			add(BP.Vagina, GetTrigger(
				BP.Vagina, "", "VaginaTrigger", "",
				genitalsIgnore));

			add(BP.DeepVagina, GetTrigger(
				BP.DeepVagina, "", "DeepVaginaTrigger", "",
				genitalsIgnore));

			add(BP.DeeperVagina, GetTrigger(
				BP.DeeperVagina, "", "DeeperVaginaTrigger", "",
				genitalsIgnore));

			add(BP.Anus, null);


			// upper body
			//
			if (Atom.IsMale)
			{
				add(BP.Chest, GetRigidbody(
					BP.Chest, new string[] { "chest1/chest1Joint" },
					"chestControl", "chest"));

				add(BP.Belly, GetRigidbody(
					BP.Belly, new string[] {
						"abdomen2/_ColliderL1",
						"abdomen/_ColliderL1b",
						"abdomen/_ColliderL1f",
						"abdomen/_ColliderL1l",
						"abdomen/_ColliderL1r",
						"abdomen/_ColliderL2b"
					}, "", "abdomen2"));

				add(BP.Hips, GetRigidbody(
					BP.Hips, new string[] {
						"pelvisB3/pelvisB3Joint",
						"pelvisF5/pelvisF5Joint",
						"pelvisF8/pelvisF8Joint",
						"pelvisL1/pelvisL1Joint",
						"pelvisR1/pelvisR1Joint"
					}, "hipControl", new string[] { "abdomen", "pelvis" }));
			}
			else
			{
				add(BP.Chest, GetRigidbody(
					BP.Chest, new string[] { "chest1/chest1Joint" },
					"chestControl", "chest"));

				add(BP.Belly, GetRigidbody(
					BP.Belly, new string[] {
						"abdomen2_3/abdomen2_3Joint",
						"abdomen3/abdomen3Joint",
						"abdomen7/abdomen7Joint",
						"abdomen12/abdomen12Joint",
						"abdomen17/abdomen17Joint",
						"abdomen20/abdomen20Joint"
					}, "", "abdomen2"));

				add(BP.Hips, GetRigidbody(
					BP.Hips, new string[] {
					"pelvisF7/pelvisF7Joint",
					"pelvisFL8/pelvisFL8Joint",
					"pelvisFR8/pelvisFR8Joint",
					"pelvisL1/pelvisL1Joint",
					"pelvisR1/pelvisR1Joint"
					}, "hipControl", new string[] { "abdomen", "pelvis" }));
			}

			add(BP.LeftGlute, GetCollider(
				BP.LeftGlute, "", "LGlute", "LGlute1Joint", ""));

			add(BP.RightGlute, GetCollider(
				BP.RightGlute, "", "RGlute", "RGlute1Joint", ""));


			// left arm
			//
			add(BP.LeftShoulder, GetCollider(
				BP.LeftShoulder, "lArmControl", "lShldr", "lShldr"));

			add(BP.LeftArm, GetCollider(
				BP.LeftArm, "lElbowControl", "lForeArm",
				"StandardColliderslShldr/_Collider1"));

			add(BP.LeftForearm, GetCollider(
				BP.LeftForearm, "lElbowControl", "lHand",
				"lForeArm/_Collider2"));

			add(BP.LeftHand, GetRigidbody(
				BP.LeftHand, new string[]
				{
					"lHand/_Collider",           // near wrist
					"lHand/lCarpal1/Collider3",  // middle of the palm

					// finger tips
					"lHand/lCarpal1/lIndex1/lIndex2/lIndex3/Collider",
					"lHand/lCarpal1/lMid1/lMid2/lMid3/Collider",
					"lHand/lCarpal2/lPinky1/lPinky2/lPinky3/Collider",
					"lHand/lCarpal2/lRing1/lRing2/lRing3/Collider",
					"lThumb1/lThumb2/lThumb3/Collider",
				},
				"lHandControl", "lHand"));


			// right arm
			//
			add(BP.RightShoulder, GetCollider(
				BP.RightShoulder, "rArmControl", "rShldr", "rShldr"));

			add(BP.RightArm, GetCollider(
				BP.RightArm, "rElbowControl", "rForeArm",
				"StandardCollidersrShldr/_Collider1"));

			add(BP.RightForearm, GetCollider(
				BP.RightForearm, "rElbowControl", "rHand",
				"rForeArm/_Collider2"));

			add(BP.RightHand, GetRigidbody(
				BP.RightHand, new string[]
				{
					"rHand/_Collider",           // near wrist
					"rHand/rCarpal1/Collider3",  // middle of the palm

					// finger tips
					"rHand/rCarpal1/rIndex1/rIndex2/rIndex3/Collider",
					"rHand/rCarpal1/rMid1/rMid2/rMid3/Collider",
					"rHand/rCarpal2/rPinky1/rPinky2/rPinky3/Collider",
					"rHand/rCarpal2/rRing1/rRing2/rRing3/Collider",
					"rThumb1/rThumb2/rThumb3/Collider",
				},
				"rHandControl", "rHand"));


			// left leg
			//
			add(BP.LeftThigh, GetCollider(
				BP.LeftThigh, "lKneeControl", "lThigh",
				"lThigh12Joint", "StandardColliderslThigh/_Collider6"));

			add(BP.LeftShin, GetCollider(
				BP.LeftShin, "lKneeControl", "lShin",
				"lShin8Joint", "StandardColliderslShin/_Collider2"));

			add(BP.LeftFoot, GetRigidbody(
				BP.LeftFoot, new string[] { "lFoot/_Collider4" },
				"lFootControl", "lFoot"));


			// right leg
			//
			add(BP.RightThigh, GetCollider(
				BP.RightThigh, "rKneeControl", "rThigh",
				"rThigh12Joint", "StandardCollidersrThigh/_Collider6"));

			add(BP.RightShin, GetCollider(
				BP.RightShin, "rKneeControl", "rShin",
				"rShin8Joint", "StandardCollidersrShin/_Collider2"));

			add(BP.RightFoot, GetRigidbody(
				BP.RightFoot, new string[] { "rFoot/_Collider4" },
				"rFootControl", "rFoot"));


			// eyes
			//
			add(BP.Eyes, new EyesBodyPart(Atom));


			// male parts
			//
			if (Atom.IsMale)
			{
				add(BP.Penis, GetRigidbody(
					BP.Penis, new string[] { "Gen1Hard", "Gen3aHard" },
					"penisBaseControl", "", "Gen1"));
			}
			else
			{
				add(BP.Penis, strapon_);
			}


			var list = new List<IBodyPart>();

			for (int i = 0; i < BP.Count; ++i)
			{
				var p = map[i];

				if (p == null)
					list.Add(new NullBodyPart(Atom, i));
				else
					list.Add(p);
			}

			return list.ToArray();
		}

		public override Hand GetLeftHand()
		{
			var h = new Hand();
			h.bones = GetHandBones("l");
			h.fist = new VamMorph(Atom, "Left Fingers Fist");
			h.inOut = new VamMorph(Atom, "Left Fingers In-Out");

			return h;
		}

		public override Hand GetRightHand()
		{
			var h = new Hand();
			h.bones = GetHandBones("r");
			h.fist = new VamMorph(Atom, "Right Fingers Fist");
			h.inOut = new VamMorph(Atom, "Right Fingers In-Out");

			return h;
		}

		private IBone[][] GetHandBones(string s)
		{
			var bones = new IBone[5][];

			for (int i = 0; i < 5; ++i)
				bones[i] = new IBone[3];

			var hand = U.FindRigidbody(Atom.Atom, $"{s}Hand");

			bones[0][0] = FindFingerBone(hand, s, $"{s}Thumb1");
			bones[0][1] = FindFingerBone(hand, s, $"{s}Thumb1/{s}Thumb2");
			bones[0][2] = FindFingerBone(hand, s, $"{s}Thumb1/{s}Thumb2/{s}Thumb3");

			bones[1][0] = FindFingerBone(hand, s, $"{s}Carpal1/{s}Index1");
			bones[1][1] = FindFingerBone(hand, s, $"{s}Carpal1/{s}Index1/{s}Index2");
			bones[1][2] = FindFingerBone(hand, s, $"{s}Carpal1/{s}Index1/{s}Index2/{s}Index3");

			bones[2][0] = FindFingerBone(hand, s, $"{s}Carpal1/{s}Mid1");
			bones[2][1] = FindFingerBone(hand, s, $"{s}Carpal1/{s}Mid1/{s}Mid2");
			bones[2][2] = FindFingerBone(hand, s, $"{s}Carpal1/{s}Mid1/{s}Mid2/{s}Mid3");

			bones[3][0] = FindFingerBone(hand, s, $"{s}Carpal2/{s}Ring1");
			bones[3][1] = FindFingerBone(hand, s, $"{s}Carpal2/{s}Ring1/{s}Ring2");
			bones[3][2] = FindFingerBone(hand, s, $"{s}Carpal2/{s}Ring1/{s}Ring2/{s}Ring3");

			bones[4][0] = FindFingerBone(hand, s, $"{s}Carpal2/{s}Pinky1");
			bones[4][1] = FindFingerBone(hand, s, $"{s}Carpal2/{s}Pinky1/{s}Pinky2");
			bones[4][2] = FindFingerBone(hand, s, $"{s}Carpal2/{s}Pinky1/{s}Pinky2/{s}Pinky3");

			return bones;
		}

		private IBone FindFingerBone(Rigidbody hand, string s, string name)
		{
			if (hipBone_ == null)
			{
				foreach (var bb in Atom.Atom.GetComponentsInChildren<DAZBone>())
				{
					if (bb.name == "hip")
					{
						hipBone_ = bb;
						break;
					}
				}

				if (hipBone_ == null)
				{
					Log.Error($"can't find hip bone");
					return null;
				}
			}


			var id =
				$"abdomen/abdomen2/" +
				$"chest/{s}Collar/{s}Shldr/{s}ForeArm/{s}Hand/{name}";

			var t = hipBone_.transform.Find(id);
			if (t == null)
			{
				Log.Error($"no finger bone {id}");
				return null;
			}

			var b = t.GetComponent<DAZBone>();
			if (b == null)
			{
				Log.Error($"no DAZBone in {id}");
				return null;
			}

			return new VamBone(this, hand, b);
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

		private string MakeName(string nameFemale, string nameMale)
		{
			if (!Atom.IsMale)
				return nameFemale;

			if (nameMale == "")
				return "";
			else if (nameMale == "same")
				return nameFemale;
			else
				return nameMale;
		}

		private IBodyPart GetRigidbody(
			int id, string[] colliders, string controller, string[] names)
		{
			var rbs = new List<Rigidbody>();

			for (int i = 0; i < names.Length; ++i)
			{
				string name = names[i];
				if (name == "")
					return null;

				var rb = U.FindRigidbody(Atom.Atom, name);
				if (rb == null)
					Log.Error($"rb {name} not found");

				rbs.Add(rb);
			}

			FreeControllerV3 fc = null;
			if (controller != "")
			{
				fc = U.FindController(Atom.Atom, controller);
				if (fc == null)
					Log.Error($"rb {rbs[0].name} has no controller {controller}");
			}

			return new RigidbodyBodyPart(Atom, id, rbs.ToArray(), fc, colliders);
		}

		private IBodyPart GetRigidbody(
			int id, string[] colliders, string controller,
			string nameFemale, string nameMale = "same")
		{
			string name = MakeName(nameFemale, nameMale);
			if (name == "")
				return null;

			var rb = U.FindRigidbody(Atom.Atom, name);
			if (rb == null)
				Log.Error($"rb {name} not found");

			FreeControllerV3 fc = null;
			if (controller != "")
			{
				fc = U.FindController(Atom.Atom, controller);
				if (fc == null)
					Log.Error($"rb {name} has no controller {controller} ");
			}

			return new RigidbodyBodyPart(Atom, id, new Rigidbody[] { rb }, fc, colliders);
		}

		private IBodyPart GetTrigger(
			int id, string controller,
			string nameFemale, string nameMale = "same",
			string[] ignoreTransforms=null, string[] colliders = null)
		{
			string name = MakeName(nameFemale, nameMale);
			if (name == "")
				return null;

			var o = U.FindChildRecursive(Atom.Atom.transform, name);
			if (o == null)
			{
				Log.Error($"trigger {name} not found");
				return null;
			}

			var t = o.GetComponentInChildren<CollisionTriggerEventHandler>();
			if (t == null)
			{
				Log.Error($"trigger {name} has no event handler");
				return null;
			}

			if (t.thisRigidbody == null)
			{
				Log.Error($"trigger {name} has no rb");
				return null;
			}

			FreeControllerV3 fc = null;
			if (controller != "")
			{
				fc = U.FindController(Atom.Atom, controller);
				if (fc == null)
					Log.Error($"trigger {name} has no controller {controller}");
			}

			return new TriggerBodyPart(
				Atom, id, t, fc, t.thisRigidbody.transform,
				ignoreTransforms, colliders);
		}

		private IBodyPart GetCollider(
			int id, string controller, string closestRb,
			string nameFemale, string nameMale = "same")
		{
			string name = MakeName(nameFemale, nameMale);
			if (name == "")
				return null;

			var c = U.FindCollider(Atom.Atom, name);
			if (c == null)
			{
				Log.Error($"collider {name} not found");
				return null;
			}

			FreeControllerV3 fc = null;
			if (controller != "")
			{
				fc = U.FindController(Atom.Atom, controller);
				if (fc == null)
					Log.Error($"collider {name} has no controller {controller}");
			}

			Rigidbody rb = null;
			if (closestRb != "")
			{
				rb = U.FindRigidbody(Atom.Atom, closestRb);
				if (rb == null)
					Log.Error($"collider {name} has no rb {closestRb}");
			}

			return new ColliderBodyPart(Atom, id, c, fc, rb);
		}
	}
}
