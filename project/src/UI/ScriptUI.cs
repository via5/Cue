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
	}


	abstract class Tab : VUI.Panel
	{
		public abstract string Title { get; }
		public abstract void Update(float s);
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

		public MiscTab()
		{
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
			tabs_.Add(new PersonExpressionTab(person_));
			tabs_.Add(new PersonBodyTab(person_));
			tabs_.Add(new PersonAnimationTab(person_));

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

		private VUI.Label breath_ = new VUI.Label();
		private VUI.Label eyes_ = new VUI.Label();
		private VUI.Label gaze_ = new VUI.Label();
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

			state.Add(new VUI.Spacer(20));
			state.Add(new VUI.Spacer(20));


			state.Add(new VUI.Label("Breath"));
			state.Add(breath_);

			state.Add(new VUI.Label("Eyes"));
			state.Add(eyes_);

			state.Add(new VUI.Label("Gaze"));
			state.Add(gaze_);

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
			nav_.Text = W.NavStates.ToString(person_.Atom.NavState);
			state_.Text = person_.State.ToString() + " " + (person_.Idle ? "(idle)" : "(not idle)");

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
			gaze_.Text = person_.Gaze.Gazer.ToString();
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
			enabled_.Text = ai_.Enabled.ToString();
			event_.Text = (ai_.Event == null ? "(none)" : ai_.Event.ToString());
			personality_.Text = person_.Personality.ToString();

			moodState_.Text = ai_.Mood.StateString;
			moodExcitement_.Text = ai_.Mood.Excitement.ToString("0.00000");
			moodLastRate_.Text = ai_.Mood.LastRate.ToString("0.00000");
			moodMouthRate_.Text = ai_.Mood.MouthRate.ToString();
			moodBreastsRate_.Text = ai_.Mood.BreastsRate.ToString();
			moodGenitalsRate_.Text = ai_.Mood.GenitalsRate.ToString();
			moodDecayRate_.Text = ai_.Mood.DecayRate.ToString();
			moodRateAdjust_.Text = ai_.Mood.RateAdjust.ToString();
		}

		private void OnForceExcitementCheck(bool b)
		{
			if (b)
				person_.AI.Mood.ForceExcitement = forceExcitementValue_.Value;
			else
				person_.AI.Mood.ForceExcitement = -1;
		}

		private void OnForceExcitement(float f)
		{
			forceExcitement_.Checked = true;
			person_.AI.Mood.ForceExcitement = f;
		}
	}


	class PersonExpressionTab : Tab
	{
		private Person person_;
		private ProceduralExpression pex_;
		private VUI.ListView<string> list_ = new VUI.ListView<string>();

		public PersonExpressionTab(Person p)
		{
			person_ = p;

			Layout = new VUI.BorderLayout();

			pex_ = p.Expression as ProceduralExpression;
			if (pex_ == null)
			{
				Add(new VUI.Label("Not procedural"));
				return;
			}

			Add(new VUI.Button("Refresh", Refresh), VUI.BorderLayout.Top);
			Add(list_, VUI.BorderLayout.Center);
		}

		public override string Title
		{
			get { return "Expression"; }
		}

		public override void Update(float s)
		{
		}

		private string I(int i)
		{
			return new string(' ', i * 4);
		}

		private void Refresh()
		{
			var items = new List<string>();

			foreach (var e in pex_.All)
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
	}


	class PersonBodyTab : Tab
	{
		struct PartWidgets
		{
			public BodyPart part;
			public VUI.Label name, triggering, position, direction;
		}


		private readonly Person person_;
		private readonly List<PartWidgets> widgets_ = new List<PartWidgets>();

		public PersonBodyTab(Person ps)
		{
			person_ = ps;

			var gl = new VUI.GridLayout(4);
			var p = new VUI.Panel(gl);

			p.Add(new VUI.Label("Name", UnityEngine.FontStyle.Bold));
			p.Add(new VUI.Label("Triggering", UnityEngine.FontStyle.Bold));
			p.Add(new VUI.Label("Position", UnityEngine.FontStyle.Bold));
			p.Add(new VUI.Label("Bearing", UnityEngine.FontStyle.Bold));

			for (int i = 0; i < person_.Body.Parts.Count; ++i)
			{
				var bp = person_.Body.Parts[i];

				var w = new PartWidgets();
				w.part = bp;
				w.name = new VUI.Label(bp.Name);
				w.triggering = new VUI.Label();
				w.position = new VUI.Label();
				w.direction = new VUI.Label();

				p.Add(w.name);
				p.Add(w.triggering);
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
						w.triggering.Text = w.part.Triggering.ToString();

						w.triggering.TextColor = (
							w.part.Triggering ?
							W.VamU.ToUnity(Color.Green) :
							VUI.Style.Theme.TextColor);

						w.position.Text = w.part.Position.ToString();
						w.direction.Text = Vector3.Bearing(w.part.Direction).ToString("0.0");
					}
				}
			}
		}
	}


	class PersonAnimationTab : Tab
	{
		private Person person_;
		private VUI.ListView<Animation> anims_ = new VUI.ListView<Animation>();
		private VUI.CheckBox loop_ = new VUI.CheckBox("Loop");
		private VUI.IntTextSlider seek_ = new VUI.IntTextSlider();

		public PersonAnimationTab(Person person)
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
			p.Add(new VUI.Button("Pause", OnPause));
			p.Add(loop_);
			top.Add(p);

			p = new VUI.Panel(new VUI.BorderLayout());
			p.Add(seek_, VUI.BorderLayout.Center);
			top.Add(p);


			Add(top, VUI.BorderLayout.Top);
			Add(anims_, VUI.BorderLayout.Center);

			var items = new List<Animation>();
			foreach (var a in Resources.Animations.GetAll(Animation.NoType, Sexes.Female))
				items.Add(a);

			anims_.SetItems(items);

			seek_.ValueChanged += OnSeek;
		}

		public override string Title
		{
			get { return "Animation"; }
		}

		public override void Update(float s)
		{
		}

		private void OnPlay()
		{
			var a = anims_.Selected;
			if (a == null)
				return;

			var b = (BVH.Animation)a.Real;

			person_.Animator.Play(a, loop_.Checked ? Animator.Loop : 0);
			seek_.Set(b.InitFrame, b.FirstFrame, b.LastFrame);
		}

		private void OnStop()
		{
			var a = anims_.Selected;
			if (a == null)
				return;

			person_.Animator.Stop();
		}

		private void OnPause()
		{
			var p = person_.Animator.CurrentPlayer as BVH.Player;
			if (p != null)
				p.Paused = true;
		}

		private void OnSeek(int f)
		{
			var p = person_.Animator.CurrentPlayer as BVH.Player;
			if (p != null)
				p.Seek(f);
		}
	}
}
