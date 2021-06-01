﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cue.W
{
	class VamHair : IHair
	{
		class HairItem
		{
			private HairSimControl c_;
			private JSONStorableFloat styleCling_;

			public HairItem(HairSimControl c)
			{
				c_ = c;
				styleCling_ = c_.GetFloatJSONParam("cling");
				if (styleCling_ == null)
					Cue.LogInfo("cling not found");
			}

			public void Reset()
			{
				if (styleCling_ != null)
					styleCling_.val = styleCling_.defaultVal;
			}

			public void SetLoose(float f)
			{
				if (styleCling_ != null)
				{
					float min = 0.02f;
					float max = styleCling_.defaultVal;

					if (min < max)
					{
						float range = max - min;
						styleCling_.val = max - (range * f);
					}
				}
			}
		}


		private VamAtom atom_;
		private DAZCharacterSelector char_;
		private float loose_ = 0;
		private List<HairItem> list_ = new List<HairItem>();

		public VamHair(VamAtom a)
		{
			atom_ = a;
			char_ = atom_.Atom.GetComponentInChildren<DAZCharacterSelector>();
			if (char_ == null)
				atom_.Log.Error("no DAZCharacterSelector for hair");

			foreach (var g in char_.hairItems)
			{
				if (!g.isActiveAndEnabled)
					continue;

				var h = g.GetComponentInChildren<HairSimControl>();
				if (h != null)
					list_.Add(new HairItem(h));
			}
		}

		public void OnPluginState(bool b)
		{
			if (!b)
				Reset();
		}

		public float Loose
		{
			set
			{
				if (loose_ != value)
				{
					loose_ = value;
					for (int i = 0; i < list_.Count; ++i)
						list_[i].SetLoose(loose_);
				}
			}
		}

		public void Update(float s)
		{
		}

		private void Reset()
		{
			for (int i = 0; i < list_.Count; ++i)
				list_[i].Reset();
		}
	}


	class VamBody : IBody
	{
		private VamAtom atom_;
		private VamFloatParameter gloss_ = null;
		private VamColorParameter color_ = null;
		private Color initialColor_;
		private DAZBone hipBone_ = null;

		public VamBody(VamAtom a)
		{
			atom_ = a;

			gloss_ = new VamFloatParameter(a, "skin", "Gloss");
			if (!gloss_.Check(true))
				atom_.Log.Error("no skin gloss parameter");

			color_ = new VamColorParameter(a, "skin", "Skin Color");
			if (!color_.Check(true))
				atom_.Log.Error("no skin color parameter");

			initialColor_ = color_.Value;


			//var t = Cue.Instance.VamSys.FindChildRecursive(atom_.Atom, "lThumb3");
			//Cue.Instance.VamSys.DumpComponentsAndUp(t);

		}

		public IBodyPart[] GetBodyParts()
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
			add(BodyParts.Labia, GetTrigger(BodyParts.Labia, "", "LabiaTrigger", ""));

			add(BodyParts.Vagina, GetTrigger(BodyParts.Vagina, "", "VaginaTrigger", ""));

			add(BodyParts.DeepVagina, GetTrigger(BodyParts.DeepVagina, "", "DeepVaginaTrigger", ""));
			add(BodyParts.DeeperVagina, GetTrigger(BodyParts.DeeperVagina, "", "DeeperVaginaTrigger", ""));
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

		public Hand GetLeftHand()
		{
			var h = new Hand();
			h.bones = GetHandBones("l");
			h.fist = new VamMorph(atom_, "Left Fingers Fist");
			h.inOut = new VamMorph(atom_, "Left Fingers In-Out");

			return h;
		}

		public Hand GetRightHand()
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

		public float Sweat
		{
			set
			{
				var p = gloss_.Parameter;
				if (p != null)
				{
					float def = p.defaultVal;
					float range = p.max - def;

					p.val = def + value * range;
				}
			}
		}

		public void LerpColor(Color target, float f)
		{
			var p = color_.Parameter;
			if (p != null)
			{
				var c = Color.Lerp(initialColor_, target, f);
				p.val = VamU.ToHSV(c);
			}
		}

		private void Reset()
		{
			if (gloss_.Parameter != null)
				gloss_.Parameter.val = gloss_.Parameter.defaultVal;

			if (color_.Parameter != null)
				color_.Parameter.val = VamU.ToHSV(initialColor_);
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
			string nameFemale, string nameMale = "same", bool ignoreTrigger = false)
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

			return new TriggerBodyPart(atom_, id, t, fc, ignoreTrigger);
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


	static class VamMorphManager
	{
		public class MorphInfo
		{
			private string id_;
			private DAZMorph m_;
			private List<MorphInfo> subMorphs_ = new List<MorphInfo>();
			private bool free_ = true;
			private int freeFrame_ = -1;
			private float multiplier_ = 1;

			public MorphInfo(VamAtom atom, string morphId, DAZMorph m)
			{
				id_ = morphId;
				m_ = m;

				if (m_ != null && (m_.deltas == null || m_.deltas.Length == 0))
				{
					foreach (var sm in m_.formulas)
					{
						if (sm.targetType == DAZMorphFormulaTargetType.MorphValue)
						{
							var smm = Get(atom, sm.target, m_.morphBank);
							smm.multiplier_ = sm.multiplier;
							subMorphs_.Add(smm);
						}
						else
						{
							subMorphs_.Clear();
							break;
						}
					}

					if (subMorphs_.Count > 0)
						m_.Reset();
				}
			}

			public override string ToString()
			{
				string s = id_ + " ";

				if (m_ == null)
					s += "notfound";
				else
					s += $"v={m_.morphValue:0.00} sub={subMorphs_.Count != 0} f={free_} ff={freeFrame_}";

				return s;
			}

			public string ID
			{
				get { return id_; }
			}

			public float Value
			{
				get { return m_?.morphValue ?? -1; }
			}

			public float DefaultValue
			{
				get { return m_?.startValue ?? 0; }
			}

			public bool Set(float f)
			{
				if (m_ == null)
					return false;

				if (free_ || freeFrame_ != Cue.Instance.Frame)
				{
					if (subMorphs_.Count == 0)
					{
						if (f > m_.morphValue)
							m_.morphValue = Math.Min(m_.morphValue + 0.02f, f);
						else
							m_.morphValue = Math.Max(m_.morphValue - 0.02f, f);
					}
					else
					{
						for (int i = 0; i < subMorphs_.Count; ++i)
						{
							float smf = f * subMorphs_[i].multiplier_;
							subMorphs_[i].Set(smf);
						}
					}

					free_ = false;
					freeFrame_ = Cue.Instance.Frame;

					return true;
				}

				return false;
			}

			public void Reset()
			{
				if (m_ != null)
					m_.morphValue = m_.startValue;
			}
		}

		private static Dictionary<string, MorphInfo> map_ =
			new Dictionary<string, MorphInfo>();

		public static MorphInfo Get(VamAtom atom, string morphId, DAZMorphBank bank = null)
		{
			string key = atom.ID + "/" + morphId;

			MorphInfo mi;
			if (map_.TryGetValue(key, out mi))
				return mi;

			DAZMorph m;

			if (bank == null)
				m = Cue.Instance.VamSys.FindMorph(atom.Atom, morphId);
			else
				m = bank.GetMorph(morphId);

			if (m == null)
				Cue.LogError($"{atom.ID}: morph '{morphId}' not found");

			mi = new MorphInfo(atom, morphId, m);
			map_.Add(key, mi);

			return mi;
		}
	}


	class VamMorph : IMorph
	{
		private VamAtom atom_;
		private string name_;
		private VamMorphManager.MorphInfo morph_ = null;
		private bool inited_ = false;

		public VamMorph(VamAtom a, string name)
		{
			atom_ = a;
			name_ = name;
		}

		public string Name
		{
			get { return name_; }
		}

		public float Value
		{
			get
			{
				GetMorph();
				return morph_?.Value ?? 0;
			}

			set
			{
				GetMorph();
				if (morph_ != null)
					morph_.Set(value);
			}
		}

		public float DefaultValue
		{
			get
			{
				GetMorph();
				return morph_?.DefaultValue ?? 0;
			}
		}

		public void Reset()
		{
			GetMorph();
			morph_?.Reset();
		}

		private void GetMorph()
		{
			if (inited_)
				return;

			morph_ = VamMorphManager.Get(atom_, name_);
			if (morph_ == null)
				atom_.Log.Error($"no morph '{name_}'");

			inited_ = true;
		}
	}


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
			get { return VamU.FromUnity(bone_.transform.position); }
		}

		public Quaternion Rotation
		{
			get
			{
				var rb = bone_.GetComponent<Rigidbody>();
				return VamU.FromUnity(bone_.transform.rotation);
			}
		}
	}


	abstract class VamBodyPart : IBodyPart
	{
		protected VamAtom atom_;
		private int type_;

		protected VamBodyPart(VamAtom a, int t)
		{
			atom_ = a;
			type_ = t;
		}

		public int Type
		{
			get { return type_; }
		}

		public abstract Transform Transform { get; }
		public abstract Rigidbody Rigidbody { get; }

		public abstract bool CanTrigger { get; }
		public abstract float Trigger { get; }
		public abstract bool CanGrab { get; }
		public abstract bool Grabbed { get; }
		public abstract Vector3 ControlPosition { get; set; }
		public abstract Quaternion ControlRotation { get; set; }
		public abstract Vector3 Position { get; }
		public abstract Quaternion Rotation { get; }
	}


	class RigidbodyBodyPart : VamBodyPart
	{
		private Rigidbody rb_;
		private FreeControllerV3 fc_ = null;

		public RigidbodyBodyPart(VamAtom a, int type, Rigidbody rb, FreeControllerV3 fc)
			: base(a, type)
		{
			rb_ = rb;
			fc_ = fc;
		}

		public override Transform Transform
		{
			get { return rb_.transform; }
		}

		public override Rigidbody Rigidbody
		{
			get { return rb_; }
		}

		public override bool CanTrigger
		{
			get { return false; }
		}

		public override float Trigger
		{
			get { return 0; }
		}

		public override bool CanGrab
		{
			get { return (fc_ != null); }
		}

		public override bool Grabbed
		{
			get { return fc_?.isGrabbing ?? false; }
		}

		public override Vector3 ControlPosition
		{
			get { return W.VamU.FromUnity(fc_.transform.position); }
			set { fc_.transform.position = VamU.ToUnity(value); }
		}

		public override Quaternion ControlRotation
		{
			get { return W.VamU.FromUnity(fc_.transform.rotation); }
			set { fc_.transform.rotation = VamU.ToUnity(value); }
		}

		public override Vector3 Position
		{
			get { return W.VamU.FromUnity(rb_.position); }
		}

		public override Quaternion Rotation
		{
			get { return W.VamU.FromUnity(rb_.rotation); }
		}

		public override string ToString()
		{
			return $"rb {rb_.name}";
		}
	}


	class ColliderBodyPart : VamBodyPart
	{
		private Collider c_;
		private FreeControllerV3 fc_;

		public ColliderBodyPart(VamAtom a, int type, Collider c, FreeControllerV3 fc)
			: base(a, type)
		{
			c_ = c;
			fc_ = fc;
		}

		public override Transform Transform
		{
			get { return c_.transform; }
		}

		public override Rigidbody Rigidbody
		{
			get { return null; }
		}

		public override bool CanTrigger
		{
			get { return false; }
		}

		public override float Trigger
		{
			get { return 0; }
		}

		public override bool CanGrab
		{
			get { return (fc_ != null); }
		}

		public override bool Grabbed
		{
			get { return (fc_?.isGrabbing ?? false); }
		}

		public override Vector3 ControlPosition
		{
			get { return W.VamU.FromUnity(c_.bounds.center); }
			set { Cue.LogError("cannot move colliders"); }
		}

		public override Quaternion ControlRotation
		{
			get { return W.VamU.FromUnity(c_.transform.rotation); }
			set { Cue.LogError("cannot rotate colliders"); }
		}

		public override Vector3 Position
		{
			get { return ControlPosition; }
		}

		public override Quaternion Rotation
		{
			get { return ControlRotation; }
		}

		public override string ToString()
		{
			return $"collider {c_.name}";
		}
	}


	class TriggerBodyPart : VamBodyPart
	{
		private CollisionTriggerEventHandler h_;
		private Trigger trigger_;
		private Rigidbody rb_;
		private FreeControllerV3 fc_;
		private bool ignoreTrigger_;

		public TriggerBodyPart(
			VamAtom a, int type, CollisionTriggerEventHandler h,
			FreeControllerV3 fc, bool ignoreTrigger = false)
				: base(a, type)
		{
			h_ = h;
			trigger_ = h.collisionTrigger.trigger;
			rb_ = h.thisRigidbody;
			fc_ = fc;
			ignoreTrigger_ = ignoreTrigger;
		}

		public override Transform Transform
		{
			get { return rb_.transform; }
		}

		public override Rigidbody Rigidbody
		{
			get { return rb_; }
		}

		public override bool CanTrigger
		{
			get { return true; }
		}

		public override float Trigger
		{
			get
			{
				if (ignoreTrigger_)
					return 0;
				else
					return trigger_.active ? 1 : 0;
			}
		}

		public override bool CanGrab
		{
			get { return (fc_ != null); }
		}

		public override bool Grabbed
		{
			get { return fc_?.isGrabbing ?? false; }
		}

		public override Vector3 ControlPosition
		{
			get
			{
				if (rb_ == null)
					return Vector3.Zero;
				else
					return W.VamU.FromUnity(rb_.position);
			}

			set { Cue.LogError("cannot move triggers"); }
		}

		public override Quaternion ControlRotation
		{
			get { return W.VamU.FromUnity(rb_.rotation); }
			set { Cue.LogError("cannot rotate triggers"); }
		}

		public override Vector3 Position
		{
			get { return ControlPosition; }
		}

		public override Quaternion Rotation
		{
			get { return ControlRotation; }
		}

		public override string ToString()
		{
			return $"trigger {trigger_.displayName}";
		}
	}


	class EyesBodyPart : VamBodyPart
	{
		private Transform lEye_ = null;
		private Transform rEye_ = null;
		private Rigidbody head_;

		public EyesBodyPart(VamAtom a)
			: base(a, BodyParts.Eyes)
		{
			foreach (var t in a.Atom.GetComponentsInChildren<DAZBone>())
			{
				if (t.name == "lEye")
					lEye_ = t.transform;
				else if (t.name == "rEye")
					rEye_ = t.transform;

				if (lEye_ != null && rEye_ != null)
					break;
			}

			if (lEye_ == null)
				Cue.LogError($"{a.ID} has no left eye");

			if (rEye_ == null)
				Cue.LogError($"{a.ID} has no right eye");

			head_ = Cue.Instance.VamSys.FindRigidbody(atom_.Atom, "head");
			if (head_ == null)
				Cue.LogError($"{a.ID} has no head");
		}

		public override Transform Transform
		{
			get { return lEye_; }
		}

		public override Rigidbody Rigidbody
		{
			get { return head_; }
		}

		public override bool CanTrigger { get { return false; } }
		public override float Trigger { get { return 0; } }
		public override bool CanGrab { get { return false; } }
		public override bool Grabbed { get { return false; } }

		public override Vector3 ControlPosition
		{
			get
			{
				if (atom_.Possessed)
					return Cue.Instance.Sys.Camera;
				else if (lEye_ != null && rEye_ != null)
					return VamU.FromUnity((lEye_.position + rEye_.position) / 2);
				else if (head_ != null)
					return VamU.FromUnity(head_.transform.position) + new Vector3(0, 0.05f, 0);
				else
					return Vector3.Zero;
			}

			set { Cue.LogError("cannot move eyes"); }
		}

		public override Quaternion ControlRotation
		{
			get { return W.VamU.FromUnity(head_.rotation); }
			set { Cue.LogError("cannot rotate eyes"); }
		}

		public override Vector3 Position
		{
			get { return ControlPosition; }
		}

		public override Quaternion Rotation
		{
			get { return ControlRotation; }
		}

		public override string ToString()
		{
			return $"eyes {Position}";
		}
	}

}
