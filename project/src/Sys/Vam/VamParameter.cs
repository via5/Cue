using System;

namespace Cue.W
{
	abstract class VamParameterChecker
	{
		private readonly float interval_;
		private float elapsed_ = 0;
		private bool stale_ = true;
		protected bool checkedOnce_ = false;
		private int backoff_ = 0;

		public VamParameterChecker(float interval = -1)
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


	abstract class VamBasicParameterRO<NativeType, StorableType> : VamParameterChecker
		where StorableType : JSONStorableParam
	{
		protected Atom atom_;
		protected string storableID_;
		protected string paramName_;
		protected StorableType param_ = null;
		protected NativeType dummyValue_;

		public VamBasicParameterRO(IAtom a, string s, string name)
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

		protected NativeType GetValue()
		{
			if (atom_ == null || !Check())
				return dummyValue_;

			try
			{
				return DoGetValue();
			}
			catch (Exception e)
			{
				Cue.LogError(
					$"{atom_.uid}: can't get val " +
					$"for '{storableID_}' '{paramName_}': " +
					e.ToString());

				param_ = null;
				MakeStale();

				return dummyValue_;
			}
		}

		protected NativeType GetDefaultValue()
		{
			if (atom_ == null || !Check())
				return dummyValue_;

			try
			{
				return DoGetDefaultValue();
			}
			catch (Exception e)
			{
				Cue.LogError(
					$"{atom_.uid}: can't get def val " +
					$"for '{storableID_}' '{paramName_}': " +
					e.ToString());

				param_ = null;
				MakeStale();

				return dummyValue_;
			}
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


	abstract class VamBasicParameter<NativeType, StorableType>
		: VamBasicParameterRO<NativeType, StorableType>
			where StorableType : JSONStorableParam
	{
		public VamBasicParameter(IAtom a, string s, string name)
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


	class VamBoolParameterRO : VamBasicParameterRO<bool, JSONStorableBool>
	{
		public VamBoolParameterRO(IObject o, string s, string name)
			: base(o.Atom, s, name)
		{
		}

		public VamBoolParameterRO(IAtom a, string s, string name)
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


	class VamBoolParameter : VamBasicParameter<bool, JSONStorableBool>
	{
		public VamBoolParameter(IObject o, string s, string name)
			: base(o.Atom, s, name)
		{
		}

		public VamBoolParameter(IAtom a, string s, string name)
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


	class VamFloatParameterRO : VamBasicParameterRO<float, JSONStorableFloat>
	{
		public VamFloatParameterRO(IObject o, string s, string name)
			: base(o.Atom, s, name)
		{
		}

		public VamFloatParameterRO(IAtom a, string s, string name)
			: base(a, s, name)
		{
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


	class VamFloatParameter : VamBasicParameter<float, JSONStorableFloat>
	{
		public VamFloatParameter(IObject o, string s, string name)
			: base(o.Atom, s, name)
		{
		}

		public VamFloatParameter(IAtom a, string s, string name)
			: base(a, s, name)
		{
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


	class VamStringChooserParameter : VamBasicParameter<string, JSONStorableStringChooser>
	{
		public VamStringChooserParameter(IObject o, string s, string name)
			: base(o.Atom, s, name)
		{
		}

		public VamStringChooserParameter(IAtom a, string s, string name)
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


	class VamStringChooserParameterRO : VamBasicParameterRO<string, JSONStorableStringChooser>
	{
		public VamStringChooserParameterRO(IObject o, string s, string name)
			: base(o.Atom, s, name)
		{
		}

		public VamStringChooserParameterRO(IAtom a, string s, string name)
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


	class VamStringParameter : VamBasicParameter<string, JSONStorableString>
	{
		public VamStringParameter(IObject o, string s, string name)
			: base(o.Atom, s, name)
		{
		}

		public VamStringParameter(IAtom a, string s, string name)
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


	class VamStringParameterRO : VamBasicParameterRO<string, JSONStorableString>
	{
		public VamStringParameterRO(IObject o, string s, string name)
			: base(o.Atom, s, name)
		{
		}

		public VamStringParameterRO(IAtom a, string s, string name)
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


	class VamColorParameter : VamBasicParameter<Color, JSONStorableColor>
	{
		public VamColorParameter(IObject o, string s, string name)
			: base(o.Atom, s, name)
		{
		}

		public VamColorParameter(IAtom a, string s, string name)
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
			return VamU.FromHSV(param_.val);
		}

		protected override void DoSetValue(Color c)
		{
			param_.val = VamU.ToHSV(c);
		}

		protected override Color DoGetDefaultValue()
		{
			return VamU.FromHSV(param_.defaultVal);
		}
	}


	class VamColorParameterRO : VamBasicParameterRO<Color, JSONStorableColor>
	{
		public VamColorParameterRO(IObject o, string s, string name)
			: base(o.Atom, s, name)
		{
		}

		public VamColorParameterRO(IAtom a, string s, string name)
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

			return VamU.FromUnity(
				UnityEngine.Color.HSVToRGB(hsv.H, hsv.S, hsv.V));
		}

		protected override Color DoGetDefaultValue()
		{
			return VamU.FromHSV(param_.defaultVal);
		}
	}


	class VamActionParameter : VamParameterChecker
	{
		protected Atom atom_;
		protected string storableID_;
		protected string paramName_;
		protected JSONStorableAction param_ = null;

		public VamActionParameter(IObject o, string s, string name)
			: this((o.Atom as VamAtom)?.Atom, s, name)
		{
		}

		public VamActionParameter(Atom a, string s, string name)
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
