using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace Cue.Sys.Vam
{
	class VamBodyLoader
	{
		private VamAtom atom_;
		private IBodyPart[] parts_;
		private Hand leftHand_, rightHand_;
		private DAZBone hipBone_ = null;

		public VamBodyLoader(VamAtom atom)
		{
			atom_ = atom;
		}

		private Logger Log
		{
			get { return atom_.Log; }
		}

		public IBodyPart[] Parts
		{
			get { return parts_; }
		}

		public Hand LeftHand
		{
			get { return leftHand_; }
		}

		public Hand RightHand
		{
			get { return rightHand_; }
		}

		public void Load()
		{
			Load(atom_.IsMale, AdvancedColliders());
		}

		private bool AdvancedColliders()
		{
			var cs = atom_.Atom.GetComponentInChildren<DAZCharacterSelector>();
			if (cs == null)
			{
				Log.Error("no DAZCharacterSelector");
				return true;
			}

			return cs.useAdvancedColliders;
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
					list.Add(new NullBodyPart(atom_, i));
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
					return new EyesBodyPart(atom_);
				else
					throw new LoadFailed($"no internal type for {BP.ToString(bpType)}");
			}
			else if (type == "strapon")
			{
				return new StraponBodyPart(atom_);
			}
			else if (type == "none")
			{
				return new NullBodyPart(atom_, bpType);
			}
			else
			{
				throw new LoadFailed($"bad type '{o["type"].Value}'");
			}
		}

		private Hand LoadHand(JSONClass o)
		{
			var h = new Hand();

			h.fist = new VamMorph(atom_, J.ReqString(o, "fistMorph"));
			h.inOut = new VamMorph(atom_, J.ReqString(o, "fingersInOutMorph"));
			h.bones = LoadHandBones(o);

			return h;
		}

		private IBone[][] LoadHandBones(JSONClass o)
		{
			var bones = new IBone[5][];
			var hand = U.FindRigidbody(atom_.Atom, J.ReqString(o, "rigidbody"));

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

			return new VamBone(atom_.Body as VamBody, hand, b);
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

				var rb = U.FindRigidbody(atom_.Atom, name);
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
				fr = U.FindRigidbody(atom_.Atom, forceReceiver);
				if (fr == null)
					Log.Error($"rb for force '{forceReceiver}' not found");
			}

			FreeControllerV3 fc = null;
			if (controller != "")
			{
				fc = U.FindController(atom_.Atom, controller);
				if (fc == null)
					Log.Error($"rb {rbs[0].name} has no controller {controller}");
			}

			return new RigidbodyBodyPart(
				atom_, bodyPart, rbs.ToArray(), fc, colliders, fr);
		}

		private IBodyPart CreateTrigger(
			int bodyPart, string[] names, string controller, string[] colliders,
			string[] ignore, string forceReceiver)
		{
			string name = names[0];
			if (name == "")
				return null;

			var o = U.FindChildRecursive(atom_.Atom.transform, name);
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
				fc = U.FindController(atom_.Atom, controller);
				if (fc == null)
					Log.Error($"trigger {name} has no controller {controller}");
			}

			return new TriggerBodyPart(
				atom_, bodyPart, t, fc, t.thisRigidbody.transform,
				ignore, colliders);
		}

		private IBodyPart CreateCollider(
			int bodyPart, string[] names, string controller, string[] colliders,
			string[] ignore, string forceReceiver)
		{
			string name = names[0];
			if (name == "")
				return null;

			var c = U.FindCollider(atom_.Atom, name);
			if (c == null)
			{
				Log.Error($"collider {name} not found");
				return null;
			}

			FreeControllerV3 fc = null;
			if (controller != "")
			{
				fc = U.FindController(atom_.Atom, controller);
				if (fc == null)
					Log.Error($"collider {name} has no controller {controller}");
			}

			Rigidbody rb = null;
			if (forceReceiver != "")
			{
				rb = U.FindRigidbody(atom_.Atom, forceReceiver);
				if (rb == null)
					Log.Error($"collider {name} has no rb {forceReceiver}");
			}

			return new ColliderBodyPart(atom_, bodyPart, c, fc, rb);
		}
	}
}
