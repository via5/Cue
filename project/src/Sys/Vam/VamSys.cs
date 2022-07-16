using MeshVR;
using MVR.FileManagementSecure;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AssetBundles;

namespace Cue.Sys.Vam
{
	sealed class VamSys : ISys
	{
		private const int PluginDataSource = 0;
		private const int PluginPathSource = 1;
		private const int AddonSource = 2;
		private const int UnknownSource = 3;

		class SysFileInfo
		{
			public string path;
			public int source;
		}


		private static readonly string PluginDataRoot = "Custom/PluginData/Cue/";
		private static string PluginPathRoot = null;

		private static VamSys instance_ = null;

		private Logger log_ = new Logger(Logger.Sys, "vamSys");
		private readonly MVRScript script_ = null;
		private readonly VamInput input_;
		private string pluginPath_ = "";
		private GameObject root_;
		private VamCameraAtom cameraAtom_ = new VamCameraAtom();
		private bool wasVR_ = false;
		private PerfMon perf_ = null;
		private VamDebugRenderer debugRenderer_ = new VamDebugRenderer();
		private Action deferredInit_ = null;
		private Linker linker_ = new Linker();
		private Dictionary<Transform, VamBodyPart> partMap_ = new Dictionary<Transform, VamBodyPart>();

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
				Log.Error("no perfmon");

			root_ = new GameObject("CueRoot");
			root_.transform.SetParent(vamroot, false);
		}

		public Logger Log
		{
			get { return log_; }
		}

		public VamDebugRenderer DebugRenderer
		{
			get { return debugRenderer_; }
		}

		public Linker Linker
		{
			get { return linker_; }
		}

		static public VamSys Instance
		{
			get { return instance_; }
		}

		public void ClearLog()
		{
			SuperController.singleton.ClearErrors();
		}

		public ILiveSaver CreateLiveSaver()
		{
			return new LiveSaver();
		}

		public void LogLines(string s, int level)
		{
			var t = DateTime.Now.ToString("hh:mm:ss.fff");
			string p = LogLevels.ToShortString(level);

			foreach (var line in s.Split('\n'))
			{
				if (level == LogLevels.Error)
					SuperController.LogError($"{t} !![{p}] {line}");
				else
					SuperController.LogError($"{t}   [{p}] {line}");
			}
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

		public bool IsAtomVRHands(Atom a)
		{
			return input_.VRInput.IsAtomVRHands(a);
		}

		public bool IsVRHand(Transform t, BodyPartType hand)
		{
			return input_.VRInput.IsTransformVRHand(t, (hand == BP.LeftHand));
		}

		public void SetMenuVisible(bool b)
		{
			GlobalSceneOptions.singleton.disableNavigation = b;
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

			Log.Error($"unknown object creator type '{type}'");
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

		public bool ForceDevMode
		{
			get
			{
				return FileManagerSecure.FileExists(
					$"{PluginDataRoot}/devmode");
			}
		}

		public void Init()
		{
			VamFixes.Run();
		}

		public void FixedUpdate(float s)
		{
			// no-op
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

		public void LateUpdate(float s)
		{
			linker_.LateUpdate(s);
		}

		public void OnPluginState(bool b)
		{
			root_.SetActive(b);

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
				Log.Verbose("scene loading, waiting to init");
				return;
			}
			else
			{
				Log.Verbose("scene already loaded, running deferred init on next frame");
				SuperController.singleton.onSceneLoadedHandlers -= OnSceneLoaded;
				SuperController.singleton.StartCoroutine(DeferredInit());
			}
		}


		public VamBodyPart BodyPartForTransform(Transform t)
		{
			// all Persons in the scene are a VamAtom and have a VamBody, but
			// there's also the special VamCameraAtom, which isn't an Atom at
			// all, which has a VamCameraBody instead
			//
			// normal VamAtoms will look up the transforms within their body
			// parts (see VamBody.BodyPartForTransform())
			//
			// things get complicated with hands, because there are many cases
			// handled in cue:
			//
			//  1) the mouse cursor (grab only)
			//  2) vr hands (grab and touch)
			//  3) hands of a possessed atom (grab and touch)
			//
			// there's also a special case for strapons, because the dildo is
			// a different atom, but should match the person wearing it (see
			// VamBody.BodyPartForTransform())
			//
			//
			// mouse grab:
			//   handled in VamCameraBody, where the VamCameraHand for the
			//   right hand will also check the mouseGrab transform
			//
			// vr or possessed hands for grab
			//   when grabbing something in vr, the camera rig is always used;
			//   it contains two transforms LeftHandAnchor and RightHandAnchor,
			//   to which the grabbed body part links to as a parent link
			//
			//   this happens even for a possessed atom: the possessed atom's
			//   hands are not used for linking, it's still the anchors from the
			//   camera rig
			//
			//   in the loop below, when grabbing in vr, the pseudo camera atom
			//   will always report as being the one that contains this
			//   transform because it handles the camera rig and cannot know
			//   that something is possessed
			//
			//   so if the atom found is the camera atom, just return the same
			//   body part for the current player instead (which might still be
			//   the camera anyway if there's no possession, but will be a
			//   different atom during possession)
			//
			// vr hands for touch
			//   the vr hands themselves are different from the anchors used
			//   when grabbing, but all the colliders are children of an object
			//   that has the HandOutput component, which is checked in
			//   VamCameraHand.ContainsTransform()
			//
			// possessed hands for touch
			//   this uses the actual hands of the possessed atoms, so they
			//   don't need special handling


			// start by looking in the cache
			{
				VamBodyPart bp;

				if (partMap_.TryGetValue(t, out bp))
					return bp.VamAtom.RealBodyPart(bp);
			}


			// not found, do a more expensive search


			// find the parent atom for this transform, used to stop going up
			// the transform's parent chain
			var stop = t.GetComponentInParent<Atom>()?.transform;

			var ps = Cue.Instance.ActivePersons;

			for (int i = 0; i < ps.Length; ++i)
			{
				var bp = (ps[i].Atom.Body as VamBasicBody)
					.BodyPartForTransform(t, stop, false);

				if (bp != null)
				{
					partMap_.Add(t, bp);
					return bp.VamAtom.RealBodyPart(bp);
				}
			}

			return null;
		}

		private IEnumerator DeferredInit()
		{
			yield return new WaitForEndOfFrame();
			Log.Verbose("running deferred init");
			deferredInit_?.Invoke();
			deferredInit_ = null;
		}

		private void OnSceneLoaded()
		{
			Log.Verbose("scene loaded, running deferred init on next frame");
			SuperController.singleton.onSceneLoadedHandlers -= OnSceneLoaded;
			SuperController.singleton.StartCoroutine(DeferredInit());
		}

		public void HardReset()
		{
			SuperController.singleton.HardReset();
		}

		public void ReloadPlugin()
		{
			var pui = GetPluginUI(CueMain.PluginCslist);
			if (pui == null)
				return;

			ClearLog();
			SuperController.LogError("reloading");
			pui.reloadButton?.onClick?.Invoke();
		}

		public void OpenScriptUI()
		{
			var pui = GetPluginUI(CueMain.PluginCslist);
			if (pui == null)
				return;

			var atomUI = script_.containingAtom.UITransform
				?.GetComponentInChildren<AtomUI>();

			if (atomUI == null)
			{
				SuperController.LogError("no AtomUI");
				return;
			}

			var tabs = atomUI.GetComponentInChildren<UITabSelector>();
			if (tabs == null)
			{
				SuperController.LogError("no UITabSelector");
				return;
			}

			tabs.SetActiveTab("Plugins");

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

			SuperController.singleton.gameMode = SuperController.GameMode.Edit;

			SuperController.singleton.SelectController(
				script_.containingAtom.mainController);

			scui.openUIButton?.onClick?.Invoke();
		}

		public MVRPluginUI GetPluginUI(string pluginCslist)
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
				if (pui.urlText.text.Contains(pluginCslist))
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
				Log.Error("failed to read '" + path + "', " + e.Message);
				return "";
			}
		}

		public string GetResourcePath(string path)
		{
			if (pluginPath_ == "")
				pluginPath_ = CueMain.Instance.PluginPath;

			if (path.StartsWith("/"))
				return pluginPath_ + "/res" + path;
			else
				return pluginPath_ + "/res/" + path;
		}

		public List<FileInfo> GetFiles(string path, string pattern)
		{
			if (PluginPathRoot == null)
				PluginPathRoot = CueMain.Instance.PluginPath.Replace('\\', '/');

			var list = new List<SysFileInfo>();

			GetFilenames(list, PluginDataRoot + path, pattern);
			GetFilenames(list, PluginPathRoot + "/res/" + path, pattern);

			for (int i = 0; i < list.Count; ++i)
				list[i].path = list[i].path.Replace('\\', '/');

			list.Sort((a, b) =>
			{
				if (a.source < b.source)
					return -1;
				else if (a.source > b.source)
					return 1;
				else
					return a.path.CompareTo(b.path);
			});

			var ret = new List<FileInfo>();

			foreach (var f in list)
				ret.Add(new FileInfo(f.path, PathSourceToString(f.source)));

			return ret;
		}

		private string PathSourceToString(int i)
		{
			switch (i)
			{
				case PluginDataSource: return "PluginData";
				case PluginPathSource: return "Scripts";
				case AddonSource: return "";
				default: return "";
			}
		}

		private int PathSource(string path)
		{
			if (path.StartsWith(PluginDataRoot, StringComparison.OrdinalIgnoreCase))
				return PluginDataSource;
			else if (path.StartsWith(PluginPathRoot, StringComparison.OrdinalIgnoreCase))
				return PluginPathSource;
			else if (path.StartsWith("AddonPackages/", StringComparison.OrdinalIgnoreCase))
				return AddonSource;
			else
				return UnknownSource;
		}

		private void GetFilenames(List<SysFileInfo> list, string path, string pattern)
		{
			var scs = FileManagerSecure.GetShortCutsForDirectory(path);

			foreach (var s in scs)
			{
				var fs = FileManagerSecure.GetFiles(s.path, pattern);
				if (fs.Length == 0)
					continue;

				foreach (var f in fs)
				{
					var fi = new SysFileInfo();

					fi.path = f;
					fi.source = PathSource(f);

					list.Add(fi);
				}
			}
		}

		private string PluginDataPath
		{
			get { return "Custom\\PluginData\\Cue"; }
		}

		public void SaveFileDialog(string ext, Action<string> f)
		{
			FileManagerSecure.CreateDirectory(PluginDataPath);
			var shortcuts = FileManagerSecure.GetShortCutsForDirectory(
				PluginDataPath);

			SuperController.singleton.GetMediaPathDialog(
				(string path) =>
				{
					if (string.IsNullOrEmpty(path))
					{
						f(null);
						return;
					}

					if (!path.Contains("."))
						path += "." + ext;

					f(path);
				},
				ext, PluginDataPath, true, true, false, null, false, shortcuts);

			var browser = SuperController.singleton.mediaFileBrowserUI;
			browser.SetTextEntry(true);
			browser.ActivateFileNameField();
		}

		public void LoadFileDialog(string ext, Action<string> f)
		{
			FileManagerSecure.CreateDirectory(PluginDataPath);
			var shortcuts = FileManagerSecure.GetShortCutsForDirectory(
				PluginDataPath);

			SuperController.singleton.GetMediaPathDialog(
				(string path) =>
				{
					if (string.IsNullOrEmpty(path))
					{
						f(null);
						return;
					}

					f(path);
				},
				ext, PluginDataPath, true, true, false, null, false, shortcuts);
		}

		public JSONNode ReadJSON(string path)
		{
			return SuperController.singleton.LoadJSON(path);
		}

		public void WriteJSON(string path, JSONNode content)
		{
			SuperController.singleton.SaveJSON(content.AsObject, path);
		}

		public IActionTrigger CreateActionTrigger()
		{
			return new ActionTrigger();
		}

		public IActionTrigger LoadActionTrigger(JSONNode n)
		{
			var t = new ActionTrigger();
			t.RestoreFromJSON(n.AsObject);
			return t;
		}
	}


	public class VamPrefabFactory : MonoBehaviour
	{
		private static bool loading_ = false;
		private static bool loaded_ = false;

		public static RectTransform triggerActionsPrefab;
		public static RectTransform triggerActionMiniPrefab;
		public static RectTransform triggerActionDiscretePrefab;
		public static RectTransform triggerActionTransitionPrefab;
		public static RectTransform scrollbarPrefab;
		public static RectTransform buttonPrefab;

		public static IEnumerator LoadUIAssets(Action onReady)
		{
			if (loading_)
				yield break;

			if (!loaded_)
			{
				loading_ = true;
				foreach (var x in LoadUIAsset("z_ui2", "TriggerActionsPanel", prefab => triggerActionsPrefab = prefab)) yield return x;
				foreach (var x in LoadUIAsset("z_ui2", "TriggerActionMiniPanel", prefab => triggerActionMiniPrefab = prefab)) yield return x;
				foreach (var x in LoadUIAsset("z_ui2", "TriggerActionDiscretePanel", prefab => triggerActionDiscretePrefab = prefab)) yield return x;
				foreach (var x in LoadUIAsset("z_ui2", "TriggerActionTransitionPanel", prefab => triggerActionTransitionPrefab = prefab)) yield return x;
				foreach (var x in LoadUIAsset("z_ui2", "DynamicTextField", prefab => scrollbarPrefab = prefab.GetComponentInChildren<ScrollRect>().verticalScrollbar.gameObject.GetComponent<RectTransform>())) yield return x;
				foreach (var x in LoadUIAsset("z_ui2", "DynamicButton", prefab => buttonPrefab = prefab)) yield return x;
				loading_ = false;
				loaded_ = true;
			}

			onReady();
		}

		private static IEnumerable LoadUIAsset(string assetBundleName, string assetName, Action<RectTransform> assign)
		{
			var request = AssetBundleManager.LoadAssetAsync(assetBundleName, assetName, typeof(GameObject));
			if (request == null) throw new NullReferenceException($"Request for {assetName} in {assetBundleName} assetbundle failed: Null request.");
			yield return request;
			var go = request.GetAsset<GameObject>();
			if (go == null) throw new NullReferenceException($"Request for {assetName} in {assetBundleName} assetbundle failed: Null GameObject.");
			var prefab = go.GetComponent<RectTransform>();
			if (prefab == null) throw new NullReferenceException($"Request for {assetName} in {assetBundleName} assetbundle failed: Null RectTransform.");
			assign(prefab);
		}
	}


	class TH : MonoBehaviour, TriggerHandler
	{
		void TriggerHandler.RemoveTrigger(Trigger t)
		{
			throw new NotImplementedException();
		}

		void TriggerHandler.DuplicateTrigger(Trigger t)
		{
			throw new NotImplementedException();
		}

		RectTransform TriggerHandler.CreateTriggerActionsUI()
		{
			return Instantiate(VamPrefabFactory.triggerActionsPrefab);
		}

		RectTransform TriggerHandler.CreateTriggerActionMiniUI()
		{
			return Instantiate(VamPrefabFactory.triggerActionMiniPrefab);
		}

		RectTransform TriggerHandler.CreateTriggerActionDiscreteUI()
		{
			return Instantiate(VamPrefabFactory.triggerActionDiscretePrefab);
		}

		RectTransform TriggerHandler.CreateTriggerActionTransitionUI()
		{
			return Instantiate(VamPrefabFactory.triggerActionTransitionPrefab);
		}

		void TriggerHandler.RemoveTriggerActionUI(RectTransform rt)
		{
			if (rt != null) Destroy(rt.gameObject);
		}
	}


	class ActionTrigger : Trigger, IActionTrigger
	{
		private bool firstEdit_ = true;
		private Action closeHandler_ = null;

		public ActionTrigger()
		{
			handler = new TH();
		}

		public string Name
		{
			get { return displayName; }
			set { displayName = value; }
		}

		public void Edit(Action onDone = null)
		{
			closeHandler_ = onDone;
			SuperController.singleton.StartCoroutine(VamPrefabFactory.LoadUIAssets(OnPrefabsReady));
		}

		private void OnPrefabsReady()
		{
			Transform parent = CueMain.Instance.UITransform;

			triggerActionsParent = parent;
			InitTriggerUI();
			OpenTriggerActionsPanel();
			SetPanelParent(parent);

			if (firstEdit_)
			{
				firstEdit_ = false;
				closeTriggerActionsPanelButton.onClick.AddListener(OnClosed);
			}
		}

		public void Fire()
		{
			active = true;
			active = false;
		}

		private void OnClosed()
		{
			closeHandler_?.Invoke();
			closeHandler_ = null;
		}

		public JSONNode ToJSON()
		{
			return this.GetJSON();
		}

		private void SetPanelParent(Transform parent)
		{
			if (triggerActionsPanel != null)
			{
				triggerActionsPanel.SetParent(parent, false);
				triggerActionsPanel.SetAsLastSibling();
			}

			if (discreteActionsStart != null)
			{
				foreach (var start in discreteActionsStart)
				{
					if (start.triggerActionPanel != null)
					{
						start.triggerActionPanel.SetParent(parent, false);
						start.triggerActionPanel.SetAsLastSibling();
					}
				}
			}

			if (transitionActions != null)
			{
				foreach (var transition in transitionActions)
				{
					if (transition.triggerActionPanel != null)
					{
						transition.triggerActionPanel.SetParent(parent, false);
						transition.triggerActionPanel.SetAsLastSibling();
					}
				}
			}

			if (discreteActionsEnd != null)
			{
				foreach (var end in discreteActionsEnd)
				{
					if (end.triggerActionPanel != null)
					{
						end.triggerActionPanel.SetParent(parent, false);
						end.triggerActionPanel.SetAsLastSibling();
					}
				}
			}
		}
	}
}
