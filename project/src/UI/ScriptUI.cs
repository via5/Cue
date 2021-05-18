using System.Collections.Generic;

namespace Cue
{
	class ScriptUI
	{
		private VUI.Root root_ = null;

		private VUI.Panel panel_ = new VUI.Panel();
		private VUI.Tabs tabsWidget_ = new VUI.Tabs();
		private List<Tab> tabs_ = new List<Tab>();
		private float updateElapsed_ = 1000;
		private MiscTab misc_;

		public void Init()
		{
			misc_ = new MiscTab();

			foreach (var p in Cue.Instance.Persons)
				tabs_.Add(new PersonTab(p));

			tabs_.Add(misc_);
			tabs_.Add(new UnityTab());

			root_ = Cue.Instance.Sys.CreateScriptUI();
			root_.ContentPanel.Layout = new VUI.BorderLayout();
			root_.ContentPanel.Add(panel_, VUI.BorderLayout.Center);

			panel_.Layout = new VUI.BorderLayout();
			panel_.Add(tabsWidget_, VUI.BorderLayout.Center);

			foreach (var t in tabs_)
				tabsWidget_.AddTab(t.Title, t);
		}

		public void Update(float s, Tickers tickers)
		{
			updateElapsed_ += s;
			if (updateElapsed_ > 0.2f)
			{
				for (int i = 0; i < tabs_.Count; ++i)
					tabs_[i].Update(s);

				updateElapsed_ = 0;
			}

			if (tickers.update.Updated)
				misc_.UpdateTickers(tickers);

			root_.Update();
		}

		public void OnPluginState(bool b)
		{
			foreach (var t in tabs_)
				t.OnPluginState(b);
		}
	}


	abstract class Tab : VUI.Panel
	{
		public abstract string Title { get; }
		public abstract void Update(float s);

		public virtual void OnPluginState(bool b)
		{
			// no-op
		}
	}


	class MiscTab : Tab
	{
		private VUI.CheckBox navmeshes_ = new VUI.CheckBox("Navmeshes");
		private VUI.Button renav_ = new VUI.Button("Update nav");
		private VUI.Label update_ = new VUI.Label();
		private VUI.Label fixedUpdate_ = new VUI.Label();
		private VUI.Label input_ = new VUI.Label();
		private VUI.Label objects_ = new VUI.Label();
		private VUI.Label ui_ = new VUI.Label();

		private VUI.CheckBox logAnimation_;
		private VUI.CheckBox logAction_;
		private VUI.CheckBox logInteraction_;
		private VUI.CheckBox logAI_;
		private VUI.CheckBox logEvent_;
		private VUI.CheckBox logIntegration_;
		private VUI.CheckBox logObject_;
		private VUI.CheckBox logSlots_;
		private VUI.CheckBox logSys_;
		private VUI.CheckBox logClothing_;

		public MiscTab()
		{
			logAnimation_ = new VUI.CheckBox("Animation", CheckLog);
			logAction_ = new VUI.CheckBox("Action", CheckLog);
			logInteraction_ = new VUI.CheckBox("Interaction", CheckLog);
			logAI_ = new VUI.CheckBox("AI", CheckLog);
			logEvent_ = new VUI.CheckBox("Event", CheckLog);
			logIntegration_ = new VUI.CheckBox("Integration", CheckLog);
			logObject_ = new VUI.CheckBox("Object", CheckLog);
			logSlots_ = new VUI.CheckBox("Slots", CheckLog);
			logSys_ = new VUI.CheckBox("Sys", CheckLog);
			logClothing_ = new VUI.CheckBox("Clothing", CheckLog);

			Layout = new VUI.VerticalFlow();
			Add(navmeshes_);
			Add(renav_);

			var gl = new VUI.GridLayout(2);
			gl.HorizontalStretch = new List<bool>() { false, true };
			gl.HorizontalSpacing = 40;

			var p = new VUI.Panel(gl);

			p.Add(new VUI.Label("Update"));
			p.Add(update_);

			p.Add(new VUI.Label("  Input"));
			p.Add(input_);

			p.Add(new VUI.Label("  Objects"));
			p.Add(objects_);

			p.Add(new VUI.Label("  UI"));
			p.Add(ui_);

			p.Add(new VUI.Label("Fixed Update"));
			p.Add(fixedUpdate_);

			Add(p);
			Add(new VUI.Spacer(30));

			Add(new VUI.Label("Logs", UnityEngine.FontStyle.Bold));
			Add(logAnimation_);
			Add(logAction_);
			Add(logInteraction_);
			Add(logAI_);
			Add(logEvent_);
			Add(logIntegration_);
			Add(logObject_);
			Add(logSlots_);
			Add(logSys_);
			Add(logClothing_);

			navmeshes_.Changed += (b) => Cue.Instance.Sys.Nav.Render = b;
			renav_.Clicked += Cue.Instance.Sys.Nav.Update;
		}

		public override string Title
		{
			get { return "Stuff"; }
		}

		public override void Update(float s)
		{
		}

		public void UpdateTickers(Tickers tickers)
		{
			if (IsVisibleOnScreen())
			{
				update_.Text = tickers.update.ToString();
				input_.Text = tickers.input.ToString();
				objects_.Text = tickers.objects.ToString();
				ui_.Text = tickers.ui.ToString();
				fixedUpdate_.Text = tickers.fixedUpdate.ToString();
			}
		}

		private void CheckLog(bool b)
		{
			int e = 0;

			if (logAnimation_.Checked) e |= Logger.Animation;
			if (logAction_.Checked) e |= Logger.Action;
			if (logInteraction_.Checked) e |= Logger.Interaction;
			if (logAI_.Checked) e |= Logger.AI;
			if (logEvent_.Checked) e |= Logger.Event;
			if (logIntegration_.Checked) e |= Logger.Integration;
			if (logObject_.Checked) e |= Logger.Object;
			if (logSlots_.Checked) e |= Logger.Slots;
			if (logSys_.Checked) e |= Logger.Sys;
			if (logClothing_.Checked) e |= Logger.Clothing;

			Logger.Enabled = e;
		}
	}


	class UnityTab : Tab
	{
		class UnityObject
		{
			public UnityEngine.Transform t;
			public bool expanded = false;
			public int indent;

			public UnityObject(UnityEngine.Transform t, int indent)
			{
				this.t = t;
				this.indent = indent;
			}

			public override string ToString()
			{
				string s = new string(' ', indent * 4);

				if (expanded)
					s += "- ";
				else
					s += "+ ";

				s += t.name;

				return s;
			}
		}

		private VUI.Button refresh_ = new VUI.Button("Refresh");
		private VUI.ListView<UnityObject> objects_ = new VUI.ListView<UnityObject>();
		private List<UnityObject> items_ = new List<UnityObject>();

		public UnityTab()
		{
			Layout = new VUI.BorderLayout();
			Add(refresh_, VUI.BorderLayout.Top);
			Add(objects_, VUI.BorderLayout.Center);

			Refresh();

			refresh_.Clicked += Refresh;
			objects_.ItemIndexActivated += OnActivated;
		}

		public override string Title
		{
			get { return "Unity"; }
		}

		public override void Update(float s)
		{
		}

		private void Refresh()
		{
			items_.Clear();
			items_.Add(new UnityObject(SuperController.singleton.transform.root, 0));

			objects_.SetItems(items_);
		}

		private void OnActivated(int i)
		{
			var o = objects_.At(i);

			if (o.expanded)
			{
			}
			else
			{
				o.expanded = true;
				var newItems = new List<UnityObject>();

				foreach (UnityEngine.Transform c in o.t)
					newItems.Add(new UnityObject(c, o.indent + 1));

				U.NatSort(newItems);
				items_.InsertRange(i + 1, newItems);
				objects_.SetItems(items_);
			}
		}
	}


	class PersonTab : Tab
	{
		private Person person_;
		private VUI.Tabs tabsWidget_ = new VUI.Tabs();
		private List<Tab> tabs_ = new List<Tab>();

		public PersonTab(Person p)
		{
			person_ = p;

			tabs_.Add(new PersonStateTab(person_));
			tabs_.Add(new PersonAITab(person_));
			tabs_.Add(new PersonDumpTab(person_));
			tabs_.Add(new PersonBodyTab(person_));
			tabs_.Add(new PersonAnimationsTab(person_));

			foreach (var t in tabs_)
				tabsWidget_.AddTab(t.Title, t);

			Layout = new VUI.BorderLayout();
			Add(tabsWidget_, VUI.BorderLayout.Center);
		}

		public override string Title
		{
			get { return person_.ID; }
		}

		public override void Update(float s)
		{
			for (int i = 0; i < tabs_.Count; ++i)
				tabs_[i].Update(s);
		}

		public override void OnPluginState(bool b)
		{
			base.OnPluginState(b);
			foreach (var t in tabs_)
				t.OnPluginState(b);
		}
	}


	class PersonStateTab : Tab
	{
		private Person person_;
		private VUI.Label id_ = new VUI.Label();
		private VUI.Label pos_ = new VUI.Label();
		private VUI.Label dir_ = new VUI.Label();
		private VUI.Label bearing_ = new VUI.Label();
		private VUI.Label action_ = new VUI.Label();
		private VUI.Label player_ = new VUI.Label();
		private VUI.Label anim_ = new VUI.Label();
		private VUI.Label nav_ = new VUI.Label();
		private VUI.Label state_ = new VUI.Label();
		private VUI.Label gaze_ = new VUI.Label();

		private VUI.Label breath_ = new VUI.Label();
		private VUI.Label eyes_ = new VUI.Label();
		private VUI.Label gazer_ = new VUI.Label();
		private VUI.Label speech_ = new VUI.Label();
		private VUI.Label kiss_ = new VUI.Label();
		private VUI.Label handjob_ = new VUI.Label();
		private VUI.Label blowjob_ = new VUI.Label();
		private VUI.Label clothing_ = new VUI.Label();

		public PersonStateTab(Person p)
		{
			person_ = p;

			var gl = new VUI.GridLayout(2);
			gl.HorizontalSpacing = 20;
			gl.HorizontalStretch = new List<bool>() { false, true };

			var state = new VUI.Panel(gl);
			state.Add(new VUI.Label("ID"));
			state.Add(id_);

			state.Add(new VUI.Label("Position"));
			state.Add(pos_);

			state.Add(new VUI.Label("Direction"));
			state.Add(dir_);

			state.Add(new VUI.Label("Bearing"));
			state.Add(bearing_);

			state.Add(new VUI.Label("Action"));
			state.Add(action_);

			state.Add(new VUI.Label("Player"));
			state.Add(player_);

			state.Add(new VUI.Label("Animation"));
			state.Add(anim_);

			state.Add(new VUI.Label("Nav"));
			state.Add(nav_);

			state.Add(new VUI.Label("State"));
			state.Add(state_);

			state.Add(new VUI.Label("Gaze"));
			state.Add(gaze_);

			state.Add(new VUI.Spacer(20));
			state.Add(new VUI.Spacer(20));


			state.Add(new VUI.Label("Breath"));
			state.Add(breath_);

			state.Add(new VUI.Label("Eyes"));
			state.Add(eyes_);

			state.Add(new VUI.Label("Gazer"));
			state.Add(gazer_);

			state.Add(new VUI.Label("Speech"));
			state.Add(speech_);

			state.Add(new VUI.Label("Kiss"));
			state.Add(kiss_);

			state.Add(new VUI.Label("Handjob"));
			state.Add(handjob_);

			state.Add(new VUI.Label("Blowjob"));
			state.Add(blowjob_);

			state.Add(new VUI.Label("Clothing"));
			state.Add(clothing_);

			Layout = new VUI.BorderLayout();
			Add(state, VUI.BorderLayout.Top);
		}

		public override string Title
		{
			get { return "State"; }
		}

		public override void Update(float s)
		{
			id_.Text = person_.ID;
			pos_.Text = person_.Position.ToString();
			dir_.Text = person_.Direction.ToString();
			bearing_.Text = person_.Bearing.ToString();
			action_.Text = person_.Actions.ToString();
			nav_.Text = person_.MoveStateString();
			state_.Text = person_.State.ToString() + " " + (person_.Idle ? "(idle)" : "(not idle)");
			gaze_.Text = person_.Gaze.ToString();

			if (person_.Animator.CurrentPlayer == null)
				player_.Text = "(none)";
			else
				player_.Text = person_.Animator.CurrentPlayer.ToString();

			if (person_.Animator.CurrentAnimation == null)
				anim_.Text = "(none)";
			else
				anim_.Text = person_.Animator.CurrentAnimation.ToString();

			breath_.Text = person_.Breathing.ToString();
			eyes_.Text = person_.Gaze.Eyes.ToString();
			gazer_.Text = person_.Gaze.Gazer.ToString();
			speech_.Text = person_.Speech.ToString();
			kiss_.Text = person_.Kisser.ToString();
			handjob_.Text = person_.Handjob.ToString();
			blowjob_.Text = person_.Blowjob.ToString();
			clothing_.Text = person_.Clothing.ToString();
		}
	}


	class PersonAITab : Tab
	{
		private Person person_;
		private PersonAI ai_;

		private VUI.Label enabled_ = new VUI.Label();
		private VUI.Label event_ = new VUI.Label();
		private VUI.Label personality_ = new VUI.Label();

		private VUI.Label moodState_ = new VUI.Label();
		private VUI.Label moodExcitement_ = new VUI.Label();
		private VUI.Label moodLastRate_ = new VUI.Label();
		private VUI.Label moodMouthRate_ = new VUI.Label();
		private VUI.Label moodBreastsRate_ = new VUI.Label();
		private VUI.Label moodGenitalsRate_ = new VUI.Label();
		private VUI.Label moodPenetration_ = new VUI.Label();
		private VUI.Label moodDecayRate_ = new VUI.Label();
		private VUI.Label moodRateAdjust_ = new VUI.Label();

		private VUI.CheckBox forceExcitement_ = new VUI.CheckBox("Force excitement");
		private VUI.FloatTextSlider forceExcitementValue_ = new VUI.FloatTextSlider();

		public PersonAITab(Person p)
		{
			person_ = p;
			ai_ = (PersonAI)person_.AI;

			var gl = new VUI.GridLayout(2);
			gl.HorizontalSpacing = 20;
			gl.HorizontalStretch = new List<bool>() { false, true };

			var state = new VUI.Panel(gl);
			state.Add(new VUI.Label("Enabled"));
			state.Add(enabled_);

			state.Add(new VUI.Label("Event"));
			state.Add(event_);

			state.Add(new VUI.Label("Personality"));
			state.Add(personality_);

			state.Add(new VUI.Spacer(30));
			state.Add(new VUI.Spacer(30));

			state.Add(new VUI.Label("Mood", UnityEngine.FontStyle.Bold));
			state.Add(new VUI.Spacer(0));

			state.Add(new VUI.Label("State"));
			state.Add(moodState_);

			state.Add(new VUI.Label("Excitement"));
			state.Add(moodExcitement_);

			state.Add(new VUI.Label("Rate"));
			state.Add(moodLastRate_);

			state.Add(new VUI.Label("Mouth"));
			state.Add(moodMouthRate_);

			state.Add(new VUI.Label("Breasts"));
			state.Add(moodBreastsRate_);

			state.Add(new VUI.Label("Genitals"));
			state.Add(moodGenitalsRate_);

			state.Add(new VUI.Label("Penetration"));
			state.Add(moodPenetration_);

			state.Add(new VUI.Label("Decay"));
			state.Add(moodDecayRate_);

			state.Add(new VUI.Label("Rate adjust"));
			state.Add(moodRateAdjust_);

			state.Add(new VUI.Spacer(30));
			state.Add(new VUI.Spacer(30));

			state.Add(forceExcitement_);
			state.Add(forceExcitementValue_);


			Layout = new VUI.BorderLayout();
			Add(state, VUI.BorderLayout.Top);

			forceExcitement_.Changed += OnForceExcitementCheck;
			forceExcitementValue_.ValueChanged += OnForceExcitement;
		}

		public override string Title
		{
			get { return "AI"; }
		}

		public override void Update(float s)
		{
			string es = "";

			if (ai_.EventsEnabled)
			{
				if (es != "")
					es += "|";

				es += "events";
			}

			if (ai_.InteractionsEnabled)
			{
				if (es != "")
					es += "|";

				es += "interactions";
			}

			if (es == "")
				es = "nothing";

			enabled_.Text = es;

			event_.Text =
				(ai_.Event == null ? "(none)" : ai_.Event.ToString()) + " " +
				(ai_.ForcedEvent == null ? "(forced: none)" : $"(forced: {ai_.ForcedEvent})");

			personality_.Text = person_.Personality.ToString();

			var ss = person_.Personality.Sensitivity;

			moodState_.Text = person_.Personality.StateString;
			moodExcitement_.Text = person_.Excitement.ToString();
			moodLastRate_.Text = ss.Change.ToString("0.000000");
			moodMouthRate_.Text = ss.MouthRate.ToString();
			moodBreastsRate_.Text = ss.BreastsRate.ToString();
			moodGenitalsRate_.Text = ss.GenitalsRate.ToString();
			moodPenetration_.Text = ss.Penetration.ToString();
			moodDecayRate_.Text = ss.DecayRate.ToString();
			moodRateAdjust_.Text = ss.RateAdjust.ToString();
		}

		private void OnForceExcitementCheck(bool b)
		{
			if (b)
				person_.Excitement.ForceValue(forceExcitementValue_.Value);
			else
				person_.Excitement.ForceValue(-1);
		}

		private void OnForceExcitement(float f)
		{
			if (forceExcitement_.Checked)
				person_.Excitement.ForceValue(f);
		}
	}


	class PersonDumpTab : Tab
	{
		private Person person_;
		private VUI.ListView<string> list_ = new VUI.ListView<string>();

		public PersonDumpTab(Person person)
		{
			person_ = person;

			Layout = new VUI.BorderLayout(10);

			var p = new VUI.Panel(new VUI.HorizontalFlow(10));
			p.Add(new VUI.Button("Expression", DumpExpression));
			p.Add(new VUI.Button("Animation", DumpAnimation));

			Add(p, VUI.BorderLayout.Top);
			Add(list_, VUI.BorderLayout.Center);
		}

		public override string Title
		{
			get { return "Dump"; }
		}

		public override void Update(float s)
		{
		}

		private string I(int i)
		{
			return new string(' ', i * 4);
		}

		private void DumpExpression()
		{
			var pex = person_.Expression as Proc.Expression;
			if (pex == null)
			{
				list_.Clear();
				list_.AddItem("not procedural");
				return;
			}

			var items = new List<string>();

			foreach (var e in pex.All)
			{
				items.Add(I(1) + e.ToString());

				foreach (var g in e.Groups)
				{
					items.Add(I(2) + g.ToString());

					foreach (var m in g.Morphs)
					{
						items.Add(I(3) + m.Name);
						foreach (var line in m.ToString().Split('\n'))
							items.Add(I(4) + line);
					}
				}
			}

			list_.SetItems(items);
		}

		private void DumpAnimation()
		{
			var player = person_.Animator.CurrentPlayer as Proc.Player;
			var p = player?.Current;

			if (p == null)
			{
				list_.Clear();
				list_.AddItem("not procedural");
				return;
			}

			var items = new List<string>();

			items.Add(p.ToString());

			foreach (var s in p.Targets)
				DumpTarget(items, s, 1);

			list_.SetItems(items);
		}

		private void DumpTarget(List<string> items, Proc.ITarget t, int indent)
		{
			var lines = t.ToString().Split('\n');
			if (lines.Length > 0)
				items.Add(I(indent) + lines[0]);

			for (int i = 1; i < lines.Length; ++i)
				items.Add(I(indent + 1) + lines[i]);

			if (t is Proc.ITargetGroup)
			{
				foreach (var c in (t as Proc.ITargetGroup).Targets)
					DumpTarget(items, c, indent + 1);
			}
		}
	}


	class PersonBodyTab : Tab
	{
		struct PartWidgets
		{
			public BodyPart part;
			public VUI.Label name, triggering, close, grab, position, direction;
		}


		private readonly Person person_;
		private readonly List<PartWidgets> widgets_ = new List<PartWidgets>();

		public PersonBodyTab(Person ps)
		{
			person_ = ps;

			var gl = new VUI.GridLayout(6);
			gl.UniformHeight = false;
			var p = new VUI.Panel(gl);

			p.Add(new VUI.Label("Name", UnityEngine.FontStyle.Bold));
			p.Add(new VUI.Label("Trigger", UnityEngine.FontStyle.Bold));
			p.Add(new VUI.Label("Close", UnityEngine.FontStyle.Bold));
			p.Add(new VUI.Label("Grab", UnityEngine.FontStyle.Bold));
			p.Add(new VUI.Label("Position", UnityEngine.FontStyle.Bold));
			p.Add(new VUI.Label("Bearing", UnityEngine.FontStyle.Bold));

			for (int i = 0; i < person_.Body.Parts.Length; ++i)
			{
				var bp = person_.Body.Parts[i];

				var w = new PartWidgets();
				w.part = bp;

				w.name = new VUI.Label(bp.Name);
				w.triggering = new VUI.Label();
				w.close = new VUI.Label();
				w.grab = new VUI.Label();
				w.position = new VUI.Label();
				w.direction = new VUI.Label();

				int fontSize = 24;
				w.name.FontSize = fontSize;
				w.triggering.FontSize = fontSize;
				w.close.FontSize = fontSize;
				w.grab.FontSize = fontSize;
				w.position.FontSize = fontSize;
				w.direction.FontSize = fontSize;

				p.Add(w.name);
				p.Add(w.triggering);
				p.Add(w.close);
				p.Add(w.grab);
				p.Add(w.position);
				p.Add(w.direction);

				widgets_.Add(w);
			}

			Layout = new VUI.BorderLayout();
			Add(p, VUI.BorderLayout.Top);
		}

		public override string Title
		{
			get { return "Body"; }
		}

		public override void Update(float s)
		{
			if (IsVisibleOnScreen())
			{
				for (int i = 0; i < widgets_.Count; ++i)
				{
					var w = widgets_[i];

					if (w.part.Exists)
					{
						if (w.part.Sys.CanTrigger)
						{
							w.triggering.Text = w.part.Triggering.ToString();

							w.triggering.TextColor = (
								w.part.Triggering ?
								W.VamU.ToUnity(Color.Green) :
								VUI.Style.Theme.TextColor);
						}
						else
						{
							w.triggering.Text = "";
						}


						w.close.Text = w.part.Close.ToString();

						w.close.TextColor = (
							w.part.Close ?
							W.VamU.ToUnity(Color.Green) :
							VUI.Style.Theme.TextColor);


						if (w.part.Sys.CanGrab)
						{
							w.grab.Text = w.part.Grabbed.ToString();

							w.grab.TextColor = (
								w.part.Grabbed ?
								W.VamU.ToUnity(Color.Green) :
								VUI.Style.Theme.TextColor);
						}
						else
						{
							w.grab.Text = "";
						}


						w.position.Text = w.part.Position.ToString();
						w.direction.Text = Vector3.Bearing(w.part.Direction).ToString("0.0");
					}
				}
			}
		}
	}


	class PersonAnimationsTab : Tab
	{
		private Person person_;
		private VUI.ListView<Animation> anims_ = new VUI.ListView<Animation>();
		private VUI.CheckBox loop_ = new VUI.CheckBox("Loop");
		private VUI.CheckBox paused_ = new VUI.CheckBox("Paused");
		private VUI.TextSlider seek_ = new VUI.TextSlider();
		private IgnoreFlag ignore_ = new IgnoreFlag();
		private Animation sel_ = null;

		public PersonAnimationsTab(Person person)
		{
			person_ = person;

			Layout = new VUI.BorderLayout();

			var top = new VUI.Panel(new VUI.VerticalFlow());

			var p = new VUI.Panel(new VUI.HorizontalFlow());
			p.Add(new VUI.Button("Stand", () => person_.SetState(PersonState.Standing)));
			p.Add(new VUI.Button("Sit", () => person_.SetState(PersonState.Sitting)));
			p.Add(new VUI.Button("Crouch", () => person_.SetState(PersonState.Crouching)));
			p.Add(new VUI.Button("Straddle sit", () => person_.SetState(PersonState.SittingStraddling)));
			top.Add(p);

			p = new VUI.Panel(new VUI.HorizontalFlow());
			p.Add(new VUI.Button("Play", OnPlay));
			p.Add(new VUI.Button("Stop", OnStop));
			p.Add(paused_);
			p.Add(loop_);
			top.Add(p);

			p = new VUI.Panel(new VUI.BorderLayout());
			p.Add(seek_, VUI.BorderLayout.Center);
			top.Add(p);


			Add(top, VUI.BorderLayout.Top);
			Add(anims_, VUI.BorderLayout.Center);

			var items = new List<Animation>();
			foreach (var a in Resources.Animations.GetAll(Animation.NoType, person_.Sex))
				items.Add(a);

			anims_.SetItems(items);

			paused_.Changed += OnPaused;
			seek_.ValueChanged += OnSeek;
		}

		public override string Title
		{
			get { return "Animations"; }
		}

		public override void Update(float s)
		{
			if (sel_ == null || person_.Animator.CurrentAnimation != sel_)
				return;

			var p = person_.Animator.CurrentPlayer;

			if (p != null && !p.Paused)
			{
				ignore_.Do(() =>
				{
					seek_.WholeNumbers = p.UsesFrames;
					seek_.Set(
						sel_.Real.FirstFrame, sel_.Real.FirstFrame,
						sel_.Real.LastFrame);
				});
			}
		}

		private void OnPlay()
		{
			sel_ = anims_.Selected;
			if (sel_ == null)
				return;

			PlaySelection();
		}

		private void PlaySelection(float frame = -1)
		{
			person_.Animator.Play(sel_, loop_.Checked ? Animator.Loop : 0);

			var p = person_.Animator.CurrentPlayer;
			if (p == null)
			{
				// todo
				return;
			}

			p.Paused = paused_.Checked;

			if (paused_.Checked)
			{
				((BVH.Player)p).ShowSkeleton();
				p.Seek(sel_.Real.InitFrame);
			}

			ignore_.Do(() =>
			{
				seek_.WholeNumbers = p.UsesFrames;

				if (frame < 0)
					frame = sel_.Real.InitFrame;

				seek_.Set(frame, sel_.Real.InitFrame, sel_.Real.LastFrame);
			});
		}

		private void OnStop()
		{
			if (sel_ == null || person_.Animator.CurrentAnimation != sel_)
				return;

			person_.Animator.Stop();
		}

		private void OnPaused(bool b)
		{
			if (sel_ == null || person_.Animator.CurrentAnimation != sel_)
				return;

			var p = person_.Animator.CurrentPlayer;
			if (p != null)
				p.Paused = b;
		}

		private void OnSeek(float f)
		{
			if (ignore_ || sel_ == null)
				return;

			if (person_.Animator.CurrentAnimation != sel_)
				PlaySelection(f);

			if (person_.Animator.CurrentPlayer == null)
			{
				Cue.LogError("no player");
				return;
			}

			Cue.LogInfo($"seeking to {f}");
			person_.Animator.CurrentPlayer.Seek(f);
		}
	}
}
