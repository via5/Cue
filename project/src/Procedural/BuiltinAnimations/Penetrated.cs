using System;
using System.Collections.Generic;

namespace Cue.Proc
{
	class PenetratedAnimation : BasicProcAnimation
	{
		private const int NoState = 0;
		private const int ReactionUpState = 1;
		private const int ReactionHoldState = 2;
		private const int PostReactionUpState = 3;
		private const int PostReactionHoldState = 4;
		private const int ResetState = 5;
		private const int DoneState = 6;

		private const float ReactionUpTime = 0.2f;
		private const float ReactionHoldTime = 1;
		private const float PostReactionUpTime = 0.2f;
		private const float PostReactionHoldTime = 1;
		private const float ResetTime = 1;

		private float elapsed_ = 0;
		private int state_ = NoState;

		private Expression pleasure_ = null;
		private Expression smile_ = null;

		public PenetratedAnimation()
			: base("procPenetrated", false)
		{
		}

		public override BasicProcAnimation Clone()
		{
			var a = new PenetratedAnimation();
			a.CopyFrom(this);
			return a;
		}

		public override bool Done
		{
			get { return (state_ == DoneState); }
		}

		public override bool Start(Person p, object ps)
		{
			base.Start(p, ps);

			elapsed_ = 0;
			state_ = ReactionUpState;
			person_.Breathing.MouthEnabled = false;

			pleasure_ = BuiltinExpressions.Pleasure(p);
			smile_ = BuiltinExpressions.Smile(p);

			pleasure_.SetTarget(U.RandomFloat(0.7f, 1.0f), ReactionUpTime);
			smile_.SetTarget(1 - pleasure_.Target, ReactionUpTime);

			return true;
		}

		public override void Reset()
		{
			base.Reset();
			elapsed_ = 0;
		}

		public override void FixedUpdate(float s)
		{
			elapsed_ += s;

			pleasure_.Update(s);
			smile_.Update(s);

			switch (state_)
			{
				case ReactionUpState:
				{
					if (elapsed_ >= ReactionUpTime)
					{
						elapsed_ = 0;
						state_ = ReactionHoldState;
					}

					break;
				}

				case ReactionHoldState:
				{
					if (elapsed_ >= ReactionHoldTime)
					{
						elapsed_ = 0;
						state_ = PostReactionUpState;

						pleasure_.SetTarget(0, PostReactionUpTime);
						smile_.SetTarget(1, PostReactionUpTime);
					}

					break;
				}

				case PostReactionUpState:
				{
					if (elapsed_ >= PostReactionUpTime)
					{
						elapsed_ = 0;
						state_ = PostReactionHoldState;
					}

					break;
				}

				case PostReactionHoldState:
				{
					if (elapsed_ >= PostReactionHoldTime)
					{
						elapsed_ = 0;
						state_ = ResetState;

						smile_.SetTarget(0, ResetTime);
					}

					break;
				}

				case ResetState:
				{
					if (elapsed_ >= ResetTime)
					{
						elapsed_ = 0;
						state_ = DoneState;
						person_.Breathing.MouthEnabled = true;
					}

					break;
				}
			}
		}
	}
}
