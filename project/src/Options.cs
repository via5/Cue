using SimpleJSON;
using System;
using System.Collections.Generic;

namespace Cue
{
	class CustomMenu
	{
		private string caption_;
		private Sys.IActionTrigger trigger_;

		private CustomMenu(string caption, Sys.IActionTrigger trigger)
		{
			caption_ = caption;
			trigger_ = trigger;
		}

		public CustomMenu()
		{
			caption_ = "Button";
			trigger_ = Cue.Instance.Sys.CreateActionTrigger();
		}

		public static CustomMenu FromJSON(JSONClass n)
		{
			try
			{
				var c = J.ReqString(n, "caption");

				if (!n.HasKey("trigger"))
					throw new LoadFailed("missing trigger");

				var t = Cue.Instance.Sys.LoadActionTrigger(n["trigger"].AsObject);
				if (t == null)
					throw new LoadFailed("failed to create trigger");

				return new CustomMenu(c, t);
			}
			catch (Exception e)
			{
				Cue.LogError("failed to load custom menu, " + e.ToString());
				return null;
			}
		}

		public string Caption
		{
			get
			{
				return caption_;
			}

			set
			{
				caption_ = value;
				trigger_.Name = value;
				Cue.Instance.Options.ForceMenusChanged();
			}
		}

		public Sys.IActionTrigger Trigger
		{
			get { return trigger_; }
		}

		public JSONNode ToJSON()
		{
			var o = new JSONClass();

			o["caption"] = caption_;
			o["trigger"] = trigger_.ToJSON();

			return o;
		}
	}


	class Options
	{
		public delegate void Handler();
		public event Handler Changed, MenusChanged;

		private bool muteSfx_ = false;
		private bool skinColor_ = true;
		private bool skinGloss_ = true;
		private bool hairLoose_ = true;
		private bool handLinking_ = true;
		private bool devMode_ = false;
		private float excitement_ = 1.0f;
		private float menuDelay_ = 0.5f;
		private bool leftMenu_ = true;
		private bool rightMenu_ = true;
		private bool straponPhysical_ = true;
		private bool ignoreCamera_ = true;
		private bool mutePlayer_ = true;

		private List<CustomMenu> menus_ = new List<CustomMenu>();

		public Options()
		{
		}

		public bool MuteSfx
		{
			get { return muteSfx_; }
			set { muteSfx_ = value; OnChanged(); }
		}

		public bool SkinColor
		{
			get { return skinColor_; }
			set { skinColor_ = value; OnChanged(); }
		}

		public bool SkinGloss
		{
			get { return skinGloss_; }
			set { skinGloss_ = value; OnChanged(); }
		}

		public bool HairLoose
		{
			get { return hairLoose_; }
			set { hairLoose_ = value; OnChanged(); }
		}

		public bool HandLinking
		{
			get { return handLinking_; }
			set { handLinking_ = value; OnChanged(); }
		}

		public bool LeftMenu
		{
			get { return leftMenu_; }
			set { leftMenu_ = value; OnChanged(); }
		}

		public bool RightMenu
		{
			get { return rightMenu_; }
			set { rightMenu_ = value; OnChanged(); }
		}

		public bool DevMode
		{
			get { return devMode_; }
			set { devMode_ = value; OnChanged(); }
		}

		public float Excitement
		{
			get { return excitement_; }
			set { excitement_ = value; OnChanged(); }
		}

		public float MenuDelay
		{
			get { return menuDelay_; }
			set { menuDelay_ = value; OnChanged(); }
		}

		public bool StraponPhysical
		{
			get { return straponPhysical_; }
			set { straponPhysical_ = value; OnChanged(); }
		}

		public bool IgnoreCamera
		{
			get { return ignoreCamera_; }
			set { ignoreCamera_ = value; OnChanged(); }
		}

		public bool MutePlayer
		{
			get { return mutePlayer_; }
			set { mutePlayer_ = value; OnChanged(); }
		}

		public CustomMenu[] Menus
		{
			get { return menus_.ToArray(); }
		}

		public CustomMenu AddCustomMenu()
		{
			var m = new CustomMenu();
			menus_.Add(m);
			OnMenusChanged();
			return m;
		}

		public void RemoveCustomMenu(CustomMenu m)
		{
			if (!menus_.Contains(m))
			{
				Cue.LogError($"custom menu '{m.Caption}' not found");
				return;
			}

			menus_.Remove(m);
			OnMenusChanged();
		}

		public void ForceMenusChanged()
		{
			OnMenusChanged();
		}

		public JSONNode ToJSON()
		{
			var o = new JSONClass();

			o["muteSfx"] = new JSONData(muteSfx_);
			o["skinColor"] = new JSONData(skinColor_);
			o["skinGloss"] = new JSONData(skinGloss_);
			o["handLinking"] = new JSONData(handLinking_);
			o["hairLoose"] = new JSONData(hairLoose_);
			o["devMode"] = new JSONData(devMode_);
			o["excitement"] = new JSONData(excitement_);
			o["menuDelay"] = new JSONData(menuDelay_);
			o["leftMenu"] = new JSONData(leftMenu_);
			o["rightMenu"] = new JSONData(rightMenu_);
			o["straponPhysical"] = new JSONData(straponPhysical_);
			o["ignoreCamera"] = new JSONData(ignoreCamera_);
			o["mutePlayer"] = new JSONData(mutePlayer_);

			if (menus_.Count > 0)
			{
				var mo = new JSONArray();

				foreach (var m in menus_)
					mo.Add(m.ToJSON());

				o["menus"] = mo;
			}

			return o;
		}

		public void Load(JSONClass o)
		{
			menus_.Clear();

			J.OptBool(o, "muteSfx", ref muteSfx_);
			J.OptBool(o, "skinColor", ref skinColor_);
			J.OptBool(o, "skinGloss", ref skinGloss_);
			J.OptBool(o, "handLinking", ref handLinking_);
			J.OptBool(o, "hairLoose", ref hairLoose_);
			J.OptBool(o, "devMode", ref devMode_);
			J.OptFloat(o, "excitement", ref excitement_);
			J.OptFloat(o, "menuDelay", ref menuDelay_);
			J.OptBool(o, "leftMenu", ref leftMenu_);
			J.OptBool(o, "rightMenu", ref rightMenu_);
			J.OptBool(o, "straponPhysical", ref straponPhysical_);
			J.OptBool(o, "ignoreCamera", ref ignoreCamera_);
			J.OptBool(o, "mutePlayer", ref mutePlayer_);

			if (o.HasKey("menus"))
			{
				var a = o["menus"].AsArray;

				foreach (var mo in a.Childs)
				{
					var m = CustomMenu.FromJSON(mo.AsObject);
					if (m != null)
						menus_.Add(m);
				}
			}

			OnChanged();
			OnMenusChanged();
		}

		private void OnChanged()
		{
			Cue.Instance.SaveLater();
			Changed?.Invoke();
		}

		private void OnMenusChanged()
		{
			Cue.Instance.SaveLater();
			MenusChanged?.Invoke();
		}
	}
}
