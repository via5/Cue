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

		public void OnPluginState(bool b)
		{
			nav_.OnPluginState(b);
			log_.OnPluginState(b);
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
						Cue.LogError("can't find storable " + id);
						continue;
					}

					var p = st.GetFloatJSONParam(param);
					if (p == null)
					{
						Cue.LogError("no '" + param + "' param");
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
						Cue.LogError("can't find storable " + id);
						continue;
					}

					var p = st.GetBoolJSONParam(param);
					if (p == null)
					{
						Cue.LogError("no '" + param + "' param");
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
						Cue.LogError("can't find storable " + id);
						continue;
					}

					var p = st.GetStringJSONParam(param);
					if (p == null)
					{
						Cue.LogError("no '" + param + "' param");
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
						Cue.LogError("can't find storable " + id);
						continue;
					}

					var p = st.GetStringChooserJSONParam(param);
					if (p == null)
					{
						Cue.LogError("no '" + param + "' param");
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
						Cue.LogError("can't find storable " + id);
						continue;
					}

					var p = st.GetAction(param);
					if (p == null)
					{
						Cue.LogError("no '" + param + "' action param");
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
	}


	class VamInput : IInput
	{
		private VamSys sys_;
		private Ray ray_ = new Ray();
		private bool controlsToggle_ = false;
		private Vector3 middlePos_ = Vector3.Zero;
		private bool middleDown_ = false;

		public VamInput(VamSys sys)
		{
			sys_ = sys;
		}

		public bool ReloadPlugin
		{
			get
			{
				return Input.GetKeyDown(KeyCode.F5);
			}
		}

		public bool MenuToggle
		{
			get
			{
				if (sys_.IsVR)
					return SuperController.singleton.GetLeftSelect();
				else
					return false;
			}
		}

		public bool Select
		{
			get
			{
				if (sys_.IsVR)
					return SuperController.singleton.GetRightSelect();
				else
					return Input.GetMouseButtonUp(0);
			}
		}

		public bool Action
		{
			get
			{
				if (sys_.IsVR)
					return SuperController.singleton.GetRightGrab();
				else
					return Input.GetMouseButtonUp(1);
			}
		}

		public bool ShowControls
		{
			get
			{
				if (sys_.IsVR)
				{
					return
						SuperController.singleton.GetLeftUIPointerShow() ||
						SuperController.singleton.GetRightUIPointerShow();
				}
				else
				{
					if (middleDown_)
					{
						if (Input.GetMouseButtonUp(2))
						{
							var cp = Vector3.FromUnity(
								SuperController.singleton.MonitorCenterCamera
									.transform.position);

							var d = Vector3.Distance(middlePos_, cp);
							if (d < 0.02f)
								controlsToggle_ = !controlsToggle_;

							middleDown_ = false;
						}
					}
					else
					{
						if (Input.GetMouseButtonDown(2))
						{
							var cp = Vector3.FromUnity(
								SuperController.singleton.MonitorCenterCamera
									.transform.position);

							middleDown_ = true;
							middlePos_ = cp;
						}
					}

					return controlsToggle_;
				}
			}
		}

		public IObject GetHovered()
		{
			if (sys_.IsVR)
			{
				if (!GetVRRay())
					return null;
			}
			else
			{
				if (!GetMouseRay())
					return null;
			}

			if (HitUI())
				return null;

			var o = HitObject();
			if (o != null)
				return o;

			o = HitPerson();
			if (o != null)
				return o;

			return null;
		}

		private bool GetMouseRay()
		{
			ray_ = SuperController.singleton.MonitorCenterCamera
				.ScreenPointToRay(Input.mousePosition);

			return true;
		}

		private bool GetVRRay()
		{
			if (SuperController.singleton.GetLeftUIPointerShow())
			{
				ray_.origin = SuperController.singleton.viveObjectLeft.position;
				ray_.direction = SuperController.singleton.viveObjectLeft.forward;
				return true;
			}
			else if (SuperController.singleton.GetRightUIPointerShow())
			{
				ray_.origin = SuperController.singleton.viveObjectRight.position;
				ray_.direction = SuperController.singleton.viveObjectRight.forward;
				return true;
			}
			else
			{
				return false;
			}
		}

		private bool HitUI()
		{
			// todo
			return false;
		}

		private IObject HitObject()
		{
			RaycastHit hit;

			bool b = Physics.Raycast(
				ray_, out hit, float.MaxValue, 1 << Controls.Layer);

			if (!b)
				return null;

			return Cue.Instance.Controls.Find(hit.transform);
		}

		private IObject HitPerson()
		{
			var a = HitAtom();
			if (a == null)
				return null;

			var ps = Cue.Instance.Persons;

			for (int i = 0; i < ps.Count; ++i)
			{
				if (((W.VamAtom)ps[i].Atom).Atom == a)
					return ps[i];
			}

			return null;
		}

		private Atom HitAtom()
		{
			RaycastHit hit;
			bool b = Physics.Raycast(
				ray_, out hit, float.MaxValue, 0x24000100);

			if (!b)
				return null;

			var fc = hit.transform.GetComponent<FreeControllerV3>();

			if (fc != null)
				return fc.containingAtom;

			var bone = hit.transform.GetComponent<DAZBone>();
			if (bone != null)
				return bone.containingAtom;

			var rb = hit.transform.GetComponent<Rigidbody>();
			var p = rb.transform;

			while (p != null)
			{
				var a = p.GetComponent<Atom>();
				if (a != null)
					return a;

				p = p.parent;
			}

			return null;
		}
	}


	class VamLog : ILog
	{
		private UnityEngine.UI.Button clear_ = null;
		private UnityEngine.UI.Text logText_ = null;
		private UIStyleText logStyle_ = null;
		private int logFontSize_ = -1;
		private Font logFont_ = null;

		public VamLog()
		{
		}

		public void Clear()
		{
			if (clear_ == null)
			{
				var t = VUI.Utilities.FindChildRecursive(
					SuperController.singleton.errorLogPanel, "ClearButton");

				clear_ = t?.GetComponent<UnityEngine.UI.Button>();
			}

			clear_?.onClick?.Invoke();
		}

		public void Verbose(string s)
		{
			SuperController.LogError("  [V] " + s);
		}

		public void Info(string s)
		{
			SuperController.LogError("  [I] " + s);
		}

		public void Error(string s)
		{
			SuperController.LogError("!![E] " + s);
		}

		public void OnPluginState(bool b)
		{
			if (logText_ == null)
			{
				var vp = VUI.Utilities.FindChildRecursive(
					SuperController.singleton.errorLogPanel, "Viewport");

				var textObject = VUI.Utilities.FindChildRecursive(vp, "Text");

				logText_ = textObject.GetComponent<UnityEngine.UI.Text>();
				logStyle_ = textObject.GetComponent<UIStyleText>();
			}

			if (b)
			{
				logFontSize_ = logStyle_.fontSize;
				logFont_ = logText_.font;
				logStyle_.fontSize = 24;
				logText_.resizeTextForBestFit = false;
				logText_.font = Font.CreateDynamicFontFromOSFont("Consolas", 24);
				logStyle_.UpdateStyle();
			}
			else
			{
				logStyle_.fontSize = logFontSize_;
				logText_.fontSize = logFontSize_;
				logText_.font = logFont_;
				logText_.resizeTextForBestFit = true;
				logStyle_.UpdateStyle();
			}
		}
	}
}
