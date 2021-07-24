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

		public VamBone(Rigidbody parent, DAZBone b)
		{
			parent_ = parent;
			bone_ = b;
			joint_ = b.GetComponent<ConfigurableJoint>();

			if (joint_ == null)
				Cue.LogError("bone has no configurable joint");
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
		public abstract float Scale { get; }
		public abstract float Sweat { get; set; }
		public abstract float Flush { get; set; }

		public abstract IBodyPart[] GetBodyParts();
		public abstract Hand GetLeftHand();
		public abstract Hand GetRightHand();

		public abstract int BodyPartForCollider(Collider c);
	}


	class VamBody : VamBasicBody
	{
		private VamAtom atom_;
		private FloatParameter scale_ = null;
		private FloatParameter gloss_ = null;
		private ColorParameter color_ = null;
		private Color initialColor_;
		private DAZBone hipBone_ = null;
		private IBodyPart[] parts_;
		private Dictionary<Collider, int> partMap_ = new Dictionary<Collider, int>();

		private float sweat_ = 0;
		private IEasing sweatEasing_ = new CubicInEasing();

		private float flush_ = 0;
		private IEasing flushEasing_ = new CubicInEasing();

		public VamBody(VamAtom a)
		{
			atom_ = a;

			scale_ = new FloatParameter(a, "rescaleObject", "scale");
			if (!scale_.Check(true))
				atom_.Log.Error("no scale parameter");

			gloss_ = new FloatParameter(a, "skin", "Gloss");
			if (!gloss_.Check(true))
				atom_.Log.Error("no skin gloss parameter");

			color_ = new ColorParameter(a, "skin", "Skin Color");
			if (!color_.Check(true))
				atom_.Log.Error("no skin color parameter");

			initialColor_ = color_.Value;
			parts_ = CreateBodyParts();
		}

		public override IBodyPart[] GetBodyParts()
		{
			return parts_;
		}

		public IBodyPart GetPart(int i)
		{
			return parts_[i];
		}

		public override int BodyPartForCollider(Collider c)
		{
			int p;
			if (partMap_.TryGetValue(c, out p))
				return p;

			var t = c.transform;

			while (t != null)
			{
				for (int i = 0; i < parts_.Length; ++i)
				{
					var vp = (VamBodyPart)parts_[i];
					if (vp != null && vp.Transform == t)
					{
						partMap_.Add(c, i);
						return i;
					}
				}

				t = t.parent;
				if (t.GetComponent<Atom>() != null)
					break;
			}

			return -1;
		}

		private IBodyPart[] CreateBodyParts()
		{
			var map = new Dictionary<int, IBodyPart>();

			Action<int, IBodyPart> add = (type, p) =>
			{
				map[type] = p;
			};

			add(BodyParts.Head, GetRigidbody(BodyParts.Head, "headControl", "head"));

			add(BodyParts.Lips, GetTrigger(BodyParts.Lips, "", "LipTrigger"));
			add(BodyParts.Mouth, GetTrigger(BodyParts.Mouth, "", "MouthTrigger"));
			add(BodyParts.LeftBreast, GetTrigger(BodyParts.LeftBreast, "lNippleControl", "lNippleTrigger", ""));
			add(BodyParts.RightBreast, GetTrigger(BodyParts.RightBreast, "rNippleControl", "rNippleTrigger", ""));

			add(BodyParts.Labia, GetTrigger(BodyParts.Labia, "", "LabiaTrigger", "", new string[] { "lThigh", "rThigh", "FemaleAutoCollidersabdomen" }));
			add(BodyParts.Vagina, GetTrigger(BodyParts.Vagina, "", "VaginaTrigger", "", new string[] { "lThigh", "rThigh", "FemaleAutoCollidersabdomen" }));
			add(BodyParts.DeepVagina, GetTrigger(BodyParts.DeepVagina, "", "DeepVaginaTrigger", "", new string[] { "lThigh", "rThigh" }));
			add(BodyParts.DeeperVagina, GetTrigger(BodyParts.DeeperVagina, "", "DeeperVaginaTrigger", "", new string[] { "lThigh", "rThigh" }));
			add(BodyParts.Anus, null);

			add(BodyParts.Chest, GetRigidbody(BodyParts.Chest, "chestControl", "chest"));
			add(BodyParts.Belly, GetRigidbody(BodyParts.Belly, "", "abdomen2"));
			add(BodyParts.Hips, GetRigidbody(BodyParts.Hips, "hipControl", "abdomen"));
			add(BodyParts.LeftGlute, GetCollider(BodyParts.LeftGlute, "", "LGlute1Joint", ""));
			add(BodyParts.RightGlute, GetCollider(BodyParts.RightGlute, "", "RGlute1Joint", ""));

			add(BodyParts.LeftShoulder, GetCollider(BodyParts.LeftShoulder, "lArmControl", "lShldr"));
			add(BodyParts.LeftArm, GetCollider(BodyParts.LeftArm, "lElbowControl", "StandardColliderslShldr/_Collider1"));
			add(BodyParts.LeftForearm, GetCollider(BodyParts.LeftForearm, "lElbowControl", "lForeArm/_Collider2"));
			add(BodyParts.LeftHand, GetRigidbody(BodyParts.LeftHand, "lHandControl", "lHand"));

			add(BodyParts.RightShoulder, GetCollider(BodyParts.RightShoulder, "rArmControl", "rShldr"));
			add(BodyParts.RightArm, GetCollider(BodyParts.RightArm, "rElbowControl", "StandardCollidersrShldr/_Collider1"));
			add(BodyParts.RightForearm, GetCollider(BodyParts.RightForearm, "rElbowControl", "rForeArm/_Collider2"));
			add(BodyParts.RightHand, GetRigidbody(BodyParts.RightHand, "rHandControl", "rHand"));

			add(BodyParts.LeftThigh, GetCollider(BodyParts.LeftThigh, "lKneeControl", "lThigh12Joint", "StandardColliderslThigh/_Collider6"));
			add(BodyParts.LeftShin, GetCollider(BodyParts.LeftShin, "lKneeControl", "lShin8Joint", "StandardColliderslShin/_Collider2"));
			add(BodyParts.LeftFoot, GetRigidbody(BodyParts.LeftFoot, "lFootControl", "lFoot"));

			add(BodyParts.RightThigh, GetCollider(BodyParts.RightThigh, "rKneeControl", "rThigh12Joint", "StandardCollidersrThigh/_Collider6"));
			add(BodyParts.RightShin, GetCollider(BodyParts.RightShin, "rKneeControl", "rShin8Joint", "StandardCollidersrShin/_Collider2"));
			add(BodyParts.RightFoot, GetRigidbody(BodyParts.RightFoot, "rFootControl", "rFoot"));

			add(BodyParts.Eyes, new EyesBodyPart(atom_));

			if (atom_.Sex == Sexes.Male)
				add(BodyParts.Genitals, GetRigidbody(BodyParts.Genitals, "penisBaseControl", "", "Gen1"));
			else
				add(BodyParts.Genitals, GetTrigger(BodyParts.Genitals, "", "LabiaTrigger", ""));

			if (atom_.Sex == Sexes.Male)
				add(BodyParts.Pectorals, GetRigidbody(BodyParts.Pectorals, "chestControl", "chest"));
			else
				add(BodyParts.Pectorals, null);


			var list = new List<IBodyPart>();

			for (int i = 0; i < BodyParts.Count; ++i)
				list.Add(map[i]);

			return list.ToArray();
		}

		public override Hand GetLeftHand()
		{
			var h = new Hand();
			h.bones = GetHandBones("l");
			h.fist = new VamMorph(atom_, "Left Fingers Fist");
			h.inOut = new VamMorph(atom_, "Left Fingers In-Out");

			return h;
		}

		public override Hand GetRightHand()
		{
			var h = new Hand();
			h.bones = GetHandBones("r");
			h.fist = new VamMorph(atom_, "Right Fingers Fist");
			h.inOut = new VamMorph(atom_, "Right Fingers In-Out");

			return h;
		}

		private IBone[][] GetHandBones(string s)
		{
			var bones = new IBone[5][];

			for (int i = 0; i < 5; ++i)
				bones[i] = new IBone[3];

			var hand = Cue.Instance.VamSys.FindRigidbody(atom_.Atom, $"{s}Hand");

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
				foreach (var bb in atom_.Atom.GetComponentsInChildren<DAZBone>())
				{
					if (bb.name == "hip")
					{
						hipBone_ = bb;
						break;
					}
				}

				if (hipBone_ == null)
				{
					Cue.LogError($"{atom_.ID} can't find hip bone");
					return null;
				}
			}


			var id =
				$"abdomen/abdomen2/" +
				$"chest/{s}Collar/{s}Shldr/{s}ForeArm/{s}Hand/{name}";

			var t = hipBone_.transform.Find(id);
			if (t == null)
			{
				Cue.LogError($"{atom_.ID}: no finger bone {id}");
				return null;
			}

			var b = t.GetComponent<DAZBone>();
			if (b == null)
			{
				Cue.LogError($"{atom_.ID}: no DAZBone in {id}");
				return null;
			}

			return new VamBone(hand, b);
		}

		public void OnPluginState(bool b)
		{
			if (!b)
				Reset();
		}

		public override float Scale
		{
			get { return scale_?.Value ?? 1; }
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
				LerpColor(Color.Red, flush_ * 0.35f);
			}
		}

		private void LerpColor(Color target, float f)
		{
			var p = color_.Parameter;
			if (p != null)
			{
				var c = Color.Lerp(initialColor_, target, flushEasing_.Magnitude(f));
				p.val = U.ToHSV(c);
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
			if (atom_.Sex == Sexes.Female)
				return nameFemale;

			if (nameMale == "")
				return "";
			else if (nameMale == "same")
				return nameFemale;
			else
				return nameMale;
		}

		private IBodyPart GetRigidbody(int id, string controller, string nameFemale, string nameMale = "same")
		{
			string name = MakeName(nameFemale, nameMale);
			if (name == "")
				return null;

			var rb = Cue.Instance.VamSys.FindRigidbody(atom_.Atom, name);
			if (rb == null)
				Cue.LogError($"rb {name} not found in {atom_.ID}");

			FreeControllerV3 fc = null;
			if (controller != "")
			{
				fc = Cue.Instance.VamSys.FindController(atom_.Atom, controller);
				if (fc == null)
					Cue.LogError($"rb {name} has no controller {controller} in {atom_.ID}");
			}

			return new RigidbodyBodyPart(atom_, id, rb, fc);
		}

		private IBodyPart GetTrigger(
			int id, string controller,
			string nameFemale, string nameMale = "same", string[] ignoreTransforms=null)
		{
			string name = MakeName(nameFemale, nameMale);
			if (name == "")
				return null;

			var o = Cue.Instance.VamSys.FindChildRecursive(atom_.Atom.transform, name);
			if (o == null)
			{
				Cue.LogError($"trigger {name} not found in {atom_.ID}");
				return null;
			}

			var t = o.GetComponentInChildren<CollisionTriggerEventHandler>();
			if (t == null)
			{
				Cue.LogError($"trigger {name} has no event handler in {atom_.ID}");
				return null;
			}

			if (t.thisRigidbody == null)
			{
				Cue.LogError($"trigger {name} has no rb in {atom_.ID}");
				return null;
			}

			FreeControllerV3 fc = null;
			if (controller != "")
			{
				fc = Cue.Instance.VamSys.FindController(atom_.Atom, controller);
				if (fc == null)
					Cue.LogError($"trigger {name} has no controller {controller} in {atom_.ID}");
			}

			return new TriggerBodyPart(atom_, id, t, fc, ignoreTransforms);
		}

		private IBodyPart GetCollider(int id, string controller, string nameFemale, string nameMale = "same")
		{
			string name = MakeName(nameFemale, nameMale);
			if (name == "")
				return null;

			var c = Cue.Instance.VamSys.FindCollider(atom_.Atom, name);
			if (c == null)
			{
				Cue.LogError($"collider {name} not found in {atom_.ID}");
				return null;
			}

			FreeControllerV3 fc = null;
			if (controller != "")
			{
				fc = Cue.Instance.VamSys.FindController(atom_.Atom, controller);
				if (fc == null)
					Cue.LogError($"collider {name} has no controller {controller} in {atom_.ID}");
			}

			return new ColliderBodyPart(atom_, id, c, fc);
		}
	}
}
