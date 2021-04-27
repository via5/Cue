﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cue.W
{
	class VamSys : ISys
	{
		private static VamSys instance_ = null;
		private readonly MVRScript script_ = null;
		private readonly VamLog log_ = new VamLog();
		private readonly VamNav nav_ = new VamNav();
		private readonly VamInput input_;
		private string pluginPath_ = "";

		public VamSys(MVRScript s)
		{
			instance_ = this;
			script_ = s;
			input_ = new VamInput(this);
		}

		static public VamSys Instance
		{
			get { return instance_; }
		}

		public ILog Log
		{
			get { return log_; }
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

			return list;
		}

		public IAtom ContainingAtom
		{
			get { return new VamAtom(script_.containingAtom); }
		}

		public bool Paused
		{
			get { return SuperController.singleton.freezeAnimation; }
		}

		public bool IsVR
		{
			get
			{
				return !SuperController.singleton.MonitorCenterCamera.isActiveAndEnabled;
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

		public VUI.Root CreateHud(Vector3 offset, Point pos, Size size)
		{
			return new VUI.Root(
				new VRTopHudRootSupport(
					VamU.ToUnity(offset),
					VamU.ToUnity(pos),
					VamU.ToUnity(size)));
		}

		public VUI.Root CreateAttached(Vector3 offset, Point pos, Size size)
		{
			return new VUI.Root(
				new VRHandRootSupport(
					VamU.ToUnity(offset),
					VamU.ToUnity(pos),
					VamU.ToUnity(size)));
		}

		public VUI.Root Create2D(float topOffset, Size size)
		{
			return new VUI.Root(
				new OverlayRootSupport(topOffset, size.Width, size.Height));
		}

		public VUI.Root CreateScriptUI()
		{
			return new VUI.Root(
				new ScriptUIRootSupport(CueMain.Instance.MVRScriptUI));
		}

		public IBoxGraphic CreateBoxGraphic(Vector3 pos)
		{
			return new VamBoxGraphic(VamU.ToUnity(pos));
		}

		public void OnPluginState(bool b)
		{
			nav_.OnPluginState(b);
			log_.OnPluginState(b);

			for (int i = 0; i < 32; ++i)
				Physics.IgnoreLayerCollision(i, VamBoxGraphic.Layer, b);
		}

		public void OnReady(Action f)
		{
			SuperController.singleton.StartCoroutine(DeferredInit(f));
		}

		public void HardReset()
		{
			SuperController.singleton.HardReset();
		}

		public void ReloadPlugin()
		{
			Transform uit = CueMain.Instance.UITransform;
			if (uit?.parent == null)
			{
				SuperController.LogError("no main ui, selecting atom");
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
					Log.Clear();
					Cue.LogInfo("reloading");
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

		private IEnumerator DeferredInit(Action f)
		{
			yield return new WaitForEndOfFrame();
			f?.Invoke();
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


		public JSONStorableFloat GetFloatParameter(
			IObject o, string storable, string param)
		{
			return GetFloatParameter(((W.VamAtom)o).Atom, storable, param);
		}

		public JSONStorableFloat GetFloatParameter(
			Atom a, string storable, string param)
		{
			foreach (var id in a.GetStorableIDs())
			{
				if (id.Contains(storable))
				{
					var st = a.GetStorableByID(id);
					if (st == null)
					{
						Cue.LogError($"{a.uid}: no storable {id}");
						continue;
					}

					var p = st.GetFloatJSONParam(param);
					if (p == null)
					{
						Cue.LogError($"{a.uid}: storable {id} has no float param '{param}'");
						continue;
					}

					return p;
				}
			}

			return null;
		}

		public JSONStorableBool GetBoolParameter(
			IObject o, string storable, string param)
		{
			return GetBoolParameter(((W.VamAtom)o.Atom).Atom, storable, param);
		}

		public JSONStorableBool GetBoolParameter(
			Atom a, string storable, string param)
		{
			foreach (var id in a.GetStorableIDs())
			{
				if (id.Contains(storable))
				{
					var st = a.GetStorableByID(id);
					if (st == null)
					{
						Cue.LogError($"{a.uid}: no storable {id}");
						continue;
					}

					var p = st.GetBoolJSONParam(param);
					if (p == null)
					{
						Cue.LogError($"{a.uid}: storable {id} has no bool param '{param}'");
						continue;
					}

					return p;
				}
			}

			return null;
		}

		public JSONStorableString GetStringParameter(
			IObject o, string storable, string param)
		{
			return GetStringParameter(((W.VamAtom)o.Atom).Atom, storable, param);
		}

		public JSONStorableString GetStringParameter(
			Atom a, string storable, string param)
		{
			foreach (var id in a.GetStorableIDs())
			{
				if (id.Contains(storable))
				{
					var st = a.GetStorableByID(id);
					if (st == null)
					{
						Cue.LogError($"{a.uid}: no storable {id}");
						continue;
					}

					var p = st.GetStringJSONParam(param);
					if (p == null)
					{
						Cue.LogError($"{a.uid}: storable {id} has no string param '{param}'");
						continue;
					}

					return p;
				}
			}

			return null;
		}


		public JSONStorableStringChooser GetStringChooserParameter(
			IObject o, string storable, string param)
		{
			return GetStringChooserParameter(((W.VamAtom)o.Atom).Atom, storable, param);
		}

		public JSONStorableStringChooser GetStringChooserParameter(
			Atom a, string storable, string param)
		{
			foreach (var id in a.GetStorableIDs())
			{
				if (id.Contains(storable))
				{
					var st = a.GetStorableByID(id);
					if (st == null)
					{
						Cue.LogError($"{a.uid}: no storable {id}");
						continue;
					}

					var p = st.GetStringChooserJSONParam(param);
					if (p == null)
					{
						Cue.LogError($"{a.uid}: storable {id} has no stringchooser param '{param}'");
						continue;
					}

					return p;
				}
			}

			return null;
		}


		public JSONStorableAction GetActionParameter(
			IObject o, string storable, string param)
		{
			return GetActionParameter(((W.VamAtom)o.Atom).Atom, storable, param);
		}

		public JSONStorableAction GetActionParameter(
			Atom a, string storable, string param)
		{
			foreach (var id in a.GetStorableIDs())
			{
				if (id.Contains(storable))
				{
					var st = a.GetStorableByID(id);
					if (st == null)
					{
						Cue.LogError($"{a.uid}: no storable {id}");
						continue;
					}

					var p = st.GetAction(param);
					if (p == null)
					{
						Cue.LogError($"{a.uid}: storable {id} has no action param '{param}'");
						continue;
					}

					return p;
				}
			}

			return null;
		}

		public Rigidbody FindRigidbody(IObject o, string name)
		{
			return FindRigidbody(((W.VamAtom)o.Atom).Atom, name);
		}

		public Rigidbody FindRigidbody(Atom a, string name)
		{
			foreach (var rb in a.rigidbodies)
			{
				if (rb.name == name)
					return rb.GetComponent<Rigidbody>();
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

		public void DumpComponents(GameObject o, int indent = 0)
		{
			foreach (var c in o.GetComponents(typeof(Component)))
				Cue.LogInfo(new string(' ', indent * 2) + c.ToString());
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

		private GenerateDAZMorphsControlUI GetMUI(Atom atom)
		{
			if (atom == null)
				return null;

			var cs = atom.GetComponentInChildren<DAZCharacterSelector>();
			if (cs == null)
				return null;

			return cs.morphsControlUI;
		}
	}


	class VamU
	{
		public static string ToString(RectTransform rt)
		{
			return
				"rect: " + rt.rect.ToString() + "\n" +
				"offsetMin: " + rt.offsetMin.ToString() + "\n" +
				"offsetMax: " + rt.offsetMax.ToString() + "\n" +
				"anchorMin: " + rt.anchorMin.ToString() + "\n" +
				"anchorMax: " + rt.anchorMax.ToString() + "\n" +
				"anchorPos: " + rt.anchoredPosition.ToString();
		}

		public static UnityEngine.Vector3 ToUnity(Vector3 v)
		{
			return new UnityEngine.Vector3(v.X, v.Y, v.Z);
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


		public static Vector3 FromUnity(UnityEngine.Vector3 v)
		{
			return new Vector3(v.x, v.y, v.z);
		}

		public static Color FromUnity(UnityEngine.Color c)
		{
			return new Color(c.r, c.g, c.b, c.a);
		}


		public static float Distance(Vector3 a, Vector3 b)
		{
			return UnityEngine.Vector3.Distance(ToUnity(a), ToUnity(b));
		}

		public static float Angle(Vector3 a, Vector3 b)
		{
			return UnityEngine.Quaternion.LookRotation(ToUnity(b - a)).eulerAngles.y;
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
	}
}
