using System.Collections.Generic;

namespace Cue
{
	class Gaze
	{
		private Person person_;
		private IEyes eyes_;
		private IGazer gazer_;
		private GazeTargets targets_;
		private GazeTargetPicker pick_;
		private IGazeEvent[] events_ = new IGazeEvent[0];
		private bool[] avoid_ = new bool[0];
		private string lastString_ = "";

		public Gaze(Person p)
		{
			person_ = p;
			eyes_ = Integration.CreateEyes(p);
			gazer_ = Integration.CreateGazer(p);
			targets_ = new GazeTargets(p);
			pick_ = new GazeTargetPicker(p);
		}

		public IEyes Eyes { get { return eyes_; } }
		public IGazer Gazer { get { return gazer_; } }
		public GazeTargets Targets { get { return targets_; } }
		public string LastString { get { return lastString_; } }


		public void Init()
		{
			targets_.Init();
			avoid_ = new bool[Cue.Instance.Everything.Count];
			pick_.SetTargets(targets_.All);
			events_ = BasicGazeEvent.All(person_);
		}

		public void Clear()
		{
			for (int i = 0; i < avoid_.Length; ++i)
				avoid_[i] = false;

			targets_.Clear();
		}

		public bool ShouldAvoid(IObject o)
		{
			return avoid_[o.ObjectIndex];
		}

		public void SetShouldAvoid(IObject o, bool b)
		{
			avoid_[o.ObjectIndex] = b;
		}

		public List<Pair<IObject, bool>> GetAllAvoidForDebug()
		{
			var list = new List<Pair<IObject, bool>>();

			for (int i = 0; i < avoid_.Length; ++i)
				list.Add(new Pair<IObject, bool>(Cue.Instance.Everything[i], avoid_[i]));

			return list;
		}

		public GazeTargetPicker Picker
		{
			get { return pick_; }
		}

		public void Update(float s)
		{
			UpdateTargets();

			if (pick_.Update(s))
				gazer_.Duration = person_.Personality.GazeDuration;

			if (pick_.HasTarget)
				eyes_.LookAt(pick_.Position);
			// else ?

			eyes_.Update(s);
			gazer_.Update(s);
		}

		public void UpdateTargets()
		{
			var ps = person_.Personality;

			Clear();
			lastString_ = "";

			bool gazerEnabled = !person_.Body.Get(BodyParts.Head).Busy;
			int flags = 0;

			for (int i = 0; i < events_.Length; ++i)
			{
				var e = events_[i];
				var r = e.Check(flags);

				if (Bits.IsSet(r, BasicGazeEvent.NoGazer))
					gazerEnabled = false;

				if (Bits.IsSet(r, BasicGazeEvent.NoRandom))
					flags |= BasicGazeEvent.NoRandom;

				if (Bits.IsSet(r, BasicGazeEvent.Busy))
					flags |= BasicGazeEvent.Busy;

				if (Bits.IsSet(r, BasicGazeEvent.Exclusive))
					break;
			}

			if (!Bits.IsSet(flags, BasicGazeEvent.Busy))
				lastString_ += "not busy";

			gazer_.Enabled = gazerEnabled;
			// todo: NoRandom, Busy
		}

		public bool ShouldAvoidPlayer()
		{
			var ps = person_.Personality;

			if (!ps.GetBool(PSE.AvoidGazePlayer))
				return false;

			return IsBored();
		}

		public bool IsBored()
		{
			var ps = person_.Personality;

			if (person_.Mood.RawExcitement >= ps.Get(PSE.MaxExcitementForAvoid))
				return false;

			if (person_.Mood.TimeSinceLastOrgasm < ps.Get(PSE.AvoidDelayAfterOrgasm))
				return false;

			return true;
		}

		public bool ShouldAvoidInsidePersonalSpace()
		{
			var ps = person_.Personality;

			if (!ps.GetBool(PSE.AvoidGazeInsidePersonalSpace))
				return false;

			return IsBored();
		}

		public bool ShouldAvoidDuringSex()
		{
			var ps = person_.Personality;

			if (!ps.GetBool(PSE.AvoidGazeDuringSex))
				return false;

			return IsBored();
		}

		public override string ToString()
		{
			return pick_.ToString();
		}
	}
}
