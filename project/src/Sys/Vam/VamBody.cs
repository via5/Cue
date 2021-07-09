using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cue.Sys.Vam
{
	class VamHair : IHair
	{
		class HairItem
		{
			private HairSimControl c_;
			private JSONStorableFloat styleCling_;
			private JSONStorableFloat rigidityRolloff_;

			public HairItem(HairSimControl c)
			{
				c_ = c;

				styleCling_ = c_.GetFloatJSONParam("cling");
				if (styleCling_ == null)
					Cue.LogInfo("cling not found");

				rigidityRolloff_ = c_.GetFloatJSONParam("rigidityRolloffPower");
				if (rigidityRolloff_ == null)
					Cue.LogInfo("rigidityRolloffPower not found");
			}

			public void Reset()
			{
				if (styleCling_ != null)
					styleCling_.val = styleCling_.defaultVal;

				if (rigidityRolloff_ != null)
					rigidityRolloff_.val = rigidityRolloff_.defaultVal;
			}

			public void SetLoose(float f)
			{
				if (styleCling_ != null)
				{
					float min = 0.01f;
					float max = styleCling_.defaultVal;

					if (min < max)
					{
						float range = max - min;
						styleCling_.val = max - (range * f);
					}
				}

				if (rigidityRolloff_ != null)
				{
					float min = rigidityRolloff_.defaultVal;
					float max = rigidityRolloff_.max;

					if (min < max)
					{
						float range = max - min;
						rigidityRolloff_.val = min + (range * f);
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
			if (atom_ == null)
				return;

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
			get
			{
				return loose_;
			}

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


	abstract class VamBasicBody : IBody
	{
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
					float range = (p.max - def) * 0.7f;  // max is too much

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

		public override string ToString()
		{
			return $"{morph_}";
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


	abstract class VamBodyPart : IBodyPart
	{
		protected VamAtom atom_;
		private int type_;

		protected VamBodyPart(VamAtom a, int t)
		{
			atom_ = a;
			type_ = t;
		}

		public int Type { get { return type_; } }

		public virtual Transform Transform { get { return null; } }
		public virtual Rigidbody Rigidbody { get { return null; } }

		public virtual bool CanTrigger { get { return false; } }
		public virtual TriggerInfo[] GetTriggers() { return null; }

		public virtual bool CanGrab{ get { return false; } }
		public virtual bool Grabbed { get { return false; } }

		public abstract Vector3 ControlPosition { get; set; }
		public abstract Quaternion ControlRotation { get; set; }
		public abstract Vector3 Position { get; }
		public abstract Quaternion Rotation { get; }

		public virtual void AddRelativeForce(Vector3 v)
		{
			// no-op
		}

		public virtual void AddRelativeTorque(Vector3 v)
		{
			// no-op
		}
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
			get { return U.FromUnity(fc_.transform.position); }
			set { fc_.transform.position = U.ToUnity(value); }
		}

		public override Quaternion ControlRotation
		{
			get { return U.FromUnity(fc_.transform.rotation); }
			set { fc_.transform.rotation = U.ToUnity(value); }
		}

		public override Vector3 Position
		{
			get { return U.FromUnity(rb_.position); }
		}

		public override Quaternion Rotation
		{
			get { return U.FromUnity(rb_.rotation); }
		}

		public override void AddRelativeForce(Vector3 v)
		{
			rb_.AddRelativeForce(U.ToUnity(v));
		}

		public override void AddRelativeTorque(Vector3 v)
		{
			rb_.AddRelativeTorque(U.ToUnity(v));
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
			get { return U.FromUnity(c_.bounds.center); }
			set { Cue.LogError("cannot move colliders"); }
		}

		public override Quaternion ControlRotation
		{
			get { return U.FromUnity(c_.transform.rotation); }
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
		private const float TriggerCheckDelay = 1;

		private CollisionTriggerEventHandler h_;
		private Trigger trigger_;
		private Rigidbody rb_;
		private FreeControllerV3 fc_;
		private Transform ignoreStop_ = null;
		private Transform[] ignoreTransforms_ = new Transform[0];
		private TriggerInfo[] triggers_ = null;
		private float lastTriggerCheck_ = 0;

		public TriggerBodyPart(
			VamAtom a, int type, CollisionTriggerEventHandler h,
			FreeControllerV3 fc, string[] ignoreTransforms)
				: base(a, type)
		{
			h_ = h;
			trigger_ = h.collisionTrigger.trigger;
			rb_ = h.thisRigidbody;
			fc_ = fc;

			if (ignoreTransforms != null)
			{
				var rb = Cue.Instance.VamSys.FindRigidbody(a.Atom, "hip");
				if (rb == null)
					Cue.LogError($"{a.ID}: trigger {h.name}: no hip");
				else
					ignoreStop_ = rb.transform;

				var list = new List<Transform>();
				for (int i = 0; i < ignoreTransforms.Length; ++i)
				{
					rb = Cue.Instance.VamSys.FindRigidbody(a.Atom, ignoreTransforms[i]);
					if (rb != null)
					{
						list.Add(rb.transform);
					}
					else
					{
						var t = Cue.Instance.VamSys.FindChildRecursive(
							a.Atom, ignoreTransforms[i])?.transform;

						if (t != null)
							list.Add(t);
						else
							Cue.LogError($"{a.ID}: trigger {h.name}: no ignore {ignoreTransforms[i]}");
					}
				}

				if (list.Count > 0)
					ignoreTransforms_ = list.ToArray();
			}
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

		public override TriggerInfo[] GetTriggers()
		{
			if (Time.realtimeSinceStartup >= (lastTriggerCheck_ + TriggerCheckDelay))
			{
				lastTriggerCheck_ = Time.realtimeSinceStartup;
				UpdateTriggers();
			}

			return triggers_;
		}

		private void UpdateTriggers()
		{
			if (!trigger_.active)
			{
				triggers_ = null;
				return;
			}

			List<TriggerInfo> list = null;

			var found = new bool[Cue.Instance.AllPersons.Count, BodyParts.Count];
			List<string> foundOther = null;

			foreach (var kv in h_.collidingWithDictionary)
			{
				if (!kv.Value || kv.Key == null)
					continue;

				if (!ValidTrigger(kv.Key))
					continue;

				if (list == null)
					list = new List<TriggerInfo>();

				var p = PersonForCollider(kv.Key);
				if (p == null)
				{
					bool skip = false;

					if (foundOther == null)
						foundOther = new List<string>();
					else if (foundOther.Contains(kv.Key.name))
						skip = true;
					else
						foundOther.Add(kv.Key.name);

					if (!skip)
						list.Add(new TriggerInfo(-1, -1, 1.0f));
				}
				else
				{
					var bp = ((VamBasicBody)p.Atom.Body).BodyPartForCollider(kv.Key);

					if (bp == -1)
					{
						//Cue.LogError($"no body part for {kv.Key.name} in {p.ID}");
					}
					else if (!found[p.PersonIndex, bp])
					{
						found[p.PersonIndex, bp] = true;
						list.Add(new TriggerInfo(p.PersonIndex, bp, 1.0f));
					}
				}
			}

			if (list == null)
				triggers_ = null;
			else
				triggers_ = list.ToArray();
		}

		private Person PersonForCollider(Collider c)
		{
			var a = c.transform.GetComponentInParent<Atom>();
			if (a == null)
				return null;

			if (Cue.Instance.VamSys.IsVRHands(a))
			{
				foreach (var p in Cue.Instance.ActivePersons)
				{
					if (p.Atom == Cue.Instance.VamSys.CameraAtom)
						return p;
				}

				return null;
			}

			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (p.VamAtom?.Atom == a)
					return p;
			}

			return null;
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
					return U.FromUnity(rb_.position);
			}

			set { Cue.LogError("cannot move triggers"); }
		}

		public override Quaternion ControlRotation
		{
			get { return U.FromUnity(rb_.rotation); }
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

		private bool ValidTrigger(Collider c)
		{
			var t = c.transform;

			while (t != null)
			{
				if (t == ignoreStop_)
					break;

				for (int i = 0; i < ignoreTransforms_.Length; ++i)
				{
					if (ignoreTransforms_[i] == t)
						return false;
				}

				t = t.parent;
			}

			return true;
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

		public override bool CanGrab { get { return false; } }
		public override bool Grabbed { get { return false; } }

		public override Vector3 ControlPosition
		{
			get
			{
				if (atom_.Possessed)
					return Cue.Instance.Sys.CameraPosition;
				else if (lEye_ != null && rEye_ != null)
					return U.FromUnity((lEye_.position + rEye_.position) / 2);
				else if (head_ != null)
					return U.FromUnity(head_.transform.position) + new Vector3(0, 0.05f, 0);
				else
					return Vector3.Zero;
			}

			set { Cue.LogError("cannot move eyes"); }
		}

		public override Quaternion ControlRotation
		{
			get { return U.FromUnity(head_.rotation); }
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
