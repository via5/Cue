﻿using SimpleJSON;
using System;
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
		private Person person_ = null;
		private bool wasBusy_ = false;
		private bool oneFrameFinished_ = false;

		private Action beforeNext_ = null;

		public Force(
			int type, int bodyPart, string rbId,
			SlidingMovement m, IEasing excitement, ISync sync,
			IEasing fwdDelayExcitement, IEasing bwdDelayExcitement)
				: base(sync)
		{
			type_ = type;
			bodyPart_ = bodyPart;
			rbId_ = rbId;
			movement_ = m;
			excitement_ = excitement;
			fwdDelayExcitement_ = fwdDelayExcitement;
			bwdDelayExcitement_ = bwdDelayExcitement;
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
				var bodyPart = BodyParts.FromString(o["bodyPart"]);
				if (bodyPart == BodyParts.None)
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
				type_, bodyPart_, rbId_,
				new SlidingMovement(movement_), excitement_,
				Sync.Clone(),
				fwdDelayExcitement_, bwdDelayExcitement_);

			f.beforeNext_ = beforeNext_;

			return f;
		}

		public override void Start(Person p)
		{
			person_ = p;

			if (p.VamAtom != null)
			{
				rb_ = Cue.Instance.VamSys.FindRigidbody(p.VamAtom.Atom, rbId_);
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

			if (bodyPart_ != BodyParts.None && person_.Body.Get(bodyPart_).Busy)
			{
				wasBusy_ = true;
				return;
			}
			else if (wasBusy_)
			{
				Reset();
				wasBusy_ = false;
			}


			movement_.Update(s);
			int r = Sync.FixedUpdate(s);
			Apply();

			switch (r)
			{
				case BasicSync.Working:
				{
					break;
				}

				case BasicSync.DurationFinished:
				{
					movement_.WindowMagnitude = person_.Mood.Excitement;
					Sync.Excitement = person_.Mood.Excitement;
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
					movement_.WindowMagnitude = person_.Mood.Excitement;
					Sync.Excitement = person_.Mood.Excitement;
					Next();
					break;
				}

				case BasicSync.SyncFinished:
				{
					movement_.WindowMagnitude = person_.Mood.Excitement;
					Sync.Excitement = person_.Mood.Excitement;
					oneFrameFinished_ = true;
					break;
				}

			}
		}

		private void Apply()
		{
			var v = Lerped();

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

		public override string ToString()
		{
			return $"{rbId_} ({BodyParts.ToString(bodyPart_)})";
		}

		public override string ToDetailedString()
		{
			return
				$"{TypeToString(type_)} {rbId_} ({BodyParts.ToString(bodyPart_)})\n" +
				$"{Sync.ToDetailedString()}\n" +
				$"{movement_}\n" +
				$"ex={ExcitementFactor():0.00}\n" +
				$"lerped={Lerped()} busy={wasBusy_}";
		}

		private float ExcitementFactor()
		{
			return excitement_.Magnitude(person_.Mood.Excitement);
		}

		private void Next()
		{
			beforeNext_?.Invoke();

			if (!movement_.Next())
				movement_.SetNext(movement_.Last);
		}
	}
}