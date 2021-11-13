using System;
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

		public static GameObject FindChildRecursive(Component c, string name)
		{
			return FindChildRecursive(c.gameObject, name);
		}

		public static GameObject FindChildRecursive(GameObject o, string name)
		{
			if (o == null)
				return null;

			if (o.name == name)
				return o;

			foreach (Transform c in o.transform)
			{
				var r = FindChildRecursive(c.gameObject, name);
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

		public static Collider FindCollider(Atom atom, string pathstring)
		{
			var path = pathstring.Split('/');
			var p = path[path.Length - 1];

			foreach (var c in atom.GetComponentsInChildren<Collider>())
			{
				if (EquivalentName(c.name, p))
				{
					if (path.Length == 1)
						return c;

					var check = c.transform.parent;
					if (check == null)
					{
						Cue.LogInfo("parent is not a collider");
						continue;
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
			}


			foreach (var c in atom.GetComponentsInChildren<Collider>())
			{
				if (EquivalentName(c.name, pathstring))
					return c;
			}

			return null;
		}

		private static bool EquivalentName(string cn, string pathstring)
		{
			if (cn == pathstring)
				return true;

			if (cn == "AutoColliderFemaleAutoColliders" + pathstring)
				return true;

			if (cn == "AutoColliderMaleAutoColliders" + pathstring)
				return true;

			if (cn == "AutoCollider" + pathstring)
				return true;

			if (cn == "AutoColliderAutoColliders" + pathstring)
				return true;

			if (cn == "FemaleAutoColliders" + pathstring)
				return true;

			if (cn == "MaleAutoColliders" + pathstring)
				return true;

			if (cn == "StandardColliders" + pathstring)
				return true;

			if (cn == "_" + pathstring)
				return true;

			return false;
		}

		public static Atom AtomForCollider(Collider c)
		{
			var p = c.transform;

			while (p != null)
			{
				var a = p.GetComponent<Atom>();
				if (a != null)
					return a;

				p = p.parent;
			}

			return null;
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
