using SimpleJSON;
using System.Collections.Generic;

namespace Cue
{
	public interface IOption
	{
		void Load(JSONClass o, string key);
		JSONNode Save();
		void Reset();
	}

	public class BoolOption : IOption
	{
		public delegate void Handler(bool b);
		public event Handler Changed;

		private readonly string name_;
		private readonly bool init_;
		private readonly Sys.IBoolParameter param_;
		private bool value_;

		public BoolOption(string name, bool init)
		{
			name_ = name;
			init_ = init;
			value_ = init;
			param_ = Cue.Instance.Sys.RegisterBoolParameter(
				name_, OnParam, value_);
		}

		public bool Value
		{
			get
			{
				return value_;
			}

			set
			{
				if (value != value_)
				{
					value_ = value;

					if (param_ != null)
						param_.Value = value;

					Changed?.Invoke(value_);
				}
			}
		}

		public void Load(JSONClass o, string key)
		{
			bool b = false;
			if (J.OptBool(o, key, ref b))
				Value = b;
		}

		public JSONNode Save()
		{
			return new JSONData(Value);
		}

		public void Reset()
		{
			Value = init_;
		}

		private void OnParam(bool b)
		{
			Value = b;
		}
	}


	public class FloatOption : IOption
	{
		public delegate void Handler(float f);
		public event Handler Changed;

		private readonly string name_;
		private readonly float init_, min_, max_;
		private readonly Sys.IFloatParameter param_;
		private float value_;

		public FloatOption(string name, float init, float min, float max)
		{
			name_ = name;
			init_ = init;
			min_ = min;
			max_ = max;
			value_ = init;
			param_ = Cue.Instance.Sys.RegisterFloatParameter(
				name_, OnParam, value_, min, max);
		}

		public float Minimum
		{
			get { return min_; }
		}

		public float Maximum
		{
			get { return max_; }
		}

		public float Value
		{
			get
			{
				return value_;
			}

			set
			{
				if (value != value_)
				{
					value_ = value;

					if (param_ != null)
						param_.Value = value;

					Changed?.Invoke(value_);
				}
			}
		}

		public void Reset()
		{
			Value = init_;
		}

		public void Load(JSONClass o, string key)
		{
			float f = 0;
			if (J.OptFloat(o, key, ref f))
				Value = f;
		}

		public JSONNode Save()
		{
			return new JSONData(Value);
		}

		private void OnParam(float f)
		{
			Value = f;
		}
	}


	public class ColorOption : IOption
	{
		public delegate void Handler(Color c);
		public event Handler Changed;

		private readonly string name_;
		private readonly Color init_;
		private readonly Sys.IColorParameter param_;
		private Color value_;

		public ColorOption(string name, Color init)
		{
			name_ = name;
			init_ = init;
			value_ = init;
			param_ = Cue.Instance.Sys.RegisterColorParameter(
				name_, OnParam, value_);
		}

		public Color Value
		{
			get
			{
				return value_;
			}

			set
			{
				if (value != value_)
				{
					value_ = value;

					if (param_ != null)
						param_.Value = value;

					Changed?.Invoke(value_);
				}
			}
		}

		public void Load(JSONClass o, string key)
		{
			if (o.HasKey(key))
			{
				var co = o[key]?.AsObject;
				if (co != null)
				{
					var c = init_;

					c.r = J.OptFloat(co, "r", c.r);
					c.g = J.OptFloat(co, "g", c.g);
					c.b = J.OptFloat(co, "b", c.b);
					c.a = J.OptFloat(co, "a", c.a);

					Value = c;
				}
			}
		}

		public JSONNode Save()
		{
			var o = new JSONClass();

			o.Add("r", new JSONData(value_.r));
			o.Add("g", new JSONData(value_.g));
			o.Add("b", new JSONData(value_.b));
			o.Add("a", new JSONData(value_.a));

			return o;
		}

		public void Reset()
		{
			Value = init_;
		}

		private void OnParam(Color b)
		{
			Value = b;
		}
	}


	public class FinishOptions
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


	public class Options
	{
		public const string DefaultExtension = "json";
		public const string DefaultFile = "Default.json";

		public delegate void Handler();
		public event Handler Changed;

		private readonly BoolOption hjAudio_;
		private readonly BoolOption bjAudio_;
		private readonly BoolOption kissAudio_;
		private readonly BoolOption skinColor_;
		private readonly BoolOption skinGloss_;
		private readonly BoolOption hairLoose_;
		private readonly BoolOption handLinking_;
		private readonly BoolOption devMode_;
		private readonly FloatOption excitement_;
		private readonly FloatOption menuDelay_;
		private readonly BoolOption leftMenu_;
		private readonly BoolOption rightMenu_;
		private readonly BoolOption straponPhysical_;
		private readonly BoolOption ignoreCamera_;
		private readonly BoolOption mutePlayer_;
		private readonly BoolOption autoHands_;
		private readonly BoolOption autoHead_;
		private readonly BoolOption idlePose_;
		private readonly BoolOption excitedPose_;
		private readonly BoolOption choking_;
		private readonly BoolOption divLeftHand_;
		private readonly BoolOption divRightHand_;

		private readonly Dictionary<string, BoolOption> bools_ =
			new Dictionary<string, BoolOption>();

		private readonly Dictionary<string, FloatOption> floats_ =
			new Dictionary<string, FloatOption>();

		private readonly FinishOptions finish_ = new FinishOptions();
		private readonly CustomMenuItems menus_ = new CustomMenuItems();


		public Options()
		{
			hjAudio_ = AddBool("hjAudio", true);
			bjAudio_ = AddBool("bjAudio", true);
			kissAudio_ = AddBool("kissAudio", true);
			skinColor_ = AddBool("skinColor", true);
			skinGloss_ = AddBool("skinGloss", true);
			handLinking_ = AddBool("handLinking", true);
			hairLoose_ = AddBool("hairLoose", true);
			devMode_ = AddBool("devMode", false);
			excitement_ = AddFloat("excitement", 1.0f, 0.0f, 10.0f, "globalExcitementSpeed");
			menuDelay_ = AddFloat("menuDelay", 0.5f, 0.0f, 10.0f);
			leftMenu_ = AddBool("leftMenu", true, "leftHandVRMenu");
			rightMenu_ = AddBool("rightMenu", true, "rightHandVRMenu");
			straponPhysical_ = AddBool("straponPhysical", true);
			ignoreCamera_ = AddBool("ignoreCamera", true);
			mutePlayer_ = AddBool("mutePlayer", true);
			autoHands_ = AddBool("autoHands", true);
			autoHead_ = AddBool("autoHead", true);
			idlePose_ = AddBool("idlePose", true, "idleAnimation");
			excitedPose_ = AddBool("excitedPose", true, "excitedAnimation");
			choking_ = AddBool("choking", true);
			divLeftHand_ = AddBool("divLeftHand", true, "diviningRodLeftHand");
			divRightHand_ = AddBool("divRightHand", true, "diviningRodRightHand");
		}


		private BoolOption AddBool(string name, bool init, string paramName = null)
		{
			if (paramName == null)
				paramName = name;

			var o = new BoolOption("CueOption." + paramName, init);
			o.Changed += (b) => OnChanged();
			bools_.Add(name, o);
			return o;
		}

		private FloatOption AddFloat(string name, float init, float min, float max, string paramName = null)
		{
			if (paramName == null)
				paramName = name;

			var o = new FloatOption("CueOption." + paramName, init, min, max);
			o.Changed += (f) => OnChanged();
			floats_.Add(name, o);
			return o;
		}


		public bool HJAudio
		{
			get { return hjAudio_.Value; }
			set { hjAudio_.Value = value; }
		}

		public bool BJAudio
		{
			get { return bjAudio_.Value; }
			set { bjAudio_.Value = value; }
		}

		public bool KissAudio
		{
			get { return kissAudio_.Value; }
			set { kissAudio_.Value = value; }
		}

		public bool SkinColor
		{
			get { return skinColor_.Value; }
			set { skinColor_.Value = value; }
		}

		public bool SkinGloss
		{
			get { return skinGloss_.Value; }
			set { skinGloss_.Value = value; }
		}

		public bool HairLoose
		{
			get { return hairLoose_.Value; }
			set { hairLoose_.Value = value; }
		}

		public bool HandLinking
		{
			get { return handLinking_.Value; }
			set { handLinking_.Value = value; }
		}

		public bool LeftMenu
		{
			get { return leftMenu_.Value; }
			set { leftMenu_.Value = value; }
		}

		public bool RightMenu
		{
			get { return rightMenu_.Value; }
			set { rightMenu_.Value = value; }
		}

		public bool DevMode
		{
			get { return devMode_.Value; }
			set { devMode_.Value = value; }
		}

		public float Excitement
		{
			get { return excitement_.Value; }
			set { excitement_.Value = value; }
		}

		public float MenuDelay
		{
			get { return menuDelay_.Value; }
			set { menuDelay_.Value = value; }
		}

		public bool StraponPhysical
		{
			get { return straponPhysical_.Value; }
			set { straponPhysical_.Value = value; }
		}

		public bool IgnoreCamera
		{
			get { return ignoreCamera_.Value; }
			set { ignoreCamera_.Value = value; }
		}

		public bool MutePlayer
		{
			get { return mutePlayer_.Value; }
			set { mutePlayer_.Value = value; }
		}

		public bool AutoHands
		{
			get { return autoHands_.Value; }
			set { autoHands_.Value = value; }
		}

		public bool AutoHead
		{
			get { return autoHead_.Value; }
			set { autoHead_.Value = value; }
		}

		public bool IdlePose
		{
			get { return idlePose_.Value; }
			set { idlePose_.Value = value; }
		}

		public bool ExcitedPose
		{
			get { return excitedPose_.Value; }
			set { excitedPose_.Value = value; }
		}

		public bool Choking
		{
			get { return choking_.Value; }
			set { choking_.Value = value; }
		}

		public bool DiviningRodLeftHand
		{
			get { return divLeftHand_.Value; }
			set { divLeftHand_.Value = value; }
		}

		public bool DiviningRodRightHand
		{
			get { return divRightHand_.Value; }
			set { divRightHand_.Value = value; }
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

			foreach (var bo in bools_)
				o[bo.Key] = new JSONData(bo.Value.Value);

			foreach (var fo in floats_)
				o[fo.Key] = new JSONData(fo.Value.Value);

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
					hjAudio_.Value = false;
					bjAudio_.Value = false;
					kissAudio_.Value = true;
				}
			}

			foreach (var bo in bools_)
			{
				bool b = false;
				if (J.OptBool(o, bo.Key, ref b))
					bo.Value.Value = b;
			}

			foreach (var fo in floats_)
			{
				float f = 0;
				if (J.OptFloat(o, fo.Key, ref f))
					fo.Value.Value = f;
			}

			finish_.Load(o);

			if (o.HasKey("menus"))
				menus_.Load(o["menus"].AsArray);

			if (Cue.Instance.Sys.ForceDevMode)
				devMode_.Value = true;

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
