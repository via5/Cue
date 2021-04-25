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
					var target =
						receiver_.Position +
						Vector3.Rotate(new Vector3(0, 0, 0.5f), receiver_.Bearing);

					person_.MoveTo(target, receiver_.Bearing + 180);
					person_.Gaze.LookAt = GazeSettings.LookAtTarget;
					person_.Gaze.Target = receiver_.Atom.HeadPosition;
					state_ = MovingState;

					break;
				}

				case MovingState:
				{
					if (!person_.HasTarget)
					{
						if (receiver_.State.Is(PersonState.Sitting))
						{
							person_.Kneel();
							state_ = PositioningState;
						}
						else
						{
							person_.Handjob.Target = receiver_;
							person_.Handjob.Active = true;
							state_ = ActiveState;
						}
					}

					break;
				}

				case PositioningState:
				{
					if (!person_.Animator.Playing)
					{
						person_.Handjob.Target = receiver_;
						person_.Handjob.Active = true;
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
		public const int ActiveState = 3;
		public const int ActiveState2 = 4;

		private Person receiver_;
		private int state_ = NoState;
		private ProceduralAnimation sex_;

		public SexEvent(Person p, Person receiver, int forceState=NoState)
			: base(p)
		{
			receiver_ = receiver;
			state_ = forceState;

			sex_ = new ProceduralAnimation(p, "sex");
			sex_.Add("hip", new Vector3(0, -200, 0), 1);
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
					person_.Gaze.LookAt = GazeSettings.LookAtTarget;
					person_.Gaze.Target = receiver_.Atom.HeadPosition;
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
							person_.Straddle();
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
						state_ = ActiveState;
					}

					break;
				}

				case ActiveState:
				{
					person_.Animator.Play(sex_);
					state_ = ActiveState2;
					break;
				}

				case ActiveState2:
				{
					break;
				}
			}

			return true;
		}
	}
}
