using System;
using System.Collections.Generic;

namespace Cue
{
	interface IAI
	{
		bool InteractWith(IObject o);
		void RunEvent(IEvent e);
		void Update(float s);
		bool Enabled { get; set; }
		IEvent Event { get; }
		void OnPluginState(bool b);
		Mood Mood { get; }
	}


	interface IPersonality
	{
		void SetMood(int state);
	}


	class NeutralPersonality : IPersonality
	{
		private readonly Person person_;

		public NeutralPersonality(Person p)
		{
			person_ = p;
		}

		public void SetMood(int state)
		{
			person_.Expression.MakeNeutral();

			switch (state)
			{
				case Mood.Idle:
				{
					person_.Expression.Set(new Pair<int, float>[]
					{
						new Pair<int, float>(Expressions.Happy, 0.5f),
						new Pair<int, float>(Expressions.Mischievous, 0.0f)
					});

					break;
				}

				case Mood.Happy:
				{
					person_.Expression.Set(new Pair<int, float>[]
					{
						new Pair<int, float>(Expressions.Happy, 1.0f),
						new Pair<int, float>(Expressions.Mischievous, 0.0f)
					});

					break;
				}
			}
		}
	}


	class QuirkyPersonality : IPersonality
	{
		private readonly Person person_;

		public QuirkyPersonality(Person p)
		{
			person_ = p;
		}

		public void SetMood(int state)
		{
			person_.Expression.MakeNeutral();

			switch (state)
			{
				case Mood.Idle:
				{
					person_.Expression.Set(new Pair<int, float>[]
					{
						new Pair<int, float>(Expressions.Happy, 0.2f),
						new Pair<int, float>(Expressions.Mischievous, 0.4f)
					});

					break;
				}

				case Mood.Happy:
				{
					person_.Expression.Set(new Pair<int, float>[]
					{
						new Pair<int, float>(Expressions.Happy, 0.4f),
						new Pair<int, float>(Expressions.Mischievous, 1.0f)
					});

					break;
				}
			}
		}
	}


	class Mood
	{
		public const int None = 0;
		public const int Idle = 1;
		public const int Happy = 2;

		private Person person_;
		private int state_ = None;
		private float excitement_ = 0;
		private float lastRate_ = 0;

		private float mouthRate_ = 0.001f;
		private float breastsRate_ = 0.01f;
		private float genitalsRate_ = 0.1f;
		private float decayRate_ = -0.01f;
		private float orgasm_ = 10;

		public Mood(Person p)
		{
			person_ = p;
		}

		public int State
		{
			get
			{
				return state_;
			}

			set
			{
				state_ = value;
				person_.Personality.SetMood(state_);
			}
		}

		public float Excitement
		{
			get { return excitement_; }
		}

		public void Update(float s)
		{
			float rate = 0;

			rate += person_.Excitement.Genitals * genitalsRate_;
			rate += person_.Excitement.Mouth * mouthRate_;
			rate += person_.Excitement.Breasts * breastsRate_;

			if (rate == 0)
				rate = decayRate_;

			excitement_ += rate * s;

			if (excitement_ >= orgasm_)
			{
				person_.Orgasmer.Orgasm();
				excitement_ = 0;
			}

			lastRate_ = rate;
		}

		public void OnPluginState(bool b)
		{
		}

		public override string ToString()
		{
			string s = "";

			//s += $"state={state_} ";
			s += $"ex={excitement_:0.##}";

			if (lastRate_ < 0)
				s += "-";
			else
				s += "+";

			s += $"({lastRate_:0.###})";

			return s;
		}
	}


	class PersonAI : IAI
	{
		private Person person_ = null;
		private int i_ = -1;
		private readonly List<IEvent> events_ = new List<IEvent>();
		private bool enabled_ = false;
		private IEvent forced_ = null;
		private Mood mood_;

		public PersonAI(Person p)
		{
			person_ = p;
			mood_ = new Mood(person_);

			foreach (var o in Cue.Instance.Objects)
			{
				if (o.Slots.Has(Slot.Sit))
					events_.Add(new SitEvent(person_, o));
				if (o.Slots.Has(Slot.Lie))
					events_.Add(new LieDownEvent(person_, o));
				if (o.Slots.Has(Slot.Stand))
					events_.Add(new StandEvent(person_, o));
			}
		}

		public bool Enabled
		{
			get
			{
				return enabled_;
			}

			set
			{
				enabled_ = value;
				if (!enabled_)
					Stop();
			}
		}

		public IEvent Event
		{
			get
			{
				if (forced_ != null)
					return forced_;
				else if (i_ >= 0 && i_ < events_.Count)
					return events_[i_];
				else
					return null;
			}
		}

		public Mood Mood
		{
			get { return mood_; }
		}

		public bool InteractWith(IObject o)
		{
			if (!person_.TryLockSlot(o))
			{
				// can't lock the given object
				return false;
			}

			person_.MakeIdle();

			var slot = person_.LockedSlot;

			if (slot.Type == Slot.Sit)
			{
				if (person_ == Cue.Instance.Player)
				{
					person_.PushAction(new SitAction(slot));
					person_.PushAction(new MoveAction(slot.Position, BasicObject.NoBearing));
				}
				else
				{
					RunEvent(new SitEvent(person_, slot));
				}
			}
			else if (slot.Type == Slot.Stand)
			{
				if (person_ == Cue.Instance.Player)
				{
					person_.PushAction(new MakeIdleAction());
					person_.PushAction(new MoveAction(slot.Position, slot.Bearing));
				}
				else
				{
					RunEvent(new StandEvent(person_, slot));
				}
			}

			return true;
		}

		public void RunEvent(IEvent e)
		{
			if (forced_ != null)
				forced_.Stop();

			forced_ = e;

			if (forced_ != null)
			{
				Stop();
				person_.MakeIdle();
			}
		}

		public void Update(float s)
		{
			if (forced_ != null)
			{
				if (!forced_.Update(s))
				{
					forced_.Stop();
					forced_ = null;
				}
			}
			else if (enabled_)
			{
				if (events_.Count > 0)
				{
					if (i_ == -1)
					{
						i_ = 0;
					}
					else
					{
						if (!events_[i_].Update(s))
						{
							events_[i_].Stop();

							++i_;
							if (i_ >= events_.Count)
								i_ = 0;
						}
					}
				}
			}

			if (i_ == -1)
			{
				if (person_ != Cue.Instance.Player)
				{
					person_.Gaze.Target = Cue.Instance.Player.HeadPosition;
					person_.Gaze.LookAt = GazeSettings.LookAtTarget;
				}
			}

			mood_.Update(s);
		}

		public void OnPluginState(bool b)
		{
			mood_.OnPluginState(b);
		}

		private void Stop()
		{
			if (i_ >= 0 && i_ < events_.Count)
			{
				events_[i_].Stop();
				i_ = -1;
			}
		}
	}
}
