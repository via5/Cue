using SimpleJSON;
using System.Collections.Generic;

namespace Cue
{
	class Clothing
	{
		private Person person_;

		public Clothing(Person p)
		{
			person_ = p;
		}

		public void Init()
		{
			person_.Atom.Clothing.Init();
		}

		public float HeelsAngle
		{
			get { return person_.Atom.Clothing.HeelsAngle; }
		}

		public float HeelsHeight
		{
			get { return person_.Atom.Clothing.HeelsHeight; }
		}

		public bool GenitalsVisible
		{
			get { return person_.Atom.Clothing.GenitalsVisible; }
			set { person_.Atom.Clothing.GenitalsVisible = value; }
		}

		public bool BreastsVisible
		{
			get { return person_.Atom.Clothing.BreastsVisible; }
			set { person_.Atom.Clothing.BreastsVisible = value; }
		}

		public void Dump()
		{
			person_.Atom.Clothing.Dump();
		}

		public override string ToString()
		{
			return person_.Atom.Clothing.ToString();
		}
	}


	class PersonOptions
	{
		private bool canKiss_ = true;

		public PersonOptions(Person p)
		{
		}

		public bool CanKiss
		{
			get { return canKiss_; }
			set { canKiss_ = value; }
		}
	}


	class Sensitivity
	{
		private Person person_;

		public Sensitivity(Person p)
		{
			person_ = p;
		}

		public float MouthRate { get { return 0.1f; } }
		public float MouthMax { get { return 0.3f; } }

		public float BreastsRate { get { return 0.01f; } }
		public float BreastsMax { get { return 0.4f; } }

		public float GenitalsRate { get { return 0.06f; } }
		public float GenitalsMax { get { return 0.8f; } }

		public float PenetrationRate { get { return 0.05f; } }
		public float PenetrationMax { get { return 1.0f; } }

		public float DecayPerSecond { get { return -0.01f; } }
		public float ExcitementPostOrgasm { get { return 0.4f; } }
		public float OrgasmTime { get { return 8; } }
		public float PostOrgasmTime { get { return 10; } }
		public float RateAdjustment { get { return 0.3f; } }
	}


	class Physiology
	{
		private Person person_;
		private Sensitivity sensitivity_;
		private float pitch_ = 0.5f;

		public Physiology(Person p, JSONClass config)
		{
			person_ = p;
			sensitivity_ = new Sensitivity(p);

			if (config.HasKey("physiology"))
			{
				string vp = config["physiology"]?["voicePitch"]?.Value ?? "";

				if (vp != "" && !float.TryParse(vp, out pitch_))
					person_.Log.Error($"bad voice pitch '{vp}'");
			}
		}

		public Sensitivity Sensitivity
		{
			get { return sensitivity_; }
		}

		public float MaxFlush
		{
			get { return 0.065f; }
		}

		public float VoicePitch
		{
			get { return pitch_; }
		}

		// todo
		//
		public string Voice
		{
			get { return "Original"; }
		}
	}


	class ForceableValue
	{
		private float value_;
		private float forced_;
		private bool isForced_;

		public ForceableValue()
		{
			value_ = 0;
			forced_ = 0;
			isForced_ = false;
		}

		public float Value
		{
			get
			{
				if (isForced_)
					return forced_;
				else
					return value_;
			}

			set
			{
				value_ = value;
			}
		}

		public float UnforcedValue
		{
			get { return value_; }
		}

		public void SetForced(float f)
		{
			isForced_ = true;
			forced_ = f;
		}

		public void UnsetForced()
		{
			isForced_ = false;
		}
	}


	class Mood
	{
		public const float NoOrgasm = 10000;

		public const int NormalState = 1;
		public const int OrgasmState = 2;
		public const int PostOrgasmState = 3;

		private readonly Person person_;
		private int state_ = NormalState;
		private float elapsed_ = 0;
		private float timeSinceLastOrgasm_ = NoOrgasm;

		private ForceableValue excitement_ = new ForceableValue();
		private ForceableValue tiredness_ = new ForceableValue();

		public Mood(Person p)
		{
			person_ = p;
		}

		public int State
		{
			get { return state_; }
		}

		public string StateString
		{
			get
			{
				switch (state_)
				{
					case NormalState:
						return "normal";

					case OrgasmState:
						return "orgasm";

					case PostOrgasmState:
						return "post orgasm";

					default:
						return $"?{state_}";
				}
			}
		}

		public float TimeSinceLastOrgasm
		{
			get { return timeSinceLastOrgasm_; }
		}

		public float Excitement
		{
			get { return excitement_.Value; }
		}

		public ForceableValue ExcitementValue
		{
			get { return excitement_; }
		}

		public float Tiredness
		{
			get { return tiredness_.Value; }
		}

		public ForceableValue TirednessValue
		{
			get { return tiredness_; }
		}

		public void ForceOrgasm()
		{
			DoOrgasm();
		}

		public void Update(float s)
		{
			elapsed_ += s;

			excitement_.Value = person_.Excitement.Value;

			person_.Breathing.Intensity = Excitement;
			person_.Body.Sweat = Excitement;
			person_.Body.Flush = Excitement;
			person_.Expression.Set(Expressions.Pleasure, Excitement);
			person_.Hair.Loose = Excitement;

			if (excitement_.UnforcedValue >= 1)
				DoOrgasm();

			switch (state_)
			{
				case NormalState:
				{
					timeSinceLastOrgasm_ += s;
					break;
				}

				case OrgasmState:
				{
					var ss = person_.Physiology.Sensitivity;

					if (elapsed_ >= ss.OrgasmTime)
					{
						person_.Animator.StopType(Animation.OrgasmType);
						SetState(PostOrgasmState);
					}

					break;
				}

				case PostOrgasmState:
				{
					var ss = person_.Physiology.Sensitivity;

					tiredness_.Value += s;

					if (elapsed_ > ss.PostOrgasmTime)
					{
						SetState(NormalState);
						person_.Excitement.FlatValue = ss.ExcitementPostOrgasm;
					}

					break;
				}
			}
		}

		private void DoOrgasm()
		{
			var ss = person_.Physiology.Sensitivity;

			person_.Log.Info("orgasm");
			person_.Orgasmer.Orgasm();
			person_.Animator.PlayType(Animation.OrgasmType);
			person_.Excitement.FlatValue = 1;
			SetState(OrgasmState);
			timeSinceLastOrgasm_ = 0;
		}

		private void SetState(int s)
		{
			state_ = s;
			elapsed_ = 0;
		}
	}


	class Person : BasicObject
	{
		private readonly int personIndex_;
		private readonly RootAction actions_;
		private PersonState state_;
		private bool deferredTransition_ = false;
		private int deferredState_ = PersonState.None;
		private int lastNavState_ = Sys.NavStates.None;
		private Vector3 uprightPos_ = new Vector3();

		private PersonOptions options_;
		private Animator animator_;
		private Excitement excitement_;
		private Body body_;
		private Hair hair_;
		private Gaze gaze_;
		private Physiology physiology_;
		private Mood mood_;
		private IAI ai_ = null;
		private Clothing clothing_;
		private IPersonality personality_;

		private IBreather breathing_;
		private IOrgasmer orgasmer_;
		private ISpeaker speech_;
		private IKisser kisser_;
		private IHandjob handjob_;
		private IBlowjob blowjob_;
		private IExpression expression_;

		private List<string> traits_ = new List<string>();

		public Person(int objectIndex, int personIndex, Sys.IAtom atom, JSONClass config)
			: base(objectIndex, atom)
		{
			personIndex_ = personIndex;

			foreach (JSONNode n in config["traits"].AsArray)
				traits_.Add(n.Value);

			actions_ = new RootAction(this);
			state_ = new PersonState(this);
			options_ = new PersonOptions(this);
			animator_ = new Animator(this);
			excitement_ = new Excitement(this);
			body_ = new Body(this);
			hair_ = new Hair(this);
			gaze_ = new Gaze(this);
			physiology_ = new Physiology(this, config);
			mood_ = new Mood(this);
			ai_ = new PersonAI(this);
			clothing_ = new Clothing(this);

			Personality = BasicPersonality.FromString(this, config["personality"]);

			breathing_ = Integration.CreateBreather(this);
			orgasmer_ = Integration.CreateOrgasmer(this);
			speech_ = Integration.CreateSpeaker(this);
			kisser_ = Integration.CreateKisser(this);
			handjob_ = Integration.CreateHandjob(this);
			blowjob_ = Integration.CreateBlowjob(this);
			expression_ = Integration.CreateExpression(this);

			Atom.SetDefaultControls("init");
		}

		public void Init()
		{
			SetState(PersonState.Standing);

			Body.Init();
			gaze_.Init();
			Clothing.Init();

			if (this == Cue.Instance.Player)
			{
				AI.EventsEnabled = false;
				AI.InteractionsEnabled = false;
			}

			Atom.Init();
		}

		public int PersonIndex
		{
			get { return personIndex_; }
		}

		public bool Idle
		{
			get { return actions_.IsIdle; }
		}

		public Vector3 UprightPosition
		{
			get { return uprightPos_; }
		}

		public bool HasTrait(string name)
		{
			for (int i = 0; i < traits_.Count; ++i)
			{
				if (traits_[i] == name)
					return true;
			}

			return false;
		}

		public PersonOptions Options { get { return options_; } }
		public Animator Animator { get { return animator_; } }
		public PersonState State { get { return state_; } }
		public Excitement Excitement { get { return excitement_; } }
		public Body Body { get { return body_; } }
		public Hair Hair { get { return hair_; } }
		public Gaze Gaze { get { return gaze_; } }
		public Physiology Physiology { get { return physiology_; } }
		public Mood Mood { get { return mood_; } }
		public IAI AI { get { return ai_; } }
		public Clothing Clothing { get { return clothing_; } }
		public RootAction Actions { get { return actions_; } }

		public IBreather Breathing { get { return breathing_; } }
		public IOrgasmer Orgasmer { get { return orgasmer_; } }
		public ISpeaker Speech { get { return speech_; } }
		public IKisser Kisser { get { return kisser_; } }
		public IHandjob Handjob { get { return handjob_; } }
		public IBlowjob Blowjob { get { return blowjob_; } }
		public IExpression Expression { get { return expression_; } }

		public int Sex
		{
			get { return Atom.Sex; }
		}

		public override Vector3 EyeInterest
		{
			get
			{
				return body_.Get(BodyParts.Eyes)?.Position ?? base.EyeInterest;
			}
		}

		public IPersonality Personality
		{
			get { return personality_; }
			set { personality_ = value; }
		}

		public bool CanMoveHead
		{
			get
			{
				return !Kisser.Active && !Blowjob.Active;
			}
		}

		public bool CanMove
		{
			get
			{
				return !Kisser.Active && !Blowjob.Active && !Handjob.Active;
			}
		}

		public void PushAction(IAction a)
		{
			actions_.Push(a);
		}

		public void PopAction()
		{
			actions_.Pop();
		}

		public override void MakeIdle()
		{
			Kisser.Stop();
			Handjob.Stop();
			Blowjob.Stop();

			actions_.Clear();
			animator_.Stop();

			Atom.SetDefaultControls("make idle");
		}

		public override void MakeIdleForMove()
		{
			if (state_.IsCurrently(PersonState.Walking))
				state_.CancelTransition();

			Kisser.Stop();
			Handjob.Stop();
			Blowjob.Stop();

			actions_.Clear();
			ai_.MakeIdle();
		}

		public override bool InteractWith(IObject o)
		{
			if (ai_ == null)
				return false;

			return ai_.InteractWith(o);
		}

		public override void FixedUpdate(float s)
		{
			base.FixedUpdate(s);
			animator_.FixedUpdate(s);

			if (this != Cue.Instance.Player)
				expression_.FixedUpdate(s);

			if (ai_ != null && !Atom.Teleporting)
				ai_.FixedUpdate(s);
		}

		public override void Update(float s)
		{
			base.Update(s);

			CheckNavState();

			if (deferredTransition_)
			{
				if (StartTransition())
					PlayTransition();
			}

			if (State.IsUpright)
				uprightPos_ = Position;

			animator_.Update(s);

			if (!deferredTransition_ && !animator_.Playing)
			{
				state_.FinishTransition();
				if (deferredState_ != PersonState.None)
				{
					log_.Info("animation finished, setting deferred state");

					var ds = deferredState_;
					deferredState_ = PersonState.None;
					SetState(ds);
				}
			}

			actions_.Tick(s);

			if (Cue.Instance.Player != this)
				gaze_.Update(s);

			Kisser.Update(s);
			Handjob.Update(s);
			Blowjob.Update(s);

			if (this != Cue.Instance.Player)
			{
				excitement_.Update(s);
				personality_.Update(s);
				mood_.Update(s);
				body_.Update(s);
				hair_.Update(s);
			}

			if (ai_ != null && !Atom.Teleporting)
				ai_.Update(s);
		}

		public override void OnPluginState(bool b)
		{
			base.OnPluginState(b);

			Atom.NavEnabled = b;

			animator_.OnPluginState(b);
			expression_.OnPluginState(b);
			Kisser.OnPluginState(b);
			Handjob.OnPluginState(b);
			Blowjob.OnPluginState(b);

			ai_.OnPluginState(b);
		}

		public override void SetPaused(bool b)
		{
			base.SetPaused(b);
			Atom.NavPaused = b;
		}

		public void SetState(int s)
		{
			if (state_.Next == s || deferredState_ == s)
			{
				// already transitioning to that state
				return;
			}

			deferredState_ = PersonState.None;
			state_.StartTransition(s);

			if (!StartTransition())
			{
				deferredTransition_ = true;
				return;
			}

			animator_.Stop();
			PlayTransition();
		}

		private void PlayTransition()
		{
			deferredTransition_ = false;

			if (!animator_.PlayTransition(state_.Current, state_.Next, Animator.Exclusive))
			{
				// no animation for this transition, stand first if not already
				// trying to stand
				if (state_.Next != PersonState.Standing)
				{
					log_.Info(
						$"no animation for transition " +
						$"{PersonState.StateToString(state_.Current)}->" +
						$"{PersonState.StateToString(state_.Next)}, standing first");

					deferredState_ = state_.Next;
					state_.StartTransition(PersonState.Standing);

					animator_.PlayTransition(
						state_.Current, PersonState.Standing,
						Animator.Exclusive);
				}
				else
				{
					// no animation, just finish transition
					state_.FinishTransition();
				}
			}
		}

		public virtual void Say(string s)
		{
			speech_.Say(s);
		}

		protected bool StartTransition()
		{
			bool canStart = true;

			if (Kisser.Active)
			{
				Kisser.Stop();
				canStart = false;
			}

			if (Handjob.Active)
			{
				Handjob.Stop();
				canStart = false;
			}

			if (Blowjob.Active)
			{
				Blowjob.Stop();
				canStart = false;
			}

			return canStart;
		}

		protected override bool StartMove()
		{
			if (!StartTransition())
				return false;

			if (!State.IsUpright)
			{
				SetState(PersonState.Standing);
				return false;
			}

			return true;
		}

		private void CheckNavState()
		{
			var navState = Atom.NavState;

			switch (navState)
			{
				case Sys.NavStates.None:
				{
					if (lastNavState_ != Sys.NavStates.None)
					{
						// force the state to standing first, there are no
						// animations for walk->stand
						state_.Set(PersonState.Standing);

						// must stop manually, it's exclusive
						animator_.Stop();

						SetState(PersonState.Standing);
					}

					break;
				}

				case Sys.NavStates.Calculating:
				{
					// wait
					break;
				}

				case Sys.NavStates.Moving:
				{
					state_.Set(PersonState.Walking);

					if ((
							lastNavState_ != Sys.NavStates.Moving &&
							lastNavState_ != Sys.NavStates.Calculating
						)
						|| !animator_.Playing)
					{
						animator_.PlayType(
							Animation.WalkType,
							Animator.Loop | Animator.Exclusive);
					}

					break;
				}

				case Sys.NavStates.TurningLeft:
				{
					if (lastNavState_ != Sys.NavStates.TurningLeft || !animator_.Playing)
					{
						if (CanMove)
						{
							animator_.PlayType(
								Animation.TurnLeftType, Animator.Exclusive);
						}
					}

					break;
				}

				case Sys.NavStates.TurningRight:
				{
					if (lastNavState_ != Sys.NavStates.TurningRight || !animator_.Playing)
					{
						if (CanMove)
						{
							animator_.PlayType(
								Animation.TurnRightType, Animator.Exclusive);
						}
					}

					break;
				}
			}

			lastNavState_ = navState;
		}
	}
}
