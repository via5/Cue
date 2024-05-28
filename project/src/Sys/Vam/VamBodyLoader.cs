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
		private string[] mouthColliders_ = null;
		private string[] tongueColliders_ = null;

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

		public string[] MouthColliders
		{
			get { return mouthColliders_; }
		}

		public string[] TongueColliders
		{
			get { return tongueColliders_; }
		}

		public void Load()
		{
			try
			{
				Load(atom_.IsMale);
			}
			catch (Exception e)
			{
				Log.Error(e.ToString());

				if (!atom_.AdvancedColliders)
					Log.Error(">>>>> Cue requires Advanced Colliders <<<<<");

				Cue.Instance.DisablePlugin();
			}
		}

		private void Load(bool male)
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

			var map = new Dictionary<BodyPartType, IBodyPart>();

			Action<BodyPartType, IBodyPart> add = (type, p) =>
			{
				map[type] = p;
			};

			foreach (JSONClass o in d["parts"].AsArray)
			{
				var bp = LoadPart(male, o, getVar);
				if (bp != null)
					map[bp.Type] = bp;
			}

			if (!map.ContainsKey(BP.Penis) || map[BP.Penis] == null)
				map[BP.Penis] = new StraponBodyPart(atom_);

			var list = new List<IBodyPart>();

			foreach (BodyPartType i in BodyPartType.Values)
			{
				IBodyPart p = null;
				if (!map.TryGetValue(i, out p))
					Log.Verbose($"missing part {BodyPartType.ToString(i)}");

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


			{
				var slist = new List<string>();
				foreach (JSONNode mouth in d["kiss"]["mouthColliders"].AsArray)
				{
					var s = mouth.Value;
					if (string.IsNullOrEmpty(s))
						continue;

					slist.Add(s);
				}

				mouthColliders_ = slist.ToArray();
			}

			{
				var slist = new List<string>();
				foreach (JSONNode mouth in d["kiss"]["tongueColliders"].AsArray)
				{
					var s = mouth.Value;
					if (string.IsNullOrEmpty(s))
						continue;

					slist.Add(s);
				}

				tongueColliders_ = slist.ToArray();
			}
		}

		struct PartSettings
		{
			public BodyPartType bodyPart;
			public List<string> names;
			public string controller;
			public List<string> colliders;
			public List<string> ignore;
			public List<string> triggers;
			public string forceReceiver;
			public string rigidbody;
			public string closestRigidbody;
			public string centerCollider;
			public string extremity;
			public bool optional;
		}

		private IBodyPart LoadPart(bool male, JSONClass o, Func<string, JSONNode> getVar)
		{
			if (o.HasKey("sex"))
			{
				if (o["sex"].Value == "male" && !male)
					return null;
				else if (o["sex"].Value == "female" && male)
					return null;
			}

			PartSettings ps;
			ps.names = new List<string>();
			ps.ignore = new List<string>();
			ps.colliders = new List<string>();
			ps.triggers = new List<string>();

			ps.bodyPart = BodyPartType.FromString(J.ReqString(o, "part"));
			if (ps.bodyPart == BP.None)
				throw new LoadFailed($"bad part '{o["part"].Value}'");

			if (o.HasKey("name"))
			{
				if (o["name"].Value != "")
				{
					ps.names.Add(o["name"].Value);
				}
				else if (o["name"].AsArray.Count > 0)
				{
					foreach (var n in o["name"].AsArray.Childs)
						ps.names.Add(n.Value);
				}
			}

			ps.controller = J.OptString(o, "controller");

			if (o.HasKey("colliders"))
			{
				foreach (var n in o["colliders"].AsArray.Childs)
					ps.colliders.Add(n.Value);
			}

			if (o.HasKey("triggers"))
			{
				foreach (var n in o["triggers"].AsArray.Childs)
					ps.triggers.Add(n.Value);
			}

			ps.extremity = J.OptString(o, "extremity");
			ps.optional = J.OptBool(o, "optional", false);

			if (o.HasKey("ignore"))
			{
				JSONNode parent;

				if (o["ignore"].Value != "" && o["ignore"].Value.StartsWith("$"))
					parent = getVar(o["ignore"].Value);
				else
					parent = o["ignore"];

				if (parent.Value != "")
				{
					ps.ignore.Add(parent.Value);
				}
				else if (parent.AsArray.Count > 0)
				{
					foreach (var n in parent.AsArray.Childs)
						ps.ignore.Add(n.Value);
				}
			}

			ps.forceReceiver = J.OptString(o, "forceReceiver");
			ps.rigidbody = J.OptString(o, "rigidbody");
			ps.closestRigidbody = J.OptString(o, "closestRigidbody");
			ps.centerCollider = J.OptString(o, "centerCollider");

			var type = J.ReqString(o, "type");

			Log.Info($"{ps.bodyPart}");

			if (type == "rigidbody")
			{
				return CreateRigidbody(ps);
			}
			else if (type == "trigger")
			{
				return CreateTrigger(ps);
			}
			else if (type == "collider")
			{
				return CreateCollider(ps);
			}
			else if (type == "internal")
			{
				if (ps.bodyPart == BP.Eyes)
					return new EyesBodyPart(atom_);
				else
					throw new LoadFailed($"no internal type for {BodyPartType.ToString(ps.bodyPart)}");
			}
			else if (type == "strapon")
			{
				return new StraponBodyPart(atom_);
			}
			else if (type == "none")
			{
				return new NullBodyPart(atom_, ps.bodyPart);
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

		private IBodyPart CreateRigidbody(PartSettings ps)
		{
			var rbs = new List<Rigidbody>();

			if (ps.optional)
			{
				bool ok = false;

				foreach (var cn in ps.colliders)
				{
					var c = atom_.FindCollider(cn);
					if (c != null)
					{
						ok = true;
						break;
					}
				}

				if (!ok)
				{
					Log.Verbose($"optional part {ps.bodyPart}: no colliders found");
					return null;
				}
			}


			for (int i = 0; i < ps.names.Count; ++i)
			{
				string name = ps.names[i];
				if (name == "")
					return null;

				var rb = U.FindRigidbody(atom_.Atom, name);
				if (rb == null)
					Log.Error($"rb {name} not found");

				rbs.Add(rb);
			}

			Rigidbody fr = null;
			if (ps.forceReceiver == "")
			{
				if (rbs.Count > 0)
					fr = rbs[0];
			}
			else
			{
				fr = U.FindRigidbody(atom_.Atom, ps.forceReceiver);
				if (fr == null)
					Log.Error($"rb for force '{ps.forceReceiver}' not found");
			}

			FreeControllerV3 fc = null;
			if (ps.controller != "")
			{
				fc = U.FindController(atom_.Atom, ps.controller);
				if (fc == null)
					Log.Error($"rb {rbs[0].name} has no controller {ps.controller}");
			}

			return new RigidbodyBodyPart(
				atom_, ps.bodyPart, rbs.ToArray(), fc,
				ps.colliders.ToArray(), fr, ps.ignore.ToArray(),
				ps.centerCollider, ps.extremity);
		}

		private IBodyPart CreateTrigger(PartSettings ps)
		{
			string name = ps.names[0];
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
			if (ps.controller != "")
			{
				fc = U.FindController(atom_.Atom, ps.controller);
				if (fc == null)
					Log.Error($"trigger {name} has no controller {ps.controller}");
			}

			return new TriggerBodyPart(
				atom_, ps.bodyPart, t, fc, t.thisRigidbody.transform,
				ps.ignore.ToArray(), ps.colliders.ToArray());
		}

		private IBodyPart CreateCollider(PartSettings ps)
		{
			var cs = new List<Collider>();
			foreach (var cn in ps.colliders)
			{
				var c = atom_.FindCollider(cn);
				if (c == null)
					Log.Error($"collider {cn} not found for {BodyPartType.ToString(ps.bodyPart)}");
				else
					cs.Add(c);
			}

			if (cs.Count == 0)
				throw new LoadFailed($"no colliders for {BodyPartType.ToString(ps.bodyPart)}");

			FreeControllerV3 fc = null;
			if (ps.controller != "")
			{
				fc = U.FindController(atom_.Atom, ps.controller);
				if (fc == null)
					Log.Error($"collider {BodyPartType.ToString(ps.bodyPart)} has no controller {ps.controller}");
			}

			Rigidbody rb = null;
			if (ps.rigidbody != "")
			{
				rb = U.FindRigidbody(atom_.Atom, ps.rigidbody);
				if (rb == null)
					Log.Error($"collider {BodyPartType.ToString(ps.bodyPart)} has no rb {ps.rigidbody}");
			}

			Rigidbody closestRb = null;
			if (ps.closestRigidbody != "")
			{
				closestRb = U.FindRigidbody(atom_.Atom, ps.closestRigidbody);
				if (closestRb == null)
					Log.Error($"collider {BodyPartType.ToString(ps.bodyPart)} has no rb {ps.closestRigidbody}");
			}

			return new ColliderBodyPart(
				atom_, ps.bodyPart, cs.ToArray(), fc, rb, closestRb,
				ps.ignore.ToArray(), ps.triggers.ToArray());
		}
	}
}
