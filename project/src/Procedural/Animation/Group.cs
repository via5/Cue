﻿using SimpleJSON;
using System.Collections.Generic;

namespace Cue.Proc
{
	interface ITargetGroup : ITarget
	{
		List<ITarget> Targets { get; }
	}


	abstract class BasicTargetGroup : BasicTarget, ITargetGroup
	{
		protected BasicTargetGroup(ISync sync)
			: base(sync)
		{
		}

		public abstract List<ITarget> Targets { get; }
	}


	class ConcurrentTargetGroup : BasicTargetGroup
	{
		private string name_;
		private readonly List<ITarget> targets_ = new List<ITarget>();
		private Duration delay_, maxDuration_;
		private bool inDelay_ = false;
		private bool[] done_ = new bool[0];
		private bool allDone_ = false;
		private bool forever_;

		public ConcurrentTargetGroup(string name, ISync sync)
			: this(name, new Duration(), new Duration(), true, sync)
		{
		}

		public ConcurrentTargetGroup(
			string name, Duration delay, Duration maxDuration,
			bool forever, ISync sync)
				: base(sync)
		{
			name_ = name;
			delay_ = delay;
			maxDuration_ = maxDuration;
			forever_ = forever;
		}

		public static ConcurrentTargetGroup Create(JSONClass o)
		{
			string name = o["name"];

			try
			{
				if (!o.HasKey("sync"))
					throw new LoadFailed("no sync");

				var g = new ConcurrentTargetGroup(
					name,
					Duration.FromJSON(o, "delay"),
					Duration.FromJSON(o, "maxDuration"),
					o["loop"].AsBool,
					BasicSync.Create(o["sync"].AsObject));

				foreach (JSONClass n in o["targets"].AsArray)
					g.AddTarget(ProcAnimation.CreateTarget(n["type"], n));

				return g;
			}
			catch (LoadFailed e)
			{
				throw new LoadFailed($"{name}/{e.Message}");
			}
		}

		public override ITarget Clone()
		{
			var s = new ConcurrentTargetGroup(
				name_, new Duration(delay_), new Duration(maxDuration_),
				forever_, Sync.Clone());

			foreach (var c in targets_)
				s.AddTarget(c.Clone());

			return s;
		}

		public override List<ITarget> Targets
		{
			get { return targets_; }
		}

		public void AddTarget(ITarget t)
		{
			targets_.Add(t);
			t.Parent = this;
		}

		public override void Reset()
		{
			base.Reset();

			inDelay_ = false;
			delay_.Reset();
			maxDuration_.Reset();

			for (int i = 0; i < targets_.Count; ++i)
				targets_[i].Reset();
		}

		public override void Start(Person p)
		{
			for (int i = 0; i < targets_.Count; ++i)
				targets_[i].Start(p);

			done_ = new bool[targets_.Count];
			maxDuration_.Reset();
		}

		public override bool Done
		{
			get { return allDone_; }
		}

		public override void FixedUpdate(float s)
		{
			allDone_ = false;
			Sync.FixedUpdate(s);

			if (inDelay_)
			{
				delay_.Update(s);
				if (delay_.Finished)
					inDelay_ = false;
			}
			else
			{
				allDone_ = true;

				for (int i = 0; i < targets_.Count; ++i)
				{
					if (!done_[i] || forever_)
					{
						targets_[i].FixedUpdate(s);
						if (targets_[i].Done)
							done_[i] = true;
						else
							allDone_ = false;
					}
				}

				if (maxDuration_.Enabled)
				{
					maxDuration_.Update(s);
					if (maxDuration_.Finished)
						allDone_ = true;
				}

				if (allDone_)
				{
					for (int i = 0; i < done_.Length; ++i)
						done_[i] = false;

					if (delay_.Enabled)
						inDelay_ = true;
				}
			}
		}

		public override string ToString()
		{
			return $"congroup {name_}";
		}

		public override string ToDetailedString()
		{
			return
				$"congroup " +
				$"{name_}, {targets_.Count} targets indelay={inDelay_} " +
				$"done={allDone_} forever={forever_}";
		}
	}


	class SequentialTargetGroup : BasicTargetGroup
	{
		struct TargetInfo
		{

			// overlap!

		}

		private string name_;
		private readonly List<ITarget> targets_ = new List<ITarget>();
		private readonly List<TargetInfo> targetInfos_ = new List<TargetInfo>();
		private Duration delay_;
		private bool inDelay_ = false;
		private int i_ = 0;
		private bool done_ = false;

		public SequentialTargetGroup(string name, ISync sync)
			: this(name, new Duration(), sync)
		{
		}

		public SequentialTargetGroup(string name, Duration delay, ISync sync)
			: base(sync)
		{
			name_ = name;
			delay_ = delay;
		}

		public static SequentialTargetGroup Create(JSONClass o)
		{
			string name = o["name"];

			try
			{
				if (!o.HasKey("sync"))
					throw new LoadFailed("no sync");

				var g = new SequentialTargetGroup(
					name, Duration.FromJSON(o, "delay"),
					BasicSync.Create(o["sync"].AsObject));

				foreach (JSONClass n in o["targets"].AsArray)
					g.AddTarget(ProcAnimation.CreateTarget(n["type"], n));

				return g;
			}
			catch (LoadFailed e)
			{
				throw new LoadFailed($"{name}/{e.Message}");
			}
		}

		public override ITarget Clone()
		{
			var s = new SequentialTargetGroup(
				name_, new Duration(delay_), Sync.Clone());

			foreach (var t in targets_)
			{
				s.AddTarget(t.Clone());
				s.targetInfos_.Add(new TargetInfo());
			}

			return s;
		}

		public override List<ITarget> Targets
		{
			get { return targets_; }
		}

		public void AddTarget(ITarget t)
		{
			targets_.Add(t);
			t.Parent = this;
		}

		public override bool Done
		{
			get { return done_; }
		}

		public override void Reset()
		{
			base.Reset();

			inDelay_ = false;
			delay_.Reset();

			for (int i = 0; i < targets_.Count; ++i)
				targets_[i].Reset();
		}

		public override void Start(Person p)
		{
			for (int i = 0; i < targets_.Count; ++i)
				targets_[i].Start(p);
		}

		public override void FixedUpdate(float s)
		{
			Sync.FixedUpdate(s);

			if (targets_.Count == 0)
			{
				done_ = true;
				return;
			}

			done_ = false;

			if (i_ < 0 || i_ >= targets_.Count)
				i_ = 0;

			if (inDelay_)
			{
				delay_.Update(s);
				if (delay_.Finished)
					inDelay_ = false;
			}
			else
			{
				targets_[i_].FixedUpdate(s);
				if (targets_[i_].Done)
				{
					++i_;
					if (i_ >= targets_.Count)
					{
						i_ = 0;
						done_ = true;
					}
				}

				if (done_ && delay_.Enabled)
					inDelay_ = true;
			}
		}

		public override string ToString()
		{
			return $"seqgroup {name_}";
		}

		public override string ToDetailedString()
		{
			return
				$"seqgroup " +
				$"{name_}, {targets_.Count} targets indelay={inDelay_} " +
				$"i={i_} done={done_}";
		}
	}
}