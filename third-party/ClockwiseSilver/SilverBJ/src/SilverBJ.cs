using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;
using MVR;
using MeshVR;
using Request = MeshVR.AssetLoader.AssetBundleFromFileRequest;
using static ClockwiseSilver.Helpers;

namespace ClockwiseSilver {
	public class BJ : MVRScript {
		
		private Atom her;
		private Atom him;
		private JSONStorableBool isActive, blink, isBJRoutine, gazeActive, hjActive;
		private JSONStorableFloat speedMultiJSON, rangeMultiJSON, frontBackSpeedJSON, frontBackRangeJSON, flipLimitJSON, randomTimeJSON;
		private JSONStorableFloat topOnlyChanceJSON, intensityVolumeScaleJSON, intensityChanceScaleJSON;
		private JSONStorableFloat headShiftXJSON, headShiftYJSON, headShiftZJSON;
		private JSONStorableFloat headAngleXMinJSON, headAngleYMinJSON, headAngleZMinJSON;
		private JSONStorableFloat headAngleXMaxJSON, headAngleYMaxJSON, headAngleZMaxJSON;
		private JSONStorableFloat holdSpringPosJSON, holdSpringRotJSON;
		private JSONStorableFloat minSpeedJSON, maxSpeedJSON, minRangeJSON, maxRangeJSON;
		private JSONStorableStringChooser maleChooserJSON;
		private JSONStorableString infoString;
		private UIDynamicTextField infoTextField;
		
		private float startRange, startSpeed, targetRange, targetSpeed, slerpScale = 0f;
		private float frontBackTarget, frontBackCurrent, currentVolume, currentChance = 0f;
		private float randomTimer = 1f;
		private float fbRangeMax, genSegmentLength = 0.2f;
		private float minIntensity, maxIntensity, intensityScale = 0f;
		private int currentIntensity, lastMoanIndex, lastSFXIndex = 0;
		private bool topOnly, isPen, flipped, bjRunning, hasGaze, hasHJ, startedOnLoad = false;
		
		private Transform headControlTransform, headRBTransform, chestRBTransform, lipTriggerTransform, gen1Transform, gen2Transform, gen3Transform;
		private Rigidbody gen1, gen2, gen3;
        
		private Vector3 headShift, headAngle, headAngleStart, headAngleTarget = Vector3.zero;
		private Vector3 startTargetPos, startPoshead, tarPosBegin = Vector3.zero;
		private Quaternion startTargetRot, startRothead, tarRotBegin = Quaternion.identity;		
		private FreeControllerV3 penisBase, headControl;
		private Quaternion targetRot = Quaternion.identity;
		
		private GenerateDAZMorphsControlUI morphControl;
		private DAZMorph mouthOpenWide, eyesClosed;
		private float mouthOpenWideTarget = 0.8f;
		private float eyesClosedMax, morphSpeed, eyesClosedTimer, morphTimer = 1.0f;
		private float eyesClosedTarget = 0.4f;
		private float eyesClosedSpeed = 10.0f;
		private bool eyesOpen = true;
		private JSONStorableFloat eyesClosedMinJSON, lipsMaxJSON, mouthOpenWideMaxJSON, eyesCloseTimeJSON, eyesOpenTimeJSON, morphTimeJSON;
		private DAZMorph[] morphsLip;
		private float[] targetsLip;
		
		private FreeControllerV3.RotationState wasHeadRotState, wasHeadRotState2 = FreeControllerV3.RotationState.On;
		private FreeControllerV3.PositionState wasHeadPosState, wasHeadPosState2 = FreeControllerV3.PositionState.On;

		private AudioClip[] audioClipsSFX, audioClipsMoan = new AudioClip[0];
		private List<AudioClip>[] sfxLists = new List<AudioClip>[6];
		private List<AudioClip>[] moanLists = new List<AudioClip>[6];
		private Atom audioSFXAtom, audioMoanAtom;
		private FreeControllerV3 audioSFXControl, audioMoanControl;
		private AudioSourceControl audioSFXSource, audioMoanSource;
		private Transform audioSFXTransform, audioMoanTransform;
		private JSONStorableFloat audioSFXVolumeJSON, audioMoanVolumeJSON, audioMoanPitchJSON, audioMoanChanceJSON;
		private bool audioMoanLoaded, audioSFXLoaded, bundleRequestComplete = false;
		
//-----------------------------------------------------------Init---------------------------------
		
		public override void Init() {
			try
			{				
				her = containingAtom;
				headControl = her.freeControllers.First(fc => fc.name == "headControl");			
				headControlTransform = headControl.transform;			
				JSONStorable geometry = her.GetStorableByID("geometry");
				DAZCharacterSelector character = geometry as DAZCharacterSelector;
				morphControl = character.morphsControlUI;				
				mouthOpenWide = morphControl.GetMorphByDisplayName("Mouth Open Wide");
				eyesClosed = morphControl.GetMorphByDisplayName("Eyes Closed");
				
				morphsLip = new DAZMorph[3];
				morphsLip[0] = morphControl.GetMorphByDisplayName("Lips Pucker Wide");
				morphsLip[1] = morphControl.GetMorphByDisplayName("Lips Part");
				morphsLip[2] = morphControl.GetMorphByDisplayName("Mouth Smile Simple");
				targetsLip = new float[morphsLip.Length];
				 
				blink = her.GetStorableByID("EyelidControl").GetBoolJSONParam("blinkEnabled");
				
				isActive = new JSONStorableBool ("isActive", false);
				RegisterBool (isActive);
				var toggle = CreateToggle(isActive);
				toggle.label = "Active";
				
				JSONStorableAction ToggleActive = new JSONStorableAction("Toggle BJ", () =>
                {
                    isActive.val = !isActive.val;
                });
                RegisterAction(ToggleActive);
				
				isBJRoutine = new JSONStorableBool("isBJRoutine", false);
				RegisterBool(isBJRoutine);

//------------------------------------UI----------------------------------------------------------
				maleChooserJSON = new JSONStorableStringChooser("Atom", null, null, "Male");                
                RegisterStringChooser(maleChooserJSON);
                UIDynamicPopup dp = CreateScrollablePopup(maleChooserJSON, false);
                dp.popupPanelHeight = 600f;
                dp.popup.onOpenPopupHandlers += () => {
						maleChooserJSON.choices = Helpers.GetMaleAndToyChoices();
					};
				
				infoString = new JSONStorableString("infoString", "");
				RegisterString(infoString);
				infoTextField = CreateTextField(infoString, true);
				infoTextField.height = 140;				
				headShiftXJSON = Helpers.SetupSlider(this, "Head Side/Side", 0f, -0.15f, 0.15f, true);
                headShiftYJSON = Helpers.SetupSlider(this, "Head Up/Down", 0.01f, -0.15f, 0.15f, true);
				headShiftZJSON = Helpers.SetupSlider(this, "Head Fwd/Bkwd", 0.01f, -0.15f, 0.15f, true);
				headAngleXMinJSON = Helpers.SetupSlider(this, "Head Angle X Min", -55f, -120f, 120f, true);
				headAngleXMaxJSON = Helpers.SetupSlider(this, "Head Angle X Max", -40f, -120f, 120f, true);
                headAngleYMinJSON = Helpers.SetupSlider(this, "Head Angle Y Min", -10f, -120f, 120f, true);
				headAngleYMaxJSON = Helpers.SetupSlider(this, "Head Angle Y Max", 10f, -120f, 120f, true);				
				headAngleZMinJSON = Helpers.SetupSlider(this, "Head Angle Z Min", -20f, -120f, 120f, true);
				headAngleZMaxJSON = Helpers.SetupSlider(this, "Head Angle Z Max", 20f, -120f, 120f, true);
				
				var heading = CreateTextField(new JSONStorableString("header4", "Time between random variations\n"
					+ "Head speed and range multipliers"));
                heading.height = 10f;				
				randomTimeJSON = Helpers.SetupSlider(this, "Random Time", 7f, 0f, 20.0f, false);	
				speedMultiJSON = Helpers.SetupSlider(this, "Overall Speed", 1f, 0.2f, 1.2f, false);
				rangeMultiJSON = Helpers.SetupSlider(this, "Overall Range", 1f, 0.2f, 1.2f, false);
				
				heading = CreateTextField(new JSONStorableString("headerAudio", "Audio"));
                heading.height = 10f;
				audioSFXVolumeJSON = Helpers.SetupSlider(this, "SFX Volume", 0.45f, 0f, 1.0f, false);
				audioMoanVolumeJSON = Helpers.SetupSlider(this, "Moan Volume", 0.45f, 0f, 1.0f, false);
				intensityVolumeScaleJSON = Helpers.SetupSlider(this, "Volume Scaling", 0.045f, 0f, 0.1f, false);
				audioMoanPitchJSON = Helpers.SetupSlider(this, "Moan Pitch", 1f, 0.75f, 1.25f, false);
				audioMoanChanceJSON = Helpers.SetupSlider(this, "Moan Chance", 0.45f, 0f, 1f, false);
				intensityChanceScaleJSON = Helpers.SetupSlider(this, "Chance Scaling", 0.08f, 0f, 0.17f, false);
				
				heading = CreateTextField(new JSONStorableString("header5", "Advanced Options (Some Automated)"
					+ "\nMin and Max Speed determine Intensity Range"));
                heading.height = 10f;
				frontBackSpeedJSON = Helpers.SetupSlider(this, "Speed", 1f, 0.8f, 5.0f, false);
				minSpeedJSON = Helpers.SetupSlider(this, "Speed Min", 0.65f, 0.5f, 1.4f, false);
				maxSpeedJSON = Helpers.SetupSlider(this, "Speed Max", 3f, 1.5f, 3.15f, false);
				frontBackRangeJSON = Helpers.SetupSlider(this, "Range", 1.0f, 0.5f, 1.2f, false);
				minRangeJSON = Helpers.SetupSlider(this, "Range Min", 0.6f, 0.5f, 0.79f, false);
				maxRangeJSON = Helpers.SetupSlider(this, "Range Max", 1.1f, 0.8f, 1.2f, false);
				flipLimitJSON = Helpers.SetupSlider(this, "Flip Limit", 0.995f, 0.98f, 0.9999f, false);
				topOnlyChanceJSON = Helpers.SetupSlider(this, "Top Only Chance", 0f, 0f, 1f, false);
				
				heading = CreateTextField(new JSONStorableString("header1_2", "Eyes and Lips"),true);
                heading.height = 10f;
				eyesClosedMinJSON = Helpers.SetupSlider(this, "Eyes Closed Min", 0.25f, 0f, 1f, true);
				eyesOpenTimeJSON = Helpers.SetupSlider(this, "Eyes Open Time", 5.5f, 0.05f, 20f, true);
				eyesCloseTimeJSON = Helpers.SetupSlider(this, "Eyes Closed Time", 8f, 0.05f, 20f, true);
				lipsMaxJSON = Helpers.SetupSlider(this, "Lip Morph Max", 0.5f, 0f, 1f, true);
				mouthOpenWideMaxJSON = Helpers.SetupSlider(this, "Mouth Open Max", 0.8f, 0.1f, 1f, true);
				morphTimeJSON = Helpers.SetupSlider(this, "Morph Duration", 0.75f, 0.05f, 2.0f, true);
				
				heading = CreateTextField(new JSONStorableString("header3_2", "Hold Spring\nReactivate to see changes"),true);
                heading.height = 10f;				
				holdSpringPosJSON = Helpers.SetupSlider(this, "Hold Spring Pos", 4000f, 2000f, 10000f, true);				
				holdSpringRotJSON = Helpers.SetupSlider(this, "Hold Spring Rot", 400f, 200f, 1000f, true);
			}
			catch (Exception e) {
				SuperController.LogError("Init Exception caught: " + e);
			}
		}
		
		//------------------------------------------------------Start---------------------------		
		private void Start()
		{
			maleChooserJSON.choices = Helpers.GetMaleAndToyChoices();
			StartCoroutine("StartRoutine");
		}
		
		private IEnumerator StartRoutine()
		{
			yield return null;
			
			infoString.val = "SilverBJ";
			
			//Set Listeners
			isActive.toggle.onValueChanged.AddListener(checkVal =>
				{
					if (checkVal) { StartBJ(); }
					else { StopBJ(); }
				});
			
			headAngleXMinJSON.slider.onValueChanged.AddListener((value) => { if (headAngleXMaxJSON.val < headAngleXMinJSON.val) {headAngleXMaxJSON.val = headAngleXMinJSON.val;}; });
			headAngleYMinJSON.slider.onValueChanged.AddListener((value) => { if (headAngleYMaxJSON.val < headAngleYMinJSON.val) {headAngleYMaxJSON.val = headAngleYMinJSON.val;}; });
			headAngleZMinJSON.slider.onValueChanged.AddListener((value) => { if (headAngleZMaxJSON.val < headAngleZMinJSON.val) {headAngleZMaxJSON.val = headAngleZMinJSON.val;}; });
			headAngleXMaxJSON.slider.onValueChanged.AddListener((value) => { if (headAngleXMaxJSON.val < headAngleXMinJSON.val) {headAngleXMinJSON.val = headAngleXMaxJSON.val;}; });
			headAngleYMaxJSON.slider.onValueChanged.AddListener((value) => { if (headAngleYMaxJSON.val < headAngleYMinJSON.val) {headAngleYMinJSON.val = headAngleYMaxJSON.val;}; });
			headAngleZMaxJSON.slider.onValueChanged.AddListener((value) => { if (headAngleZMaxJSON.val < headAngleZMinJSON.val) {headAngleZMinJSON.val = headAngleZMaxJSON.val;}; });
			
			yield return new WaitForSeconds(1f);
			
			//Get AudioClipsMoan
			bundleRequestComplete = false;
			string audioPath = Helpers.GetPluginPath(this) + "/audio/silverbj_miuwa_moans.audiobundle";
			Request request = new AssetLoader.AssetBundleFromFileRequest
				{ path = audioPath, callback = _ => { bundleRequestComplete = true; }};
			AssetLoader.QueueLoadAssetBundleFromFile(request);
			yield return new WaitForSeconds(0.15f);
			float elapsed = 0f;
			while (!bundleRequestComplete && elapsed < 15f)
			{
				elapsed += Time.deltaTime;
				yield return null;
			}
			audioMoanLoaded = true;
			if (request != null && request.assetBundle)
			{
				audioClipsMoan = request.assetBundle.LoadAllAssets<AudioClip>();
			}
			else
			{
				audioMoanLoaded = false;
				AssetLoader.DoneWithAssetBundleFromFile(request.path);
			}
			string atomName = "";
			if (audioClipsMoan == null || audioClipsMoan.Length == 0)
			{
				SuperController.LogMessage("SilverBJ: No Moan AudioClips found.");
				audioMoanLoaded = false;
			}
			else
			{
				atomName = "Audio_BJMoan_" + containingAtom.uid;
				audioMoanAtom = SuperController.singleton.GetAtomByUid(atomName);
				yield return null;
				if (audioMoanAtom == null)
				{
					yield return SuperController.singleton.AddAtomByType("AudioSource", atomName);
					audioMoanAtom = SuperController.singleton.GetAtomByUid(atomName);
				}
				audioMoanAtom.hidden = true;
				audioMoanControl = audioMoanAtom.freeControllers.First(fc => fc.name == "control");
				audioMoanControl.canGrabPosition = false;
				audioMoanControl.canGrabRotation = false;
				audioMoanTransform = audioMoanControl.transform;
				audioMoanSource = audioMoanAtom.GetStorableByID("AudioSource") as AudioSourceControl;
				if (audioMoanSource == null)
				{
					SuperController.LogMessage("SilverBJ: AudioSourceControl not found for Audio_BJMoan_" + containingAtom.uid);
					audioMoanLoaded = false;
				}
			}
			if (audioMoanLoaded)
			{
				for (int i = 0; i < moanLists.Length; i++) { moanLists[i] = new List<AudioClip>(); }
				foreach (AudioClip clip in audioClipsMoan)
				{
					string[] splitName = clip.name.ToLower().Split('i');
					if (splitName.Length == 0)
					{
						SuperController.LogError("SilverBJ: Unable to split " + clip.name);
						continue;
					}
					
					int iValue = -1;
					if (System.Int32.TryParse(splitName[1][0].ToString(), out iValue))
					{
						//SuperController.LogMessage("SilverBJ: " + clip.name + " has iValue " + iValue.ToString());
						//if (iValue > 6)
						//{
						//	SuperController.LogMessage("SilverBJ: -------------- > 6 !!!!!!!!!!!!!!");
						//}
						moanLists[iValue-1].Add(clip);
					}
					else
					{
						SuperController.LogError("SilverBJ: Unable to parse " + clip.name + " value " + splitName[1][0].ToString());
					}
				}
				infoString.val += "\nMoan Audio Loaded";
			}
			
			//Get AudioClipsSFX
			bundleRequestComplete = false;
			audioPath = Helpers.GetPluginPath(this) + "/audio/silverbj_miuwa_sfx.audiobundle";
			request = new AssetLoader.AssetBundleFromFileRequest
				{ path = audioPath, callback = _ => { bundleRequestComplete = true; }};
			AssetLoader.QueueLoadAssetBundleFromFile(request);
			yield return new WaitForSeconds(0.15f);
			elapsed = 0f;
			while (!bundleRequestComplete && elapsed < 15f)
			{
				elapsed += Time.deltaTime;
				yield return null;
			}
			audioSFXLoaded = true;
			if (request != null && request.assetBundle)
			{
				audioClipsSFX = request.assetBundle.LoadAllAssets<AudioClip>();
			}
			else
			{
				audioSFXLoaded = false;
				AssetLoader.DoneWithAssetBundleFromFile(request.path);
			}
			
			if (audioClipsSFX == null || audioClipsSFX.Length == 0)
			{
				SuperController.LogMessage("SilverBJ: No SFX AudioClips found.");
				audioSFXLoaded = false;
			}
			else
			{
				atomName = "Audio_BJSFX_" + containingAtom.uid;
				audioSFXAtom = SuperController.singleton.GetAtomByUid(atomName);
				yield return null;
				if (audioSFXAtom == null)
				{
					yield return SuperController.singleton.AddAtomByType("AudioSource", atomName);
					audioSFXAtom = SuperController.singleton.GetAtomByUid(atomName);
				}
				audioSFXAtom.hidden = true;
				audioSFXControl = audioSFXAtom.freeControllers.First(fc => fc.name == "control");
				audioSFXControl.canGrabPosition = false;
				audioSFXControl.canGrabRotation = false;
				audioSFXTransform = audioSFXControl.transform;
				audioSFXSource = audioSFXAtom.GetStorableByID("AudioSource") as AudioSourceControl;
				if (audioSFXSource == null)
				{
					SuperController.LogMessage("SilverBJ: AudioSourceControl not found for Audio_BJSFX_" + containingAtom.uid);
					audioSFXLoaded = false;
				}
			}
			if (audioSFXLoaded)
			{
				for (int i = 0; i < sfxLists.Length; i++) { sfxLists[i] = new List<AudioClip>(); }
				foreach (AudioClip clip in audioClipsSFX)
				{
					string[] splitName = clip.name.ToLower().Split('i');
					if (splitName.Length == 0)
					{
						SuperController.LogError("SilverBJ: Unable to split " + clip.name);
						continue;
					}
					
					int iValue = -1;
					if (System.Int32.TryParse(splitName[1][0].ToString(), out iValue))
					{
						//SuperController.LogMessage("SilverBJ: " + clip.name + " has iValue " + iValue.ToString());
						//if (iValue > 6)
						//{
						//	SuperController.LogMessage("SilverBJ: -------------- > 6 !!!!!!!!!!!!!!");
						//}
						sfxLists[iValue-1].Add(clip);
					}
					else
					{
						SuperController.LogError("SilverBJ: Unable to parse " + clip.name + " value " + splitName[1][0].ToString());
					}
				}
				infoString.val += "\nSFX Audio Loaded";
			}
			
			minIntensity = minSpeedJSON.defaultVal;
			maxIntensity = maxSpeedJSON.val;
			currentIntensity = 0;
			intensityScale = (1f / (maxIntensity - minIntensity)) * 6;
			
			if (isActive.val && !bjRunning) { startedOnLoad = true; StartBJ(); }
		}
		
		//-=-=-=-=-=-=-BIG RANDOM-=-=-=-=-=-=-=-=-=-=-=-=-BIG RANDOM-=-=-=-=-=-=-=-=-=-=-=-=-BIG RANDOM-=-=-=-=-=-=-
		
		private void BigRandom()
		{
			UnityEngine.Random.InitState((int)Time.time);
			float rt = randomTimeJSON.val * UnityEngine.Random.Range(0.8f, 1.2f);
			randomTimer = rt;
			if (randomTimer <= 0f) { randomTimer = 4f; }
			slerpScale = 1f / randomTimer;
			if (hasHJ && hjActive.val)
			{
				topOnly = true;
			}
			else { topOnly = UnityEngine.Random.Range(0f, 1f) <= topOnlyChanceJSON.val; }
			startRange = frontBackRangeJSON.val;
			startSpeed = frontBackSpeedJSON.val;
			headAngleStart = headAngle;
			targetSpeed = UnityEngine.Random.Range(minSpeedJSON.val, maxSpeedJSON.val) * speedMultiJSON.val;
			targetRange = UnityEngine.Random.Range(minRangeJSON.val, maxRangeJSON.val) * rangeMultiJSON.val;
			currentIntensity = (int)((targetSpeed - minIntensity) * intensityScale);
			if (currentIntensity > 5) { currentIntensity = 5; }
			currentChance = currentIntensity * intensityChanceScaleJSON.val;
			currentVolume = currentIntensity * intensityVolumeScaleJSON.val;
			
			headAngleTarget.x = UnityEngine.Random.Range(headAngleXMinJSON.val, headAngleXMaxJSON.val);
			headAngleTarget.y = UnityEngine.Random.Range(headAngleYMinJSON.val, headAngleYMaxJSON.val);
			headAngleTarget.z = UnityEngine.Random.Range(headAngleZMinJSON.val, headAngleZMaxJSON.val);
			
			infoString.val = "Current Intensity: " + (currentIntensity + 1).ToString()
				+ "\nSpeed: " + targetSpeed.ToString("F3")+ "  Range: " + targetRange.ToString("F3")
				+ "\nSFX Vol: " + (audioSFXVolumeJSON.val + currentVolume).ToString("F3")
				+ "  Moan Vol: " + (audioMoanVolumeJSON.val + currentVolume).ToString("F3")
				+ "\nMoan Chance: " + (audioMoanChanceJSON.val + currentChance).ToString("F3");
		}
		
		protected void SetMorphTargets()
		{
			mouthOpenWideTarget = mouthOpenWideMaxJSON.val;
			for (int i = 0; i < morphsLip.Length; i++)
			{
				targetsLip[i] = UnityEngine.Random.Range(0f, lipsMaxJSON.val);
			}
		}
		
		private void PlayRandomAudioMoan()
		{
			if (!audioMoanLoaded || audioMoanSource.playingClip != null || audioMoanVolumeJSON.val == 0f) { return; }
			if (UnityEngine.Random.Range(0f, 1f) > audioMoanChanceJSON.val + currentChance) { return; }
			audioMoanSource.volume = (audioMoanVolumeJSON.val + currentVolume) * UnityEngine.Random.Range(0.9f, 1.0f);
			audioMoanSource.pitch = audioMoanPitchJSON.val * UnityEngine.Random.Range(0.98f, 1.02f);
			List<AudioClip> al = moanLists[currentIntensity];
			int clipIndex = (int)UnityEngine.Random.Range(0, al.Count);
			if (clipIndex == lastMoanIndex)
			{
				UnityEngine.Random.InitState((int)Time.time);
				clipIndex = (int)UnityEngine.Random.Range(0, al.Count);
			}
			if (clipIndex >= al.Count) { clipIndex = 0; }
			audioMoanSource.audioSource.PlayOneShot(al[clipIndex]);
			lastMoanIndex = clipIndex;
		}
		
		private void PlayRandomAudioSFX()
		{
			if (!audioSFXLoaded || audioSFXSource.playingClip != null || audioSFXVolumeJSON.val == 0f) { return; }
			audioSFXSource.volume = (audioSFXVolumeJSON.val + currentVolume) * UnityEngine.Random.Range(0.9f, 1.0f);
			audioSFXSource.pitch = UnityEngine.Random.Range(0.98f, 1.05f);
			List<AudioClip> al = sfxLists[currentIntensity];
			int clipIndex = (int)UnityEngine.Random.Range(0, al.Count);
			if (clipIndex == lastSFXIndex)
			{
				UnityEngine.Random.InitState((int)Time.time);
				clipIndex = (int)UnityEngine.Random.Range(0, al.Count);
			}
			if (clipIndex >= al.Count) { clipIndex = 0; }
			audioSFXSource.audioSource.PlayOneShot(al[clipIndex]);
			lastSFXIndex = clipIndex;
		}
		
		private void SyncMale()
		{
			if (maleChooserJSON.val == null || maleChooserJSON.val == "") { return; }			
			try
			{
				him = SuperController.singleton.GetAtomByUid(maleChooserJSON.val);				
				if (maleChooserJSON.val.Contains("Dildo"))
				{
					penisBase = him.freeControllers.First(fc => fc.name == "control");
					gen1 = him.rigidbodies.First(rb => rb.name == "b1");
					gen2 = him.rigidbodies.First(rb => rb.name == "b2");
					gen3 = him.rigidbodies.First(rb => rb.name == "b3");
					isPen = false;
				}
				else if (maleChooserJSON.val.Contains("Cock"))
				{
					penisBase = him.freeControllers.First(fc => fc.name == "control");					
					Rigidbody[] rbs = him.gameObject.GetComponentsInChildren<Rigidbody>();					
					foreach (Rigidbody rb in rbs)
					{
						if (rb.gameObject.name == "b1") { gen1 = rb; }
						else if (rb.gameObject.name == "b2") { gen2 = rb; }
						else if (rb.gameObject.name == "b3") { gen3 = rb; }
					}					
					isPen = false;
				}
				else
				{
					penisBase = him.freeControllers.First(fc => fc.name == "penisBaseControl");
					gen1 = him.rigidbodies.First(rb => rb.name == "Gen1");
					gen2 = him.rigidbodies.First(rb => rb.name == "Gen2");
					gen3 = him.rigidbodies.First(rb => rb.name == "Gen3");
					isPen = true;
				}				
				gen1Transform = gen1.transform; gen2Transform = gen2.transform; gen3Transform = gen3.transform;				
				fbRangeMax = Vector3.Distance(gen1Transform.position, gen3Transform.position);
				genSegmentLength = Vector3.Distance(gen2Transform.position, gen3Transform.position);
			}
			catch (Exception e) { SuperController.LogError("SyncMale Exception caught: " + e); }
		}
		
		private void SyncFemale()
		{
			headControl = her.freeControllers.First(fc => fc.name == "headControl");
			headControlTransform = headControl.transform;
			lipTriggerTransform = her.rigidbodies.First(rb => rb.name == "LipTrigger").transform;
			headRBTransform = her.rigidbodies.First(rb => rb.name == "head").transform;
			chestRBTransform = her.rigidbodies.First(rb => rb.name == "chest").transform;
		}
		
		//----------------------------------------BJ----------------------------------------------
		private void StartBJ()
		{
			SyncFemale();
			if (her == null) { SuperController.LogError("Missing BJ Giver"); return; }
			SyncMale();
			if (him == null) { SuperController.LogError("Missing BJ Receiver"); return; }			
			if (isBJRoutine.val) { SuperController.LogError("BJ Already Running"); bjRunning = false; return; }
			
			StartCoroutine("BJRoutine");
		}
		
		private void StopBJ()
		{
			bjRunning = false;
		}
		
		private IEnumerator BJRoutine()
		{
			isBJRoutine.val = true; bjRunning = true;
			
			//--------------------------------Silver Gaze
			JSONStorable gazePlugin = Helpers.FindPlugin(this, "ClockwiseSilver.Gaze");
			bool wasGaze = false;
			if (gazePlugin != null)
			{
				hasGaze = true;
				gazeActive = gazePlugin.GetBoolJSONParam("isActive");
				if (gazeActive != null && gazeActive.val)
				{
					wasGaze = true;
					gazeActive.val = false;
					yield return null;
				}
			}
			
			//--------------------------------Silver HJ
			JSONStorable hjPlugin = Helpers.FindPlugin(this, "ClockwiseSilver.HJ");
			JSONStorableFloat hjFrontBack = null;
			if (hjPlugin != null)
			{
				hjActive = hjPlugin.GetBoolJSONParam("isActive");
				if (hjActive != null)
				{
					hasHJ = true;
					hjFrontBack = hjPlugin.GetFloatJSONParam("frontBackOutput");
					flipLimitJSON.val = hjPlugin.GetFloatJSONParam("Flip Limit").val;
				}
			}
			
			wasHeadPosState = headControl.currentPositionState;
			wasHeadRotState = headControl.currentRotationState;
			headControl.currentPositionState = FreeControllerV3.PositionState.On;
			headControl.currentRotationState = FreeControllerV3.RotationState.On;
			startRothead = headControlTransform.rotation;
			if (startedOnLoad)
			{
				startPoshead = gen3Transform.position
				+ (gen3Transform.position - gen2Transform.position) * 2
				+ headRBTransform.position - lipTriggerTransform.position
				+ gen1Transform.TransformVector(headShift);
			}
			else { startPoshead = headControlTransform.position; }
			
			headShift.x = headShiftXJSON.val; headShift.y = headShiftYJSON.val; headShift.z = headShiftZJSON.val;			
			frontBackCurrent = 0f;			
			tarPosBegin = headControlTransform.position;
			tarRotBegin = headControlTransform.rotation;		
			headControl.RBHoldPositionSpring = holdSpringPosJSON.val;
            headControl.RBHoldRotationSpring = holdSpringRotJSON.val;			
			fbRangeMax = Vector3.Distance(gen1Transform.position, gen3Transform.position);
			
			bool wasBlink = blink.val;
			float giveUpTimer = 0f; float dTime = Time.fixedDeltaTime;
			SetMorphTargets();
			morphTimer = morphTimeJSON.val;
			eyesClosedTimer = eyesOpen ? eyesOpenTimeJSON.val : eyesCloseTimeJSON.val;
			startTargetPos = gen3Transform.position
				+ headRBTransform.position - lipTriggerTransform.position
				+ gen1Transform.TransformVector(headShift);

			//--------------------------------------------------Begin----------------------
			
			while (bjRunning && giveUpTimer < 5f && Vector3.Distance(headControlTransform.position, startTargetPos) > 0.001f)
			{
				dTime = Time.fixedDeltaTime;
				float dMorphTime = dTime * 20;
				mouthOpenWide.morphValue = Mathf.Lerp(mouthOpenWide.morphValue, mouthOpenWideTarget, dMorphTime);
				
				headShift.x = headShiftXJSON.val; headShift.y = headShiftYJSON.val; headShift.z = 0f;
				startTargetPos = gen3Transform.position
					+ (gen3Transform.forward * genSegmentLength)
					+ headRBTransform.position - lipTriggerTransform.position
					+ gen1Transform.TransformVector(headShift);
				headControlTransform.position = Vector3.MoveTowards(headControlTransform.position, startTargetPos, dTime * 0.3f);
								
				headAngle.x = headAngleXMinJSON.val; headAngle.y = 0f; headAngle.z = 0f;				
				targetRot = Quaternion.LookRotation(gen3Transform.position - lipTriggerTransform.position, headRBTransform.up) * Quaternion.Euler(headAngle);
				headControlTransform.rotation =	Quaternion.RotateTowards(headControlTransform.rotation, targetRot, dTime * 100);
				
				giveUpTimer += dTime; yield return new WaitForFixedUpdate();
			}
			
			if (bjRunning)
			{
				if (randomTimeJSON.val > 0f)
				{
					BigRandom();
				}				
				frontBackCurrent = 0.000001f;			
				tarPosBegin = headControlTransform.position;
				tarRotBegin = headControlTransform.rotation;
				headAngle.x = headAngleXMinJSON.val; headAngle.y = 0f; headAngle.z = 0f;
				targetRot = Quaternion.LookRotation(gen1Transform.position - lipTriggerTransform.position, chestRBTransform.up) * Quaternion.Euler(headAngle);
			}
			
			//--------------------------------------------------BJ----------------------
			while (bjRunning)
			{
				if (audioMoanLoaded)
				{
					audioMoanTransform.position = headRBTransform.position;
				}
				if (audioSFXLoaded)
				{
					audioSFXTransform.position = headRBTransform.position;
				}

				dTime = Time.fixedDeltaTime;
				eyesClosedTimer -= dTime;
				if (eyesClosedTimer < 0.0f)
				{					
					eyesOpen = !eyesOpen;
					if (eyesOpen)
					{		
						blink.val = wasBlink; eyesClosedTarget = eyesClosedMinJSON.val;
						eyesClosedTimer = eyesOpenTimeJSON.val * UnityEngine.Random.Range(0.8f, 1.2f);
					}
					else
					{
						blink.val = false; eyesClosedTarget = 1f;
						eyesClosedTimer = eyesCloseTimeJSON.val * UnityEngine.Random.Range(0.8f, 1.2f);
					}
				}
				
				morphTimer -= dTime;
				if (morphTimer < 0.0f)
				{
					SetMorphTargets();
					morphTimer = morphTimeJSON.val;
				}
					
				if (randomTimeJSON.val > 0f)
				{
					randomTimer -= dTime;
					float slerpProgress = randomTimer * slerpScale;
					frontBackRangeJSON.val = Mathf.SmoothStep(targetRange, startRange, slerpProgress); 
					frontBackSpeedJSON.val = Mathf.SmoothStep(targetSpeed, startSpeed, slerpProgress);
					headAngle.x = Mathf.SmoothStep(headAngleTarget.x, headAngleStart.x, slerpProgress);
					headAngle.y = Mathf.SmoothStep(headAngleTarget.y, headAngleStart.y, slerpProgress);
					headAngle.z = Mathf.SmoothStep(headAngleTarget.z, headAngleStart.z, slerpProgress);
					if (randomTimer < 0.0f)
					{
						BigRandom();
					}
				}
				else
				{
					headAngle.x = headAngleXMinJSON.val; headAngle.y = headAngleYMinJSON.val; headAngle.z = headAngleZMinJSON.val;
				}
				
				float dMorphTime = dTime * 2;
				mouthOpenWide.morphValue = Mathf.Lerp(mouthOpenWide.morphValue, mouthOpenWideTarget, dMorphTime);
				eyesClosed.morphValue = Mathf.LerpUnclamped(eyesClosed.morphValue, eyesClosedTarget, dMorphTime * 4);						
				for (int i = 0; i < morphsLip.Length; i++)
				{
					morphsLip[i].morphValue = Mathf.LerpUnclamped(morphsLip[i].morphValue, targetsLip[i], dMorphTime);
				}
				
				headShift.x = headShiftXJSON.val; headShift.y = headShiftYJSON.val; headShift.z = -headShiftZJSON.val;
				if (hasHJ && hjActive.val)
				{
					frontBackCurrent = hjFrontBack.val;
				}
				else { frontBackCurrent += dTime * frontBackSpeedJSON.val; }
				if (frontBackCurrent > flipLimitJSON.val)
				{				
					tarPosBegin = headControlTransform.position;
					tarRotBegin = headControlTransform.rotation;
					targetRot = Quaternion.LookRotation(gen1Transform.position - lipTriggerTransform.position, chestRBTransform.up) * Quaternion.Euler(headAngle);					
					frontBackCurrent = 0f;
					flipped = !flipped;
					if (flipped) { PlayRandomAudioSFX(); }
					else { PlayRandomAudioMoan(); }
				}
				
				Vector3 tbPos1;
				if (topOnly) { tbPos1 = gen2Transform.position; }
				else { tbPos1 = gen1Transform.position; }
				
				Vector3 tbPos2 = gen3Transform.position;
				
				float progress = Helpers.QuadraticInOut(frontBackCurrent);
				Vector3 targetPos;
				Vector3 dir = (tbPos2 - tbPos1);
				float dist = Vector3.Magnitude(dir) * frontBackRangeJSON.val;
				dir = Vector3.Normalize(dir);
				
				if (flipped)
				{
					targetPos = tbPos2
						+ headRBTransform.position - lipTriggerTransform.position
						+ gen1Transform.TransformVector(headShift) + (dir * -dist);
				}
				else
				{
					targetPos = tbPos1
						+ headRBTransform.position - lipTriggerTransform.position
						+ gen1Transform.TransformVector(headShift) + (dir * dist);
				}
				
				headControlTransform.position = Vector3.LerpUnclamped(tarPosBegin, targetPos, progress);
				headControlTransform.rotation =	Quaternion.LerpUnclamped(tarRotBegin, targetRot, progress);
				
				yield return new WaitForFixedUpdate();
			}
			
			bool doPos = (wasHeadPosState == FreeControllerV3.PositionState.On);
			bool doRot = (wasHeadRotState == FreeControllerV3.RotationState.On);
			headControl.RBHoldPositionSpring = 2000;
			headControl.RBHoldRotationSpring = 200;			
			giveUpTimer = (doPos || doRot) ? 0f : 5f;
			
			//--------------------------------------------------Return----------------------
			while (giveUpTimer < 5f && Vector3.Distance(headControlTransform.position, startPoshead) > 0.002f)
			{
				if (audioMoanLoaded)
				{
					audioMoanTransform.position = headRBTransform.position;
				}
				if (audioSFXLoaded)
				{
					audioSFXTransform.position = headRBTransform.position;
				}
				
				dTime = Time.fixedDeltaTime;
				float dMorphTime = dTime * 20;
				mouthOpenWide.morphValue = Mathf.Lerp(mouthOpenWide.morphValue, 0f, dMorphTime);
				eyesClosed.morphValue = Mathf.LerpUnclamped(eyesClosed.morphValue, 0f, dMorphTime);						
				for (int i = 0; i < morphsLip.Length; i++)
				{
					morphsLip[i].morphValue = Mathf.LerpUnclamped(morphsLip[i].morphValue, 0f, dMorphTime);
				}
				
				headControlTransform.position = Vector3.MoveTowards(headControlTransform.position, startPoshead, 0.4f * dTime);
				headControlTransform.rotation = Quaternion.RotateTowards(headControlTransform.rotation, startRothead, dTime * 50);							
				giveUpTimer += dTime; yield return new WaitForFixedUpdate();
			}
			
			mouthOpenWide.morphValue = 0f;
			eyesClosed.morphValue = 0f;						
			for (int i = 0; i < morphsLip.Length; i++)
			{
				morphsLip[i].morphValue = 0f;
			}
			blink.val = wasBlink;
			if (audioMoanLoaded)
			{
				audioMoanTransform.position = Vector3.zero;
			}
			if (audioSFXLoaded)
			{
				audioSFXTransform.position = Vector3.zero;
			}
			
			hasHJ = false;
			hasGaze = false;			
			if (gazeActive != null && wasGaze)
			{
				gazeActive.val = true;
				yield return null;
			}
			else
			{
				if (doPos) { headControlTransform.position = startPoshead; }
				if (doRot) { headControlTransform.rotation = startRothead; }
				headControl.currentPositionState = wasHeadPosState;
				headControl.currentRotationState = wasHeadRotState;
			}
			
			isBJRoutine.val = false;
		}
		
		private void OnDisable()
		{
			StopBJ();
		}
	}
}