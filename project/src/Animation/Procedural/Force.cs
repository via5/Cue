using SimpleJSON;
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

		private const float BusyResetTime = 1;

		private int type_;
		private int bodyPartType_;
		private BodyPart bp_ = null;
		private IEasing easing_;

		private bool oneFrameFinished_ = false;
		private Action beforeNext_ = null;

		private bool wasBusy_ = false;
		private Vector3 forceBeforeBusy_;
		private float busyElapsed_ = 0;
		private IEasing busyResetEasing_ = new SinusoidalEasing();

		private Vector3 min_, max_;
		private Vector3 last_, target_;
		private Vector3 window_;
		private IEasing windowEasing_ = new LinearEasing();
		private Duration next_;
		private bool needRange_ = false;


		public Force(
			string name, int type, int bodyPart,
			Vector3 min, Vector3 max, float next, Vector3 window,
			ISync sync, IEasing easing = null)
				: this(name, type, bodyPart, min, max, new Duration(next), window, sync, easing)
		{
		}

		public Force(
			string name, int type, int bodyPart,
			Vector3 min, Vector3 max, Duration next, Vector3 window,
			ISync sync, IEasing easing = null)
				: base(name, sync)
		{
			type_ = type;
			bodyPartType_ = bodyPart;
			min_ = min;
			max_ = max;
			next_ = next ?? new Duration();
			window_ = window;
			easing_ = easing ?? new SinusoidalEasing();

			if (window_ == Vector3.Zero)
				window_ = Vector3.Abs(max_ - min);
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

				Vector3 min, max, window;
				IEasing windowEasing = null;
				Duration next;

				{
					if (!o.HasKey("movement"))
						throw new LoadFailed($"movement is missing");

					var oo = o["movement"].AsObject;

					if (oo == null)
						throw new LoadFailed($"movement not an object");

					min = Vector3.FromJSON(oo, "min", true);
					max = Vector3.FromJSON(oo, "max", true);

					if (oo.HasKey("window"))
						window = Vector3.FromJSON(oo, "window", true);
					else
						window = max - min;

					if (oo.HasKey("windowEasing") && oo["windowEasing"].Value != "")
					{
						string en = oo["windowEasing"];
						windowEasing = EasingFactory.FromString(en);
						if (windowEasing == null)
							throw new LoadFailed($"movement bad easing name");
					}

					float nextMin = 0;
					if (oo.HasKey("nextMinTime"))
					{
						if (!float.TryParse(oo["nextMinTime"], out nextMin))
							throw new LoadFailed($"movement nextMinTime is not a number");
					}

					float nextMax = 0;
					if (oo.HasKey("nextMaxTime"))
					{
						if (!float.TryParse(oo["nextMaxTime"], out nextMax))
							throw new LoadFailed($"movement nextMaxTime is not a number");
					}

					next = new Duration(nextMin, nextMax);
				}

				return new Force(
					"", type, bodyPart, min, max, next, window, sync);
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

		public override ITarget Clone()
		{
			var f = new Force(
				Name, type_, bodyPartType_,
				min_, max_, next_.Clone(), window_, Sync.Clone());

			f.beforeNext_ = beforeNext_;

			return f;
		}

		protected override void DoStart(Person p, AnimationContext cx)
		{
			bp_ = p.Body.Get(bodyPartType_);
			needRange_ = true;
			Next();
		}

		public override void Reset()
		{
			base.Reset();
			last_ = Vector3.Zero;
			target_ = Vector3.Zero;
			next_.Reset(MovementEnergy);
			Next();
		}

		public override void RequestStop()
		{
			last_ = Lerped();
			target_ = Vector3.Zero;
			base.RequestStop();
		}

		public void SetRange(Vector3 min, Vector3 max, Vector3 win)
		{
			min_ = min;
			max_ = max;
			window_ = win;
			needRange_ = true;
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

		protected override void DoFixedUpdate(float s)
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


			next_.Update(s, 1);
			if (next_.Finished)
				needRange_ = true;

			Apply(Lerped());

			switch (Sync.UpdateResult)
			{
				case BasicSync.Working:
				{
					break;
				}

				case BasicSync.DurationFinished:
				{
					var temp = last_;
					last_ = target_;
					target_ = temp;

					break;
				}

				case BasicSync.Delaying:
				case BasicSync.DelayFinished:
				{
					break;
				}

				case BasicSync.Looping:
				{
					Next();
					break;
				}

				case BasicSync.SyncFinished:
				{
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
			return Vector3.Lerp(last_, target_, easing_.Magnitude(Sync.Magnitude));
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
				$"last={last_} target={target_}\n" +
				$"min={min_} max={max_} win={window_}\n" +
				$"next={next_}\n" +
				$"lerped={Lerped()} busy={wasBusy_}";
		}

		private void Next()
		{
			beforeNext_?.Invoke();

			var temp = last_;
			last_ = target_;
			target_ = temp;

			if (needRange_)
			{
				NextTarget();
				needRange_ = false;
			}
		}

		private void NextTarget()
		{
			Vector3 min, max;

			CalculateWindow(min_.X, max_.X, window_.X, out min.X, out max.X);
			CalculateWindow(min_.Y, max_.Y, window_.Y, out min.Y, out max.Y);
			CalculateWindow(min_.Z, max_.Z, window_.Z, out min.Z, out max.Z);

			target_ = new Vector3(
				U.RandomFloat(min.X, max.X),
				U.RandomFloat(min.Y, max.Y),
				U.RandomFloat(min.Z, max.Z));
		}

		private void CalculateWindow(
			float min, float max, float size, out float wMin, out float wMax)
		{
			var range = max - min;

			if (min < max)
				range -= size;
			else
				range += size;

			wMin = min + range * windowEasing_.Magnitude(MovementEnergy);

			if (min < max)
				wMax = wMin + size;
			else
				wMax = wMin - size;
		}
	}
}
