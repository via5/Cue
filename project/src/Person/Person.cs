using SimpleJSON;
using System.Collections.Generic;

namespace Cue
{
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
		private Gaze gaze_;
		private Physiology physiology_;
		private Mood mood_;
		private IAI ai_ = null;
		private Personality personality_;

		private IBreather breathing_;
		private IOrgasmer orgasmer_;
		private ISpeaker speech_;
		private IKisser kisser_;
		private IHandjob handjob_;
		private IBlowjob blowjob_;
		private IClothing clothing_;
		private IExpression expression_;

		private string[] traits_ = new string[0];

		public Person(int objectIndex, int personIndex, Sys.IAtom atom)
			: base(objectIndex, atom)
		{
			personIndex_ = personIndex;

			actions_ = new RootAction(this);
			state_ = new PersonState(this);
			options_ = new PersonOptions(this);
			animator_ = new Animator(this);
			excitement_ = new Excitement(this);
			body_ = new Body(this);
			gaze_ = new Gaze(this);
			physiology_ = Resources.Physiologies.Clone(Resources.DefaultPhysiology, this);
			mood_ = new Mood(this);
			ai_ = new PersonAI(this);

			Personality = Resources.Personalities.Clone(Resources.DefaultPersonality, this);

			breathing_ = Integration.CreateBreather(this);
			orgasmer_ = Integration.CreateOrgasmer(this);
			speech_ = Integration.CreateSpeaker(this);
			kisser_ = Integration.CreateKisser(this);
			handjob_ = Integration.CreateHandjob(this);
			blowjob_ = Integration.CreateBlowjob(this);
			clothing_ = Integration.CreateClothing(this);
			expression_ = Integration.CreateExpression(this);

			Atom.SetDefaultControls("init");
		}

		public void Init()
		{
			SetState(PersonState.Standing);

			Body.Init();
			gaze_.Init();

			if (this == Cue.Instance.Player)
			{
				AI.CommandsEnabled = false;
				AI.EventsEnabled = false;
			}

			Atom.Init();
			Atom.SetBodyDamping(Sys.BodyDamping.Normal);
		}

		public override void Load(JSONClass r)
		{
			base.Load(r);

			var ts = new List<string>();
			foreach (JSONNode n in r["traits"].AsArray)
				ts.Add(n.Value);
			traits_ = ts.ToArray();

			if (r.HasKey("personality"))
			{
				var po = r["personality"].AsObject;

				if (po.HasKey("name"))
				{
					var p = Resources.Personalities.Clone(po["name"], this);
					if (p != null)
						personality_ = p;
				}

				personality_.Load(po);
			}

			if (r.HasKey("commands"))
				ai_.CommandsEnabled = r["commands"].AsBool;
		}

		public override JSONNode ToJSON()
		{
			var o = new JSONClass();

			var p = personality_.ToJSON();
			if (p.Count > 0)
				o.Add("personality", p);

			if (o.Count > 0)
				o.Add("id", ID);

			return o;
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
			for (int i = 0; i < traits_.Length; ++i)
			{
				if (traits_[i] == name)
					return true;
			}

			return false;
		}

		public string[] Traits
		{
			get { return traits_; }
			set { traits_ = value; }
		}

		public PersonOptions Options { get { return options_; } }
		public Animator Animator { get { return animator_; } }
		public PersonState State { get { return state_; } }
		public Excitement Excitement { get { return excitement_; } }
		public Body Body { get { return body_; } }
		public Gaze Gaze { get { return gaze_; } }
		public Physiology Physiology { get { return physiology_; } }
		public Mood Mood { get { return mood_; } }
		public IAI AI { get { return ai_; } }
		public IClothing Clothing { get { return clothing_; } }
		public RootAction Actions { get { return actions_; } }

		public IBreather Breathing { get { return breathing_; } }
		public IOrgasmer Orgasmer { get { return orgasmer_; } }
		public ISpeaker Speech { get { return speech_; } }
		public IKisser Kisser { get { return kisser_; } }
		public IHandjob Handjob { get { return handjob_; } }
		public IBlowjob Blowjob { get { return blowjob_; } }
		public IExpression Expression { get { return expression_; } }

		public int MovementStyle
		{
			get
			{
				if (Atom.IsMale)
					return MovementStyles.Masculine;
				else
					return MovementStyles.Feminine;
			}
		}

		public override Vector3 EyeInterest
		{
			get
			{
				return body_.Get(BP.Eyes)?.Position ?? base.EyeInterest;
			}
		}

		public Personality Personality
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

		public bool IsInteresting
		{
			get { return Body.Exists; }
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

			I.Do(I.UpdatePersonTransitions, () =>
			{
				CheckNavState();

				if (deferredTransition_)
				{
					if (StartTransition())
						PlayTransition();
				}

				if (State.IsUpright)
					uprightPos_ = Position;

				animator_.Update(s);

				if (!deferredTransition_ && !animator_.IsPlayingTransition())
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
			});

			I.Do(I.UpdatePersonActions, () =>
			{
				actions_.Tick(s);
			});

			I.Do(I.UpdatePersonGaze, () =>
			{
				if (Cue.Instance.Player != this)
					gaze_.Update(s);
			});

			I.Do(I.UpdatePersonEvents, () =>
			{
				Kisser.Update(s);
				Handjob.Update(s);
				Blowjob.Update(s);
			});

			if (this != Cue.Instance.Player)
			{
				I.Do(I.UpdatePersonExcitement, () =>
				{
					excitement_.Update(s);
				});

				I.Do(I.UpdatePersonPersonality, () =>
				{
					personality_.Update(s);
				});

				I.Do(I.UpdatePersonMood, () =>
				{
					mood_.Update(s);
				});

				I.Do(I.UpdatePersonBody, () =>
				{
					body_.Update(s);
				});
			}

			I.Do(I.UpdatePersonAI, () =>
			{
				if (ai_ != null && !Atom.Teleporting)
					ai_.Update(s);
			});
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
						|| !animator_.IsPlayingTransition())
					{
						animator_.PlayType(
							Animations.Walk,
							Animator.Loop | Animator.Exclusive);
					}

					break;
				}

				case Sys.NavStates.TurningLeft:
				{
					if (lastNavState_ != Sys.NavStates.TurningLeft || !animator_.IsPlayingTransition())
					{
						if (CanMove)
						{
							animator_.PlayType(
								Animations.TurnLeft, Animator.Exclusive);
						}
					}

					break;
				}

				case Sys.NavStates.TurningRight:
				{
					if (lastNavState_ != Sys.NavStates.TurningRight || !animator_.IsPlayingTransition())
					{
						if (CanMove)
						{
							animator_.PlayType(
								Animations.TurnRight, Animator.Exclusive);
						}
					}

					break;
				}
			}

			lastNavState_ = navState;
		}
	}
}
