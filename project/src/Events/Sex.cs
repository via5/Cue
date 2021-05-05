namespace Cue
{
	class HandjobEvent : BasicEvent
	{
		private const int NoState = 0;
		private const int MovingState = 1;
		private const int PositioningState = 2;
		private const int ActiveState = 3;

		private Person receiver_;
		private int state_ = NoState;

		public HandjobEvent(Person p, Person receiver)
			: base(p)
		{
			receiver_ = receiver;
		}

		public override bool Update(float s)
		{
			switch (state_)
			{
				case NoState:
				{
					//var target =
					//	receiver_.Position +
					//	Vector3.Rotate(new Vector3(0, 0, 0.5f), receiver_.Bearing);
					//
					//person_.MoveTo(target, receiver_.Bearing + 180);
					//person_.Gaze.LookAt(receiver_);
					//state_ = MovingState;
					state_ = PositioningState;

					break;
				}

				case MovingState:
				{
					if (!person_.HasTarget)
					{
						if (receiver_.State.Is(PersonState.Sitting))
						{
							person_.SetState(PersonState.Crouching);
							state_ = PositioningState;
						}
						else
						{
							person_.Handjob.Start(receiver_);
							state_ = ActiveState;
						}
					}

					break;
				}

				case PositioningState:
				{
					if (!person_.Animator.Playing)
					{
						person_.Handjob.Start(receiver_);
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
		private const int MovingState = 1;
		private const int PositioningState = 2;
		private const int ActiveState = 3;

		private Person receiver_;
		private int state_ = NoState;

		public BlowjobEvent(Person p, Person receiver)
			: base(p)
		{
			receiver_ = receiver;
		}

		public override bool Update(float s)
		{
			switch (state_)
			{
				case NoState:
				{
					//var target =
					//	receiver_.Position +
					//	Vector3.Rotate(new Vector3(0, 0, 0.5f), receiver_.Bearing);
					//
					//person_.MoveTo(target, receiver_.Bearing + 180);
					//person_.Gaze.LookAt(receiver_);
					//state_ = MovingState;
					state_ = PositioningState;

					break;
				}

				case MovingState:
				{
					if (!person_.HasTarget)
					{
						if (receiver_.State.Is(PersonState.Sitting))
						{
							person_.SetState(PersonState.Crouching);
							state_ = PositioningState;
						}
						else
						{
							person_.Blowjob.Start(receiver_);
							state_ = ActiveState;
						}
					}

					break;
				}

				case PositioningState:
				{
					if (!person_.Animator.Playing)
					{
						person_.Blowjob.Start(receiver_);
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
		public const int MovingState = 1;
		public const int PositioningState = 2;
		public const int PlayState = 3;
		public const int ActiveState = 4;

		private Person receiver_;
		private int state_ = NoState;

		public SexEvent(Person p, Person receiver, int forceState=NoState)
			: base(p)
		{
			receiver_ = receiver;
			state_ = forceState;
		}

		public void ForceState(int s)
		{
			state_ = s;
		}

		public override bool Update(float s)
		{
			switch (state_)
			{
				case NoState:
				{
					var target =
						receiver_.Position +
						Vector3.Rotate(new Vector3(0, 0, 0.5f), receiver_.Bearing);

					person_.MoveTo(target, receiver_.Bearing + 180);
					person_.Gaze.LookAt(receiver_);
					state_ = MovingState;

					break;
				}

				case MovingState:
				{
					if (!person_.HasTarget)
					{
						person_.Clothing.GenitalsVisible = true;
						receiver_.Clothing.GenitalsVisible = true;

						if (receiver_.State.Is(PersonState.Sitting))
						{
							person_.SetState(PersonState.SittingStraddling);
							state_ = PositioningState;
						}
						else
						{
							state_ = ActiveState;
						}
					}

					break;
				}

				case PositioningState:
				{
					if (!person_.Animator.Playing)
					{
						person_.Clothing.GenitalsVisible = true;
						receiver_.Clothing.GenitalsVisible = true;
						state_ = PlayState;
					}

					break;
				}

				case PlayState:
				{
					person_.Animator.PlaySex(PersonState.SittingStraddling);
					state_ = ActiveState;
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
