﻿using SimpleJSON;
using System;
using System.Collections.Generic;

namespace Cue.Proc
{
	class Force : BasicTarget
	{
		public const int RelativeForce = 1;
		public const int RelativeTorque = 2;
		public const int AbsoluteForce = 3;
		public const int AbsoluteTorque = 4;

		private int type_;
		private int bodyPartType_;
		private BodyPart bp_ = null;
		private SlidingMovement movement_;
		private IEasing easing_;

		private bool oneFrameFinished_ = false;
		private Action beforeNext_ = null;

		private bool wasBusy_ = false;
		private Vector3 forceBeforeBusy_;
		private float busyElapsed_ = 0;
		private const float BusyResetTime = 1;
		private IEasing busyResetEasing_ = new SinusoidalEasing();


		public Force(
			int type, int bodyPart,
			SlidingMovement m, ISync sync, IEasing easing=  null)
				: this("", type, bodyPart, m, sync, easing)
		{
		}

		public Force(
			string name, int type, int bodyPart,
			SlidingMovement m, ISync sync, IEasing easing = null)
				: base(name, sync)
		{
			type_ = type;
			bodyPartType_ = bodyPart;
			movement_ = m;
			easing_ = easing ?? new SinusoidalEasing();

			Next();
		}

		public static Force Create(int type, JSONClass o)
		{
			try
			{
				var bodyPart = BP.FromString(o["bodyPart"]);
				if (bodyPart == BP.None)
					throw new LoadFailed($"bad body part '{o["bodyPart"]}'");

				ISync sync = null;
				if (o.HasKey("sync"))
					sync = BasicSync.Create(o["sync"].AsObject);
				else
					sync = new ParentTargetSync();

				return new Force(
					type, bodyPart,
					SlidingMovement.FromJSON(o, "movement", true),
					sync);
			}
			catch (LoadFailed e)
			{
				throw new LoadFailed($"{TypeToString(type)}/{e.Message}");
			}
		}

		public Action BeforeNextAction
		{
			get { return beforeNext_; }
			set { beforeNext_ = value; }
		}

		public virtual int Type
		{
			get { return type_; }
		}

		public override bool Done
		{
			get { return oneFrameFinished_; }
		}

		public SlidingMovement Movement
		{
			get { return movement_; }
		}

		public override ITarget Clone()
		{
			var f = new Force(
				Name, type_, bodyPartType_,
				new SlidingMovement(movement_),
				Sync.Clone());

			f.beforeNext_ = beforeNext_;

			return f;
		}

		protected override void DoStart(Person p, AnimationContext cx)
		{
			if (p.VamAtom != null)
			{
				bp_ = p.Body.Get(bodyPartType_);
				Reset();
			}
		}

		public override void Reset()
		{
			base.Reset();
			movement_.WindowMagnitude = MovementEnergy;
			movement_.Reset();
		}

		public override void RequestStop()
		{
			movement_.SetNext(Lerped());
			movement_.SetNext(Vector3.Zero);
			base.RequestStop();
		}

		private bool CanAppyForce()
		{
			if (bp_ != null)
			{
				if (bp_.LockedFor(BodyPartLock.Move, LockKey))
					return false;

				if (!bp_.CanApplyForce())
					return false;
			}

			return true;
		}

		public override void FixedUpdate(float s)
		{
			oneFrameFinished_ = false;

			if (!CanAppyForce())
			{
				if (wasBusy_)
				{
					busyElapsed_ += s;
				}
				else
				{
					forceBeforeBusy_ = Lerped();
					busyElapsed_ = 0;
				}

				wasBusy_ = true;

				var p = U.Clamp(busyElapsed_ / BusyResetTime, 0, 1);
				var mag = busyResetEasing_.Magnitude(p);
				var v = Vector3.Lerp(forceBeforeBusy_, Vector3.Zero, mag);

				Apply(v);

				if (p >= 1)
					oneFrameFinished_ = true;

				return;
			}
			else if (wasBusy_)
			{
				Reset();
				wasBusy_ = false;
			}


			movement_.Update(s);
			int r = Sync.FixedUpdate(s);
			Apply(Lerped());

			switch (r)
			{
				case BasicSync.Working:
				{
					break;
				}

				case BasicSync.DurationFinished:
				{
					movement_.WindowMagnitude = MovementEnergy;
					Sync.Energy = MovementEnergy;
					movement_.SetNext(Vector3.Zero);
					break;
				}

				case BasicSync.Delaying:
				case BasicSync.DelayFinished:
				{
					break;
				}

				case BasicSync.Looping:
				{
					movement_.WindowMagnitude = MovementEnergy;
					Sync.Energy = MovementEnergy;
					Next();
					break;
				}

				case BasicSync.SyncFinished:
				{
					movement_.WindowMagnitude = MovementEnergy;
					Sync.Energy = MovementEnergy;
					oneFrameFinished_ = true;
					break;
				}
			}
		}

		private void Apply(Vector3 v)
		{
			switch (type_)
			{
				case RelativeForce:
				{
					bp_?.AddRelativeForce(v);
					break;
				}

				case RelativeTorque:
				{
					bp_?.AddRelativeTorque(v);
					break;
				}

				case AbsoluteForce:
				{
					bp_?.AddForce(v);
					break;
				}

				case AbsoluteTorque:
				{
					bp_?.AddTorque(v);
					break;
				}
			}
		}

		private Vector3 Lerped()
		{
			return movement_.Lerped(easing_.Magnitude(Sync.Magnitude));
		}

		public static string TypeToString(int i)
		{
			switch (i)
			{
				case RelativeForce: return "rforce";
				case RelativeTorque: return "rtorque";
				case AbsoluteForce: return "force";
				case AbsoluteTorque: return "torque";
				default: return $"?{i}";
			}
		}

		public override void GetAllForcesDebug(List<string> list)
		{
			list.Add(
				$"{bp_} ({BP.ToString(bodyPartType_)}) " +
				$"{TypeToString(type_)} {Sys.Vam.U.ToUnity(Lerped())}");
		}

		public override string ToString()
		{
			return
				$"{TypeToString(type_)}.{BP.ToString(bodyPartType_)}" +
				(Name == "" ? "" : $" '{Name}'");
		}

		public override string ToDetailedString()
		{
			return
				$"{TypeToString(type_)} {Name} {bp_}\n" +
				$"{movement_}\n" +
				$"lerped={Lerped()} busy={wasBusy_}";
		}

		private void Next()
		{
			beforeNext_?.Invoke();

			if (!movement_.Next())
				movement_.SetNext(movement_.Last);
		}
	}
}
