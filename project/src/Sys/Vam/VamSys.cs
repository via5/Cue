using MeshVR;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cue.Sys.Vam
{
	class VamSys : ISys
	{
		private static VamSys instance_ = null;
		private readonly MVRScript script_ = null;
		private readonly VamLog log_ = new VamLog();
		private readonly VamNav nav_ = new VamNav();
		private readonly VamInput input_;
		private string pluginPath_ = "";
		private GameObject root_;
		private VamCameraAtom cameraAtom_ = new VamCameraAtom();
		private bool wasVR_ = false;
		private PerfMon perf_ = null;

		public VamSys(MVRScript s)
		{
			instance_ = this;
			script_ = s;
			input_ = new VamInput(this);

			var vamroot = SuperController.singleton.transform.root;

			foreach (Transform t in vamroot)
			{
				if (t.name == "CueRoot")
				{
					var temp = new GameObject().transform;
					t.transform.SetParent(temp);
					UnityEngine.Object.Destroy(temp.gameObject);
				}
			}

			perf_ = SuperController.singleton.transform.root
				.GetComponentInChildren<PerfMon>();

			if (perf_ == null)
				Cue.LogError("no perfmon");

			root_ = new GameObject("CueRoot");
			root_.transform.SetParent(vamroot, false);
		}

		static public VamSys Instance
		{
			get { return instance_; }
		}

		public void ClearLog()
		{
			log_.Clear();
		}

		public void Log(string s, int level)
		{
			log_.Log(s, level);
		}

		public JSONClass GetConfig()
		{
			var a = SuperController.singleton.GetAtomByUid("cueconfig");
			if (a == null)
				return null;

			var t = a.GetStorableByID("Text");
			if (t == null)
			{
				Cue.LogError($"cueconfig has no Text storable");
				return null;
			}

			var p = t.GetStringJSONParam("text");
			if (p == null)
			{
				Cue.LogError($"cueconfig Text storable has no text parameter");
				return null;
			}

			Cue.LogVerbose($"found cueconfig");

			try
			{
				var doc = JSON.Parse(p.val);
				if (doc == null)
				{
					Cue.LogError("cueconfig bad json");
					return null;
				}

				return doc.AsObject;
			}
			catch (Exception e)
			{
				Cue.LogError("cueconfig bad json, " + e.Message);
			}

			return null;
		}

		public INav Nav
		{
			get { return nav_; }
		}

		public IInput Input
		{
			get { return input_; }
		}

		public IAtom GetAtom(string id)
		{
			var a = SuperController.singleton.GetAtomByUid(id);
			if (a == null)
				return null;

			return new VamAtom(a);
		}

		public List<IAtom> GetAtoms(bool alsoOff = false)
		{
			var list = new List<IAtom>();

			foreach (var a in SuperController.singleton.GetAtoms())
			{
				if (a.on || alsoOff)
					list.Add(new VamAtom(a));
			}

			list.Add(cameraAtom_);

			return list;
		}

		public VamCameraAtom CameraAtom
		{
			get { return cameraAtom_; }
		}

		public Transform RootTransform
		{
			get { return root_.transform; }
		}

		public bool IsVRHands(Atom a)
		{
			return (a.uid == "[CameraRig]");
		}

		public IAtom ContainingAtom
		{
			get { return new VamAtom(script_.containingAtom); }
		}

		public Vector3 CameraPosition
		{
			get { return U.FromUnity(SuperController.singleton.lookCamera.transform.position); }
		}

		public bool Paused
		{
			get { return SuperController.singleton.freezeAnimation; }
		}

		public bool IsVR
		{
			get
			{
				return
					!SuperController.singleton.MonitorCenterCamera.isActiveAndEnabled &&
					!SuperController.singleton.IsMonitorRigActive;
			}
		}

		public bool IsPlayMode
		{
			get
			{
				return (SuperController.singleton.gameMode == SuperController.GameMode.Play);
			}
		}

		public float DeltaTime
		{
			get { return Time.deltaTime; }
		}

		public float RealtimeSinceStartup
		{
			get { return Time.realtimeSinceStartup; }
		}

		public string Fps
		{
			get
			{
				if (perf_ == null)
					return "";

				return perf_.fps;
			}
		}

		public Vector3 InteractiveLeftHandPosition
		{
			get
			{
				return U.FromUnity(SuperController.singleton.leftHand.position);
			}
		}

		public Vector3 InteractiveRightHandPosition
		{
			get
			{
				return U.FromUnity(SuperController.singleton.rightHand.position);
			}
		}

		// [begin, end]
		//
		public int RandomInt(int first, int last)
		{
			return UnityEngine.Random.Range(first, last + 1);
		}

		// [begin, end]
		//
		public float RandomFloat(float first, float last)
		{
			return UnityEngine.Random.Range(first, last);
		}

		public IObjectCreator CreateObjectCreator(string name, string type, JSONClass opts)
		{
			if (type == "cua")
				return new VamCuaObjectCreator(name, opts);

			Cue.LogError($"unknown object creator type '{type}'");
			return null;
		}

		public VUI.Root CreateHud(Vector3 offset, Point pos, Size size)
		{
			return new VUI.Root(
				new VRTopHudRootSupport(
					U.ToUnity(offset),
					U.ToUnity(pos),
					U.ToUnity(size)));
		}

		public VUI.Root CreateAttached(bool left, Vector3 offset, Point pos, Size size)
		{
			return new VUI.Root(
				new VRHandRootSupport(
					left,
					U.ToUnity(offset),
					U.ToUnity(pos),
					U.ToUnity(size)));
		}

		public VUI.Root Create2D(float topOffset, Size size)
		{
			return new VUI.Root(
				new OverlayRootSupport(topOffset, size.Width, size.Height));
		}

		public VUI.Root CreateScriptUI()
		{
			return new VUI.Root(CueMain.Instance.MVRScriptUI);
		}

		public IGraphic CreateBoxGraphic(string name, Box box, Color c)
		{
			return new VamBoxGraphic(name, box.center, box.size, c);
		}

		public IGraphic CreateBoxGraphic(string name, Vector3 pos, Vector3 size, Color c)
		{
			return new VamBoxGraphic(name, pos, size, c);
		}

		public IGraphic CreateSphereGraphic(
			string name, Vector3 pos, float radius, Color c)
		{
			return new VamSphereGraphic(name, pos, radius, c);
		}

		public void Update(float s)
		{
			bool vr = IsVR;

			if (wasVR_ != vr)
			{
				wasVR_ = vr;
				//SuperController.singleton.commonHandModelControl.useCollision = true;
			}
		}

		public void OnPluginState(bool b)
		{
			root_.SetActive(b);

			nav_.OnPluginState(b);
			log_.OnPluginState(b);

			for (int i = 0; i < 32; ++i)
				Physics.IgnoreLayerCollision(i, VamBoxGraphic.Layer, b);

			VamMorphManager.Instance.OnPluginState(b);
		}

		private Action deferredInit_ = null;

		public void OnReady(Action f)
		{
			SuperController.singleton.onSceneLoadedHandlers += OnSceneLoaded;
			deferredInit_ = f;

			if (SuperController.singleton.isLoading)
			{
				Cue.LogVerbose("scene loading, waiting to init");
				return;
			}
			else
			{
				Cue.LogVerbose("scene already loaded, running deferred init on next frame");
				SuperController.singleton.onSceneLoadedHandlers -= OnSceneLoaded;
				SuperController.singleton.StartCoroutine(DeferredInit());
			}
		}

		private IEnumerator DeferredInit()
		{
			yield return new WaitForEndOfFrame();
			Cue.LogVerbose("running deferred init");
			deferredInit_?.Invoke();
			deferredInit_ = null;
		}

		private void OnSceneLoaded()
		{
			Cue.LogVerbose("scene loaded, running deferred init on next frame");
			SuperController.singleton.onSceneLoadedHandlers -= OnSceneLoaded;
			SuperController.singleton.StartCoroutine(DeferredInit());
		}

		public void HardReset()
		{
			SuperController.singleton.HardReset();
		}

		public void ReloadPlugin()
		{
			// don't use cue for logging in case something went wrong when
			// loading

			Transform uit = CueMain.Instance.UITransform;
			if (uit?.parent == null)
			{
				SuperController.LogError("no main ui, selecting atom");

				SuperController.singleton.gameMode = SuperController.GameMode.Edit;
				SuperController.singleton.SelectController(
					script_.containingAtom.mainController);

				uit = CueMain.Instance.UITransform;
				if (uit?.parent == null)
				{
					SuperController.LogError("sill no main ui, can't reload, open main UI once");
					return;
				}
			}

			foreach (var pui in uit.parent.GetComponentsInChildren<MVRPluginUI>())
			{
				if (pui.urlText.text.Contains("Cue.cslist"))
				{
					ClearLog();
					SuperController.LogError("reloading");
					pui.reloadButton.onClick.Invoke();
				}
			}
		}

		public string ReadFileIntoString(string path)
		{
			try
			{
				return SuperController.singleton.ReadFileIntoString(path);
			}
			catch (Exception e)
			{
				Cue.LogError("failed to read '" + path + "', " + e.Message);
				return "";
			}
		}

		public string GetResourcePath(string path)
		{
			if (pluginPath_ == "")
				pluginPath_ = CueMain.Instance.PluginPath;

			path = path.Replace('/', '\\');

			if (path.StartsWith("\\"))
				return pluginPath_ + "\\res" + path;
			else
				return pluginPath_ + "\\res\\" + path;
		}

		public void ForEachChild(Component c, Action<Transform> f)
		{
			ForEachChild(c.transform, f);
		}

		public void ForEachChild(GameObject o, Action<Transform> f)
		{
			ForEachChild(o.transform, f);
		}

		public void ForEachChild(Transform t, Action<Transform> f)
		{
			f(t);

			foreach (Transform c in t)
				ForEachChild(c, f);
		}

		public GameObject FindChildRecursive(Component c, string name)
		{
			return FindChildRecursive(c.gameObject, name);
		}

		public GameObject FindChildRecursive(GameObject o, string name)
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

		private JSONStorable FindStorable(Atom a, string name)
		{
			var s = a.GetStorableByID(name);
			if (s != null)
				return s;

			string p = "";
			for (int i = 0; i < 20; ++i)
			{
				p = $"plugin#{i}_{name}";
				s = a.GetStorableByID(p);
				if (s != null)
					return s;
			}

			return null;
		}

		public JSONStorableFloat GetFloatParameter(
			IObject o, string storable, string param)
		{
			return GetFloatParameter((o as VamAtom)?.Atom, storable, param);
		}

		public JSONStorableFloat GetFloatParameter(
			Atom a, string storable, string param)
		{
			if (a == null)
				return null;

			var st = FindStorable(a, storable);
			if (st == null)
			{
				//Cue.LogError($"{a.uid}: no storable {storable}");
				return null;
			}

			return st.GetFloatJSONParam(param);
		}

		public JSONStorableBool GetBoolParameter(
			IObject o, string storable, string param)
		{
			return GetBoolParameter((o.Atom as VamAtom)?.Atom, storable, param);
		}

		public JSONStorableBool GetBoolParameter(
			Atom a, string storable, string param)
		{
			if (a == null)
				return null;

			var st = FindStorable(a, storable);
			if (st == null)
			{
				//Cue.LogError($"{a.uid}: no storable {storable}");
				return null;
			}

			return st.GetBoolJSONParam(param);
		}

		public JSONStorableString GetStringParameter(
			IObject o, string storable, string param)
		{
			return GetStringParameter((o.Atom as VamAtom)?.Atom, storable, param);
		}

		public JSONStorableString GetStringParameter(
			Atom a, string storable, string param)
		{
			if (a == null)
				return null;

			var st = FindStorable(a, storable);
			if (st == null)
			{
				//Cue.LogError($"{a.uid}: no storable {storable}");
				return null;
			}

			return st.GetStringJSONParam(param);
		}


		public JSONStorableStringChooser GetStringChooserParameter(
			IObject o, string storable, string param)
		{
			return GetStringChooserParameter((o.Atom as VamAtom)?.Atom, storable, param);
		}

		public JSONStorableStringChooser GetStringChooserParameter(
			Atom a, string storable, string param)
		{
			if (a == null)
				return null;

			var st = FindStorable(a, storable);
			if (st == null)
			{
				//Cue.LogError($"{a.uid}: no storable {storable}");
				return null;
			}

			return st.GetStringChooserJSONParam(param);
		}


		public JSONStorableColor GetColorParameter(
		IObject o, string storable, string param)
		{
			return GetColorParameter((o.Atom as VamAtom)?.Atom, storable, param);
		}

		public JSONStorableColor GetColorParameter(
			Atom a, string storable, string param)
		{
			if (a == null)
				return null;

			var st = FindStorable(a, storable);
			if (st == null)
			{
				//Cue.LogError($"{a.uid}: no storable {storable}");
				return null;
			}

			return st.GetColorJSONParam(param);
		}


		public JSONStorableAction GetActionParameter(
			IObject o, string storable, string param)
		{
			return GetActionParameter((o.Atom as VamAtom)?.Atom, storable, param);
		}

		public JSONStorableAction GetActionParameter(
			Atom a, string storable, string param)
		{
			if (a == null)
				return null;

			var st = FindStorable(a, storable);
			if (st == null)
			{
				//Cue.LogError($"{a.uid}: no storable {storable}");
				return null;
			}

			return st.GetAction(param);
		}

		public Rigidbody FindRigidbody(IObject o, string name)
		{
			return FindRigidbody((o.Atom as VamAtom)?.Atom, name);
		}

		public Rigidbody FindRigidbody(Atom a, string name)
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

		public FreeControllerV3 FindController(Atom a, string name)
		{
			for (int i = 0; i < a.freeControllers.Length; ++i)
			{
				if (a.freeControllers[i].name == name)
					return a.freeControllers[i];
			}

			return null;
		}

		public DAZMorph FindMorph(
			Atom atom, GenerateDAZMorphsControlUI mui, DAZMorph m)
		{
			var nm = mui.GetMorphByUid(m.uid);
			if (nm != null)
				return nm;

			nm = mui.GetMorphByDisplayName(m.displayName);
			if (nm != null)
				return nm;

			Cue.LogWarning(
				"morph '" + m.displayName + "' doesn't " +
				"exist in " + atom.uid);

			return null;
		}

		public DAZMorph FindMorph(Atom atom, string morphUID)
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

			return null;
		}

		public Collider FindCollider(Atom atom, string pathstring)
		{
			var path = pathstring.Split('/');
			var p = path[path.Length - 1];

			foreach (var c in atom.GetComponentsInChildren<Collider>())
			{
				if (c.name == p)
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
						if (check.name != path[path.Length - i - 1])
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
				if (c.name == "AutoColliderFemaleAutoColliders" + pathstring)
					return c;

				if (c.name == "AutoColliderMaleAutoColliders" + pathstring)
					return c;

				if (c.name == "AutoCollider" + pathstring)
					return c;
			}

			return null;
		}

		public Atom AtomForCollider(Collider c)
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

		public void DumpComponents(Transform t, int indent = 0)
		{
			DumpComponents(t.gameObject, indent);
		}

		public void DumpComponents(GameObject o, int indent = 0)
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

		public void DumpComponentsAndUp(Component c)
		{
			DumpComponentsAndUp(c.gameObject);
		}

		public void DumpComponentsAndUp(GameObject o)
		{
			Cue.LogInfo(o.name);

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

		public void DumpComponentsAndDown(Component c, bool dumpRt = false)
		{
			DumpComponentsAndDown(c.gameObject, dumpRt);
		}

		public void DumpComponentsAndDown(
			GameObject o, bool dumpRt = false, int indent = 0)
		{
			Cue.LogInfo(new string(' ', indent * 2) + o.name);

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

		public GenerateDAZMorphsControlUI GetMUI(Atom atom)
		{
			if (atom == null)
				return null;

			var cs = atom.GetComponentInChildren<DAZCharacterSelector>();
			if (cs == null)
				return null;

			return cs.morphsControlUI;
		}
	}


	class U : global::Cue.U
	{
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
			return new UnityEngine.Plane(
				ToUnity(p.a), ToUnity(p.b), ToUnity(p.c));
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

		public static bool TestPlanesAABB(Plane[] planes, Box box)
		{
			var ps = new UnityEngine.Plane[]
			{
				ToUnity(planes[0]).flipped,
				ToUnity(planes[1]).flipped,
				ToUnity(planes[2]).flipped,
				ToUnity(planes[3]).flipped,
				ToUnity(planes[4]).flipped,
				ToUnity(planes[5]).flipped,
			};

			return GeometryUtility.TestPlanesAABB(ps, ToUnity(box));
		}
	}
}
