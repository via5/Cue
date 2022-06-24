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
	public class HJ : MVRScript {
		private const float GiveUpTime = 4;


		private Atom her;
		private Atom him;
		private JSONStorableBool isActive, ejEnabled, baseUpJSON, bjActive, doReturn;
		private JSONStorableFloat speedMultiJSON, trackingSpeedJSON, frontBackSpeedJSON, frontBackRangeJSON, flipLimitJSON, randomTimeJSON, frontBackOutputJSON;
		private JSONStorableFloat ejWaitJSON, ejDurationJSON, topOnlyChanceJSON, zStrokeRotMinJSON, zStrokeRotMaxJSON, rotSpeedJSON;
		private JSONStorableFloat handShiftXJSON, handShiftYJSON, handShiftZJSON, handAngleXJSON, handAngleYJSON, handAngleZJSON;
		private JSONStorableFloat handShiftX2JSON, handShiftY2JSON, handShiftZ2JSON, holdSpringPosJSON, holdSpringRotJSON;
		private JSONStorableFloat minSpeedJSON, maxSpeedJSON, minRangeJSON, maxRangeJSON, rangeMultiJSON;
		private JSONStorableStringChooser handSelectorJSON, maleChooserJSON;
		private JSONStorable ejPlugin;
		private JSONStorableBool isHJRoutine;
		private UIDynamicPopup handPopup;

		private float startRange, startSpeed, targetRange, targetSpeed, slerpScale, knuckleDistance = 0f;
		private float randomRotZ, randomRotTarget, randomRotX, frontBackTarget, frontBackCurrent = 0f;
		private float directionSign, randomTimer, knuckleSign = 1f;
		private float fbRangeMax = 0.2f;
		private float ejTimer = 5.5f;
		private bool topOnly, baseUp, isPen, isEJing, bothHands, flipped, ejFiring, hjRunning, hasBJ, isBJ = false;

		private Transform handControlTransform, handControl2Transform, gen1Transform, gen2Transform, gen3Transform;
		private Rigidbody gen1, gen2, gen3, foreArmRB, midKnuckle;

		private Vector3 knuckleOffset, handTravelUp, handShift, handAngle = Vector3.zero;
		private Vector3 startTargetPos, startTargetPos2, startPosHand, startPosHand2, tarPosBegin, tarPosBegin2 = Vector3.zero;
		private Quaternion startTargetRot, startTargetRot2, startRotHand, startRotHand2, tarRotBegin, tarRotBegin2 = Quaternion.identity;
		private FreeControllerV3 penisBase, leftShoulder, rightShoulder, handControl, handControl2, hjShoulder, hjShoulder2;

		private GenerateDAZMorphsControlUI morphControl;
		private JSONStorableFloat handCloseMaxJSON, thumbOutMaxJSON, gripVariationJSON;
		private DAZMorph handOpenMorph, handCloseMorph, thumbOutMorph, handOpenMorph2, handCloseMorph2, thumbOutMorph2;
		private DAZMorph m_c, m_f, m_p;
		private float handOpenStart, handCloseStart, thumbOutStart = 0f;
		private float m_variation = 1f;
		private float handOpenTarget = 0.75f;
		private float handCloseTarget = 0.52f;
		private float thumbOutTarget = 0.5f;

		private FreeControllerV3.RotationState wasHandRotState, wasHandRotState2 = FreeControllerV3.RotationState.On;
		private FreeControllerV3.PositionState wasHandPosState, wasHandPosState2 = FreeControllerV3.PositionState.On;

		private AudioClip[] audioClipsHJ = new AudioClip[0];
		private Atom audioAtom;
		private FreeControllerV3 audioControl;
		private AudioSourceControl audioSource;
		private JSONStorableFloat audioVolumeJSON, audioPitchJSON, audioTriggerSpeedJSON;
		private bool audioLoaded, bundleRequestComplete = false;

//-----------------------------------------------------------Init---------------------------------

		public override void Init() {
			try
			{
				her = containingAtom;
				handControl = her.freeControllers.First(fc => fc.name == "rHandControl");
				handControl2 = her.freeControllers.First(fc => fc.name == "lHandControl");
				handControlTransform = handControl.transform;
				handControl2Transform = handControl2.transform;
				JSONStorable geometry = her.GetStorableByID("geometry");
				DAZCharacterSelector character = geometry as DAZCharacterSelector;
				morphControl = character.morphsControlUI;

				isActive = new JSONStorableBool ("isActive", false);
				RegisterBool (isActive);
				var toggle = CreateToggle(isActive);
				toggle.label = "Active";

				doReturn = new JSONStorableBool ("doReturn", false);
				RegisterBool (doReturn);

				JSONStorableAction ToggleActive = new JSONStorableAction("Stop HJ", () =>
                {
                    isActive.val = !isActive.val;
                });
                RegisterAction(ToggleActive);

				isHJRoutine = new JSONStorableBool("isHJRoutine", false);
				RegisterBool(isHJRoutine);

//------------------------------------UI----------------------------------------------------------
				maleChooserJSON = new JSONStorableStringChooser("Atom", null, null, "Male");
                RegisterStringChooser(maleChooserJSON);
                UIDynamicPopup dp = CreateScrollablePopup(maleChooserJSON, false);
                dp.popupPanelHeight = 600f;
                dp.popup.onOpenPopupHandlers += () => {
						maleChooserJSON.choices = Helpers.GetMaleAndToyChoices();
					};

				List<string> handChoices = new List<string>();
				handChoices.Add("Left");
				handChoices.Add("Right");
				handChoices.Add("Both");
				handSelectorJSON = new JSONStorableStringChooser("handedness", handChoices, "Right", "Handedness");
				RegisterStringChooser(handSelectorJSON);
				handPopup = CreateScrollablePopup(handSelectorJSON, false);
				handPopup.popupPanelHeight = 250f;
				handPopup.label = "Hand";

				ejEnabled = new JSONStorableBool ("ejEnabled", false);
				RegisterBool (ejEnabled);
				ejWaitJSON = new JSONStorableFloat("EJ Wait", 30f, 4f, 120f, true, true);
				ejWaitJSON.storeType = JSONStorableParam.StoreType.Full;
				RegisterFloat(ejWaitJSON);
				ejDurationJSON = new JSONStorableFloat("EJ Duration", 15f, 3f, 60f, true, true);
				ejDurationJSON.storeType = JSONStorableParam.StoreType.Full;
				RegisterFloat(ejDurationJSON);

				var heading = CreateTextField(new JSONStorableString("header0", "Hand Position/Rotation"),true);
                heading.height = 10f;
				handShiftXJSON = Helpers.SetupSlider(this, "Hand Side/Side", 0f, -0.15f, 0.15f, true);
                handShiftYJSON = Helpers.SetupSlider(this, "Hand Fwd/Bkwd", 0f, -0.15f, 0.15f, true);
				handShiftZJSON = Helpers.SetupSlider(this, "Hand Shift Up/Down", 0.01f, -0.15f, 0.15f, true);
				handAngleXJSON = Helpers.SetupSlider(this, "Hand Angle X", 0f, -120f, 120f, true);
                handAngleYJSON = Helpers.SetupSlider(this, "Hand Angle Y", 0f, -120f, 120f, true);
				handAngleZJSON = Helpers.SetupSlider(this, "Hand Angle Z", 0f, -120f, 120f, true);

				heading = CreateTextField(new JSONStorableString("header2", "Hand Morphs"),true);
                heading.height = 10f;
				handCloseMaxJSON = Helpers.SetupSlider(this, "Hand Close Max", 0.5f, -0.5f, 1f, true);
				thumbOutMaxJSON = Helpers.SetupSlider(this, "Thumb Out Max", 0.5f, 0f, 1f, true);
				gripVariationJSON = Helpers.SetupSlider(this, "Grip Variation", 0.1f, 0f, 0.5f, true);

				heading = CreateTextField(new JSONStorableString("header3", "Second Hand (Both Only)"),true);
                heading.height = 10f;
				handShiftX2JSON = Helpers.SetupSlider(this, "Hand2 Side/Side", 0f, -0.15f, 0.15f, true);
				handShiftY2JSON = Helpers.SetupSlider(this, "Hand2 Shift Fwd/Bkwd", 0f, -0.15f, 0.15f, true);
				handShiftZ2JSON = Helpers.SetupSlider(this, "Hand2 Shift Up/Down", 0f, -0.15f, 0.15f, true);

				heading = CreateTextField(new JSONStorableString("header3_2", "Hold Spring\nReactivate to see changes"),true);
                heading.height = 10f;
				holdSpringPosJSON = Helpers.SetupSlider(this, "Hold Spring Pos", 2000f, 2000f, 10000f, true);
				holdSpringRotJSON = Helpers.SetupSlider(this, "Hold Spring Rot", 200f, 200f, 1000f, true);

				heading = CreateTextField(new JSONStorableString("header4", "*Time between random variations \n*Hand speed"));
                heading.height = 10f;
				randomTimeJSON = Helpers.SetupSlider(this, "Random Time", 4.0f, 0f, 10.0f, false);
				speedMultiJSON = Helpers.SetupSlider(this, "Overall Speed", 1f, 0.2f, 1.5f, false);
				rangeMultiJSON = Helpers.SetupSlider(this, "Overall Range", 1f, 0.2f, 1.2f, false);
				audioVolumeJSON = Helpers.SetupSlider(this, "Audio Volume", 0.45f, 0.01f, 1.0f, false);

				heading = CreateTextField(new JSONStorableString("header5", "Advanced Options (Some Automated)"));
                heading.height = 10f;
				rotSpeedJSON = Helpers.SetupSlider(this, "Rotation Speed", 180f, 10f, 400f, false);
				frontBackSpeedJSON = Helpers.SetupSlider(this, "Speed", 1f, 0.8f, 10.0f, false);
				minSpeedJSON = Helpers.SetupSlider(this, "Speed Min", 0.875f, 0.1f, 10, false);
				maxSpeedJSON = Helpers.SetupSlider(this, "Speed Max", 4.9f, 0.1f, 10, false);
				frontBackRangeJSON = Helpers.SetupSlider(this, "Range", 1.0f, 0.1f, 10, false);
				minRangeJSON = Helpers.SetupSlider(this, "Range Min", 0.7f, 0.5f, 0.75f, false);
				maxRangeJSON = Helpers.SetupSlider(this, "Range Max", 1.2f, 1f, 1.2f, false);
				flipLimitJSON = Helpers.SetupSlider(this, "Flip Limit", 0.995f, 0.98f, 0.9999f, false);
				trackingSpeedJSON = Helpers.SetupSlider(this, "Tracking Speed", 0.5f, 0.05f, 2.0f, false);
				topOnlyChanceJSON = Helpers.SetupSlider(this, "Top Only Chance", 0f, 0f, 1f, false);
				zStrokeRotMinJSON = Helpers.SetupSlider(this, "Z Stroke Min", 0f, 0f, 90f, false);
				zStrokeRotMaxJSON = Helpers.SetupSlider(this, "Z Stroke Max", 15f, 0f, 90f, false);

				baseUpJSON = new JSONStorableBool ("baseUpJSON", false);
				RegisterBool (baseUpJSON);
				toggle = CreateToggle(baseUpJSON, false);
				toggle.label = "Base Up";

				frontBackOutputJSON = new JSONStorableFloat("frontBackOutput", 0f, 0f, 1f, true, true);
				RegisterFloat(frontBackOutputJSON);
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

		private void AddEJUI()
		{
			var heading = CreateTextField(new JSONStorableString("header1", "EJ Settings"));
			heading.height = 10f;
			var toggle = CreateToggle(ejEnabled);
			toggle.label = "EJ Enabled";
			CreateSlider(ejWaitJSON);
			CreateSlider(ejDurationJSON);
		}

		private IEnumerator StartRoutine()
		{
			//Get AudioClips
			bundleRequestComplete = false;
			string audioPath = Helpers.GetPluginPath(this) + "/audio/hjaudio.audiobundle";
			Request request = new AssetLoader.AssetBundleFromFileRequest
				{ path = audioPath, callback = _ => { bundleRequestComplete = true; }};
			AssetLoader.QueueLoadAssetBundleFromFile(request);
			yield return new WaitForSeconds(0.25f);
			float elapsed = 0f;
			while (!bundleRequestComplete && elapsed < 15f)
			{
				elapsed += Time.deltaTime;
				yield return null;
			}
			audioLoaded = true;
			if (request != null && request.assetBundle)
			{
				audioClipsHJ = request.assetBundle.LoadAllAssets<AudioClip>();
			}
			else
			{
				audioLoaded = false;
				AssetLoader.DoneWithAssetBundleFromFile(request.path);
			}

			if (audioClipsHJ == null || audioClipsHJ.Length == 0)
			{
				SuperController.LogMessage("SilverHJ: No AudioClips found.");
				audioLoaded = false;
			}

			string atomName = "Audio_HJ_" + containingAtom.uid;
            audioAtom = SuperController.singleton.GetAtomByUid(atomName);
            if (audioAtom == null)
            {
                yield return SuperController.singleton.AddAtomByType("AudioSource", atomName);
                audioAtom = SuperController.singleton.GetAtomByUid(atomName);
            }
			audioAtom.hidden = true;
			audioControl = audioAtom.freeControllers.First(fc => fc.name == "control");
			audioSource = audioAtom.GetStorableByID("AudioSource") as AudioSourceControl;
			if (audioSource == null)
			{
				SuperController.LogMessage("SilverHJ: AudioSourceControl not found.");
				audioLoaded = false;
			}

			ejPlugin = Helpers.FindPlugin(this, "ClockwiseSilver.EJ");
			if (ejPlugin != null)
			{
				AddEJUI();
			}

			//Set Listeners
			isActive.toggle.onValueChanged.AddListener(checkVal =>
				{
					if (checkVal) { StartHJ(); }
					else { StopHJ(); }
				});

			if (isActive.val && !hjRunning) { StartHJ(); }
		}

		//-=-=-=-=-=-=-BIG RANDOM-=-=-=-=-=-=-=-=-=-=-=-=-BIG RANDOM-=-=-=-=-=-=-=-=-=-=-=-=-BIG RANDOM-=-=-=-=-=-=-

		private void BigRandom()
		{
			UnityEngine.Random.InitState((int)Time.time);
			randomTimer = randomTimeJSON.val * UnityEngine.Random.Range(0.8f, 1.2f);
			if (randomTimer <= 0f) { randomTimer = 4f; }
			slerpScale = 1f / randomTimer;
			topOnly = UnityEngine.Random.Range(0f, 1f) <= topOnlyChanceJSON.val;
			randomRotTarget = UnityEngine.Random.Range(zStrokeRotMinJSON.val, zStrokeRotMaxJSON.val);
			startRange = frontBackRangeJSON.val;
			startSpeed = frontBackSpeedJSON.val;
			targetSpeed = UnityEngine.Random.Range(minSpeedJSON.val, maxSpeedJSON.val) * speedMultiJSON.val;
			targetRange = UnityEngine.Random.Range(minRangeJSON.val, maxRangeJSON.val) * rangeMultiJSON.val;

			if (bothHands)
			{
				targetRange *= 0.7f;
			}
			//else
			//{
			//	targetRange = UnityEngine.Random.Range(0.7f, 1f);
			//}
		}

		private void LittleRandom(bool flipped)
		{
			if (flipped)
			{
				randomRotZ = -randomRotTarget + 5f;
				randomRotX = 0f;
			}
			else
			{
				randomRotZ = randomRotTarget + 5f;
				randomRotX = UnityEngine.Random.Range(0f, 8f);
			}
			float gvVal = gripVariationJSON.val;
			handCloseTarget = handCloseMaxJSON.val + UnityEngine.Random.Range(-gvVal, gvVal);
			gvVal *= 0.5f;
			thumbOutTarget = thumbOutMaxJSON.val + UnityEngine.Random.Range(-gvVal, gvVal);
			if (hasBJ) { isBJ = bjActive.val; }
		}

		private void PlayRandomAudioHJ()
		{
			if (!audioLoaded) { return; }
			audioSource.volume = audioVolumeJSON.val * UnityEngine.Random.Range(0.7f, 1.0f);
			audioSource.pitch = UnityEngine.Random.Range(0.825f, 1.05f);
			audioSource.audioSource.PlayOneShot(audioClipsHJ[UnityEngine.Random.Range(0, audioClipsHJ.Length)]);
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
					JSONStorable geometry = him.GetStorableByID("geometry");
					DAZCharacterSelector character = geometry as DAZCharacterSelector;
					GenerateDAZMorphsControlUI morphC = character.morphsControlUI;
					m_c = morphC.GetMorphByDisplayName("Contempt");
					m_f = morphC.GetMorphByDisplayName("Fear");
					m_p = morphC.GetMorphByDisplayName("Pain");
				}
				gen1Transform = gen1.transform; gen2Transform = gen2.transform; gen3Transform = gen3.transform;
				fbRangeMax = Vector3.Distance(gen1Transform.position, gen3Transform.position);
			}
			catch (Exception e) { SuperController.LogError("SyncMale Exception caught: " + e); }
		}

		private void SyncFemale()
		{
			if (handSelectorJSON.val == "Left")
			{
				handControl = her.freeControllers.First(fc => fc.name == "lHandControl");
				hjShoulder = her.freeControllers.First(fc => fc.name == "lArmControl");
				handOpenMorph = morphControl.GetMorphByDisplayName("Left Hand Straighten");
				handCloseMorph = morphControl.GetMorphByDisplayName("Left Hand Fist");
				thumbOutMorph = morphControl.GetMorphByDisplayName("Left Thumb In-Out");
				foreArmRB = her.rigidbodies.First(rb => rb.name == "lForeArm");
				midKnuckle = her.rigidbodies.First(rb => rb.name == "lMid1");
				knuckleSign = 1f;
			}
			else
			{
				handControl = her.freeControllers.First(fc => fc.name == "rHandControl");
				hjShoulder = her.freeControllers.First(fc => fc.name == "rArmControl");
				handOpenMorph = morphControl.GetMorphByDisplayName("Right Hand Straighten");
				handCloseMorph = morphControl.GetMorphByDisplayName("Right Hand Fist");
				thumbOutMorph = morphControl.GetMorphByDisplayName("Right Thumb In-Out");
				foreArmRB = her.rigidbodies.First(rb => rb.name == "rForeArm");
				midKnuckle = her.rigidbodies.First(rb => rb.name == "rMid1");
				knuckleSign = -1f;
			}

			if (handSelectorJSON.val == "Both")
			{
				handControl2 = her.freeControllers.First(fc => fc.name == "lHandControl");
				handOpenMorph2 = morphControl.GetMorphByDisplayName("Left Hand Straighten");
				handCloseMorph2 = morphControl.GetMorphByDisplayName("Left Hand Fist");
				thumbOutMorph2 = morphControl.GetMorphByDisplayName("Left Thumb In-Out");
				bothHands = true;
			}
			else { bothHands = false; }
		}

		//----------------------------------------HJ----------------------------------------------
		private void StartHJ()
		{
			SyncMale(); SyncFemale();
			if (him == null || her == null) { return; }
			if (isHJRoutine.val) { hjRunning = false; return; }
			if (ejEnabled.val)
			{
				ejPlugin = Helpers.FindPlugin(this, "ClockwiseSilver.EJ");
				if (!ejPlugin)
				{
					SuperController.LogError("SilverHJ: EJ is enabled but EJ script not found.");
					ejEnabled.val = false;
				}
			}

			wasHandPosState = handControl.currentPositionState;
			wasHandRotState = handControl.currentRotationState;
			handControl.currentPositionState = FreeControllerV3.PositionState.On;
			handControl.currentRotationState = FreeControllerV3.RotationState.On;
			handControlTransform = handControl.transform;
			handOpenStart = handOpenMorph.morphValue;
			handCloseStart = handCloseMorph.morphValue;
			thumbOutStart = thumbOutMorph.morphValue;
			startPosHand = handControlTransform.position;
			startRotHand = handControlTransform.rotation;
			handTravelUp = startPosHand + (handControlTransform.up * 0.08f);
			if (bothHands)
			{
				startPosHand2 = handControl2Transform.position;
				startRotHand2 = handControl2Transform.rotation;
				wasHandPosState2 = handControl2.currentPositionState;
				wasHandRotState2 = handControl2.currentRotationState;
			}

			handShift.x = handShiftXJSON.val; handShift.y = handShiftYJSON.val; handShift.z = handShiftZJSON.val;
			float handLength = Vector3.Distance(handControlTransform.position, midKnuckle.transform.position);
			Vector3 shoulderDir = Vector3.Normalize(hjShoulder.transform.position - gen3Transform.position);
			knuckleDistance = Vector3.Distance(handControlTransform.position, midKnuckle.transform.position) * 0.8f;

			Vector3 gen2Dir;

			if (baseUpJSON.val)
			{
				startTargetPos = gen1Transform.position + (hjShoulder.transform.up * 0.05f)
						+ penisBase.transform.TransformVector(handShift) + (shoulderDir * (handLength * 1.15f));
				baseUp = true; gen2Dir = isPen ? gen1Transform.forward : gen1Transform.up;
			}
			else
			{
				startTargetPos = gen2Transform.position + (hjShoulder.transform.up * 0.05f)
						+ penisBase.transform.TransformVector(handShift) + (shoulderDir * (handLength * 1.15f));
				baseUp = false; gen2Dir = isPen ? gen2Transform.forward : gen2Transform.up;
			}

			directionSign = (Vector3.Dot(penisBase.transform.forward, handControlTransform.forward) > 0f) ? 1f : -1f;
			startTargetRot = foreArmRB.transform.rotation;
			if (bothHands)
			{
				startTargetRot2 = her.rigidbodies.First(rb => rb.name == "lForeArm").transform.rotation;
				hjShoulder2 = her.freeControllers.First(fc => fc.name == "lArmControl");
			}
			frontBackCurrent = 0f;
			tarPosBegin = handControlTransform.position;
			tarRotBegin = handControlTransform.rotation;

			handOpenMorph.morphValue = handOpenTarget;
			handControl.RBHoldPositionSpring = holdSpringPosJSON.val;
            handControl.RBHoldRotationSpring = holdSpringRotJSON.val;
			if (bothHands)
			{
				tarPosBegin2 = handControl2Transform.position;
				tarRotBegin2 = handControl2Transform.rotation;
				handOpenMorph2.morphValue = handOpenTarget;
				handControl2.RBHoldPositionSpring = holdSpringPosJSON.val;
				handControl2.RBHoldRotationSpring = holdSpringRotJSON.val;
			}
			audioControl.transform.SetPositionAndRotation(gen2Transform.position, gen2Transform.rotation);
			audioControl.currentPositionState = FreeControllerV3.PositionState.ParentLink;
			audioControl.currentRotationState = FreeControllerV3.RotationState.ParentLink;
			audioControl.canGrabPosition = false;
			audioControl.canGrabRotation = false;
			audioControl.SelectLinkToRigidbody(gen2, FreeControllerV3.SelectLinkState.PositionAndRotation);
			fbRangeMax = Vector3.Distance(gen1Transform.position, gen3Transform.position);
			handCloseTarget = handCloseMaxJSON.val;
			thumbOutTarget = thumbOutMaxJSON.val;
			StartCoroutine("HJRoutine");
		}

		private void StopHJ()
		{
			if (audioControl)
			{
				audioControl.transform.position = Vector3.zero;
				audioControl.currentPositionState = FreeControllerV3.PositionState.On;
				audioControl.currentRotationState = FreeControllerV3.RotationState.On;
			}

			if (handControl)
			{
				handControl.currentPositionState = wasHandPosState;
				handControl.currentRotationState = wasHandRotState;
				handControl.RBHoldPositionSpring = 2000;
				handControl.RBHoldRotationSpring = 200;
			}

			if (handControl2)
			{
				handControl2.currentPositionState = wasHandPosState2;
				handControl2.currentRotationState = wasHandRotState2;
				if (bothHands)
				{
					handControl2.RBHoldPositionSpring = 2000;
					handControl2.RBHoldRotationSpring = 200;
				}
			}

			hjRunning = false;
		}

		private Quaternion SetHandRotation(Transform genTransform)
		{
			startTargetRot =
				Quaternion.LookRotation(directionSign * genTransform.transform.forward, hjShoulder.transform.up);

			if (bothHands)
			{
				startTargetRot2 =
					Quaternion.LookRotation(directionSign * genTransform.transform.forward, hjShoulder2.transform.up);
			}
			return startTargetRot;
		}

		private IEnumerator EJRoutine()
		{
			isEJing = true;
			topOnly = false;
			randomRotTarget = 0f;
			float stageBaseTime = ejDurationJSON.val * 0.333f;
			StartCoroutine("VaryMorph");

			//Build Up
			startRange = frontBackRangeJSON.val;
			startSpeed = frontBackSpeedJSON.val;
			targetSpeed = UnityEngine.Random.Range(6.4f, 6.9f) * speedMultiJSON.val;

			if (bothHands)
			{
				targetRange = UnityEngine.Random.Range(0.675f, 0.7f);
			}
			else
			{
				targetRange = UnityEngine.Random.Range(0.94f, 1f);
			}

			float stageTime = stageBaseTime * UnityEngine.Random.Range(0.8f, 1.2f) * 0.6f;
			slerpScale = 1f / stageTime;
			float m_cStart = m_c.morphValue;
			float m_fStart = m_f.morphValue;
			float m_pStart = m_p.morphValue;
			while (stageTime > 0f)
			{
				stageTime -= Time.deltaTime;
				float progress = 1f - (stageTime * slerpScale);
				frontBackRangeJSON.val = Mathf.SmoothStep(startRange, targetRange, progress);
				frontBackSpeedJSON.val = Mathf.SmoothStep(startSpeed, targetSpeed, progress);

				m_c.morphValue = Mathf.SmoothStep(m_cStart, 0.5f, progress) * m_variation;

				yield return null;
			}


			stageTime = stageBaseTime * UnityEngine.Random.Range(0.8f, 1.2f) * 0.4f;
			slerpScale = 1f / stageTime;
			m_cStart = m_c.morphValue;
			m_fStart = m_f.morphValue;
			m_pStart = m_p.morphValue;
			while (stageTime > 0f)
			{
				stageTime -= Time.deltaTime;
				float progress = 1f - (stageTime * slerpScale);

				m_c.morphValue = Mathf.SmoothStep(m_cStart, 0.8f, progress) * m_variation;
				m_f.morphValue = Mathf.SmoothStep(m_fStart, 0.25f, progress) * m_variation;

				yield return null;
			}

			//EJ
			ejPlugin.CallAction("Fire EJ");
			stageTime = stageBaseTime * UnityEngine.Random.Range(0.8f, 1.2f);
			slerpScale = 1f / stageTime;
			m_cStart = m_c.morphValue;
			m_fStart = m_f.morphValue;
			m_pStart = m_p.morphValue;
			while (stageTime > 0f)
			{
				stageTime -= Time.deltaTime;
				float progress = 1f - (stageTime * slerpScale);

				m_c.morphValue = Mathf.SmoothStep(m_cStart, 0f, progress);
				m_p.morphValue = Mathf.SmoothStep(m_pStart, 0.5f, progress) * m_variation;
				m_f.morphValue = Mathf.SmoothStep(m_fStart, 0.8f, progress) * m_variation;

				yield return null;
			}

			//Slow down
			randomRotTarget = UnityEngine.Random.Range(zStrokeRotMinJSON.val, zStrokeRotMaxJSON.val);
			startRange = frontBackRangeJSON.val;
			startSpeed = frontBackSpeedJSON.val;
			targetSpeed = UnityEngine.Random.Range(1.25f, 2.0f) * speedMultiJSON.val;

			if (bothHands) { targetRange = UnityEngine.Random.Range(0.65f, 0.7f); }
			else { targetRange = UnityEngine.Random.Range(0.9f, 1f); }

			stageTime = stageBaseTime * UnityEngine.Random.Range(0.8f, 1.2f) * 0.5f;
			slerpScale = 1f / stageTime;
			m_cStart = m_c.morphValue; m_fStart = m_f.morphValue; m_pStart = m_p.morphValue;
			while (stageTime > 0f)
			{
				stageTime -= Time.deltaTime;
				float progress = 1f - (stageTime * slerpScale);
				frontBackRangeJSON.val = Mathf.SmoothStep(startRange, targetRange, progress);
				frontBackSpeedJSON.val = Mathf.SmoothStep(startSpeed, targetSpeed, progress);
				m_p.morphValue = Mathf.SmoothStep(m_pStart, 0f, progress);
				m_f.morphValue = Mathf.SmoothStep(m_fStart, 0.4f, progress) * m_variation;
				yield return null;
			}

			stageTime = stageBaseTime * UnityEngine.Random.Range(0.8f, 1.2f) * 0.5f;
			slerpScale = 1f / stageTime;
			m_cStart = m_c.morphValue; m_fStart = m_f.morphValue; m_pStart = m_p.morphValue;
			while (stageTime > 0f)
			{
				stageTime -= Time.deltaTime;
				float progress = 1f - (stageTime * slerpScale);
				m_c.morphValue = Mathf.SmoothStep(m_cStart, 0f, progress) * m_variation;
				m_f.morphValue = Mathf.SmoothStep(m_fStart, 0f, progress);
				yield return null;
			}
			BigRandom(); isEJing = false;
		}

		private IEnumerator HJRoutine()
		{
			isHJRoutine.val = true; hjRunning = true; doReturn.val = true;

			//--------------------------------Silver BJ
			JSONStorable bjPlugin = Helpers.FindPlugin(this, "ClockwiseSilver.BJ");
			if (bjPlugin != null)
			{
				bjActive = bjPlugin.GetBoolJSONParam("isActive");
				if (bjActive != null) { hasBJ = true; isBJ = bjActive.val; }
			}

			float giveUpTimer = 0f; float dTime = Time.fixedDeltaTime;

			//--------------------------------------------------Begin----------------------
			while (hjRunning && giveUpTimer < GiveUpTime && Vector3.Distance(handControlTransform.position, startTargetPos) > 0.001f)
			{
				float dMorphTime = dTime * 7;
				handOpenMorph.morphValue = Mathf.LerpUnclamped(handOpenMorph.morphValue, handOpenTarget, dMorphTime);
				handCloseMorph.morphValue = Mathf.LerpUnclamped(handCloseMorph.morphValue, 0f, dMorphTime);
				thumbOutMorph.morphValue = Mathf.LerpUnclamped(thumbOutMorph.morphValue, thumbOutTarget, dMorphTime);

				handControlTransform.position = Vector3.MoveTowards(handControlTransform.position, startTargetPos, dTime * 0.5f);
				handControlTransform.rotation = Quaternion.RotateTowards(handControlTransform.rotation, startTargetRot, dTime * 250);

				if (bothHands)
				{
					handControl2Transform.position = Vector3.MoveTowards(handControl2Transform.position, startTargetPos + (handControlTransform.forward * knuckleDistance), dTime * 0.5f);
					handControl2Transform.rotation = Quaternion.RotateTowards(handControl2Transform.rotation, startTargetRot2, dTime * 250);
					handOpenMorph2.morphValue = Mathf.LerpUnclamped(handOpenMorph.morphValue, handOpenTarget, dMorphTime);
					handCloseMorph2.morphValue = Mathf.LerpUnclamped(handCloseMorph.morphValue, 0f, dMorphTime);
					thumbOutMorph2.morphValue = Mathf.LerpUnclamped(thumbOutMorph.morphValue, thumbOutTarget, dMorphTime);
				}
				giveUpTimer += dTime; yield return new WaitForFixedUpdate();
			}

			SetHandRotation(penisBase.transform); giveUpTimer = 0f;

			//--------------------------------------------------Grip----------------------
			while (hjRunning && giveUpTimer < GiveUpTime && handCloseMorph.morphValue < handCloseTarget - 0.05f)
			{
				handShift.x = handShiftXJSON.val; handShift.y = handShiftYJSON.val; handShift.z = handShiftZJSON.val;
				Vector3 tPos;
				if (baseUp)
				{
					tPos =
						gen1Transform.position + penisBase.transform.TransformVector(handShift)
						+ handControlTransform.right * knuckleDistance * knuckleSign + handControlTransform.up * (0.4f * knuckleDistance);
				}
				else
				{
					tPos =
						gen2Transform.position + penisBase.transform.TransformVector(handShift)
						+ handControlTransform.right * knuckleDistance * knuckleSign + handControlTransform.up * (0.4f * knuckleDistance);
				}

				handControlTransform.position =
					Vector3.MoveTowards(handControlTransform.position,	tPos, dTime * 0.25f);

				handAngle.x = handAngleXJSON.val + randomRotX; handAngle.y = handAngleYJSON.val; handAngle.z = handAngleZJSON.val + randomRotZ;
				handControlTransform.rotation = Quaternion.RotateTowards(handControlTransform.rotation, startTargetRot * Quaternion.Euler(handAngle), dTime * 150);

				float dMorphTime = dTime * 5;
				handOpenMorph.morphValue = Mathf.LerpUnclamped(handOpenMorph.morphValue, 0f, dMorphTime);
				handCloseMorph.morphValue = Mathf.LerpUnclamped(handCloseMorph.morphValue, handCloseTarget, dMorphTime);
				thumbOutMorph.morphValue = Mathf.LerpUnclamped(thumbOutMorph.morphValue, thumbOutTarget, dMorphTime);

				if (bothHands)
				{
					handControl2Transform.position = Vector3.MoveTowards(handControl2Transform.position, tPos + (handControlTransform.forward * knuckleDistance), dTime * 0.5f);
					handControl2Transform.rotation = Quaternion.RotateTowards(handControl2Transform.rotation, startTargetRot2 * Quaternion.Euler(handAngle), dTime * 150);
					handOpenMorph2.morphValue = handOpenMorph.morphValue; handCloseMorph2.morphValue = handCloseMorph.morphValue; thumbOutMorph2.morphValue = thumbOutMorph.morphValue;
				}
				giveUpTimer += dTime; yield return new WaitForFixedUpdate();
			}

			if (hjRunning)
			{
				if (randomTimeJSON.val > 0f)
				{
					BigRandom();
				}
				frontBackCurrent = 0.000001f;
				//knuckleOffset = (handControlTransform.position - midKnuckle.transform.position) * 0.95f;
				tarPosBegin = handControlTransform.position; tarRotBegin = handControlTransform.rotation;
				if (bothHands)
				{
					tarPosBegin2 = handControl2Transform.position; tarRotBegin2 = handControl2Transform.rotation;
				}
			}

			//--------------------------------------------------Stroke----------------------
			while (hjRunning)
			{
				if (!isEJing)
				{
					if (ejEnabled.val)
					{
						ejTimer -= dTime;
						if (ejTimer < 0f)
						{
							if (!ejPlugin)
							{
								ejPlugin = Helpers.FindPlugin(this, "ClockwiseSilver.EJ");
							}
							if (ejPlugin)
							{
								StartCoroutine("EJRoutine");
								ejTimer = ejWaitJSON.val * UnityEngine.Random.Range(0.6f, 1.4f);
							}
							else { ejEnabled.val = false; }
						}
					}
					if (randomTimeJSON.val > 0f)
					{
						randomTimer -= dTime;
						float slerpProgress = randomTimer * slerpScale;
						frontBackRangeJSON.val = Mathf.SmoothStep(targetRange, startRange, slerpProgress);
						frontBackSpeedJSON.val = Mathf.SmoothStep(targetSpeed, startSpeed, slerpProgress);
						if (randomTimer < 0.0f)
						{
							BigRandom();
						}
					}
				}

				float dMorphTime = dTime * 2;
				handCloseMorph.morphValue = Mathf.LerpUnclamped(handCloseMorph.morphValue, handCloseTarget, dMorphTime);
				thumbOutMorph.morphValue = Mathf.LerpUnclamped(thumbOutMorph.morphValue, thumbOutTarget, dMorphTime);

				handShift.x = handShiftXJSON.val; handShift.y = handShiftYJSON.val; handShift.z = handShiftZJSON.val;

				frontBackCurrent += dTime * frontBackSpeedJSON.val;
				frontBackOutputJSON.val = frontBackCurrent;
				if (frontBackCurrent > flipLimitJSON.val)
				{
					LittleRandom(flipped);
					tarPosBegin = handControlTransform.position;
					tarRotBegin = handControlTransform.rotation;

					if (bothHands)
					{
						tarPosBegin2 = handControl2Transform.position;
						tarRotBegin2 = handControl2Transform.rotation;
					}
					frontBackCurrent = 0f;
					flipped = !flipped;
					if (flipped) { PlayRandomAudioHJ(); }
				}

				Vector3 tbPos1;
				Vector3 tbPos2;
				if (isBJ)
				{
					tbPos1 = gen1Transform.position;
					tbPos2 = gen2Transform.position;
				}
				else
				{
					if (topOnly) { tbPos1 = gen2Transform.position; }
					else { tbPos1 = gen1Transform.position; }

					if (baseUp)
					{
						Vector3 genDir = isPen ? gen1Transform.forward : gen1Transform.up;
						tbPos2 = gen1Transform.position + (genDir * fbRangeMax);
					}
					else { tbPos2 = gen3Transform.position; }
				}

				float progress = Helpers.QuadraticInOut(frontBackCurrent);
				Vector3 targetPos;
				Vector3 dir = (tbPos2 - tbPos1);
				float dist = Vector3.Magnitude(dir) * frontBackRangeJSON.val;
				dir = Vector3.Normalize(dir);

				if (flipped)
				{
					SetHandRotation(penisBase.transform);
					targetPos = tbPos2 + gen1Transform.TransformVector(handShift) + (dir * -dist)
						+ handControlTransform.right * (knuckleDistance * knuckleSign) + handControlTransform.up * (0.4f * knuckleDistance);
				}
				else
				{
					SetHandRotation(penisBase.transform);
					targetPos = tbPos1 + gen1Transform.TransformVector(handShift) + (dir * dist)
						+ handControlTransform.right * (knuckleDistance * knuckleSign) + handControlTransform.up * (0.4f * knuckleDistance);
				}

				if (!handControl.isGrabbing)
					handControlTransform.position = Vector3.LerpUnclamped(tarPosBegin, targetPos, progress);

				handAngle.x = handAngleXJSON.val + randomRotX; handAngle.y = handAngleYJSON.val; handAngle.z = handAngleZJSON.val + randomRotZ;

				if (!handControl.isGrabbing)
					handControlTransform.rotation = Quaternion.LerpUnclamped(tarRotBegin, startTargetRot * Quaternion.Euler(handAngle), progress);

				if (bothHands)
				{
					handShift.x = handShiftX2JSON.val; handShift.y = handShiftY2JSON.val; handShift.z = handShiftZ2JSON.val;

					if (!handControl2.isGrabbing)
					{
						handControl2Transform.position = Vector3.LerpUnclamped(tarPosBegin2, targetPos
							+ (handControl2Transform.forward * knuckleDistance) + gen1Transform.TransformVector(handShift) - (handControl2Transform.up * (0.8f * -knuckleDistance)), progress);
					}

					handAngle.z = -handAngle.z;

					if (!handControl2.isGrabbing)
					{
						handControl2Transform.rotation = Quaternion.LerpUnclamped(tarRotBegin2, startTargetRot2 * Quaternion.Euler(handAngle), progress);
					}

					handCloseMorph2.morphValue = handCloseMorph.morphValue; thumbOutMorph2.morphValue = thumbOutMorph.morphValue;
				}
				yield return new WaitForFixedUpdate();
			}

			bool doPos = (wasHandPosState == FreeControllerV3.PositionState.On);
			bool doRot = (wasHandRotState == FreeControllerV3.RotationState.On);
			giveUpTimer = 0f;

			if (doReturn.val)
			{
				//--------------------------------------------------Stop----------------------
				while (giveUpTimer < GiveUpTime && Vector3.Distance(handControlTransform.position, handTravelUp) > 0.002f)
				{
					float dMorphTime = dTime * 24;
					handOpenMorph.morphValue = Mathf.LerpUnclamped(handOpenMorph.morphValue, handOpenStart, dMorphTime);
					handCloseMorph.morphValue = Mathf.LerpUnclamped(handCloseMorph.morphValue, handCloseStart, dMorphTime);
					thumbOutMorph.morphValue = Mathf.LerpUnclamped(thumbOutMorph.morphValue, thumbOutStart, dMorphTime);

					if (doRot) { handControlTransform.rotation = Quaternion.RotateTowards(handControlTransform.rotation, startRotHand, dTime * 150); }
					if (doPos) { handControlTransform.position = Vector3.MoveTowards(handControlTransform.position, handTravelUp, trackingSpeedJSON.val * dTime); }

					if (bothHands)
					{
						handControl2Transform.rotation = Quaternion.RotateTowards(handControl2Transform.rotation, startRotHand2, dTime * 150);
						handControl2Transform.position = Vector3.MoveTowards(handControl2Transform.position, startPosHand2, trackingSpeedJSON.val * dTime);
						handOpenMorph2.morphValue = handOpenMorph.morphValue; handCloseMorph2.morphValue = handCloseMorph.morphValue; thumbOutMorph2.morphValue = thumbOutMorph.morphValue;
					}

					giveUpTimer += dTime; yield return new WaitForFixedUpdate();
				}
				giveUpTimer = (doPos || doRot) ? 0f : GiveUpTime;

				//--------------------------------------------------Return----------------------
				while (giveUpTimer < GiveUpTime && Vector3.Distance(handControlTransform.position, startPosHand) > 0.002f)
				{
					handControlTransform.position = Vector3.MoveTowards(handControlTransform.position, startPosHand, trackingSpeedJSON.val * dTime);
					handControlTransform.rotation = Quaternion.RotateTowards(handControlTransform.rotation, startRotHand, dTime * 150);
					if (bothHands)
					{
						handControl2Transform.position = Vector3.MoveTowards(handControl2Transform.position, startPosHand2, trackingSpeedJSON.val * dTime);
						handControl2Transform.rotation = Quaternion.RotateTowards(handControl2Transform.rotation, startRotHand2, dTime * 150);
					}
					giveUpTimer += dTime; yield return new WaitForFixedUpdate();
				}
			}

			handOpenMorph.morphValue = handOpenStart;
			handCloseMorph.morphValue = handCloseStart;
			thumbOutMorph.morphValue = thumbOutStart;

			if (doPos && doReturn.val) { handControlTransform.position = startPosHand; }
			if (doRot && doReturn.val) { handControlTransform.rotation = startRotHand; }
			if (bothHands)
			{
				if (doPos && doReturn.val) { handControl2Transform.position = startPosHand2; }
				if (doRot && doReturn.val) { handControl2Transform.rotation = startRotHand2; }
			}
			hasBJ = false;
			isBJ = false;
			isHJRoutine.val = false;
		}

		private IEnumerator VaryMorph()
		{
			float randomTime = UnityEngine.Random.Range(0.1f, 0.8f);
			float rtScale = 1f / randomTime;
			float startVal = m_variation;
			float targetVal = UnityEngine.Random.Range(0.4f, 1.2f);

			while (isEJing)
			{
				randomTime -= Time.deltaTime;
				float progress = 1f - (rtScale * randomTime);
				m_variation = Mathf.LerpUnclamped(startVal, targetVal, progress);
				if (randomTime < 0f)
				{
					UnityEngine.Random.InitState((int)(Time.time + UnityEngine.Random.Range(1, 1000)));
					randomTime = UnityEngine.Random.Range(0.1f, 0.8f);
					rtScale = 1f / randomTime;
					startVal = m_variation;
					targetVal = UnityEngine.Random.Range(0.4f, 1.2f);
				}

				yield return null;
			}
		}

		private void OnDisable()
		{
			StopHJ();
		}
	}
}