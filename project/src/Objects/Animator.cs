using System.Collections.Generic;

namespace Cue
{
	class Animator
	{
		public const int Loop = 0x01;
		public const int Reverse = 0x02;
		public const int Rewind = 0x04;

		private Person person_;
		private readonly List<IPlayer> players_ = new List<IPlayer>();
		private IPlayer active_ = null;
		private int activeFlags_ = 0;

		public Animator(Person p)
		{
			person_ = p;
			players_.Add(new BVH.Player(p));
			players_.Add(new TimelinePlayer(p));
			players_.Add(new ProceduralPlayer());
		}

		public bool Playing
		{
			get { return (active_ != null); }
		}

		public void Play(int type, int flags = 0)
		{
			Play(Resources.Animations.GetAny(type, person_.Sex), flags);
		}

		public void Play(IAnimation a, int flags = 0)
		{
			foreach (var p in players_)
			{
				if (p.Play(a, flags))
				{
					Cue.LogInfo(person_.ID + ": " + p.ToString());
					active_ = p;
					activeFlags_ = flags;
					return;
				}
			}

			Cue.LogError("no player can play " + a.ToString());
		}

		public void Stop()
		{
			if (active_ != null)
				active_.Stop(Bits.IsSet(activeFlags_, Rewind));
		}

		public void FixedUpdate(float s)
		{
			if (active_ != null)
			{
				active_.FixedUpdate(s);
				if (!active_.Playing)
					active_ = null;
			}
		}

		public void Update(float s)
		{
			if (active_ != null)
			{
				active_.Update(s);
				if (!active_.Playing)
					active_ = null;
			}
		}

		public override string ToString()
		{
			if (active_ != null)
				return active_.ToString();
			else
				return "(none)";
		}
	}
}
