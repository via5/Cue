using System;

namespace Cue
{
	public class ForceableFloat
	{
		private float value_;
		private float forced_;
		private bool isForced_;

		public ForceableFloat(float value=0)
		{
			value_ = value;
			forced_ = value;
			isForced_ = false;
		}

		public float Value
		{
			get
			{
				if (isForced_)
					return forced_;
				else
					return value_;
			}

			set
			{
				value_ = value;
			}
		}

		public bool IsForced
		{
			get { return isForced_; }
		}

		public float UnforcedValue
		{
			get { return value_; }
		}

		public void SetForced(float f)
		{
			isForced_ = true;
			forced_ = f;
		}

		public void UnsetForced()
		{
			isForced_ = false;
		}

		public override string ToString()
		{
			if (isForced_)
				return $"{forced_:0.000000} (forced)";
			else
				return $"{value_:0.000000}";
		}
	}


	public class ForceableBool
	{
		private bool value_;
		private bool forced_;
		private bool isForced_;

		public ForceableBool(bool value = false)
		{
			value_ = value;
			forced_ = value;
			isForced_ = false;
		}

		public bool Value
		{
			get
			{
				if (isForced_)
					return forced_;
				else
					return value_;
			}

			set
			{
				value_ = value;
			}
		}

		public bool IsForced
		{
			get { return isForced_; }
		}

		public bool UnforcedValue
		{
			get { return value_; }
		}

		public void SetForced(bool value)
		{
			isForced_ = true;
			forced_ = value;
		}

		public void UnsetForced()
		{
			isForced_ = false;
		}

		public override string ToString()
		{
			if (isForced_)
				return $"{forced_} (forced)";
			else
				return $"{value_}";
		}
	}


	// [0, 1], starts at 0
	//
	public class DampedFloat : ForceableFloat
	{
		private float target_;
		private float up_;
		private float down_;
		private float old_;

		public DampedFloat(float upFactor = 0.1f, float downFactor = 0.1f)
		{
			target_ = 0;
			up_ = upFactor;
			down_ = downFactor;
			old_ = Value;
		}

		public float Target
		{
			get { return target_; }
			set { target_ = value; }
		}

		public new float Value
		{
			get { return base.Value; }
		}

		public float UpRate
		{
			get { return up_; }
			set { up_ = value; }
		}

		public float DownRate
		{
			get { return down_; }
			set { down_ = value; }
		}

		public float CurrentRate
		{
			get
			{
				if (target_ > Value)
					return up_;
				else
					return -down_;
			}
		}

		public void SetValue(float f)
		{
			base.Value = f;
		}

		public bool Update(float s)
		{
			if (target_ > Value)
				base.Value = U.Clamp(Value + s * up_, 0, target_);
			else
				base.Value = U.Clamp(Value - s * down_, target_, 1);

			bool changed = (old_ != Value);
			old_ = Value;

			return changed;
		}

		public override string ToString()
		{
			if (IsForced)
				return base.ToString();
			else if (Math.Abs(target_ - Value) > 0.0001f)
				return $"{Value:0.000}=>{target_:0.000}@{CurrentRate:0.00000}";
			else
				return $"{Value:0.000}";
		}
	}
}
