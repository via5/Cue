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
		private VamDebugRenderer debugRenderer_ = new VamDebugRenderer();
		private Action deferredInit_ = null;

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

			VamFixes.Run();
		}

		public VamDebugRenderer DebugRenderer
		{
			get { return debugRenderer_; }
		}

		static public VamSys Instance
		{
			get { return instance_; }
		}

		public void ClearLog()
		{
			log_.Clear();
		}

		public ILiveSaver CreateLiveSaver()
		{
			return new LiveSaver();
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

		public bool HasUI
		{
			get { return true; }
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

		public IObjectCreator CreateObjectCreator(
			string name, string type, JSONClass opts, Sys.ObjectParameters ps)
		{
			if (type == "cua")
				return new VamCuaObjectCreator(name, opts, ps);
			else if (type == "atom")
				return new VamAtomObjectCreator(name, opts, ps);
			else if (type == "clothing")
				return new VamClothingObjectCreator(name, opts, ps);

			Cue.LogError($"unknown object creator type '{type}'");
			return null;
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

		public IGraphic CreateCapsuleGraphic(string name, Color c)
		{
			return new VamCapsuleGraphic(name, c);
		}

		public void Update(float s)
		{
			bool vr = IsVR;

			if (wasVR_ != vr)
			{
				wasVR_ = vr;
				//SuperController.singleton.commonHandModelControl.useCollision = true;
			}

			debugRenderer_.Update(s);
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
			var pui = GetPluginUI();
			if (pui == null)
				return;

			ClearLog();
			SuperController.LogError("reloading");
			pui.reloadButton?.onClick?.Invoke();
		}

		public void OpenScriptUI()
		{
			var pui = GetPluginUI();
			if (pui == null)
				return;

			var scui = pui.GetComponentInChildren<MVRScriptControllerUI>();
			if (scui == null)
			{
				SuperController.LogError("no MVRScriptControllerUI");
				return;
			}

			if (scui.openUIButton == null)
			{
				SuperController.LogError("no openUIButton");
				return;
			}

			if (scui.openUIButton.onClick == null)
			{
				SuperController.LogError("openUIButton.onClick is null");
				return;
			}

			SuperController.singleton.SelectController(
				script_.containingAtom.mainController);

			scui.openUIButton?.onClick?.Invoke();
		}

		private MVRPluginUI GetPluginUI()
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
					return null;
				}
			}

			foreach (var pui in uit.parent.GetComponentsInChildren<MVRPluginUI>())
			{
				if (pui.urlText.text.Contains("Cue.cslist"))
					return pui;
			}

			SuperController.LogError("no MVRPluginUI found for this plugin");
			return null;
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
	}
}
