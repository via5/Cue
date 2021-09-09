namespace Cue
{
	class HandjobCommand : BasicCommand
	{
		private const int NoState = 0;
		private const int CallingState = 1;
		private const int CrouchingState = 2;
		private const int WaitState = 3;
		private const int ActiveState = 4;

		private Person receiver_;
		private int state_ = NoState;
		private float wait_ = 0;

		public HandjobCommand(Person p, Person receiver)
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


	class BlowjobCommand : BasicCommand
	{
		private const int NoState = 0;
		private const int CallingState = 1;
		private const int CrouchingState = 2;
		private const int WaitState = 3;
		private const int ActiveState = 4;

		private Person receiver_;
		private int state_ = NoState;
		private float wait_ = 0;

		public BlowjobCommand(Person p, Person receiver)
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


}
