using SimpleJSON;
using System;
using System.Collections.Generic;

namespace Cue
{
	public interface ICustomMenuItem
	{
		CustomMenuItems Parent { get; set; }
		VUI.Panel CreateConfigWidget();
		VUI.Panel CreateMenuWidget();
		void Activate();
		JSONNode ToJSON();
	}


	public abstract class BasicCustomMenuItem : ICustomMenuItem
	{
		private CustomMenuItems parent_ = null;

		public CustomMenuItems Parent
		{
			get { return parent_; }
			set { parent_ = value; }
		}

		public VUI.Panel CreateConfigWidget()
		{
			var p = new VUI.Panel(new VUI.HorizontalFlow(10));
			DoCreateConfigWidget(p);
			return p;
		}

		public VUI.Panel CreateMenuWidget()
		{
			var p = new VUI.Panel(new VUI.HorizontalFlow(10));
			DoCreateMenuWidget(p);
			return p;
		}

		public abstract void Activate();

		public JSONNode ToJSON()
		{
			var o = new JSONClass();
			DoToJSON(o);
			return o;
		}

		protected abstract void DoCreateConfigWidget(VUI.Panel p);
		protected abstract void DoCreateMenuWidget(VUI.Panel p);
		protected abstract void DoToJSON(JSONClass o);
	}


	public class CustomButtonItem : BasicCustomMenuItem
	{
		private string caption_;
		private Sys.IActionTrigger trigger_;

		private CustomButtonItem(string caption, Sys.IActionTrigger trigger)
		{
			caption_ = caption;
			trigger_ = trigger;
		}

		public CustomButtonItem(string caption)
			: this(caption, Cue.Instance.Sys.CreateActionTrigger())
		{
		}

		public static CustomButtonItem FromJSON(JSONClass n)
		{
			try
			{
				var c = J.ReqString(n, "caption");

				if (!n.HasKey("trigger"))
					throw new LoadFailed("missing trigger");

				var t = Cue.Instance.Sys.LoadActionTrigger(n["trigger"].AsObject);
				if (t == null)
					throw new LoadFailed("failed to create trigger");

				return new CustomButtonItem(c, t);
			}
			catch (Exception e)
			{
				Logger.Global.Error("failed to load custom menu, " + e.ToString());
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
				Parent?.FireTriggersChanged();
			}
		}

		public Sys.IActionTrigger Trigger
		{
			get { return trigger_; }
		}

		public override void Activate()
		{
			trigger_?.Fire();
		}

		public void Edit(Action onDone = null)
		{
			trigger_?.Edit(onDone);
		}

		public override string ToString()
		{
			return $"CustomButtonItem.[{Caption}]";
		}

		protected override void DoCreateConfigWidget(VUI.Panel p)
		{
			var c = p.Add(new VUI.TextBox(Caption, "Button name"));
			c.Edited += OnCaption;
			p.Add(new VUI.Button("Edit actions...", OnEditTrigger));
			p.Add(new VUI.ToolButton("X", OnDelete));
		}

		protected override void DoCreateMenuWidget(VUI.Panel p)
		{
			p.Add(new VUI.Button(Caption, () =>
			{
				Activate();
			}));
		}

		protected override void DoToJSON(JSONClass o)
		{
			o["caption"] = caption_;
			o["trigger"] = trigger_.ToJSON();
		}

		private void OnEditTrigger()
		{
			Trigger.Edit(() => Parent.FireTriggersChanged());
		}

		private void OnCaption(string s)
		{
			Caption = s;
		}

		private void OnDelete()
		{
			Parent.RemoveItem(this);
		}
	}


	public class CustomMenuItems
	{
		public delegate void Handler();
		public event Handler Changed;

		private string defaultCaption_;
		private List<ICustomMenuItem> items_ = new List<ICustomMenuItem>();

		public CustomMenuItems(string defaultCaption="")
		{
			defaultCaption_ = defaultCaption;
		}

		public ICustomMenuItem[] Items
		{
			get { return items_.ToArray(); }
		}

		public void Save(JSONArray a)
		{
			if (items_.Count > 0)
			{
				foreach (var m in items_)
					a.Add(m.ToJSON());
			}
		}

		public void Load(JSONArray a)
		{
			items_.Clear();

			foreach (var mo in a.Childs)
			{
				var m = CustomButtonItem.FromJSON(mo.AsObject);
				if (m != null)
					Add(m);
			}

			OnTriggersChanged();
		}

		public ICustomMenuItem AddCustomItem()
		{
			var m = new CustomButtonItem(defaultCaption_);
			Add(m);
			OnTriggersChanged();
			return m;
		}

		private void Add(ICustomMenuItem t)
		{
			items_.Add(t);
			t.Parent = this;
		}

		public void RemoveItem(ICustomMenuItem m)
		{
			if (!items_.Contains(m))
			{
				Logger.Global.Error($"custom menu '{m}' not found");
				return;
			}

			m.Parent = null;
			items_.Remove(m);

			OnTriggersChanged();
		}

		public void FireTriggersChanged()
		{
			OnTriggersChanged();
		}

		private void OnTriggersChanged()
		{
			Cue.Instance.SaveLater();
			Changed?.Invoke();
		}
	}


	class FinishOptions
	{
		private float initialDelay_ = 5;
		private int lookAt_ = Finish.LookAtPersonality;
		private int orgasms_ = Finish.OrgasmsPersonality;
		private float orgasmsTime_ = 1;
		private int events_ = Finish.StopEventsAll;
		private CustomButtonItem button_ = new CustomButtonItem("Finish");

		public float InitialDelay
		{
			get { return initialDelay_; }
			set { initialDelay_ = value; Cue.Instance.Options.FireOnChanged(); }
		}

		public int LookAt
		{
			get { return lookAt_; }
			set { lookAt_ = value; Cue.Instance.Options.FireOnChanged(); }
		}

		public int Orgasms
		{
			get { return orgasms_; }
			set { orgasms_ = value; Cue.Instance.Options.FireOnChanged(); }
		}

		public float OrgasmsTime
		{
			get { return orgasmsTime_; }
			set { orgasmsTime_ = value; Cue.Instance.Options.FireOnChanged(); }
		}

		public int Events
		{
			get { return events_; }
			set { events_ = value; Cue.Instance.Options.FireOnChanged(); }
		}

		public CustomButtonItem Button
		{
			get { return button_; }
		}

		public void Load(JSONClass o)
		{
			J.OptFloat(o, "finishInitialDelay", ref initialDelay_);
			J.OptInt(o, "finishLookAt", ref lookAt_);
			J.OptInt(o, "finishOrgasms", ref orgasms_);
			J.OptFloat(o, "finishOrgasmsTime", ref orgasmsTime_);
			J.OptInt(o, "finishEvents", ref events_);

			if (o.HasKey("finishTrigger"))
				button_ = CustomButtonItem.FromJSON(o["finishTrigger"].AsObject);
		}

		public void Save(JSONClass o)
		{
			o["finishInitialDelay"] = new JSONData(initialDelay_);
			o["finishLookAt"] = new JSONData(lookAt_);
			o["finishOrgasms"] = new JSONData(orgasms_);
			o["finishOrgasmsTime"] = new JSONData(orgasmsTime_);
			o["finishEvents"] = new JSONData(events_);
			o["finishTrigger"] = button_.ToJSON();
		}
	}


	class Options
	{
		public const string DefaultExtension = "json";
		public const string DefaultFile = "Default.json";

		public delegate void Handler();
		public event Handler Changed;

		private bool hjAudio_ = true;
		private bool bjAudio_ = true;
		private bool kissAudio_ = true;
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
		private bool autoHands_ = true;
		private bool autoHead_ = true;
		private bool idlePose_ = true;
		private bool excitedPose_ = true;
		private bool choking_ = true;
		private bool divLeftHand_ = true;
		private bool divRightHand_ = true;

		private FinishOptions finish_ = new FinishOptions();
		private CustomMenuItems menus_ = new CustomMenuItems("Button");

		public Options()
		{
		}

		public bool HJAudio
		{
			get { return hjAudio_; }
			set { hjAudio_ = value; OnChanged(); }
		}

		public bool BJAudio
		{
			get { return bjAudio_; }
			set { bjAudio_ = value; OnChanged(); }
		}

		public bool KissAudio
		{
			get { return kissAudio_; }
			set { kissAudio_ = value; OnChanged(); }
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

		public bool AutoHands
		{
			get { return autoHands_; }
			set { autoHands_ = value; OnChanged(); }
		}

		public bool AutoHead
		{
			get { return autoHead_; }
			set { autoHead_ = value; OnChanged(); }
		}

		public bool IdlePose
		{
			get { return idlePose_; }
			set { idlePose_ = value; OnChanged(); }
		}

		public bool ExcitedPose
		{
			get { return excitedPose_; }
			set { excitedPose_ = value; OnChanged(); }
		}

		public bool Choking
		{
			get { return choking_; }
			set { choking_ = value; OnChanged(); }
		}

		public bool DiviningRodLeftHand
		{
			get { return divLeftHand_; }
			set { divLeftHand_ = value; OnChanged(); }
		}

		public bool DiviningRodRightHand
		{
			get { return divRightHand_; }
			set { divRightHand_ = value; OnChanged(); }
		}

		public FinishOptions Finish
		{
			get { return finish_; }
		}

		public CustomMenuItems CustomMenuItems
		{
			get { return menus_; }
		}

		public JSONNode ToJSON()
		{
			var o = new JSONClass();

			o["hjAudio"] = new JSONData(hjAudio_);
			o["bjAudio"] = new JSONData(bjAudio_);
			o["kissAudio"] = new JSONData(kissAudio_);
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
			o["autoHands"] = new JSONData(autoHands_);
			o["autoHead"] = new JSONData(autoHead_);
			o["idlePose"] = new JSONData(idlePose_);
			o["excitedPose"] = new JSONData(excitedPose_);
			o["choking"] = new JSONData(choking_);
			o["divLeftHand"] = new JSONData(divLeftHand_);
			o["divRightHand"] = new JSONData(divRightHand_);
			o["version"] = new JSONData(Version.String);

			finish_.Save(o);

			var menusArray = new JSONArray();
			menus_.Save(menusArray);
			o["menus"] = menusArray;

			return o;
		}

		public void Load(JSONClass o)
		{
			if (o.HasKey("muteSfx"))
			{
				bool b = J.OptBool(o, "muteSfx", false);
				if (b)
				{
					hjAudio_ = false;
					bjAudio_ = false;
					kissAudio_ = true;
				}
			}
			else
			{
				J.OptBool(o, "hjAudio", ref hjAudio_);
				J.OptBool(o, "bjAudio", ref bjAudio_);
				J.OptBool(o, "kissAudio", ref kissAudio_);
			}

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
			J.OptBool(o, "autoHands", ref autoHands_);
			J.OptBool(o, "autoHead", ref autoHead_);
			J.OptBool(o, "idlePose", ref idlePose_);
			J.OptBool(o, "excitedPose", ref excitedPose_);
			J.OptBool(o, "divLeftHand", ref divLeftHand_);
			J.OptBool(o, "divRightHand", ref divRightHand_);
			J.OptBool(o, "choking", ref choking_);

			finish_.Load(o);

			if (o.HasKey("menus"))
				menus_.Load(o["menus"].AsArray);

			if (Cue.Instance.Sys.ForceDevMode)
				devMode_ = true;

			OnChanged();
		}

		public void FireOnChanged()
		{
			OnChanged();
		}

		private void OnChanged()
		{
			Cue.Instance.SaveLater();
			Changed?.Invoke();
		}
	}
}
