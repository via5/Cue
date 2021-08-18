namespace Cue
{
	class HandjobEvent : BasicEvent
	{
		private const int NoState = 0;
		private const int CallingState = 1;
		private const int CrouchingState = 2;
		private const int WaitState = 3;
		private const int ActiveState = 4;

		private Person receiver_;
		private int state_ = NoState;
		private float wait_ = 0;

		public HandjobEvent(Person p, Person receiver)
			: base(p, "HJ")
		{
			receiver_ = receiver;
		}

		public override bool Update(float s)
		{
			switch (state_)
			{
				case NoState:
				{
				//	person_.PushAction(new CallAction(person_, receiver_));
					state_ = CallingState;
					wait_ = 0;

					break;
				}

				case CallingState:
				{
					//if (person_.Idle)
					{
						if (receiver_.State.Is(PersonState.Sitting))
						{
							person_.SetState(PersonState.Crouching);
							state_ = CrouchingState;
						}
						else
						{
							state_ = WaitState;
						}
					}

					break;
				}

				case CrouchingState:
				{
					if (person_.State.IsCurrently(PersonState.Crouching))
						state_ = WaitState;

					break;
				}

				case WaitState:
				{
					wait_ += s;
					if (wait_ >= 0.5f)
					{
						//person_.Handjob.Start(receiver_);
						receiver_.Clothing.GenitalsVisible = true;
						state_ = ActiveState;
					}

					break;
				}

				case ActiveState:
				{
					break;
				}
			}

			return true;
		}
	}


	class BlowjobEvent : BasicEvent
	{
		private const int NoState = 0;
		private const int CallingState = 1;
		private const int CrouchingState = 2;
		private const int WaitState = 3;
		private const int ActiveState = 4;

		private Person receiver_;
		private int state_ = NoState;
		private float wait_ = 0;

		public BlowjobEvent(Person p, Person receiver)
			: base(p, "BJ")
		{
			receiver_ = receiver;
		}

		public override bool Update(float s)
		{
			switch (state_)
			{
				case NoState:
				{
					//person_.PushAction(new CallAction(person_, receiver_));
					person_.Kisser.Stop();
					state_ = CallingState;
					wait_ = 0;

					break;
				}

				case CallingState:
				{
				//	if (person_.Idle)
					{
						person_.SetState(PersonState.Crouching);
						state_ = CrouchingState;
					}

					break;
				}

				case CrouchingState:
				{
					if (person_.State.IsCurrently(PersonState.Crouching))
						state_ = WaitState;

					break;
				}

				case WaitState:
				{
					wait_ += s;
					if (wait_ >= 0.5f)
					{
						//person_.Blowjob.Start(receiver_);
						receiver_.Clothing.GenitalsVisible = true;
						state_ = ActiveState;
					}

					break;
				}

				case ActiveState:
				{
					break;
				}
			}

			return true;
		}
	}


	class SexEvent : BasicEvent
	{
		public const int NoState = 0;
		public const int PlayState = 1;

		private Person receiver_;
		private int state_ = NoState;

		public SexEvent(Person p, Person receiver, int forceState=NoState)
			: base(p, "Sex")
		{
			receiver_ = receiver;
			state_ = forceState;
		}

		public void ForceState(int s)
		{
			state_ = s;
		}

		public override void Stop()
		{
			base.Stop();
			person_.Atom.SetBodyDamping(Sys.BodyDamping.Normal);
		}

		public override bool Update(float s)
		{
			switch (state_)
			{
				case NoState:
				{
					person_.Clothing.GenitalsVisible = true;
					receiver_.Clothing.GenitalsVisible = true;
					person_.Animator.Stop();
					person_.Atom.SetBodyDamping(Sys.BodyDamping.Sex);
					state_ = PlayState;
					break;
				}

				case PlayState:
				{
					if (!person_.Animator.Playing && person_.Mood.State == Mood.NormalState)
						person_.Animator.PlaySex(person_.State.Current);

					break;
				}
			}

			return true;
		}
	}
}
