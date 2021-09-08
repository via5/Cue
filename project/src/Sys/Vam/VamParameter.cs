using System;

namespace Cue.Sys.Vam
{
	abstract class ParameterChecker
	{
		private readonly float interval_;
		private float elapsed_ = 0;
		private bool stale_ = true;
		protected bool checkedOnce_ = false;
		private int backoff_ = 0;

		public ParameterChecker(float interval = -1)
		{
			// so they don't all check on the same frame
			interval_ = (interval < 0 ? U.RandomFloat(1.5f, 2.5f) : interval);

			// force check the first time
			elapsed_ = interval_ + 1;
		}

		public void MakeStale()
		{
			stale_ = true;
		}

		public bool Check(bool force = false)
		{
			if (!DeadCheck())
			{
				stale_ = true;
				elapsed_ = interval_ + 1;
				backoff_ = 0;
			}

			if (stale_)
			{
				elapsed_ += Cue.Instance.Sys.DeltaTime;
				if (elapsed_ > (interval_ + backoff_) || force)
				{
					if (StaleCheck())
					{
						stale_ = false;
						backoff_ = 0;
					}
					else
					{
						backoff_ = Math.Min(backoff_ + 1, 5);
					}

					elapsed_ = 0;
				}
			}

			checkedOnce_ = true;

			return !stale_;
		}

		protected abstract bool DeadCheck();
		protected abstract bool StaleCheck();
	}


	abstract class BasicParameterRO<NativeType, StorableType> : ParameterChecker
		where StorableType : JSONStorableParam
	{
		protected Atom atom_;
		protected string storableID_;
		protected string paramName_;
		protected StorableType param_ = null;
		protected NativeType dummyValue_;

		public BasicParameterRO(IAtom a, string s, string name)
		{
			atom_ = (a as VamAtom)?.Atom;
			storableID_ = s;
			paramName_ = name;
		}

		public NativeType Value
		{
			get { return GetValue(); }
		}

		public NativeType DefaultValue
		{
			get { return GetDefaultValue(); }
		}

		public StorableType Parameter
		{
			get
			{
				Check();
				return param_;
			}
		}

		protected bool Wrap(string what, Action f)
		{
			if (atom_ == null || !Check())
				return false;

			try
			{
				f();
				return true;
			}
			catch (Exception e)
			{
				Cue.LogError(
					$"{atom_.uid}: can't {what} " +
					$"for '{storableID_}' '{paramName_}': " +
					e.ToString());

				param_ = null;
				MakeStale();

				return false;
			}
		}

		protected NativeType GetValue()
		{
			NativeType v = default(NativeType);

			if (Wrap("get val", () => { v = DoGetValue(); }))
				return v;
			else
				return dummyValue_;
		}

		protected NativeType GetDefaultValue()
		{
			NativeType v = default(NativeType);

			if (Wrap("get def val", () => { v = DoGetDefaultValue(); }))
				return v;
			else
				return dummyValue_;
		}

		protected override bool DeadCheck()
		{
			if (atom_ == null)
				return false;

			if (param_ != null)
			{
				if (param_.storable == null)
				{
					if (StaleCheck())
					{
						// ok
						return true;
					}
					else
					{
						Cue.LogInfo(
							$"{atom_.uid}: param " +
							$"{storableID_} {paramName_} is dead");

						param_ = null;
						return false;
					}
				}
			}

			return true;
		}

		protected override bool StaleCheck()
		{
			if (atom_ == null)
				return false;

			param_ = DoGetParameter();

			if (param_ == null)
			{
				if (!checkedOnce_)
					Cue.LogVerbose($"{atom_.uid}: {storableID_} {paramName_} not found");
			}
			else
			{
				Cue.LogVerbose($"{atom_.uid}: found {storableID_} {paramName_}");
			}

			return (param_ != null);
		}

		public override string ToString()
		{
			if (param_ == null)
			{
				if (checkedOnce_)
					return "(dead)";
				else
					return "(?)";
			}
			else
			{
				return Value.ToString();
			}
		}

		protected abstract StorableType DoGetParameter();
		protected abstract NativeType DoGetValue();
		protected abstract NativeType DoGetDefaultValue();
	}


	abstract class BasicParameter<NativeType, StorableType>
		: BasicParameterRO<NativeType, StorableType>
			where StorableType : JSONStorableParam
	{
		public BasicParameter(IAtom a, string s, string name)
			: base(a, s, name)
		{
		}

		public new NativeType Value
		{
			get { return GetValue(); }
			set { SetValue(value); }
		}

		protected void SetValue(NativeType v)
		{
			dummyValue_ = v;

			if (!Check())
				return;

			try
			{
				DoSetValue(v);
			}
			catch (Exception e)
			{
				Cue.LogError(
					$"{atom_.uid}: can't set val " +
					$"for '{storableID_}' '{paramName_}': " +
					e.ToString());

				param_ = null;
				MakeStale();
			}
		}

		protected abstract void DoSetValue(NativeType v);
	}


	class BoolParameterRO : BasicParameterRO<bool, JSONStorableBool>
	{
		public BoolParameterRO(IObject o, string s, string name)
			: base(o.Atom, s, name)
		{
		}

		public BoolParameterRO(IAtom a, string s, string name)
			: base(a, s, name)
		{
		}

		protected override JSONStorableBool DoGetParameter()
		{
			return Cue.Instance.VamSys?.GetBoolParameter(
				atom_, storableID_, paramName_);
		}

		protected override bool DoGetValue()
		{
			return param_.val;
		}

		protected override bool DoGetDefaultValue()
		{
			return param_.defaultVal;
		}
	}


	class BoolParameter : BasicParameter<bool, JSONStorableBool>
	{
		public BoolParameter(IObject o, string s, string name)
			: base(o.Atom, s, name)
		{
		}

		public BoolParameter(IAtom a, string s, string name)
			: base(a, s, name)
		{
		}

		protected override JSONStorableBool DoGetParameter()
		{
			return Cue.Instance.VamSys?.GetBoolParameter(
				atom_, storableID_, paramName_);
		}

		protected override bool DoGetValue()
		{
			return param_.val;
		}

		protected override void DoSetValue(bool b)
		{
			param_.val = b;
		}

		protected override bool DoGetDefaultValue()
		{
			return param_.defaultVal;
		}
	}


	class FloatParameterRO : BasicParameterRO<float, JSONStorableFloat>
	{
		public FloatParameterRO(IObject o, string s, string name)
			: base(o.Atom, s, name)
		{
		}

		public FloatParameterRO(IAtom a, string s, string name)
			: base(a, s, name)
		{
		}

		public float Minimum
		{
			get
			{
				float v = 0;
				Wrap("get min", () => { v = param_.min; });
				return v;
			}
		}

		public float Maximum
		{
			get
			{
				float v = 0;
				Wrap("get max", () => { v = param_.max; });
				return v;
			}
		}

		protected override JSONStorableFloat DoGetParameter()
		{
			return Cue.Instance.VamSys?.GetFloatParameter(
				atom_, storableID_, paramName_);
		}

		protected override float DoGetValue()
		{
			return param_.val;
		}

		protected override float DoGetDefaultValue()
		{
			return param_.defaultVal;
		}
	}


	class FloatParameter : BasicParameter<float, JSONStorableFloat>
	{
		public FloatParameter(IObject o, string s, string name)
			: base(o.Atom, s, name)
		{
		}

		public FloatParameter(IAtom a, string s, string name)
			: base(a, s, name)
		{
		}

		public float Minimum
		{
			get
			{
				float v = 0;
				Wrap("get min", () => { v = param_.min; });
				return v;
			}
		}

		public float Maximum
		{
			get
			{
				float v = 0;
				Wrap("get max", () => { v = param_.max; });
				return v;
			}
		}

		public void SetValueInRange(float p)
		{
			float range;

			if (Maximum > Minimum)
			{
				range = Maximum - Minimum;
				Value = Minimum + range * p;
			}
			else
			{
				range = Minimum - Maximum;
				Value = Maximum + range * p;
			}
		}

		public void SetValueInRangeAboveDefault(float p)
		{
			float range = Maximum - DefaultValue;
			Value = DefaultValue + range * p;
		}

		protected override JSONStorableFloat DoGetParameter()
		{
			return Cue.Instance.VamSys?.GetFloatParameter(
				atom_, storableID_, paramName_);
		}

		protected override float DoGetValue()
		{
			return param_.val;
		}

		protected override void DoSetValue(float b)
		{
			param_.val = b;
		}

		protected override float DoGetDefaultValue()
		{
			return param_.defaultVal;
		}
	}


	class StringChooserParameter : BasicParameter<string, JSONStorableStringChooser>
	{
		public StringChooserParameter(IObject o, string s, string name)
			: base(o.Atom, s, name)
		{
		}

		public StringChooserParameter(IAtom a, string s, string name)
			: base(a, s, name)
		{
		}

		protected override JSONStorableStringChooser DoGetParameter()
		{
			return Cue.Instance.VamSys?.GetStringChooserParameter(
				atom_, storableID_, paramName_);
		}

		protected override string DoGetValue()
		{
			return param_.val;
		}

		protected override void DoSetValue(string b)
		{
			param_.val = b;
		}

		protected override string DoGetDefaultValue()
		{
			return param_.defaultVal;
		}
	}


	class StringChooserParameterRO : BasicParameterRO<string, JSONStorableStringChooser>
	{
		public StringChooserParameterRO(IObject o, string s, string name)
			: base(o.Atom, s, name)
		{
		}

		public StringChooserParameterRO(IAtom a, string s, string name)
			: base(a, s, name)
		{
		}

		protected override JSONStorableStringChooser DoGetParameter()
		{
			return Cue.Instance.VamSys?.GetStringChooserParameter(
				atom_, storableID_, paramName_);
		}

		protected override string DoGetValue()
		{
			return param_.val;
		}

		protected override string DoGetDefaultValue()
		{
			return param_.defaultVal;
		}
	}


	class StringParameter : BasicParameter<string, JSONStorableString>
	{
		public StringParameter(IObject o, string s, string name)
			: base(o.Atom, s, name)
		{
		}

		public StringParameter(IAtom a, string s, string name)
			: base(a, s, name)
		{
		}

		protected override JSONStorableString DoGetParameter()
		{
			return Cue.Instance.VamSys?.GetStringParameter(
				atom_, storableID_, paramName_);
		}

		protected override string DoGetValue()
		{
			return param_.val;
		}

		protected override void DoSetValue(string b)
		{
			param_.val = b;
		}

		protected override string DoGetDefaultValue()
		{
			return param_.defaultVal;
		}
	}


	class StringParameterRO : BasicParameterRO<string, JSONStorableString>
	{
		public StringParameterRO(IObject o, string s, string name)
			: base(o.Atom, s, name)
		{
		}

		public StringParameterRO(IAtom a, string s, string name)
			: base(a, s, name)
		{
		}

		protected override JSONStorableString DoGetParameter()
		{
			return Cue.Instance.VamSys?.GetStringParameter(
				atom_, storableID_, paramName_);
		}

		protected override string DoGetValue()
		{
			return param_.val;
		}

		protected override string DoGetDefaultValue()
		{
			return param_.defaultVal;
		}
	}


	class ColorParameter : BasicParameter<Color, JSONStorableColor>
	{
		public ColorParameter(IObject o, string s, string name)
			: base(o.Atom, s, name)
		{
		}

		public ColorParameter(IAtom a, string s, string name)
			: base(a, s, name)
		{
		}

		protected override JSONStorableColor DoGetParameter()
		{
			return Cue.Instance.VamSys?.GetColorParameter(
				atom_, storableID_, paramName_);
		}

		protected override Color DoGetValue()
		{
			return U.FromHSV(param_.val);
		}

		protected override void DoSetValue(Color c)
		{
			param_.val = U.ToHSV(c);
		}

		protected override Color DoGetDefaultValue()
		{
			return U.FromHSV(param_.defaultVal);
		}
	}


	class ColorParameterRO : BasicParameterRO<Color, JSONStorableColor>
	{
		public ColorParameterRO(IObject o, string s, string name)
			: base(o.Atom, s, name)
		{
		}

		public ColorParameterRO(IAtom a, string s, string name)
			: base(a, s, name)
		{
		}

		protected override JSONStorableColor DoGetParameter()
		{
			return Cue.Instance.VamSys?.GetColorParameter(
				atom_, storableID_, paramName_);
		}

		protected override Color DoGetValue()
		{
			var hsv = param_.val;
			return U.FromUnity(UnityEngine.Color.HSVToRGB(hsv.H, hsv.S, hsv.V));
		}

		protected override Color DoGetDefaultValue()
		{
			return U.FromHSV(param_.defaultVal);
		}
	}


	class ActionParameter : ParameterChecker
	{
		protected Atom atom_;
		protected string storableID_;
		protected string paramName_;
		protected JSONStorableAction param_ = null;

		public ActionParameter(IObject o, string s, string name)
			: this((o.Atom as VamAtom)?.Atom, s, name)
		{
		}

		public ActionParameter(Atom a, string s, string name)
		{
			atom_ = a;
			storableID_ = s;
			paramName_ = name;
		}

		public void Fire()
		{
			if (!Check())
				return;

			try
			{
				param_.actionCallback?.Invoke();
			}
			catch (Exception e)
			{
				Cue.LogError(
					$"{atom_.uid}: can't fire action " +
					$"for '{storableID_}' '{paramName_}': " +
					e.ToString());

				param_ = null;
				MakeStale();
			}
		}

		protected override bool DeadCheck()
		{
			if (param_ != null)
			{
				if (param_.storable == null)
				{
					if (StaleCheck())
					{
						// ok
						return true;
					}
					else
					{
						Cue.LogInfo(
							$"{atom_.uid}: action param " +
							$"{storableID_} {paramName_} is dead");

						param_ = null;
						return false;
					}
				}
			}

			return true;
		}

		protected override bool StaleCheck()
		{
			param_ = Cue.Instance.VamSys?.GetActionParameter(
				atom_, storableID_, paramName_);

			return (param_ != null);
		}
	}
}
