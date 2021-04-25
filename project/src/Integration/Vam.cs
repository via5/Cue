using Battlehub.UIControls;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Cue
{
	class VamEyesBehaviour : MonoBehaviour
	{
		private Vector3 pos_ = new Vector3();
		private bool hasPos_ = false;

		public void SetPosition(Vector3 v)
		{
			pos_ = v;
			hasPos_ = true;
			transform.position = Vector3.ToUnity(v);
		}

		public void LateUpdate()
		{
			if (hasPos_)
			{
				transform.position = Vector3.ToUnity(pos_);
				hasPos_ = false;
			}
		}
	}

	class VamEyes
	{
		private Person person_ = null;
		private Rigidbody eyes_ = null;
		private VamEyesBehaviour eyesImpl_ = null;
		private JSONStorableStringChooser lookMode_ = null;
		private JSONStorableFloat leftRightAngle_ = null;
		private JSONStorableFloat upDownAngle_ = null;
		private Vector3 target_ = Vector3.Zero;
		private int lookAt_ = GazeSettings.LookAtDisabled;

		public VamEyes(Person p)
		{
			person_ = p;
		}

		public int LookAt
		{
			get
			{
				Get();
				if (lookMode_ == null)
					return GazeSettings.LookAtDisabled;

				string s = lookMode_.val;

				if (s == "Player")
					return GazeSettings.LookAtPlayer;
				else if (s == "Target")
					return GazeSettings.LookAtTarget;
				else
					return GazeSettings.LookAtDisabled;
			}

			set
			{
				Get();
				if (lookMode_ == null)
					return;

				lookAt_ = value;

				switch (value)
				{
					case GazeSettings.LookAtDisabled:
					{
						lookMode_.val = "None";
						break;
					}

					case GazeSettings.LookAtTarget:
					{
						lookMode_.val = "Target";
						break;
					}
					case GazeSettings.LookAtPlayer:
					{
						lookMode_.val = "Player";
						break;
					}
				}
			}
		}

		public Vector3 Target
		{
			get
			{
				Get();
				if (eyes_ == null)
					return Vector3.Zero;

				return Vector3.FromUnity(eyes_.position);
			}

			set
			{
				Get();
				if (eyes_ == null)
					return;

				target_ = value;
				eyesImpl_.SetPosition(value);
			}
		}

		public void Update(float s)
		{
			if (lookAt_ == GazeSettings.LookAtTarget)
				eyesImpl_.SetPosition(target_);
		}

		private void Get()
		{
			var a = ((W.VamAtom)person_.Atom).Atom;

			if (eyes_ == null)
			{
				eyes_ = Cue.Instance.VamSys?.FindRigidbody(person_, "eyeTargetControl");
				if (eyes_ == null)
				{
					Cue.LogError("atom " + a.uid + " has no eyeTargetControl");
				}
				else
				{
					foreach (var c in eyes_.gameObject.GetComponents<Component>())
					{
						if (c != null && c.ToString().Contains("Cue.VamEyesBehaviour"))
							UnityEngine.Object.Destroy(c);
					}

					eyesImpl_ = eyes_.gameObject.AddComponent<VamEyesBehaviour>();
				}
			}

			var eyesStorable = a.GetStorableByID("Eyes");
			if (eyesStorable != null)
			{
				lookMode_ = eyesStorable.GetStringChooserJSONParam("lookMode");
				if (lookMode_ == null)
					Cue.LogError("atom " + a.uid + " has no lookMode");

				leftRightAngle_ = eyesStorable.GetFloatJSONParam("leftRightAngleAdjust");
				if (leftRightAngle_ == null)
					Cue.LogError("atom " + a.uid + " has no leftRightAngleAdjust");

				upDownAngle_ = eyesStorable.GetFloatJSONParam("upDownAngleAdjust");
				if (upDownAngle_ == null)
					Cue.LogError("atom " + a.uid + " has no upDownAngleAdjust");
			}
		}
	}


	class VamSpeaker : ISpeaker
	{
		private Person person_ = null;
		private JSONStorableString text_ = null;

		public VamSpeaker(Person p)
		{
			person_ = p;
		}

		public void Say(string s)
		{
			GetParameters();
			if (text_ == null)
				return;

			try
			{
				text_.val = s;
			}
			catch (Exception e)
			{
				Cue.LogError(
					person_.ToString() + " can't speak, " + e.Message + " " +
					"(while trying to say '" + s + "'");
			}
		}

		private void GetParameters()
		{
			if (text_ != null)
				return;

			text_ = Cue.Instance.VamSys?.GetStringParameter(
				person_, "SpeechBubble", "bubbleText");
		}
	}


	class VamClothing : IClothing
	{
		class Item
		{
			private Person person_;
			private DAZClothingItem ci_;
			private DAZSkinWrapSwitcher wrap_ = null;

			public Item(Person p, DAZClothingItem ci)
			{
				person_ = p;
				ci_ = ci;
			}

			public bool Enabled
			{
				set
				{
					if (value != ci_.isActiveAndEnabled)
					{
						Cue.LogInfo(
							ToString() + ": " + (value ? "enabled" : "disabled"));

						ci_.characterSelector.SetActiveClothingItem(ci_, value);
					}
				}
			}

			public string State
			{
				set
				{
					if (wrap_ == null)
					{
						wrap_ = ci_.GetComponentInChildren<DAZSkinWrapSwitcher>();
						if (wrap_ == null)
						{
							Cue.LogError("clothing " + ci_.name + " has no wrap switcher");
							return;
						}
					}

					if (value != wrap_.currentWrapName)
					{
						Cue.LogInfo(
							ToString() + ": state " +
							wrap_.currentWrapName + "->" + value);

						wrap_.currentWrapName = value;
					}
				}
			}

			public void SetToShowGenitals()
			{
				if (ci_.disableAnatomy)
				{
					Enabled = false;
				}
				else
				{
					var item = Resources.Clothing.FindItem(
						person_.Sex, ci_.name, ci_.tagsArray);

					if (item == null)
						return;

					if (item.hidesGenitalsBool)
					{
						Enabled = false;
					}
					else if (item.showsGenitalsBool)
					{
						Enabled = true;
					}
					else if (item.showsGenitalsState != "")
					{
						Enabled = true;
						State = item.showsGenitalsState;
					}
				}
			}

			public void SetToHideGenitals()
			{
				if (ci_.disableAnatomy)
				{
					Enabled = true;
				}
				else
				{
					var item = Resources.Clothing.FindItem(
						person_.Sex, ci_.name, ci_.tagsArray);

					if (item == null)
						return;

					if (item.showsGenitalsBool)
					{
						Enabled = false;
					}
					else if (item.hidesGenitalsBool)
					{
						Enabled = true;
					}
					else if (item.hidesGenitalsState != "")
					{
						Enabled = true;
						State = item.hidesGenitalsState;
					}
				}
			}

			public void SetToShowBreasts()
			{
				var item = Resources.Clothing.FindItem(
					person_.Sex, ci_.name, ci_.tagsArray);

				if (item == null)
					return;

				if (item.hidesBreastsBool)
				{
					Enabled = false;
				}
				else if (item.showsBreastsBool)
				{
					Enabled = true;
				}
				else if (item.showsBreastsState != "")
				{
					Enabled = true;
					State = item.showsBreastsState;
				}
			}

			public void SetToHideBreasts()
			{
				var item = Resources.Clothing.FindItem(
					person_.Sex, ci_.name, ci_.tagsArray);

				if (item == null)
					return;

				if (item.showsBreastsBool)
				{
					Enabled = false;
				}
				else if (item.hidesBreastsBool)
				{
					Enabled = true;
				}
				else if (item.hidesBreastsState != "")
				{
					Enabled = true;
					State = item.hidesBreastsState;
				}
			}

			public override string ToString()
			{
				return ci_.name;
			}
		}

		private Person person_;
		private DAZCharacterSelector char_;
		private List<Item> items_ = new List<Item>();
		private bool genitalsVisible_ = false;
		private bool breastsVisible_ = false;

		public VamClothing(Person p)
		{
			try
			{
				person_ = p;
				char_ = ((W.VamAtom)person_.Atom).Atom
					.GetComponentInChildren<DAZCharacterSelector>();

				foreach (var c in char_.clothingItems)
				{
					if (c.isActiveAndEnabled)
						items_.Add(new Item(person_, c));
				}

				GenitalsVisible = false;
				BreastsVisible = false;
			}
			catch (Exception e)
			{
				Cue.LogError("VamClothing: ctor failed, " + e.ToString());
			}
		}

		public bool GenitalsVisible
		{
			get
			{
				return genitalsVisible_;
			}

			set
			{
				genitalsVisible_ = value;

				if (value)
				{
					Cue.LogInfo(person_.ID + ": showing genitals");

					foreach (var i in items_)
						i.SetToShowGenitals();
				}
				else
				{
					Cue.LogInfo(person_.ID + ": hiding genitals");

					foreach (var i in items_)
						i.SetToHideGenitals();
				}
			}
		}

		public bool BreastsVisible
		{
			get
			{
				return breastsVisible_;
			}

			set
			{
				breastsVisible_ = value;

				if (value)
				{
					Cue.LogInfo(person_.ID + ": showing breasts");

					foreach (var i in items_)
						i.SetToShowBreasts();
				}
				else
				{
					Cue.LogInfo(person_.ID + ": hiding breasts");

					foreach (var i in items_)
						i.SetToHideBreasts();
				}
			}
		}

		public void OnPluginState(bool b)
		{
			if (!b)
			{
				foreach (var i in items_)
					i.Enabled = true;
			}
		}

		public void Dump()
		{
			foreach (var c in char_.clothingItems)
			{
				if (c.isActiveAndEnabled)
					Cue.LogInfo(c.name);
			}
		}
	}
}
