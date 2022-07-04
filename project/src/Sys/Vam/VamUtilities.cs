using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cue.Sys.Vam
{
	class U : global::Cue.U
	{
		public static void ForEachChildRecursive(Component c, Action<Transform> f)
		{
			ForEachChildRecursive(c.transform, f);
		}

		public static void ForEachChildRecursive(GameObject o, Action<Transform> f)
		{
			ForEachChildRecursive(o.transform, f);
		}

		public static void ForEachChildRecursive(Transform t, Action<Transform> f)
		{
			f(t);

			foreach (Transform c in t)
				ForEachChildRecursive(c, f);
		}

		public static Transform FindChildRecursive(Component c, string name)
		{
			return FindChildRecursive(c.transform, name);
		}

		public static Transform FindChildRecursive(GameObject o, string name)
		{
			return FindChildRecursive(o.transform, name);
		}

		public static Transform FindChildRecursive(Transform t, string name)
		{
			if (t == null)
				return null;

			if (t.name == name)
				return t;

			var cs = name.Split('/');

			Transform r = FindChildRecursiveImpl(t, cs[0]);
			if (r == null)
				return null;

			for (int i = 1; i < cs.Length; ++i)
			{
				r = r.Find(cs[i]);
				if (r == null)
					return null;
			}

			return r;
		}

		private static Transform FindChildRecursiveImpl(Transform t, string name)
		{
			if (t == null)
				return null;

			if (t.name == name)
				return t;

			foreach (Transform c in t)
			{
				var r = FindChildRecursiveImpl(c, name);
				if (r != null)
					return r;
			}

			return null;
		}

		public static Rigidbody FindRigidbody(IObject o, string name)
		{
			return FindRigidbody((o?.Atom as VamAtom)?.Atom, name);
		}

		public static Rigidbody FindRigidbody(Atom a, string name)
		{
			if (a == null)
				return null;

			foreach (var rb in a.rigidbodies)
			{
				if (rb.name == name)
					return rb.GetComponent<Rigidbody>();
			}

			return null;
		}

		public static FreeControllerV3 FindController(Atom a, string name)
		{
			for (int i = 0; i < a.freeControllers.Length; ++i)
			{
				if (a.freeControllers[i].name == name)
					return a.freeControllers[i];
			}

			return null;
		}

		public static DAZMorph FindMorph(
			Atom atom, GenerateDAZMorphsControlUI mui, DAZMorph m)
		{
			var nm = mui.GetMorphByUid(m.uid);
			if (nm != null)
				return nm;

			return mui.GetMorphByDisplayName(m.displayName);
		}

		public static DAZMorph FindMorph(Atom atom, string morphUID)
		{
			var mui = GetMUI(atom);
			if (mui == null)
				return null;

			var m = mui.GetMorphByUid(morphUID);
			if (m != null)
				return m;

			// try normalized, will convert .latest to .version for packaged
			// morphs
			string normalized = SuperController.singleton.NormalizeLoadPath(morphUID);
			m = mui.GetMorphByUid(normalized);
			if (m != null)
				return m;

			// try display name
			m = mui.GetMorphByDisplayName(morphUID);
			if (m != null)
				return m;

			if (mui.morphBank1 != null)
			{
				m = mui.morphBank1.GetMorph(morphUID);
				if (m != null)
					return m;
			}

			if (mui.morphBank2 != null)
			{
				m = mui.morphBank2.GetMorph(morphUID);
				if (m != null)
					return m;
			}

			if (mui.morphBank3 != null)
			{
				m = mui.morphBank3.GetMorph(morphUID);
				if (m != null)
					return m;
			}

			return null;
		}


		public static Collider FindCollider(List<Collider> cs, string pathstring)
		{
			var path = pathstring.Split('/');

			foreach (var c in cs)
			{
				var t = FindTransform(c.transform, pathstring, path);
				if (t != null)
				{
					var cc = t.GetComponent<Collider>();
					if (cc == null)
						Cue.LogError($"FindCollider: found {pathstring}, but not a collider");

					return cc;
				}
			}

			return null;
		}

		private static Transform FindTransform(Transform c, string pathstring, string[] path)
		{
			var p = path[path.Length - 1];

			if (EquivalentName(c.name, p))
			{
				if (path.Length == 1)
					return c;

				var check = c.transform.parent;
				if (check == null)
				{
					Cue.LogInfo("parent is not a collider");
					return null;
				}

				bool okay = true;

				for (int i = 1; i < path.Length; ++i)
				{
					var thispath = path[path.Length - i - 1];

					if (!EquivalentName(check.name, thispath))
					{
						okay = false;
						break;
					}

					check = check.parent;
					if (check == null)
					{
						Cue.LogInfo("parent is not a collider");
						okay = false;
						break;
					}
				}

				if (okay)
					return c;
			}


			if (EquivalentName(c.name, pathstring))
				return c;

			return null;
		}

		private static string[] NameFormat = new string[]
		{
			"AutoColliderFemaleAutoColliders{0}Joint",
			"AutoColliderFemaleAutoColliders{0}",
			"AutoColliderMaleAutoColliders{0}",
			"AutoColliderAutoColliders{0}Hard",
			"AutoColliderAutoColliders{0}",
			"AutoColliders{0}HardJoint",
			"AutoColliders{0}Hard",
			"AutoColliders{0}Joint",
			"AutoColliders{0}",
			"AutoCollider{0}Joint",
			"AutoCollider{0}",
			"FemaleAutoColliders{0}",
			"MaleAutoColliders{0}",
			"StandardColliders{0}",
			"{0}StandardColliders",
			"{0}HardJoint",
			"{0}Joint",
			"_Collider{0}",
			"_{0}"
		};

		private static bool EquivalentName(string cn, string pathstring)
		{
			return DoEquivalentName(cn, pathstring);
		}

		private static bool DoEquivalentName(string cn, string pathstring)
		{
			if (cn == pathstring)
				return true;

			for (int i = 0; i < NameFormat.Length; ++i)
			{
				if (cn == string.Format(NameFormat[i], pathstring))
					return true;
			}

			return false;
		}

		private static Transform sceneAtoms_ = null;
		private static Dictionary<Collider, VamAtom> atomCache_ = new Dictionary<Collider, VamAtom>();

		public static VamAtom AtomForCollider(Collider c)
		{
			VamAtom va;
			if (atomCache_.TryGetValue(c, out va))
				return va;

			Atom a;

			{
				a = AtomForColliderFast(c);
				if (a != null)
				{
					va = new VamAtom(a);
					atomCache_.Add(c, va);
					return va;
				}
			}

			var p = c.transform;

			while (p != null)
			{
				a = p.GetComponent<Atom>();
				if (a != null)
				{
					va = new VamAtom(a);
					atomCache_.Add(c, va);
					return va;
				}

				p = p.parent;
			}

			return null;
		}

		private static Atom AtomForColliderFast(Collider c)
		{
			if (sceneAtoms_ == null)
				sceneAtoms_ = c.transform.root;

			Transform last = c.transform;
			Transform t = c.transform.parent;

			// atoms are always direct children of the scene root, get parent
			// until the root is found and return the previous one
			while (t != sceneAtoms_ && t != null)
			{
				last = t;
				t = t.parent;
			}

			return last.GetComponent<Atom>();
		}

		public static void DumpComponents(Transform t, int indent = 0)
		{
			DumpComponents(t.gameObject, indent);
		}

		public static void DumpComponents(GameObject o, int indent = 0)
		{
			foreach (var c in o.GetComponents(typeof(Component)))
			{
				string s = "";

				var t = c as UnityEngine.UI.Text;
				if (t != null)
				{
					s += " (\"";
					if (t.text.Length > 20)
						s += t.text.Substring(0, 20) + "[...]";
					else
						s += t.text;
					s += "\")";
				}

				Cue.LogInfo(new string(' ', indent * 2) + c.ToString() + s);
			}
		}

		public static void DumpComponentsAndUp(Component c)
		{
			DumpComponentsAndUp(c.gameObject);
		}

		public static void DumpComponentsAndUp(GameObject o)
		{
			Cue.LogInfo($"{o.name} {(o.activeInHierarchy ? "on" : "off")}");

			var rt = o.GetComponent<RectTransform>();
			if (rt != null)
			{
				Cue.LogInfo("  rect: " + rt.rect.ToString());
				Cue.LogInfo("  offsetMin: " + rt.offsetMin.ToString());
				Cue.LogInfo("  offsetMax: " + rt.offsetMax.ToString());
				Cue.LogInfo("  anchorMin: " + rt.anchorMin.ToString());
				Cue.LogInfo("  anchorMax: " + rt.anchorMax.ToString());
				Cue.LogInfo("  anchorPos: " + rt.anchoredPosition.ToString());
			}

			DumpComponents(o);
			Cue.LogInfo("---");

			var parent = o?.transform?.parent?.gameObject;
			if (parent != null)
				DumpComponentsAndUp(parent);
		}

		public static void DumpComponentsAndDown(IObject o, bool dumpRt = false)
		{
			DumpComponentsAndDown((o.Atom as VamAtom).Atom.transform, dumpRt);
		}

		public static void DumpComponentsAndDown(Component c, bool dumpRt = false)
		{
			DumpComponentsAndDown(c.gameObject, dumpRt);
		}

		public static void DumpComponentsAndDown(
			GameObject o, bool dumpRt = false, int indent = 0)
		{
			Cue.LogInfo(new string(' ', indent * 2) + $"{o.name} {(o.activeInHierarchy ? "on" : "off")}");

			if (dumpRt)
			{
				var rt = o.GetComponent<RectTransform>();
				if (rt != null)
				{
					Cue.LogInfo(new string(' ', indent * 2) + "->rect: " + rt.rect.ToString());
					Cue.LogInfo(new string(' ', indent * 2) + "->offsetMin: " + rt.offsetMin.ToString());
					Cue.LogInfo(new string(' ', indent * 2) + "->offsetMax: " + rt.offsetMax.ToString());
					Cue.LogInfo(new string(' ', indent * 2) + "->anchorMin: " + rt.anchorMin.ToString());
					Cue.LogInfo(new string(' ', indent * 2) + "->anchorMax: " + rt.anchorMax.ToString());
					Cue.LogInfo(new string(' ', indent * 2) + "->anchorPos: " + rt.anchoredPosition.ToString());
				}
			}

			DumpComponents(o, indent);

			foreach (Transform c in o.transform)
				DumpComponentsAndDown(c.gameObject, dumpRt, indent + 1);
		}

		public static GenerateDAZMorphsControlUI GetMUI(Atom atom)
		{
			if (atom == null)
				return null;

			var cs = atom.GetComponentInChildren<DAZCharacterSelector>();
			if (cs == null)
				return null;

			return cs.morphsControlUI;
		}

		public static string FullName(Transform t)
		{
			string s = "";

			while (t != null)
			{
				if (s != "")
					s = "." + s;

				s = t.name + s;
				t = t.parent;
			}

			return s;
		}

		public static string FullName(UnityEngine.Object o)
		{
			if (o is Component)
				return FullName(((Component)o).transform);
			else if (o is GameObject)
				return FullName(((GameObject)o).transform);
			else
				return o.ToString();
		}

		public static string QualifiedName(UnityEngine.Component o)
		{
			var t = o?.transform;
			if (t == null)
				return "null";

			string s = CleanedUp(t.name);

			string ps = CleanedUp(t.parent.name);
			if (ps == "")
				s = CleanedUp(t.parent.parent.name) + "/" + t.parent.name + "/" + s;
			else
				s = ps + "/" + s;

			return s;
		}

		private static string CleanedUp(string name)
		{
			if (string.IsNullOrEmpty(name))
				return "?";

			for (int i = 0; i < NameFormat.Length; ++i)
			{
				var p = NameFormat[i].IndexOf("{0}");
				if (p == -1)
					continue;

				string prefix = "";
				string suffix = "";

				if (p > 0)
					prefix = NameFormat[i].Substring(0, p);

				if (p < (NameFormat[i].Length - 3))
					suffix = NameFormat[i].Substring(p + 3);

				if (name.StartsWith(prefix))
					name = name.Substring(prefix.Length);

				if (name.EndsWith(suffix))
					name = name.Substring(0, name.Length - suffix.Length);
			}

			return name;
		}


		public static string ToString(RectTransform rt)
		{
			return
				$"offsetMax {rt.offsetMax}\n" +
				$"offsetMin {rt.offsetMin}\n" +
				$"pivot {rt.pivot}\n" +
				$"sizeDelta {rt.sizeDelta}\n" +
				$"anchorPos {rt.anchoredPosition}\n" +
				$"anchorMin {rt.anchorMin}\n" +
				$"anchorMax {rt.anchorMax}\n" +
				$"anchorPos3D {rt.anchoredPosition3D}\n" +
				$"rect {rt.rect}";

		}

		public static UnityEngine.Vector3 ToUnity(Vector3 v)
		{
			return new UnityEngine.Vector3(v.X, v.Y, v.Z);
		}

		public static UnityEngine.Quaternion ToUnity(Quaternion v)
		{
			return v.Internal;
		}

		public static UnityEngine.Vector2 ToUnity(Size s)
		{
			return new UnityEngine.Vector2(s.Width, s.Height);
		}

		public static UnityEngine.Vector2 ToUnity(Point p)
		{
			return new UnityEngine.Vector2(p.X, p.Y);
		}

		public static UnityEngine.Color ToUnity(Color c)
		{
			return new UnityEngine.Color(c.r, c.g, c.b, c.a);
		}

		public static UnityEngine.Bounds ToUnity(Box b)
		{
			return new UnityEngine.Bounds(
				ToUnity(b.center), ToUnity(b.size));
		}

		public static UnityEngine.Plane ToUnity(Plane p)
		{
			return p.p_;
		}


		public static Vector3 FromUnity(UnityEngine.Vector3 v)
		{
			return new Vector3(v.x, v.y, v.z);
		}

		public static Quaternion FromUnity(UnityEngine.Quaternion q)
		{
			return Quaternion.FromInternal(q);
		}

		public static Color FromUnity(UnityEngine.Color c)
		{
			return new Color(c.r, c.g, c.b, c.a);
		}

		public static Box FromUnity(UnityEngine.Bounds b)
		{
			return new Box(FromUnity(b.center), FromUnity(b.size));
		}


		public static float Distance(Vector3 a, Vector3 b)
		{
			return UnityEngine.Vector3.Distance(ToUnity(a), ToUnity(b));
		}

		public static float Angle(Vector3 a, Vector3 b)
		{
			return UnityEngine.Quaternion.LookRotation(ToUnity(b - a)).eulerAngles.y;
		}

		public static Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDistance)
		{
			return FromUnity(UnityEngine.Vector3.MoveTowards(
				ToUnity(current), ToUnity(target), maxDistance));
		}

		public static Vector3 Rotate(float x, float y, float z)
		{
			return FromUnity(
				UnityEngine.Quaternion.Euler(x, y, z) *
				UnityEngine.Vector3.forward);
		}

		public static Vector3 Rotate(Vector3 v, float bearing)
		{
			return FromUnity(UnityEngine.Quaternion.Euler(0, bearing, 0) * ToUnity(v));
		}

		public static Vector3 Rotate(Vector3 v, Vector3 dir)
		{
			return FromUnity(
				UnityEngine.Quaternion.LookRotation(ToUnity(dir)) * ToUnity(v));
		}

		public static Vector3 RotateEuler(Vector3 v, Vector3 angles)
		{
			return FromUnity(
				UnityEngine.Quaternion.Euler(ToUnity(angles)) * ToUnity(v));
		}

		public static Vector3 RotateInv(Vector3 v, Vector3 dir)
		{
			var q = UnityEngine.Quaternion.LookRotation(ToUnity(dir));
			return FromUnity(UnityEngine.Quaternion.Inverse(q) * ToUnity(v));
		}

		public static Vector3 Lerp(Vector3 a, Vector3 b, float p)
		{
			return FromUnity(
				UnityEngine.Vector3.Lerp(ToUnity(a), ToUnity(b), p));
		}

		public static Color Lerp(Color a, Color b, float f)
		{
			return FromUnity(UnityEngine.Color.Lerp(
				ToUnity(a), ToUnity(b), f));
		}

		public static Color FromHSV(HSVColor hsv)
		{
			return FromUnity(UnityEngine.Color.HSVToRGB(hsv.H, hsv.S, hsv.V));
		}

		public static HSVColor ToHSV(Color c)
		{
			var hsv = new HSVColor();
			UnityEngine.Color.RGBToHSV(ToUnity(c), out hsv.H, out hsv.S, out hsv.V);
			return hsv;
		}

		private static UnityEngine.Plane[] cached_ = new UnityEngine.Plane[6];

		public static bool TestPlanesAABB(Plane[] planes, Box box)
		{
			cached_[0] = ToUnity(planes[0]).flipped;
			cached_[1] = ToUnity(planes[1]).flipped;
			cached_[2] = ToUnity(planes[2]).flipped;
			cached_[3] = ToUnity(planes[3]).flipped;
			cached_[4] = ToUnity(planes[4]).flipped;
			cached_[5] = ToUnity(planes[5]).flipped;

			return GeometryUtility.TestPlanesAABB(cached_, ToUnity(box));
		}
	}
}
