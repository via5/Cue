﻿using System;
using System.Collections.Generic;

namespace Cue.UI
{
	class ScriptUI
	{
		private VUI.Root root_ = null;

		private VUI.Panel panel_ = new VUI.Panel();
		private VUI.Tabs tabsWidget_ = new VUI.Tabs();
		private List<Tab> tabs_ = new List<Tab>();
		private float updateElapsed_ = 1000;

		public void Init()
		{
			foreach (var p in Cue.Instance.Persons)
				tabs_.Add(new PersonTab(p));

			tabs_.Add(new MiscTab());

			root_ = Cue.Instance.Sys.CreateScriptUI();
			root_.ContentPanel.Layout = new VUI.BorderLayout();
			root_.ContentPanel.Add(panel_, VUI.BorderLayout.Center);

			panel_.Layout = new VUI.BorderLayout();
			panel_.Add(tabsWidget_, VUI.BorderLayout.Center);

			foreach (var t in tabs_)
				tabsWidget_.AddTab(t.Title, t);
		}

		public void Update(float s)
		{
			updateElapsed_ += s;
			if (updateElapsed_ > 0.2f)
			{
				for (int i = 0; i < tabs_.Count; ++i)
					tabs_[i].Update();

				updateElapsed_ = 0;
			}

			root_.Update();
		}
	}


	abstract class Tab : VUI.Panel
	{
		public abstract string Title { get; }
		public abstract void Update();
	}


	class MiscTab : Tab
	{
		private VUI.CheckBox navmeshes_ = new VUI.CheckBox("Navmeshes");
		private VUI.Button renav_ = new VUI.Button("Update nav");

		public MiscTab()
		{
			Layout = new VUI.VerticalFlow();
			Add(navmeshes_);
			Add(renav_);

			navmeshes_.Changed += (b) => Cue.Instance.Sys.Nav.Render = b;
			renav_.Clicked += Cue.Instance.Sys.Nav.Update;
		}

		public override string Title
		{
			get { return "Stuff"; }
		}

		public override void Update()
		{
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

		public override void Update()
		{
			for (int i = 0; i < tabs_.Count; ++i)
				tabs_[i].Update();
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
		private VUI.Label anim_ = new VUI.Label();
		private VUI.Label nav_ = new VUI.Label();
		private VUI.Label state_ = new VUI.Label();

		private VUI.Label breath_ = new VUI.Label();
		private VUI.Label gaze_ = new VUI.Label();
		private VUI.Label speech_ = new VUI.Label();
		private VUI.Label kiss_ = new VUI.Label();
		private VUI.Label handjob_ = new VUI.Label();
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

			state.Add(new VUI.Label("Anim"));
			state.Add(anim_);

			state.Add(new VUI.Label("Nav"));
			state.Add(nav_);

			state.Add(new VUI.Label("State"));
			state.Add(state_);

			state.Add(new VUI.Spacer(20));
			state.Add(new VUI.Spacer(20));


			state.Add(new VUI.Label("Breath"));
			state.Add(breath_);

			state.Add(new VUI.Label("Gaze"));
			state.Add(gaze_);

			state.Add(new VUI.Label("Speech"));
			state.Add(speech_);

			state.Add(new VUI.Label("Kiss"));
			state.Add(kiss_);

			state.Add(new VUI.Label("Handjob"));
			state.Add(handjob_);

			state.Add(new VUI.Label("Clothing"));
			state.Add(clothing_);

			Layout = new VUI.BorderLayout();
			Add(state, VUI.BorderLayout.Top);
		}

		public override string Title
		{
			get { return "State"; }
		}

		public override void Update()
		{
			id_.Text = person_.ID;
			pos_.Text = person_.Position.ToString();
			dir_.Text = person_.Direction.ToString();
			bearing_.Text = person_.Bearing.ToString();
			action_.Text = person_.Actions.ToString();
			anim_.Text = person_.Animator.ToString();
			nav_.Text = W.NavStates.ToString(person_.Atom.NavState);
			state_.Text = person_.State.ToString() + " " + (person_.Idle ? "(idle)" : "(not idle)");

			breath_.Text = person_.Breathing.ToString();
			gaze_.Text = person_.Gaze.ToString();
			speech_.Text = person_.Speech.ToString();
			kiss_.Text = person_.Kisser.ToString();
			handjob_.Text = person_.Handjob.ToString();
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

			state.Add(new VUI.Label("Mood"));
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

			Layout = new VUI.BorderLayout();
			Add(state, VUI.BorderLayout.Top);
		}

		public override string Title
		{
			get { return "AI"; }
		}

		public override void Update()
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
	}


	class PersonAnimationTab : Tab
	{
		private Person person_;
		private VUI.Button play_ = new VUI.Button("Play");
		private VUI.ListView<IAnimation> anims_ = new VUI.ListView<IAnimation>();

		public PersonAnimationTab(Person p)
		{
			person_ = p;

			Layout = new VUI.BorderLayout();
			Add(play_, VUI.BorderLayout.Top);
			Add(anims_, VUI.BorderLayout.Center);

			play_.Clicked += OnPlay;

			var items = new List<IAnimation>();
			foreach (var a in Resources.Animations.GetAll(Resources.Animations.NoType, Sexes.Female))
				items.Add(a);

			anims_.SetItems(items);
		}

		public override string Title
		{
			get { return "Animation"; }
		}

		public override void Update()
		{
		}

		private void OnPlay()
		{
			var a = anims_.Selected;
			if (a == null)
				return;

			person_.Animator.Play(a);
		}
	}
}
