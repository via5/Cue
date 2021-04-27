using System;
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

		public ICanvas CreateHud(Vector3 offset, Point pos, Size size)
		{
			return new WorldSpaceAttachedCanvas(
				Vector3.ToUnity(offset),
				Point.ToUnity(pos),
				Size.ToUnity(size));
		}

		public ICanvas CreateAttached(Vector3 offset, Point pos, Size size)
		{
			return new WorldSpaceCameraCanvas(
				Vector3.ToUnity(offset),
				Point.ToUnity(pos),
				Size.ToUnity(size));
		}

		public ICanvas Create2D()
		{
			return new OverlayCanvas();
		}

		public IBoxGraphic CreateBoxGraphic(Vector3 pos)
		{
			return new VamBoxGraphic(Vector3.ToUnity(pos));
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

		public void ReloadPlugin()
		{
			Transform uit = CueMain.Instance.UITransform;
			if (uit?.parent == null)
			{
				SuperController.LogError("can't reload, open main UI once");
				return;
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
			var a = ((W.VamAtom)o.Atom).Atom;

			foreach (var id in a.GetStorableIDs())
			{
				if (id.Contains(storable))
				{
					var st = a.GetStorableByID(id);
					if (st == null)
					{
						Cue.LogError($"{o.ID}: no storable {id}");
						continue;
					}

					var p = st.GetFloatJSONParam(param);
					if (p == null)
					{
						Cue.LogError($"{o.ID}: storable {id} has no float param '{param}'");
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
			var a = ((W.VamAtom)o.Atom).Atom;

			foreach (var id in a.GetStorableIDs())
			{
				if (id.Contains(storable))
				{
					var st = a.GetStorableByID(id);
					if (st == null)
					{
						Cue.LogError($"{o.ID}: no storable {id}");
						continue;
					}

					var p = st.GetBoolJSONParam(param);
					if (p == null)
					{
						Cue.LogError($"{o.ID}: storable {id} has no bool param '{param}'");
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
			var a = ((W.VamAtom)o.Atom).Atom;

			foreach (var id in a.GetStorableIDs())
			{
				if (id.Contains(storable))
				{
					var st = a.GetStorableByID(id);
					if (st == null)
					{
						Cue.LogError($"{o.ID}: no storable {id}");
						continue;
					}

					var p = st.GetStringJSONParam(param);
					if (p == null)
					{
						Cue.LogError($"{o.ID}: storable {id} has no string param '{param}'");
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
			var a = ((W.VamAtom)o.Atom).Atom;

			foreach (var id in a.GetStorableIDs())
			{
				if (id.Contains(storable))
				{
					var st = a.GetStorableByID(id);
					if (st == null)
					{
						Cue.LogError($"{o.ID}: no storable {id}");
						continue;
					}

					var p = st.GetStringChooserJSONParam(param);
					if (p == null)
					{
						Cue.LogError($"{o.ID}: storable {id} has no stringchooser param '{param}'");
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
}
