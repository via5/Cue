using System;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

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
		private DAZBone hipBone_ = null;
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
			Load();
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

		private bool AdvancedColliders()
		{
			var cs = Atom.Atom.GetComponentInChildren<DAZCharacterSelector>();
			if (cs == null)
			{
				Log.Error("no DAZCharacterSelector");
				return true;
			}

			return cs.useAdvancedColliders;
		}

		private void Load()
		{
			Load(Atom.IsMale, AdvancedColliders());
		}

		private void Load(bool male, bool advancedColliders)
		{
			var d = JSON.Parse(
				Cue.Instance.Sys.ReadFileIntoString(
					Cue.Instance.Sys.GetResourcePath("vambody.json")));

			var vars = new Dictionary<string, JSONNode>();
			foreach (var k in d["vars"].AsObject.Keys)
				vars[k] = d["vars"][k];

			Func<string, JSONNode> getVar = (string key) =>
			{
				if (key.StartsWith("$"))
					key = key.Substring(1);

				JSONNode n;
				if (vars.TryGetValue(key, out n))
					return n;

				throw new LoadFailed($"variable '{key}' not found");
			};

			var map = new Dictionary<int, IBodyPart>();

			Action<int, IBodyPart> add = (type, p) =>
			{
				map[type] = p;
			};

			foreach (JSONClass o in d["parts"].AsArray)
			{
				var bp = LoadPart(male, advancedColliders, o, getVar);
				if (bp != null)
					map[bp.Type] = bp;
			}

			var list = new List<IBodyPart>();

			for (int i = 0; i < BP.Count; ++i)
			{
				IBodyPart p = null;
				if (!map.TryGetValue(i, out p))
					Log.Verbose($"missing part {BP.ToString(i)}");

				if (p == null)
					list.Add(new NullBodyPart(Atom, i));
				else
					list.Add(p);
			}

			parts_ = list.ToArray();



			foreach (JSONClass o in d["hands"].AsArray)
			{
				if (!o.HasKey("type"))
					throw new LoadFailed("hand missing type");

				if (o["type"].Value == "left")
					leftHand_ = LoadHand(o);
				else if (o["type"].Value == "right")
					rightHand_ = LoadHand(o);
				else
					throw new LoadFailed($"bad hand type '{o["type"].Value}'");
			}

			if (leftHand_.bones == null)
				Log.Error("missing left hand");
			if (rightHand_.bones == null)
				Log.Error("missing right hand");
		}

		private IBodyPart LoadPart(bool male, bool advancedColliders, JSONClass o, Func<string, JSONNode> getVar)
		{
			if (o.HasKey("sex"))
			{
				if (o["sex"].Value == "male" && !male)
					return null;
				else if (o["sex"].Value == "female" && male)
					return null;
			}

			if (o.HasKey("advanced"))
			{
				if (o["advanced"].Value == "yes" && !advancedColliders)
					return null;
				else if (o["advanced"].Value == "no" && advancedColliders)
					return null;
			}

			var bpType = BP.FromString(J.ReqString(o, "part"));
			if (bpType == BP.None)
				throw new LoadFailed($"bad part '{o["part"].Value}'");

			var names = new List<string>();

			if (o.HasKey("name"))
			{
				if (o["name"].Value != "")
				{
					names.Add(o["name"].Value);
				}
				else if (o["name"].AsArray.Count > 0)
				{
					foreach (var n in o["name"].AsArray.Childs)
						names.Add(n.Value);
				}
			}

			string controller = J.OptString(o, "controller");

			var colliders = new List<string>();
			if (o.HasKey("colliders"))
			{
				foreach (var n in o["colliders"].AsArray.Childs)
					colliders.Add(n.Value);
			}

			var ignore = new List<string>();
			if (o.HasKey("ignore"))
			{
				JSONNode parent;

				if (o["ignore"].Value != "" && o["ignore"].Value.StartsWith("$"))
					parent = getVar(o["ignore"].Value);
				else
					parent = o["ignore"];

				if (parent.Value != "")
				{
					ignore.Add(parent.Value);
				}
				else if (parent.AsArray.Count > 0)
				{
					foreach (var n in parent.AsArray.Childs)
						ignore.Add(n.Value);
				}
			}

			string forceReceiver = J.OptString(o, "forceReceiver");
			if (forceReceiver == "")
				forceReceiver = J.OptString(o, "rigidbody");

			var type = J.ReqString(o, "type");

			if (type == "rigidbody")
			{
				return CreateRigidbody(
					bpType, names.ToArray(), controller, colliders.ToArray(),
					ignore.ToArray(), forceReceiver);
			}
			else if (type == "trigger")
			{
				return CreateTrigger(
					bpType, names.ToArray(), controller, colliders.ToArray(),
					ignore.ToArray(), forceReceiver);
			}
			else if (type == "collider")
			{
				return CreateCollider(
					bpType, names.ToArray(), controller, colliders.ToArray(),
					ignore.ToArray(), forceReceiver);
			}
			else if (type == "internal")
			{
				if (bpType == BP.Eyes)
					return new EyesBodyPart(Atom);
				else
					throw new LoadFailed($"no internal type for {BP.ToString(bpType)}");
			}
			else if (type == "strapon")
			{
				if (strapon_ != null)
					throw new LoadFailed($"can only have one strapon");

				strapon_ = new StraponBodyPart(Atom);
				return strapon_;
			}
			else if (type == "none")
			{
				return new NullBodyPart(Atom, bpType);
			}
			else
			{
				throw new LoadFailed($"bad type '{o["type"].Value}'");
			}
		}

		public override Hand GetLeftHand()
		{
			return leftHand_;
		}

		public override Hand GetRightHand()
		{
			return rightHand_;
		}

		private Hand LoadHand(JSONClass o)
		{
			var h = new Hand();

			h.fist = new VamMorph(Atom, J.ReqString(o, "fistMorph"));
			h.inOut = new VamMorph(Atom, J.ReqString(o, "fingersInOutMorph"));
			h.bones = LoadHandBones(o);

			return h;
		}

		private IBone[][] LoadHandBones(JSONClass o)
		{
			var bones = new IBone[5][];
			var hand = U.FindRigidbody(Atom.Atom, J.ReqString(o, "rigidbody"));

			if (!o.HasKey("bones"))
				throw new LoadFailed("hand missing bones");

			var bo = o["bones"].AsObject;

			bones[0] = LoadFingerBones(hand, o, "thumb");
			bones[1] = LoadFingerBones(hand, o, "index");
			bones[2] = LoadFingerBones(hand, o, "middle");
			bones[3] = LoadFingerBones(hand, o, "ring");
			bones[4] = LoadFingerBones(hand, o, "little");

			return bones;
		}

		private IBone[] LoadFingerBones(Rigidbody hand, JSONClass o, string key)
		{
			var bones = new IBone[3];

			var a = o["bones"][key].AsArray;
			if (a.Count != 3)
				throw new LoadFailed($"hand finger '{key}' not an array of 3");

			bones[0] = FindFingerBone(hand, a[0].Value);
			bones[1] = FindFingerBone(hand, a[1].Value);
			bones[2] = FindFingerBone(hand, a[2].Value);

			return bones;
		}

		private IBone FindFingerBone(Rigidbody hand, string name)
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


			var t = U.FindChildRecursive(hipBone_, name);
			if (t == null)
			{
				Log.Error($"no finger bone {name}");
				return null;
			}

			var b = t.GetComponent<DAZBone>();
			if (b == null)
			{
				Log.Error($"no DAZBone in {name}");
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

		private IBodyPart CreateRigidbody(
			int bodyPart, string[] names, string controller, string[] colliders,
			string[] ignore, string forceReceiver)
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

			Rigidbody fr = null;
			if (forceReceiver == "")
			{
				if (rbs.Count > 0)
					fr = rbs[0];
			}
			else
			{
				fr = U.FindRigidbody(Atom.Atom, forceReceiver);
				if (fr == null)
					Log.Error($"rb for force '{forceReceiver}' not found");
			}

			FreeControllerV3 fc = null;
			if (controller != "")
			{
				fc = U.FindController(Atom.Atom, controller);
				if (fc == null)
					Log.Error($"rb {rbs[0].name} has no controller {controller}");
			}

			return new RigidbodyBodyPart(
				Atom, bodyPart, rbs.ToArray(), fc, colliders, fr);
		}

		private IBodyPart CreateTrigger(
			int bodyPart, string[] names, string controller, string[] colliders,
			string[] ignore, string forceReceiver)
		{
			string name = names[0];
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
				Atom, bodyPart, t, fc, t.thisRigidbody.transform,
				ignore, colliders);
		}

		private IBodyPart CreateCollider(
			int bodyPart, string[] names, string controller, string[] colliders,
			string[] ignore, string forceReceiver)
		{
			string name = names[0];
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
			if (forceReceiver != "")
			{
				rb = U.FindRigidbody(Atom.Atom, forceReceiver);
				if (rb == null)
					Log.Error($"collider {name} has no rb {forceReceiver}");
			}

			return new ColliderBodyPart(Atom, bodyPart, c, fc, rb);
		}
	}
}
