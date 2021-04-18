using System.Collections.Generic;

namespace Cue
{
	class Animator
	{
		public const int Loop = 0x01;
		public const int Reverse = 0x02;

		private Person person_;
		private readonly List<IPlayer> players_ = new List<IPlayer>();
		private IPlayer active_ = null;

		public Animator(Person p)
		{
			person_ = p;
			players_.Add(new BVH.Player(p));
			players_.Add(new TimelinePlayer(p));
		}

		public bool Playing
		{
			get { return (active_ != null); }
		}

		public void Play(IAnimation a, int flags = 0)
		{
			foreach (var p in players_)
			{
				if (p.Play(a, flags))
				{
					active_ = p;
					break;
				}
			}
		}

		public void Stop()
		{
			if (active_ != null)
				active_.Stop();
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

		public override string ToString()
		{
			if (active_ != null)
				return active_.ToString();
			else
				return "(none)";
		}
	}
}
