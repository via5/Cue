using SimpleJSON;

namespace Cue
{
	class UI
	{
		public static readonly bool VRMenuDebug = false;
		public static readonly bool VRMenuAlwaysVisible = false;

		private Sys.ISys sys_;
		private ScriptUI sui_ = new ScriptUI();
		private IMenu menu_ = null;
		private bool vr_ = false;

		public UI(Sys.ISys sys)
		{
			sys_ = sys;
			vr_ = sys_.IsVR;
		}

		public ScriptUI ScriptUI
		{
			get { return sui_; }
		}

		public JSONClass ToJSON()
		{
			var sui = sui_.ToJSON();
			if (sui == null)
				return null;

			var o = new JSONClass();
			o["sui"] = sui;

			if (menu_ != null && menu_.SelectedPerson != null)
				o["sel"] = menu_.SelectedPerson.ID;

			return o;
		}

		public void Load(JSONClass o)
		{
			if (o != null && o.HasKey("sui"))
				sui_.Load(o["sui"].AsObject);

			if (o != null && o.HasKey("sel"))
			{
				var id = o["sel"].Value;
				if (!string.IsNullOrEmpty(id))
				{
					var s = Cue.Instance.FindPerson(id);
					if (s != null)
						menu_.SelectedPerson = s;
				}
			}
		}

		public void CheckInput(float s)
		{
			var vr = sys_.IsVR || VRMenuDebug;

			if (vr_ != vr)
			{
				vr_ = vr;

				if (vr_)
					Logger.Global.Info("switched to vr");
				else
					Logger.Global.Info("switched to desktop");

				DestroyUI();
				CreateUI();
			}

			menu_.CheckInput(s);
		}

		public void OnPluginState(bool b)
		{
			if (b)
				CreateUI();
			else
				DestroyUI();

			if (sui_ != null)
				sui_.OnPluginState(true);
		}

		private void CreateUI()
		{
			Logger.Global.Info("creating ui");

			if (vr_ || VRMenuDebug)
				menu_ = new VRMenu(VRMenuDebug);
			else
				menu_ = new DesktopMenu();
		}

		private void DestroyUI()
		{
			Logger.Global.Info("destroying ui");
			menu_?.Destroy();
		}

		public void Update(float s)
		{
			menu_.Update();
			sui_.Update(s);
		}

		public void PostUpdate()
		{
			Instrumentation.Instance.Enabled = false;
			sui_.UpdateTickers();
		}
	}
}
