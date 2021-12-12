using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;

namespace ClockwiseSilver {
	public static class Helpers
	{
		public static List<string> GetFemaleChoices(JSONStorableStringChooser chooser=null)
        {
			List<string> choices = SuperController.singleton.GetAtoms().Where(a => a.category == "People" && a.GetComponentInChildren<DAZCharacter>().name.StartsWith("female")).Select(a => a.name).ToList();
			if (chooser != null)
			{
				chooser.choices = choices;
				if (chooser.val == null && choices.Count > 0)
				{
					chooser.val = choices[0];
				}
			}
			return choices;
        }
		
		public static List<string> GetMaleChoices(JSONStorableStringChooser chooser=null)
        {
			List<string> choices = SuperController.singleton.GetAtoms().Where(a => a.category == "People" && !a.GetComponentInChildren<DAZCharacter>().name.Contains("female")).Select(a => a.name).ToList();
			if (chooser != null)
			{
				chooser.choices = choices;
				if (chooser.val == null && choices.Count > 0)
				{
					chooser.val = choices[0];
				}
			}
			return choices;
        }
		
		public static List<string> GetMaleAndToyChoices(JSONStorableStringChooser chooser=null)
        {
            List<string> choices = new List<string>();
			List<string> mChoices = SuperController.singleton.GetAtoms().Where(a => a.category == "People" && !a.GetComponentInChildren<DAZCharacter>().name.Contains("female")).Select(a => a.name).ToList();
            List<string> dChoices = SuperController.singleton.GetAtoms().Where(atom => atom.name.StartsWith("Dildo")).Select(atom => atom.name).ToList();
			List<string> cChoices = SuperController.singleton.GetAtoms().Where(atom => atom.name.StartsWith("Cock")).Select(atom => atom.name).ToList();			
			
			for (int i = 0; i < mChoices.Count; i++)
			{
				choices.Add(mChoices[i]);
			}
			for (int i = 0; i < dChoices.Count; i++)
			{
				choices.Add(dChoices[i]);
			}
			for (int i = 0; i < cChoices.Count; i++)
			{
				choices.Add(cChoices[i]);
			}
			
			if (chooser != null)
			{
				chooser.choices = choices;
				if (chooser.val == null && choices.Count > 0)
				{
					chooser.val = choices[0];
				}
			}
			return choices;
        }
		
		public static string[] CommonFreeControllers()
		{
			return new string[] { "lHandControl", "rHandControl", "lFootControl", "rFootControl",
				"lArmControl", "rArmControl", "lElbowControl", "rElbowControl",	"lThighControl", "rThighControl", "pelvisControl",
				"hipControl", "chestControl", "neckControl", "headControl", "abdomenControl", "abdomen2Control"};
		}
		
		public static string[] CommonRigidbodies()
		{
			return new string[] { "lHand", "rHand", "lShldr", "rShldr", "lForeArm", "rForeArm", "lFoot", "rFoot",
				"lThigh", "rThigh", "pelvis", "hip", "chest", "neck", "head", "abdomen", "abdomen2"};
		}
		
		public static string RigidbodyToFreeController(string rigidbodyName)
		{
			return rigidbodyName.Replace("Shldr", "Arm").Replace("ForeArm", "Elbow") + "Control";
		}
		
		public static string FreeControllerToRigidbody(string freeControllerName)
		{
			return freeControllerName.Replace("Arm", "Shldr").Replace("Elbow", "ForeArm").Replace("Control", "");
		}
		
		public static float Rollover(float x)
		{
			if (x > 360f) { return x - 360f; }
			else if (x < 0f) { return 360f - x; }
			return x;
		}
		
		//----------------------------------------------------------Easings
		public static float QuadraticInOut(float x)
		{
			if ((x *= 2f) < 1f) return 0.5f * x * x;
            return -0.5f * ((x -= 1f) * (x - 2f) - 1f);
		}
		
		public static float QuartOut(float x)
		{
			return 1 - Mathf.Pow(1 - x, 4);
		}
		
		//----------------------------------------------------------UI
		
		public static JSONStorableString SetupHeader(MVRScript script, string label, string text, bool right, float height=10f)
		{
			JSONStorableString header = new JSONStorableString(label, text);
			script.CreateTextField(header, right).height = height;
			return header;
		}
		
		//----------------------------------------------------------MacGruber Utils
		//Credit MacGruber 2020
		public static JSONStorable FindPlugin(MVRScript self, string fullClassName)
		{
			List<string> names = self.containingAtom.GetStorableIDs();
			//foreach (string str in names) { SuperController.LogMessage(str); }
			string pluginName = names.Find(s => s.StartsWith("plugin#") && s.Contains(fullClassName));
			if (pluginName != null && pluginName != "")
			{
				return self.containingAtom.GetStorableByID(pluginName);
			}
			else
			{
				return null;
			}
		}
		// Create VaM-UI Toggle button
		public static JSONStorableBool SetupToggle(MVRScript script, string label, bool defaultValue, bool rightSide)
		{
			JSONStorableBool storable = new JSONStorableBool(label, defaultValue);
			storable.storeType = JSONStorableParam.StoreType.Full;
			script.CreateToggle(storable, rightSide);
			script.RegisterBool(storable);
			return storable;
		}
		
		// Create VaM-UI Float slider
		public static JSONStorableFloat SetupSlider(MVRScript script, string label, float defaultValue, float minValue, float maxValue, bool rightSide)
		{
			JSONStorableFloat storable = new JSONStorableFloat(label, defaultValue, minValue, maxValue, true, true);
			storable.storeType = JSONStorableParam.StoreType.Full;
			script.CreateSlider(storable, rightSide);
			script.RegisterFloat(storable);
			return storable;
		}
		
		// Create VaM-UI ColorPicker
		public static JSONStorableColor SetupColor(MVRScript script, string label, Color color, bool rightSide)
		{
			HSVColor hsvColor = HSVColorPicker.RGBToHSV(color.r, color.g, color.b);
			JSONStorableColor storable = new JSONStorableColor(label, hsvColor);
			storable.storeType = JSONStorableParam.StoreType.Full;
			script.CreateColorPicker(storable, rightSide);
			script.RegisterColor(storable);
			return storable;
		}
		
		// Get directory path where the plugin is located. Based on Alazi's & VAMDeluxe's method.
		public static string GetPluginPath(MVRScript self)
		{
			string id = self.name.Substring(0, self.name.IndexOf('_'));
            string filename = self.manager.GetJSON()["plugins"][id].Value;
            return filename.Substring(0, filename.LastIndexOfAny(new char[] { '/', '\\' }));
		}
	}
}