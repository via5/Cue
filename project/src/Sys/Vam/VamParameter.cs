﻿using System;

namespace Cue.W
{
	abstract class VamParameterChecker
	{
		private readonly float interval_;
		private float elapsed_ = 0;
		private bool stale_ = true;
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
			if (stale_)
			{
				elapsed_ += Cue.Instance.Sys.DeltaTime;
				if (elapsed_ > (interval_ + backoff_) || force)
				{
					if (DoCheck())
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

			return !stale_;
		}

		protected abstract bool DoCheck();
	}


	abstract class VamBasicParameter<NativeType, StorableType> : VamParameterChecker
		where StorableType : JSONStorableParam
	{
		protected Atom atom_;
		protected string storableID_;
		protected string paramName_;
		protected StorableType param_ = null;

		public VamBasicParameter(IAtom a, string s, string name)
		{
			atom_ = ((W.VamAtom)a).Atom;
			storableID_ = s;
			paramName_ = name;
		}

		public NativeType GetValue(NativeType def = default(NativeType))
		{
			if (!Check())
				return def;

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

				return def;
			}
		}

		public void SetValue(NativeType v)
		{
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

		protected override bool DoCheck()
		{
			param_ = DoGetParameter();
			if (param_ != null)
				Cue.LogVerbose($"{atom_.uid}: found {storableID_} {paramName_}");

			return (param_ != null);
		}

		protected abstract StorableType DoGetParameter();
		protected abstract NativeType DoGetValue();
		protected abstract void DoSetValue(NativeType v);
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
	}


	class VamActionParameter : VamParameterChecker
	{
		protected Atom atom_;
		protected string storableID_;
		protected string paramName_;
		protected JSONStorableAction param_ = null;

		public VamActionParameter(IObject o, string s, string name)
			: this(((W.VamAtom)o.Atom).Atom, s, name)
		{
		}

		public VamActionParameter(Atom a, string s, string name)
		{
			atom_ = a;
			storableID_ = s;
			paramName_ = name;
			DoCheck();
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

		protected override bool DoCheck()
		{
			param_ = Cue.Instance.VamSys?.GetActionParameter(
				atom_, storableID_, paramName_);

			return (param_ != null);
		}
	}
}
