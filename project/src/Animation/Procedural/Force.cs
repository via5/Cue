using SimpleJSON;
using System;
using System.Collections.Generic;

namespace Cue.Proc
{
	public class Force : BasicTarget
	{
		struct VectorTarget
		{
			public Vector3 min, max, window, last, target;

			public VectorTarget(Vector3 min, Vector3 max, Vector3 window)
			{
				this.min = min;
				this.max = max;
				this.window = window;
				this.last = Vector3.Zero;
				this.target = Vector3.Zero;

				if (this.window == Vector3.Zero)
					this.window = Vector3.Abs(max - min);
			}

			public void Swap()
			{
				var temp = last;
				last = target;
				target = temp;
			}

			public void Reset()
			{
				last = Vector3.Zero;
				target = Vector3.Zero;
			}

			public override string ToString()
			{
				return
					$"last={last} target={target}\n" +
					$"min={min} max={max} win={window}";
			}
		}

		struct DirTarget
		{
			public float min, max, window, last, target;
			public Vector3 dir;

			public void Swap()
			{
				var temp = last;
				last = target;
				target = temp;
			}

			public void Reset()
			{
				last = 0;
				target = 0;
			}

			public override string ToString()
			{
				return
					$"last={last} target={target}\n" +
					$"min={min} max={max} win={window} dir={dir}";
			}
		}

		public const int RelativeForce = 1;
		public const int RelativeTorque = 2;
		public const int AbsoluteForce = 3;
		public const int AbsoluteTorque = 4;

		private const float BusyResetTime = 1;
		private const float NotBusyCatchUpTime = 1;

		private int type_;
		private BodyPartTypes bodyPartType_;
		private BodyPart bp_ = null;
		private IEasing easing_;

		private bool oneFrameFinished_ = false;
		private Action beforeNext_ = null;

		private bool wasBusy_ = false;
		private Vector3 forceBeforeBusy_;
		private float busyElapsed_ = 0;
		private float notBusyElapsed_ = 0;
		private IEasing busyResetEasing_ = new SinusoidalEasing();
		private IEasing notBusyResetEasing_ = new SinusoidalEasing();

		private IEasing windowEasing_ = new LinearEasing();
		private Duration next_;
		private bool needsNewsTarget_ = false;

		private VectorTarget vtarget_;
		private DirTarget dtarget_;
		private bool useDir_ = false;


		public Force(
			string name, int type, BodyPartTypes bodyPart,
			Vector3 min, Vector3 max, float next, Vector3 window,
			ISync sync, IEasing easing = null)
				: this(name, type, bodyPart, min, max, new Duration(next), window, sync, easing)
		{
		}

		// used in tests
		//
		public Force(
			string name, int type, BodyPart bodyPart,
			Vector3 min, Vector3 max, Duration next, Vector3 window,
			ISync sync, IEasing easing = null)
				: this(name, type, bodyPart.Type, min, max, next, window, sync, easing)
		{
			bp_ = bodyPart;
		}

		public Force(
			string name, int type, BodyPartTypes bodyPart,
			Vector3 min, Vector3 max, Duration next, Vector3 window,
			ISync sync, IEasing easing = null)
				: base(name, sync)
		{
			type_ = type;
			bodyPartType_ = bodyPart;
			vtarget_ = new VectorTarget(min, max, window);
			next_ = next ?? new Duration();
			easing_ = easing ?? new SinusoidalEasing();
		}

		public static Force Create(int type, JSONClass o)
		{
			try
			{
				var bodyPart = BodyPartTypes.FromString(o["bodyPart"]);
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
				vtarget_.min, vtarget_.max, next_.Clone(),
				vtarget_.window, Sync.Clone());

			f.beforeNext_ = beforeNext_;

			return f;
		}

		protected override void DoStart(Person p, AnimationContext cx)
		{
			if (bp_ == null)
				bp_ = p.Body.Get(bodyPartType_);

			vtarget_.Reset();
			dtarget_.Reset();
			needsNewsTarget_ = true;
			Next();
		}

		public override void Reset()
		{
			base.Reset();
			vtarget_.Reset();
			dtarget_.Reset();
			needsNewsTarget_ = true;
			next_.Reset(MovementEnergy);
			Next();
		}

		public override void RequestStop()
		{
			if (useDir_)
			{
				dtarget_.last = LerpedForDir();
				dtarget_.target = 0;
			}
			else
			{
				vtarget_.last = LerpedForVector();
				vtarget_.target = Vector3.Zero;
			}

			base.RequestStop();
		}

		public void SetRangeWithDirection(
			float min, float max, float win, Vector3 dir)
		{
			// ignore dir
			if (!useDir_ || min != dtarget_.min || max != dtarget_.max || win != dtarget_.window)
				needsNewsTarget_ = true;

			dtarget_.min = min;
			dtarget_.max = max;
			dtarget_.dir = dir;
			dtarget_.window = win;
			useDir_ = true;
		}

		public void SetRange(Vector3 min, Vector3 max, Vector3 win)
		{
			if (useDir_ || min != vtarget_.min || max != vtarget_.max || win != vtarget_.window)
				needsNewsTarget_ = true;

			vtarget_.min = min;
			vtarget_.max = max;
			vtarget_.window = win;
			useDir_ = false;
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

			next_.Update(s, 1);
			if (next_.Finished)
				needsNewsTarget_ = true;


			if (!CanAppyForce())
			{
				notBusyElapsed_ = 0;

				if (wasBusy_)
				{
					busyElapsed_ += s;
				}
				else
				{
					forceBeforeBusy_ = LerpedForce();
					busyElapsed_ = 0;
				}

				wasBusy_ = true;

				var p = U.Clamp(busyElapsed_ / BusyResetTime, 0, 1);
				var mag = busyResetEasing_.Magnitude(p);
				var v = Vector3.Lerp(forceBeforeBusy_, Vector3.Zero, mag);

				Apply(v);

				if (p >= 1)
					oneFrameFinished_ = true;
			}
			else if (wasBusy_)
			{
				notBusyElapsed_ += s;

				if (notBusyElapsed_ >= NotBusyCatchUpTime)
				{
					wasBusy_ = false;
					Apply(LerpedForce());
				}
				else
				{
					var p = U.Clamp(notBusyElapsed_ / NotBusyCatchUpTime, 0, 1);
					var mag = notBusyResetEasing_.Magnitude(p);
					var v = Vector3.Lerp(Vector3.Zero, LerpedForce(), mag);

					Apply(v);
				}
			}
			else
			{
				Apply(LerpedForce());
			}


			switch (Sync.UpdateResult)
			{
				case BasicSync.Working:
				{
					break;
				}

				case BasicSync.DurationFinished:
				{
					if (useDir_)
						dtarget_.Swap();
					else
						vtarget_.Swap();

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

		private Vector3 LerpedForce()
		{
			if (useDir_)
				return LerpedForDir() * dtarget_.dir;
			else
				return LerpedForVector();
		}

		private float LerpedForDir()
		{
			float f = easing_.Magnitude(Sync.Magnitude);
			return U.Lerp(dtarget_.last, dtarget_.target, f);
		}

		private Vector3 LerpedForVector()
		{
			float f = easing_.Magnitude(Sync.Magnitude);
			return Vector3.Lerp(vtarget_.last, vtarget_.target, f);
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
				$"{bp_} ({BodyPartTypes.ToString(bodyPartType_)}) " +
				$"{TypeToString(type_)} {Sys.Vam.U.ToUnity(LerpedForce())}");
		}

		public override string ToString()
		{
			return
				$"{TypeToString(type_)}.{BodyPartTypes.ToString(bodyPartType_)}" +
				(Name == "" ? "" : $" '{Name}'");
		}

		public override string ToDetailedString()
		{
			return
				$"{TypeToString(type_)} {Name} {bp_}\n" +
				$"usedir={useDir_} {(useDir_ ? dtarget_.ToString() : vtarget_.ToString())}\n" +
				$"next={next_}\n" +
				$"lerped={LerpedForce()} busy={wasBusy_} e={MovementEnergy}";
		}

		private void Next()
		{
			beforeNext_?.Invoke();

			if (useDir_)
				dtarget_.Swap();
			else
				vtarget_.Swap();

			if (needsNewsTarget_)
			{
				NextTarget();
				needsNewsTarget_ = false;
			}
		}

		private void NextTarget()
		{
			if (useDir_)
			{
				float min, max;

				CalculateWindow(dtarget_.min, dtarget_.max, dtarget_.window, out min, out max);

				dtarget_.target = U.RandomFloat(min, max);
			}
			else
			{
				Vector3 min, max;

				CalculateWindow(vtarget_.min.X, vtarget_.max.X, vtarget_.window.X, out min.X, out max.X);
				CalculateWindow(vtarget_.min.Y, vtarget_.max.Y, vtarget_.window.Y, out min.Y, out max.Y);
				CalculateWindow(vtarget_.min.Z, vtarget_.max.Z, vtarget_.window.Z, out min.Z, out max.Z);

				vtarget_.target = new Vector3(
					U.RandomFloat(min.X, max.X),
					U.RandomFloat(min.Y, max.Y),
					U.RandomFloat(min.Z, max.Z));
			}
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
