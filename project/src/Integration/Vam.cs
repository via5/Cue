using System;
using System.Collections.Generic;
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
			transform.position = W.VamU.ToUnity(v);
		}

		public void LateUpdate()
		{
			if (hasPos_)
			{
				transform.position = W.VamU.ToUnity(pos_);
				hasPos_ = false;
			}
		}
	}

	class VamEyes : IEyes
	{
		private Person person_;
		private W.VamStringChooserParameter lookMode_;
		private W.VamFloatParameter leftRightAngle_;
		private W.VamFloatParameter upDownAngle_;
		private W.VamBoolParameter blink_;
		private Rigidbody eyes_;
		private VamEyesBehaviour eyesImpl_ = null;
		private IObject object_ = null;

		public VamEyes(Person p)
		{
			person_ = p;
			lookMode_ = new W.VamStringChooserParameter(p, "Eyes", "lookMode");
			leftRightAngle_ = new W.VamFloatParameter(p, "Eyes", "leftRightAngleAdjust");
			upDownAngle_ = new W.VamFloatParameter(p, "Eyes", "upDownAngleAdjust");
			blink_ = new W.VamBoolParameter(p, "EyelidControl", "blinkEnabled");

			eyes_ = Cue.Instance.VamSys?.FindRigidbody(person_, "eyeTargetControl");

			if (eyes_ == null)
			{
				Cue.LogError("atom " + p.ID + " has no eyeTargetControl");
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

		public bool Blink
		{
			get { return blink_.GetValue(); }
			set { blink_.SetValue(value); }
		}

		public void LookAt(IObject o)
		{
			object_ = o;
			lookMode_.SetValue("Target");
			eyesImpl_.SetPosition(object_.EyeInterest);
		}

		public void LookAt(Vector3 p)
		{
			object_ = null;
			lookMode_.SetValue("Target");
			eyesImpl_.SetPosition(p);
		}

		public void LookInFront()
		{
			object_ = null;

			eyesImpl_.SetPosition(
				object_.EyeInterest +
				Vector3.Rotate(new Vector3(0, 0, 1), person_.Bearing));

			lookMode_.SetValue("None");
		}

		public void LookAtNothing()
		{
			object_ = null;
			lookMode_.SetValue("None");
		}

		public void Update(float s)
		{
			if (object_ != null)
				eyesImpl_.SetPosition(object_.EyeInterest);
		}

		public override string ToString()
		{
			return $"vam: blink={blink_.GetValue()} mode={lookMode_.GetValue()}";
		}
	}


	class VamSpeaker : ISpeaker
	{
		private Person person_;
		private W.VamStringParameter text_;
		private string lastText_ = "";

		public VamSpeaker(Person p)
		{
			person_ = p;
			text_ = new W.VamStringParameter(p, "SpeechBubble", "bubbleText");
		}

		public void Say(string s)
		{
			text_.SetValue(s);
			lastText_ = s;
		}

		public override string ToString()
		{
			string s = "Vam: lastText=";

			if (lastText_ == "")
				s += "(none)";
			else
				s += lastText_;

			return s;
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

		public override string ToString()
		{
			return $"Vam: genitals={genitalsVisible_} breasts={breastsVisible_}";
		}
	}
}
