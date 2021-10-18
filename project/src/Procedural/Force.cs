using SimpleJSON;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cue.Proc
{
	class Force : BasicTarget
	{
		public const int RelativeForce = 1;
		public const int RelativeTorque = 2;
		public const int AbsoluteForce = 3;
		public const int AbsoluteTorque = 4;

		private int type_;
		private int bodyPart_;
		private string rbId_;
		private SlidingMovement movement_;
		private IEasing excitement_;
		private IEasing fwdDelayExcitement_, bwdDelayExcitement_;

		private Rigidbody rb_ = null;
		private bool oneFrameFinished_ = false;
		private Action beforeNext_ = null;

		private bool wasBusy_ = false;
		private Vector3 forceBeforeBusy_;
		private float busyElapsed_ = 0;
		private const float BusyResetTime = 1;
		private IEasing busyResetEasing_ = new SinusoidalEasing();


		public Force(
			int type, int bodyPart, string rbId,
			SlidingMovement m, IEasing excitement, ISync sync,
			IEasing fwdDelayExcitement, IEasing bwdDelayExcitement)
				: this(
					  "", type, bodyPart, rbId, m, excitement, sync,
					  fwdDelayExcitement, bwdDelayExcitement)
		{
		}

		public Force(
			string name, int type, int bodyPart, string rbId,
			SlidingMovement m, IEasing excitement, ISync sync,
			IEasing fwdDelayExcitement, IEasing bwdDelayExcitement)
				: base(name, sync)
		{
			type_ = type;
			bodyPart_ = bodyPart;
			rbId_ = rbId;
			movement_ = m;
			excitement_ = excitement;
			fwdDelayExcitement_ = fwdDelayExcitement;
			bwdDelayExcitement_ = bwdDelayExcitement;

			Next();
		}

		public static IEasing EasingFromJson(JSONClass o, string key)
		{
			if (!o.HasKey(key) || o[key].Value == "")
				return new ConstantOneEasing();

			var e = EasingFactory.FromString(o[key]);
			if (e == null)
				throw new LoadFailed($"easing type {o[key].Value} not found");

			return e;
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
					type, bodyPart, o["rigidbody"],
					SlidingMovement.FromJSON(o, "movement", true),
					EasingFromJson(o, "excitement"),
					sync,
					EasingFromJson(o, "fwdDelayExcitement"),
					EasingFromJson(o, "bwdDelayExcitement"));
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
				Name, type_, bodyPart_, rbId_,
				new SlidingMovement(movement_), excitement_,
				Sync.Clone(),
				fwdDelayExcitement_, bwdDelayExcitement_);

			f.beforeNext_ = beforeNext_;

			return f;
		}

		protected override void DoStart(Person p)
		{
			if (p.VamAtom != null)
			{
				rb_ = Sys.Vam.U.FindRigidbody(p.VamAtom.Atom, rbId_);
				if (rb_ == null)
				{
					Cue.LogError($"Force: rigidbody {rbId_} not found");
					return;
				}

				Reset();
			}
		}

		public override void Reset()
		{
			base.Reset();
			movement_.Reset();
		}

		public override void FixedUpdate(float s)
		{
			oneFrameFinished_ = false;

			if (bodyPart_ != BP.None &&
				person_.Body.Get(bodyPart_).LockedFor(BodyPartLock.Move))
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
					rb_?.AddRelativeForce(Sys.Vam.U.ToUnity(v));
					break;
				}

				case RelativeTorque:
				{
					rb_?.AddRelativeTorque(Sys.Vam.U.ToUnity(v));
					break;
				}

				case AbsoluteForce:
				{
					rb_?.AddForce(Sys.Vam.U.ToUnity(v));
					break;
				}

				case AbsoluteTorque:
				{
					rb_?.AddTorque(Sys.Vam.U.ToUnity(v));
					break;
				}
			}
		}

		private Vector3 Lerped()
		{
			return movement_.Lerped(Sync.Magnitude);
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
				$"{rbId_} ({BP.ToString(bodyPart_)}) " +
				$"{TypeToString(type_)} {Sys.Vam.U.ToUnity(Lerped())}");
		}

		public override string ToString()
		{
			return $"{rbId_} ({BP.ToString(bodyPart_)})";
		}

		public override string ToDetailedString()
		{
			return
				$"{TypeToString(type_)} {Name} {rbId_} ({BP.ToString(bodyPart_)})\n" +
				$"{movement_}\n" +
				$"en={EnergyFactor():0.00}\n" +
				$"lerped={Lerped()} busy={wasBusy_}";
		}

		private float EnergyFactor()
		{
			if (person_ == null)
				return 0;

			return excitement_.Magnitude(MovementEnergy);
		}

		private void Next()
		{
			beforeNext_?.Invoke();

			if (!movement_.Next())
				movement_.SetNext(movement_.Last);
		}
	}
}
