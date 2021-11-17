using SimpleJSON;
using System;
using System.Collections.Generic;

namespace Cue.Proc
{
	public interface ITargetGroup : ITarget
	{
		List<ITarget> Targets { get; }
		void AddTarget(ITarget t);
	}


	public abstract class BasicTargetGroup : BasicTarget, ITargetGroup
	{
		protected BasicTargetGroup(string name, ISync sync)
			: base(name, sync)
		{
		}

		public override void RequestStop()
		{
			foreach (var t in Targets)
				t.RequestStop();

			base.RequestStop();
		}

		public abstract List<ITarget> Targets { get; }
		public abstract void AddTarget(ITarget t);

		public override void GetAllForcesDebug(List<string> list)
		{
			var ts = Targets;
			for (int i = 0; i < ts.Count; ++i)
				ts[i].GetAllForcesDebug(list);
		}

		public override ITarget FindTarget(string name)
		{
			var t = base.FindTarget(name);
			if (t != null)
				return t;

			for (int i = 0; i < Targets.Count; ++i)
			{
				t = Targets[i].FindTarget(name);
				if (t != null)
					return t;
			}

			return null;
		}
	}


	public class ConcurrentTargetGroup : BasicTargetGroup
	{
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
				: base(name, sync)
		{
			delay_ = delay;
			maxDuration_ = maxDuration;
			forever_ = forever;
		}

		public static ConcurrentTargetGroup Create(JSONClass o)
		{
			string name = o["name"];

			try
			{
				ISync sync = null;
				if (o.HasKey("sync"))
					sync = BasicSync.Create(o["sync"].AsObject);
				else
					sync = new ParentTargetSync();

				var g = new ConcurrentTargetGroup(
					name,
					Duration.FromJSON(o, "delay"),
					Duration.FromJSON(o, "maxDuration"),
					o["loop"].AsBool,
					sync);

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
				Name, delay_.Clone(), maxDuration_.Clone(),
				forever_, Sync.Clone());

			s.CopyFrom(this);

			return s;
		}

		protected void CopyFrom(ConcurrentTargetGroup o)
		{
			foreach (var c in o.targets_)
				AddTarget(c.Clone());
		}

		public override List<ITarget> Targets
		{
			get { return targets_; }
		}

		public override void AddTarget(ITarget t)
		{
			targets_.Add(t);
			t.Parent = this;
		}

		public override void Reset()
		{
			base.Reset();

			inDelay_ = false;
			delay_.Reset(MovementEnergy);
			maxDuration_.Reset(MovementEnergy);

			for (int i = 0; i < targets_.Count; ++i)
				targets_[i].Reset();
		}

		protected override void DoStart(Person p, AnimationContext cx)
		{
			for (int i = 0; i < targets_.Count; ++i)
				targets_[i].Start(p, cx);

			done_ = new bool[targets_.Count];
			maxDuration_.Reset(MovementEnergy);
		}

		public override bool Done
		{
			get
			{
				if (Stopping)
					return allDone_;
				else
					return !forever_ && allDone_;
			}
		}

		protected override void DoFixedUpdate(float s)
		{
			allDone_ = false;

			if (inDelay_)
			{
				delay_.Update(s, MovementEnergy);
				if (delay_.Finished)
					inDelay_ = false;
			}
			else
			{
				allDone_ = true;

				for (int i = 0; i < targets_.Count; ++i)
				{
					if (!done_[i] || (forever_ && !Stopping))
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
					maxDuration_.Update(s, MovementEnergy);
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
			return $"congroup" + (Name == "" ? "" : $" '{Name}'");
		}

		public override string ToDetailedString()
		{
			return
				$"{this} indelay={inDelay_} " +
				$"done={allDone_} forever={forever_} maxD={maxDuration_.Enabled}";
		}
	}


	public class SequentialTargetGroup : BasicTargetGroup
	{
		struct TargetInfo
		{

			// overlap!

		}

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
			: base(name, sync)
		{
			delay_ = delay;
		}

		public static SequentialTargetGroup Create(JSONClass o)
		{
			string name = o["name"];

			try
			{
				ISync sync = null;
				if (o.HasKey("sync"))
					sync = BasicSync.Create(o["sync"].AsObject);
				else
					sync = new ParentTargetSync();

				var g = new SequentialTargetGroup(
					name, Duration.FromJSON(o, "delay"),
					sync);

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
				Name, delay_.Clone(), Sync.Clone());

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

		public override void AddTarget(ITarget t)
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
			delay_.Reset(MovementEnergy);

			for (int i = 0; i < targets_.Count; ++i)
				targets_[i].Reset();
		}

		protected override void DoStart(Person p, AnimationContext cx)
		{
			for (int i = 0; i < targets_.Count; ++i)
				targets_[i].Start(p, cx);
		}

		protected override void DoFixedUpdate(float s)
		{
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
				delay_.Update(s, 0);
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
			return $"seqgroup" + (Name == "" ? "" : $" '{Name}'");
		}

		public override string ToDetailedString()
		{
			return
				$"seqgroup " +
				$"{Name}, {targets_.Count} targets indelay={inDelay_} " +
				$"i={i_} done={done_}";
		}
	}


	public class RootTargetGroup : ConcurrentTargetGroup
	{
		private BasicProcAnimation anim_ = null;
		private Person energySource_ = null;
		private ulong key_ = BodyPartLock.NoKey;

		public RootTargetGroup(ISync sync = null)
			: base("root", sync ?? new NoSync())
		{
		}

		public BasicProcAnimation ParentAnimation
		{
			get { return anim_; }
			set { anim_ = value; }
		}

		public override ITarget Clone()
		{
			var s = new RootTargetGroup(Sync.Clone());
			s.CopyFrom(this);
			return s;
		}

		protected override void DoStart(Person p, AnimationContext cx)
		{
			base.DoStart(p, cx);
			key_ = cx?.key ?? BodyPartLock.NoKey;
		}

		public void SetEnergySource(Person p)
		{
			energySource_ = p;
		}

		public override float MovementEnergy
		{
			get
			{
				return Mood.MultiMovementEnergy(person_, energySource_);
			}
		}

		public override ulong LockKey
		{
			get { return key_; }
		}

		public override string ToString()
		{
			string s = $"root {Name}";

			if (key_ != BodyPartLock.NoKey)
				s += $" key={key_}";

			return s;
		}
	}
}
