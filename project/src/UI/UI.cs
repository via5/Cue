using SimpleJSON;

namespace Cue
{
	class UI
	{
		public static readonly bool VRMenuDebug = false;
		public static readonly bool VRMenuAlwaysOpened = false;

		private Sys.ISys sys_;
		private ScriptUI sui_ = null;
		private VRMenu vrMenu_ = null;
		private DesktopMenu desktopMenu_ = null;
		private Controls controls_ = null;
		private bool vr_ = false;

		public UI(Sys.ISys sys)
		{
			sys_ = sys;
			vr_ = sys_.IsVR;
			sui_ = new ScriptUI();
			vrMenu_ = new VRMenu();
			desktopMenu_ = new DesktopMenu();
			controls_ = new Controls();
		}

		public Controls Controls
		{
			get { return controls_; }
		}

		public JSONClass ToJSON()
		{
			var sui = sui_.ToJSON();
			if (sui == null)
				return null;

			var o = new JSONClass();
			o["sui"] = sui;

			return o;
		}

		public void Load(JSONClass o)
		{
			if (o != null && o.HasKey("sui"))
				sui_.Load(o["sui"].AsObject);
		}

		public void CheckInput()
		{
			var vr = sys_.IsVR || VRMenuDebug;

			if (vr_ != vr)
			{
				vr_ = vr;

				if (vr_)
					Cue.LogInfo("switched to vr");
				else
					Cue.LogInfo("switched to desktop");

				DestroyUI();
				CreateUI();
			}

			if (sys_.IsPlayMode || VRMenuAlwaysOpened)
			{
				if (vr_ || VRMenuDebug)
					CheckVRInput();

				if (!vr_ || VRMenuDebug)
					CheckDesktopInput();
			}
			else
			{
				controls_.HoverTargetVisible = false;
				vrMenu_.Hide();
			}
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

		public void OpenScriptUI()
		{
		}

		private void CreateUI()
		{
			Cue.LogInfo("creating ui");

			controls_.Create();
			sui_.Init();

			if (vr_)
			{
				vrMenu_?.Create(VRMenuDebug);
			}
			else
			{
				desktopMenu_?.Create();
				desktopMenu_.Visible = true;

				var ps = Cue.Instance.ActivePersons;

				if (ps.Length > 0)
				{
					foreach (var p in Cue.Instance.ActivePersons)
					{
						if (p.Atom.Selected)
						{
							desktopMenu_.Selected = p;
							break;
						}
					}

					if (desktopMenu_.Selected == null)
						desktopMenu_.Selected = ps[0];
				}
			}
		}

		private void DestroyUI()
		{
			Cue.LogInfo("destroying ui");

			controls_?.Destroy();
			vrMenu_?.Destroy();
			desktopMenu_?.Destroy();
		}

		private void CheckVRInput()
		{
			var lh = sys_.Input.GetLeftHovered();
			var rh = sys_.Input.GetRightHovered();

			bool hoverTargetVisible = false;

			if (sys_.Input.ShowLeftMenu || VRMenuAlwaysOpened)
			{
				vrMenu_.ShowLeft();

				if (!(rh.o is Person))
				{
					hoverTargetVisible = true;
					controls_.HoverTargetPosition = rh.pos;
				}
			}
			else if (sys_.Input.ShowRightMenu)
			{
				vrMenu_.ShowRight();

				if (!(lh.o is Person))
				{
					hoverTargetVisible = true;
					controls_.HoverTargetPosition = lh.pos;
				}
			}
			else
			{
				vrMenu_.Hide();
			}

			controls_.HoverTargetVisible = hoverTargetVisible;

			if (lh.o != null)
			{
				if (sys_.Input.RightAction)
					DoAction(lh.o, rh);
			}
			else if (rh.o != null)
			{
				if (sys_.Input.LeftAction)
					DoAction(rh.o, lh);
			}
		}

		private bool DoAction(IObject src, Sys.HoveredInfo hit)
		{
			if (Cue.Instance.Options.AllowMovement)
			{
				if (src != null && hit.o != null && hit.o != src)
				{
					Cue.LogInfo($"{src}: interacting with {hit.o}");
					if (src.InteractWith(hit.o))
						return true;
				}

				if (src != null && hit.hit)
				{
					Cue.LogInfo($"{src}: hit on {hit.pos}");

					if (src == Cue.Instance.Player && src.Possessed)
					{
						Cue.LogInfo("refusing to move the player");
						return true;
					}

					src.MoveToManual(null, hit.pos, Vector3.Bearing(hit.pos - src.Position));
					return true;
				}
			}

			return false;
		}

		private void CheckDesktopInput()
		{
			var h = sys_.Input.GetMouseHovered();

			bool hoverTargetVisible = false;

			if (h.hit)
			{
				if (!(h.o is Person))
				{
					hoverTargetVisible = true;
					controls_.HoverTargetPosition = h.pos;
				}
			}

			controls_.HoverTargetVisible = hoverTargetVisible;


			if (sys_.Input.Select)
			{
				desktopMenu_.Selected = h.o as Person;
			}

			desktopMenu_.Hovered = h.o;
			controls_.Hovered = h.o;

			if (sys_.Input.Action)
				DoAction(desktopMenu_.Selected, h);

			if (sys_.Input.ToggleControls)
				controls_.Visible = !controls_.Visible;
		}

		public void Update(float s)
		{
			controls_?.Update();
			vrMenu_?.Update();

			desktopMenu_?.Update();
			sui_.Update(s);
		}

		public void PostUpdate()
		{
			sui_.UpdateTickers();
		}
	}
}
