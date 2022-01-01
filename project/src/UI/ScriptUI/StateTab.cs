using System.Collections.Generic;

namespace Cue
{
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
		private VUI.Label gaze_ = new VUI.Label();

		private VUI.Label voice_ = new VUI.Label();
		private VUI.Label eyes_ = new VUI.Label();
		private VUI.Label gazer_ = new VUI.Label();
		private VUI.Label speech_ = new VUI.Label();
		private VUI.Label clothing_ = new VUI.Label();

		private VUI.CheckBox forcePlayer_ = new VUI.CheckBox("Force as player");

		public PersonStateTab(Person p)
			: base("State", false)
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


			state.Add(new VUI.Label("Voice"));
			state.Add(voice_);

			state.Add(new VUI.Label("Eyes"));
			state.Add(eyes_);

			state.Add(new VUI.Label("Gazer"));
			state.Add(gazer_);

			state.Add(new VUI.Label("Speech"));
			state.Add(speech_);

			state.Add(new VUI.Label("Clothing"));
			state.Add(clothing_);


			var buttons = new VUI.Panel(new VUI.VerticalFlow(10));
			buttons.Add(forcePlayer_);

			Layout = new VUI.VerticalFlow();
			Add(state);
			Add(new VUI.Spacer(30));
			Add(buttons);

			forcePlayer_.Changed += OnForcePlayer;
		}

		protected override void DoUpdate(float s)
		{
			id_.Text = $"{person_.ID}{(person_.IsPlayer ? " (player)" : "")}";
			pos_.Text = person_.Position.ToString();
			dir_.Text = person_.Rotation.ToString();
			bearing_.Text = $"{person_.Rotation.Bearing:0.00}";
			gaze_.Text = person_.Gaze.ToString();
			anim_.Text = person_.Animator.ToString();

			voice_.Text = person_.Voice?.ToString() ?? "(null)";
			eyes_.Text = person_.Gaze.Eyes.ToString();
			gazer_.Text = person_.Gaze.Gazer.ToString();
			speech_.Text = person_.Speech.ToString();
			clothing_.Text = person_.Clothing.ToString();

			forcePlayer_.Checked = (Cue.Instance.ForcedPlayer == person_);
		}

		private void OnForcePlayer(bool b)
		{
			if (b)
				Cue.Instance.ForcedPlayer = person_;
			else if (Cue.Instance.ForcedPlayer == person_)
				Cue.Instance.ForcedPlayer = null;
		}
	}
}
