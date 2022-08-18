using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;

namespace ClockwiseSilver {
	public class Kiss : MVRScript {
		private const float GiveUpTime = 4;

		public static string Version = "2";

		private FreeControllerV3 headControl;
        private Rigidbody lipTrigger, headRB, chestRB, targetBody;
		private Transform targetBodyTransform, targetTransform, camTransform;
		private GenerateDAZMorphsControlUI morphControl;
		private Atom kissAtom;
		private JSONStorableStringChooser kissObjectChooser, kissTargetChooser;
		private JSONStorable kissTargetJSON;
		private JSONStorableBool isActive, trackPosition, trackRotation, closeEyes, triggerByDistance;
		private JSONStorableBool blink, kissActive;
		private JSONStorableFloat periodMorphJSON, speedMorphJSON;
		private JSONStorableFloat periodClosedJSON, periodOpenJSON;
		private JSONStorableFloat trackingSpeedJSON;
		private JSONStorableFloat lipOffsetYJSON, lipOffsetZJSON;
		private JSONStorableFloat upDownSpeedJSON, upDownRangeJSON, frontBackSpeedJSON, frontBackRangeJSON;
		private JSONStorableFloat headAngleXJSON, headAngleYJSON, headAngleZJSON;
		private JSONStorableFloat eyesClosedMinJSON, eyesClosedMaxJSON;
		private JSONStorableFloat tongueLengthJSON, lipsMaxJSON, tongueMaxJSON;
		private JSONStorableFloat triggerDistanceJSON;
		private Vector3 headAngle = Vector3.zero;
		private Vector3 headStartPos;
		private Quaternion headStartRot;
		private DAZMorph mouthOpenWide, tongueLength, tongueRaise, eyesClosed;
		private DAZMorph[] morphsLip;
		private DAZMorph[] morphsTongue;
		private bool isOpen, kissStopping, kissStoppingDoMove, wasGaze = false;
		private float[] targetsLip;
		private float[] targetsTongue;
		private float openTimer, morphTimer = 0f;
		private float giveUpTimer = GiveUpTime;
		private float mouthOpenWideMax = 0.8f;
		private float mouthOpenWideTarget = 0.8f;
		private float tongueLengthTarget = 0.15f;
		private float tongueRaiseMin = 0.3f;
		private float tongueRaiseMax = 0.4f;
		private float tongueRaiseTarget = 0.4f;
		private float eyesClosedTarget = 0.4f;
		private float eyesClosedSpeed = 10.0f;
		private float morphSpeed = 1.0f;
		private float triggerOutDistance = 0.25f;
		private float upDownStart, upDownTarget, upDownCurrent = 0f;
		private float frontBackStart, frontBackTarget, frontBackCurrent = 0f;
		private JSONStorableString version;


		public override void Init() {
			try
			{;
				headControl = containingAtom.freeControllers.First(fc => fc.name == "headControl");
                lipTrigger = containingAtom.rigidbodies.First(rb => rb.name == "LipTrigger");
				headRB = containingAtom.rigidbodies.First(rb => rb.name == "head");
				chestRB = containingAtom.rigidbodies.First(rb => rb.name == "chest");

                JSONStorable geometry = containingAtom.GetStorableByID("geometry");
                DAZCharacterSelector character = geometry as DAZCharacterSelector;
                morphControl = character.morphsControlUI;

				mouthOpenWide = morphControl.GetMorphByDisplayName("Mouth Open Wide");
				tongueLength = morphControl.GetMorphByDisplayName("Tongue Length");
				tongueRaise = morphControl.GetMorphByDisplayName("Tongue Raise-Lower");
				eyesClosed = morphControl.GetMorphByDisplayName("Eyes Closed");

				morphsLip = new DAZMorph[2];
				morphsLip[0] = morphControl.GetMorphByDisplayName("Lips Part");
				morphsLip[1] = morphControl.GetMorphByDisplayName("Lips Pucker Wide");
				targetsLip = new float[morphsLip.Length];

				morphsTongue = new DAZMorph[5];
				morphsTongue[0] = morphControl.GetMorphByDisplayName("Tongue Curl");
				morphsTongue[1] = morphControl.GetMorphByDisplayName("Tongue In-Out");
				morphsTongue[2] = morphControl.GetMorphByDisplayName("Tongue Side-Side");
				morphsTongue[3] = morphControl.GetMorphByDisplayName("Tongue Twist");
				morphsTongue[4] = morphControl.GetMorphByDisplayName("Tongue Up-Down");
				targetsTongue = new float[morphsTongue.Length];

                blink = containingAtom.GetStorableByID("EyelidControl").GetBoolJSONParam("blinkEnabled");
				camTransform = SuperController.singleton.lookCamera.transform;

				//-------------------UI------------------------------------

				isActive = new JSONStorableBool ("isActive", false);
				RegisterBool(isActive);
				var toggle = CreateToggle(isActive, false);
				toggle.label = "Active";

				kissActive = new JSONStorableBool("Is Kissing", false);
				RegisterBool(kissActive);
				toggle = CreateToggle(kissActive, false);
				toggle.label = "Running";
				toggle.toggle.interactable = false;

				kissObjectChooser = new JSONStorableStringChooser("atom", SuperController.singleton.GetAtomUIDs(), null, "Kiss Atom", SyncAtom);
				RegisterStringChooser(kissObjectChooser);
				SyncAtomChocies();
				UIDynamicPopup dp = CreateFilterablePopup(kissObjectChooser);
				dp.popupPanelHeight = 1000f;
				dp.popup.onOpenPopupHandlers += SyncAtomChocies;

				kissTargetChooser = new JSONStorableStringChooser("kissTargetJSON", null, null, "Kiss Target", SynckissTargetJSON);
				RegisterStringChooser(kissTargetChooser);
				dp = CreateFilterablePopup(kissTargetChooser);
				dp.popupPanelHeight = 1000f;

				var heading = CreateTextField(new JSONStorableString("header0",
					"Target Tracking is disabled\n" +
					"when Kiss Cam is enabled."), true);
				heading.height = 40f;

				triggerByDistance = new JSONStorableBool ("triggerByDistance", false);
				RegisterBool(triggerByDistance);
				toggle = CreateToggle(triggerByDistance, true);
				toggle.label = "Kiss Cam by Distance";

				triggerDistanceJSON = new JSONStorableFloat("Kiss Cam Trigger Distance", 0.25f, 0.01f, 1.5f, false);
				RegisterFloat(triggerDistanceJSON );
				CreateSlider(triggerDistanceJSON , true);

				CreateSpacer(true).height = 20;

				heading = CreateTextField(new JSONStorableString("header1",
					"Tracking: When having 2 people kiss, \n" +
					"disable position on one of the two\n" +
					"e.g. Person1 track Pos and Rot\nPerson2 track ONLY Rot."));
				heading.height = 140f;

				trackingSpeedJSON = new JSONStorableFloat("Tracking Speed", 0.5f, 0.05f, 1.5f, false);
				RegisterFloat(trackingSpeedJSON);
				CreateSlider(trackingSpeedJSON, true);

				trackPosition = new JSONStorableBool ("trackPosition", true);
				RegisterBool(trackPosition);
				toggle = CreateToggle(trackPosition, true);
				toggle.label = "Track Position";

				trackRotation = new JSONStorableBool ("trackRotation", true);
				RegisterBool(trackRotation);
				toggle = CreateToggle(trackRotation, true);
				toggle.label = "Track Rotation";

				CreateSpacer(true).height = 20;

				lipOffsetYJSON = new JSONStorableFloat("Lip Height", 0f, -0.14f, 0.14f, false);
				RegisterFloat(lipOffsetYJSON);
				CreateSlider(lipOffsetYJSON, false);

				lipOffsetZJSON = new JSONStorableFloat("Lip Depth", 0f, -0.14f, 0.14f, false);
				RegisterFloat(lipOffsetZJSON);
				CreateSlider(lipOffsetZJSON, false);

				headAngleXJSON = new JSONStorableFloat("Head Angle X", 0f, -90f, 90f, false);
				RegisterFloat(headAngleXJSON);
				CreateSlider(headAngleXJSON, false);

				headAngleYJSON = new JSONStorableFloat("Head Angle Y", 0f, -90f, 90f, false);
				RegisterFloat(headAngleYJSON);
				CreateSlider(headAngleYJSON, false);

				headAngleZJSON = new JSONStorableFloat("Head Angle Z", 0f, -90f, 90f, false);
				RegisterFloat(headAngleZJSON);
				CreateSlider(headAngleZJSON, false);

				CreateSpacer(false).height = 20;

				upDownSpeedJSON = new JSONStorableFloat("Up Down Speed", 0.7f, 0.001f, 4f, false);
				RegisterFloat(upDownSpeedJSON);
				CreateSlider(upDownSpeedJSON, true);

				upDownRangeJSON = new JSONStorableFloat("Up Down Range", 0.01f, 0f, 0.1f, false);
				RegisterFloat(upDownRangeJSON);
				CreateSlider(upDownRangeJSON, true);

				frontBackSpeedJSON = new JSONStorableFloat("Front Back Speed", 0.7f, 0.001f, 4f, false);
				RegisterFloat(frontBackSpeedJSON);
				CreateSlider(frontBackSpeedJSON, true);

				frontBackRangeJSON = new JSONStorableFloat("Front Back Range", 0.01f, 0f, 0.1f, false);
				RegisterFloat(frontBackRangeJSON);
				CreateSlider(frontBackRangeJSON, true);

				CreateSpacer(true).height = 20;

				periodMorphJSON = new JSONStorableFloat("Morph Duration", 0.7f, 0.05f, 2.0f, false);
				RegisterFloat(periodMorphJSON);
				CreateSlider(periodMorphJSON, false);

				speedMorphJSON = new JSONStorableFloat("Morph Speed", 1.2f, 0.05f, 5.0f, false);
				RegisterFloat(speedMorphJSON);
				CreateSlider(speedMorphJSON, false);

				periodClosedJSON = new JSONStorableFloat("Eyes Time Open", 1.5f, 0f, 20f, false);
				RegisterFloat(periodClosedJSON);
				CreateSlider(periodClosedJSON, true);

				periodOpenJSON = new JSONStorableFloat("Eyes Time Closed", 5.5f, 0f, 20f, false);
				RegisterFloat(periodOpenJSON);
				CreateSlider(periodOpenJSON, true);

				eyesClosedMinJSON = new JSONStorableFloat("Eyes Closed Min", 0.25f, 0f, 1f, false);
				RegisterFloat(eyesClosedMinJSON);
				CreateSlider(eyesClosedMinJSON, true);

				eyesClosedMaxJSON = new JSONStorableFloat("Eyes Closed Max", 0.75f, 0.25f, 1f, false);
				RegisterFloat(eyesClosedMaxJSON);
				CreateSlider(eyesClosedMaxJSON, true);

				closeEyes = new JSONStorableBool ("closeEyes", true);
				RegisterBool (closeEyes);
				toggle = CreateToggle(closeEyes, true);
				toggle.label = "Close Eyes";

				lipsMaxJSON = new JSONStorableFloat("Lip Morph Max", 0.8f, 0f, 1f, false);
				RegisterFloat(lipsMaxJSON);
				CreateSlider(lipsMaxJSON, false);

				tongueMaxJSON = new JSONStorableFloat("Tongue Morph Max", 0.8f, 0f, 1f, false);
				RegisterFloat(tongueMaxJSON);
				CreateSlider(tongueMaxJSON, false);

				tongueLengthJSON = new JSONStorableFloat("Tongue Length", 0.15f, 0f, 1f, false);
				RegisterFloat(tongueLengthJSON);
				CreateSlider(tongueLengthJSON, false);

				var btn = CreateButton("Reset Morphs", false);
                btn.button.onClick.AddListener(() => { ZeroMorphs(); });

				version = new JSONStorableString("version", Version);
				RegisterString(version);
			}
			catch (Exception e) {
				SuperController.LogError("Exception caught: " + e);
			}
		}

		void Start()
		{
			try
			{
				if (kissObjectChooser.val == null)
				{
					if (kissObjectChooser.choices.Count > 0 && kissObjectChooser.choices[0] != containingAtom.uid)
					{
						kissObjectChooser.val = kissObjectChooser.choices[0];
					}
					else if (kissObjectChooser.choices.Count > 1 && kissObjectChooser.choices[1] != containingAtom.uid)
					{
						kissObjectChooser.val = kissObjectChooser.choices[1];
					}
				}

				if (kissObjectChooser.val != null && kissTargetChooser.val == null)
				{
					SyncAtom(kissObjectChooser.val);
					//foreach (string uid in kissTargetChooser.choices)
					//{
					//	if (uid == "LipTrigger")
					//	{
							kissTargetChooser.val = "LipTrigger";
					//		break;
					//	}
					//}
				}

				isActive.toggle.onValueChanged.AddListener((checkedVal) => {
					if (checkedVal)
					{
						if (triggerByDistance.val) { triggerByDistance.val = false; }
						StartKiss(false);
					}
					else
					{
						StopKiss(false);
					}
				});

				triggerByDistance.toggle.onValueChanged.AddListener((checkedVal) => {
					if (checkedVal)
					{
						if (isActive.val) { isActive.val = false; }
						StartKiss(false);
					}
				});

				if (isActive.val)
				{
					StartKiss(false);
				}

			}
			catch (Exception e) {
				SuperController.LogError("Exception caught: " + e);
			}
		}

		private void SyncAtomChocies() {
			List<string> atomChoices = new List<string>();
			List<string> peopleChoices = SuperController.singleton.GetAtoms().Where(a => a.category == "People").Select(a => a.name).ToList();
			List<string> otherChoices = SuperController.singleton.GetAtoms().Where(a => a.category != "People").Select(a => a.name).ToList();
			foreach (string atomUID in peopleChoices)
			{
				atomChoices.Add(atomUID);
			}
			foreach (string atomUID in otherChoices)
			{
				atomChoices.Add(atomUID);
			}
			kissObjectChooser.choices = atomChoices;
		}

		private void SyncAtom(string atomUID) {
			//List<string> kissTargetJSONChoices = new List<string>();
			if (atomUID != null) {
				kissAtom = SuperController.singleton.GetAtomByUid(atomUID);
				//if (kissAtom != null) {
				//	foreach (Rigidbody rb in kissAtom.rigidbodies) {
				//		if (!rb.name.Contains("Collider"))
				//			kissTargetJSONChoices.Add(rb.name);
				//	}
				//}
			} else {
				kissAtom = null;
			}
			//kissTargetChooser.choices = kissTargetJSONChoices;
		}

		private void SynckissTargetJSON(string kissTargetJSONID) {
			List<string> kissTargetJSONTargetChoices = new List<string>();
			if (kissAtom != null && kissTargetJSONID != null)
			{
				try
				{
					targetBody = kissAtom.rigidbodies.First(fc => fc.name == kissTargetJSONID);
				}
				catch(Exception)
				{
				}

				if (targetBody != null)
				{
					targetBodyTransform = targetBody.transform;
				}
				else
				{
					targetBodyTransform = null;
				}
			}
			else
			{
				targetBody = null;
				targetBodyTransform = null;
			}
		}

		private void StartKiss(bool isCamKiss, bool isResume=false)
		{
			if (isCamKiss)
			{
				targetTransform = camTransform;
				//wasGaze = check gaze control
			}
			else
			{
				targetTransform = targetBodyTransform;
			}

			if (targetTransform == null)
			{
				SuperController.LogMessage("SilverKiss: No target selected");
				return;
			}

			if (!isResume)
			{
				triggerOutDistance = triggerDistanceJSON.val + 0.05f;
				headStartPos = headControl.transform.position;
				headStartRot = headControl.transform.rotation;
			}

			kissStopping = false;
			kissActive.val = true;
			upDownCurrent = 0f;
			frontBackCurrent = 0f;
		}

		private void StopKiss(bool isCamKiss)
		{
			if (!kissActive.val || kissStopping) { return; }

			//Restore gaze control

			giveUpTimer = GiveUpTime;
			kissStopping = true;
			kissStoppingDoMove = true;
			SetMorphTargets();
		}

		private void SetMorphTargets()
		{
			morphSpeed = speedMorphJSON.val;

			if (kissStopping)
			{
				mouthOpenWideTarget = 0f;
				tongueLengthTarget = 0f;
				tongueRaiseTarget = 0f;
				eyesClosedTarget = eyesClosedMinJSON.val;

				blink.val = true;

				for (int i = 0; i < morphsLip.Length; i++)
				{
					targetsLip[i] = 0f;
				}

				for (int i = 0; i < morphsTongue.Length; i++)
				{
					targetsTongue[i] = 0f;
				}
			}
			else if (isOpen)
			{
				mouthOpenWideTarget = mouthOpenWideMax;
				tongueLengthTarget = tongueLengthJSON.val;
				tongueRaiseTarget = tongueRaiseMax;

				if (closeEyes.val)
				{
					blink.val = false;
					eyesClosedTarget = eyesClosedMaxJSON.val;
				}
				else
				{
					blink.val = true;
					eyesClosedTarget = eyesClosedMinJSON.val;
				}

				for (int i = 0; i < morphsLip.Length; i++)
				{
					targetsLip[i] = UnityEngine.Random.Range(0f, lipsMaxJSON.val);
				}

				for (int i = 0; i < morphsTongue.Length; i++)
				{
					targetsTongue[i] = UnityEngine.Random.Range(-tongueMaxJSON.val, tongueMaxJSON.val);
				}
			}
			else
			{
				mouthOpenWideTarget = 0f;
				tongueLengthTarget = 0f;
				tongueRaiseTarget = tongueRaiseMin;
				eyesClosedTarget = eyesClosedMinJSON.val;

				blink.val = true;

				for (int i = 0; i < morphsLip.Length; i++)
				{
					targetsLip[i] = UnityEngine.Random.Range(0f, lipsMaxJSON.val);
				}

				for (int i = 0; i < morphsTongue.Length; i++)
				{
					targetsTongue[i] = 0f;
				}
			}
		}

		private void ZeroMorphs()
		{
			mouthOpenWide.morphValue = 0f;
			tongueLength.morphValue = 0f;
			tongueRaise.morphValue = 0f;
			eyesClosed.morphValue = 0f;

			blink.val = true;

			for (int i = 0; i < morphsLip.Length; i++)
			{
				morphsLip[i].morphValue = 0f;
			}

			for (int i = 0; i < morphsTongue.Length; i++)
			{
				morphsTongue[i].morphValue = 0f;
			}
		}

		private void Update()
		{
			if (SuperController.singleton.freezeAnimation)
				return;

			try
			{

				if (kissStopping)
				{
					//Update Morphs
					float dTime = Time.deltaTime * morphSpeed * 3;

				mouthOpenWide.morphValue = Mathf.Lerp(mouthOpenWide.morphValue, mouthOpenWideTarget, dTime);
					tongueLength.morphValue = Mathf.Lerp(tongueLength.morphValue, tongueLengthTarget, dTime);
					tongueRaise.morphValue = Mathf.Lerp(tongueRaise.morphValue, tongueRaiseTarget, dTime);

					eyesClosed.morphValue = Mathf.Lerp(eyesClosed.morphValue, eyesClosedTarget, Time.deltaTime * eyesClosedSpeed);

					for (int i = 0; i < morphsLip.Length; i++)
					{
						morphsLip[i].morphValue = Mathf.Lerp(morphsLip[i].morphValue, targetsLip[i], dTime);
					}

					for (int i = 0; i < morphsTongue.Length; i++)
					{
						morphsTongue[i].morphValue = Mathf.Lerp(morphsTongue[i].morphValue, targetsTongue[i], dTime);
					}

					if (kissStoppingDoMove)
					{
						if (headControl.isGrabbing)
						{
							kissStoppingDoMove = false;
						}
						else
						{
							headControl.transform.position = Vector3.MoveTowards(headControl.transform.position, headStartPos, trackingSpeedJSON.val * Time.deltaTime);
							headControl.transform.rotation = Quaternion.Lerp(headControl.transform.rotation, headStartRot, trackingSpeedJSON.val * Time.deltaTime * 8);
						}
					}

					if ((!kissStoppingDoMove || Vector3.Distance(headControl.transform.position, headStartPos) < 0.002f)
						&& mouthOpenWide.morphValue <= mouthOpenWideTarget + 0.01f)
					{
						ZeroMorphs();
						kissStopping = false;
						kissActive.val = false;
					}

					giveUpTimer -= Time.deltaTime;
					if (giveUpTimer < 0f)
					{
						ZeroMorphs();
						kissStopping = false;
						kissActive.val = false;
					}

					if (triggerByDistance.val)
					{
						//Resume if camera returned before finished stopping
						if (Vector3.Distance(headStartPos, camTransform.position) < triggerDistanceJSON.val)
						{
							StartKiss(true, true);
						}
					}
				}
				else if (kissActive.val)
				{
					//Update Timers
					openTimer -= Time.deltaTime;
					if (openTimer < 0.0f)
					{
						isOpen = !isOpen;

						if (isOpen)
						{
							openTimer = periodOpenJSON.val * UnityEngine.Random.Range(0.5f, 1.5f);
						}
						else
						{
							openTimer = periodClosedJSON.val * UnityEngine.Random.Range(0.5f, 1.5f);
						}
					}

					morphTimer -= Time.deltaTime;
					if (morphTimer < 0.0f)
					{
						SetMorphTargets();
						morphTimer = periodMorphJSON.val;
					}

					float dTime = Time.deltaTime * morphSpeed;

					//Update Morphs
					mouthOpenWide.morphValue = Mathf.Lerp(mouthOpenWide.morphValue, mouthOpenWideTarget, dTime);
					tongueLength.morphValue = Mathf.Lerp(tongueLength.morphValue, tongueLengthTarget, dTime);
					tongueRaise.morphValue = Mathf.Lerp(tongueRaise.morphValue, tongueRaiseTarget, dTime);

					eyesClosed.morphValue = Mathf.Lerp(eyesClosed.morphValue, eyesClosedTarget, Time.deltaTime * eyesClosedSpeed);

					for (int i = 0; i < morphsLip.Length; i++)
					{
						morphsLip[i].morphValue = Mathf.Lerp(morphsLip[i].morphValue, targetsLip[i], dTime);
					}

					for (int i = 0; i < morphsTongue.Length; i++)
					{
						morphsTongue[i].morphValue = Mathf.Lerp(morphsTongue[i].morphValue, targetsTongue[i], dTime);
					}

					if (targetTransform != null)
					{
						Vector3 headUp = headRB.transform.up;
						Vector3 chestUp = chestRB.transform.up;
						Vector3 headForward = headRB.transform.forward;

						if (trackRotation.val)
						{
							headAngle.x = headAngleXJSON.val; headAngle.y = headAngleYJSON.val; headAngle.z = headAngleZJSON.val;
							Quaternion headTargetRotation =
								Quaternion.LookRotation(targetTransform.position - lipTrigger.transform.position, chestUp)
								* Quaternion.Euler(headAngle);

							headControl.transform.rotation = Quaternion.Lerp(headControl.transform.rotation, headTargetRotation, trackingSpeedJSON.val * Time.deltaTime * 4);
						}

						if (trackPosition.val)
						{
							Vector3 targetPos =
								targetTransform.position + (headRB.transform.position - lipTrigger.transform.position)
								+ (headForward * lipOffsetZJSON.val)
								+ (headUp * lipOffsetYJSON.val);

							if (upDownRangeJSON.val != 0f)
							{
								upDownCurrent += Time.deltaTime * upDownSpeedJSON.val;
								float upDownPos = Mathf.SmoothStep(upDownStart, upDownTarget, upDownCurrent);
								if (upDownCurrent >= 1f)
								{
									upDownStart = upDownPos;
									upDownTarget = upDownPos > 0 ? -upDownRangeJSON.val : upDownRangeJSON.val;
									upDownCurrent = 0f;
								}

								targetPos += headUp * upDownPos;
							}

							if (frontBackRangeJSON.val != 0f)
							{
								frontBackCurrent += Time.deltaTime * frontBackSpeedJSON.val;
								float frontBackPos = Mathf.SmoothStep(frontBackStart, frontBackTarget, frontBackCurrent);
								if (frontBackCurrent >= 1f)
								{
									frontBackStart = frontBackPos;
									frontBackTarget = frontBackPos > 0 ? -frontBackRangeJSON.val : frontBackRangeJSON.val;
									frontBackCurrent = 0f;
								}

								targetPos += headForward * frontBackPos;
							}

							headControl.transform.position = Vector3.MoveTowards(headControl.transform.position, targetPos, trackingSpeedJSON.val * Time.deltaTime);
						}
					}

					if (triggerByDistance.val)
					{
						if (Vector3.Distance(headStartPos, camTransform.position) > triggerOutDistance)
						{
							StopKiss(true);
						}
					}
				}
				//Not Active Not Stopping
				else if (triggerByDistance.val)
				{
					if (Vector3.Distance(lipTrigger.transform.position, camTransform.position) < triggerDistanceJSON.val)
					{
						StartKiss(true);
					}
				}
			}
			catch (Exception e) {
				SuperController.LogError("Exception caught: " + e);
			}
		}

		// OnDestroy is where you should put any cleanup
		// if you registered objects to supercontroller or atom, you should unregister them here
		void OnDestroy()
		{
		}

	}
}