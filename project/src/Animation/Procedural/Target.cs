using System;
using System.Collections.Generic;

namespace Cue.Proc
{
	interface ITarget
	{
		string Name { get; }
		ITarget Parent { get; set; }
		ISync Sync { get; }
		bool Done { get; }

		float MovementEnergy { get; }
		ulong LockKey { get; }

		ITarget Clone();
		void Reset();
		void Start(Person p, AnimationContext cx);
		void RequestStop();
		void FixedUpdate(float s);

		void GetAllForcesDebug(List<string> list);
		string ToDetailedString();
		ITarget FindTarget(string name);
	}


	abstract class BasicTarget : ITarget
	{
		protected Person person_ = null;
		private ITarget parent_ = null;
		private ISync sync_;
		private string name_ = "";
		private bool stopping_ = false;

		protected BasicTarget(string name, ISync sync)
		{
			sync_ = sync;
			sync_.Target = this;
			name_ = name;
		}

		public string Name
		{
			get { return name_; }
		}

		public ITarget Parent
		{
			get { return parent_; }
			set { parent_ = value; }
		}

		public ISync Sync
		{
			get { return sync_; }
			set { sync_ = value; }
		}

		public virtual float MovementEnergy
		{
			get { return parent_.MovementEnergy; }
		}

		public virtual ulong LockKey
		{
			get { return parent_.LockKey; }
		}

		public abstract bool Done { get; }

		public bool Stopping
		{
			get { return stopping_; }
		}

		public abstract ITarget Clone();
		public abstract void FixedUpdate(float s);
		public abstract string ToDetailedString();

		public void Start(Person p, AnimationContext cx)
		{
			person_ = p;
			Reset();
			DoStart(p, cx);
		}

		protected abstract void DoStart(Person p, AnimationContext cx);

		public virtual void RequestStop()
		{
			stopping_ = true;
			sync_.RequestStop();
		}

		public virtual void GetAllForcesDebug(List<string> list)
		{
			// no-op
		}

		public virtual void Reset()
		{
			sync_.Energy = MovementEnergy;
			sync_.Reset();
		}

		public virtual ITarget FindTarget(string name)
		{
			if (name_ == name)
				return this;
			else
				return null;
		}
	}
}
