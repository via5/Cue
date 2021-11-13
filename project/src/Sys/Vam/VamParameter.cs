using System;

namespace Cue.Sys.Vam
{
	static class Parameters
	{
		private const int CheckPluginCount = 20;

		public static string[] MakeStorableNamesCache(string name)
		{
			var c = new string[CheckPluginCount];

			for (int i = 0; i < CheckPluginCount; ++i)
				c[i] = $"plugin#{i}_{name}";

			return c;
		}

		private static JSONStorable FindStorable(
			Atom a, string name, string[] storableNamesCache)
		{
			var s = a.GetStorableByID(name);

			if (s == null)
			{
				for (int i = 0; i < CheckPluginCount; ++i)
				{
					string p;

					if (storableNamesCache == null)
						p = $"plugin#{i}_{name}";
					else
						p = storableNamesCache[i];

					s = a.GetStorableByID(p);
					if (s != null)
						break;
				}
			}

			return s;
		}

		public static JSONStorableFloat GetFloat(
			IObject o, string storable, string param,
			string[] storableNamesCache = null)
		{
			return GetFloat(
				(o?.Atom as VamAtom)?.Atom,
				storable, param, storableNamesCache);
		}

		public static JSONStorableFloat GetFloat(
			Atom a, string storable, string param,
			string[] storableNamesCache = null)
		{
			if (a == null)
				return null;

			var st = FindStorable(a, storable, storableNamesCache);
			if (st == null)
				return null;

			return st.GetFloatJSONParam(param);
		}

		public static JSONStorableBool GetBool(
			IObject o, string storable, string param,
			string[] storableNamesCache = null)
		{
			return GetBool(
				(o?.Atom as VamAtom)?.Atom,
				storable, param, storableNamesCache);
		}

		public static JSONStorableBool GetBool(
			Atom a, string storable, string param,
			string[] storableNamesCache = null)
		{
			if (a == null)
				return null;

			var st = FindStorable(a, storable, storableNamesCache);
			if (st == null)
				return null;

			return st.GetBoolJSONParam(param);
		}

		public static JSONStorableString GetString(
			IObject o, string storable, string param,
			string[] storableNamesCache = null)
		{
			return GetString(
				(o?.Atom as VamAtom)?.Atom,
				storable, param, storableNamesCache);
		}

		public static JSONStorableString GetString(
			Atom a, string storable, string param,
			string[] storableNamesCache = null)
		{
			if (a == null)
				return null;

			var st = FindStorable(a, storable, storableNamesCache);
			if (st == null)
				return null;

			return st.GetStringJSONParam(param);
		}


		public static JSONStorableStringChooser GetStringChooser(
			IObject o, string storable, string param,
			string[] storableNamesCache = null)
		{
			return GetStringChooser(
				(o?.Atom as VamAtom)?.Atom, storable, param, storableNamesCache);
		}

		public static JSONStorableStringChooser GetStringChooser(
			Atom a, string storable, string param,
			string[] storableNamesCache = null)
		{
			if (a == null)
				return null;

			var st = FindStorable(a, storable, storableNamesCache);
			if (st == null)
				return null;

			return st.GetStringChooserJSONParam(param);
		}


		public static JSONStorableColor GetColor(
		IObject o, string storable, string param,
			string[] storableNamesCache = null)
		{
			return GetColor(
				(o?.Atom as VamAtom)?.Atom, storable, param, storableNamesCache);
		}

		public static JSONStorableColor GetColor(
			Atom a, string storable, string param,
			string[] storableNamesCache = null)
		{
			if (a == null)
				return null;

			var st = FindStorable(a, storable, storableNamesCache);
			if (st == null)
				return null;

			return st.GetColorJSONParam(param);
		}


		public static JSONStorableAction GetAction(
			IObject o, string storable, string param,
			string[] storableNamesCache = null)
		{
			return GetAction(
				(o?.Atom as VamAtom)?.Atom, storable, param, storableNamesCache);
		}

		public static JSONStorableAction GetAction(
			Atom a, string storable, string param,
			string[] storableNamesCache = null)
		{
			if (a == null)
				return null;

			var st = FindStorable(a, storable, storableNamesCache);
			if (st == null)
				return null;

			return st.GetAction(param);
		}
	}


	abstract class ParameterChecker
	{
		protected VamAtom atom_;
		protected string storableID_;
		protected string paramName_;
		private readonly float interval_;
		private Logger log_ = null;

		private float lastCheck_ = -1;
		private bool stale_ = true;
		protected bool checkedOnce_ = false;
		private int backoff_ = 0;
		protected string[] storableNamesCache_;

		protected ParameterChecker(
			VamAtom a, string storableId, string paramName, float interval = -1)
		{
			atom_ = a;
			storableID_ = storableId;
			paramName_ = paramName;

			// so they don't all check on the same frame
			interval_ = (interval < 0 ? U.RandomFloat(1.5f, 2.5f) : interval);

			storableNamesCache_ = Parameters.MakeStorableNamesCache(storableId);
		}

		public Logger Log
		{
			get
			{
				if (log_ == null)
				{
					log_ = new Logger(
						Logger.Sys, atom_, $"{storableID_}.{paramName_}");
				}

				return log_;
			}
		}

		public void MakeStale()
		{
			stale_ = true;
		}

		public bool Check(bool force = false)
		{
			if (CheckIfDead())
			{
				stale_ = true;
				lastCheck_ = -1;
				backoff_ = 0;
			}

			if (stale_)
			{
				var now = Cue.Instance.Sys.RealtimeSinceStartup;

				if (TimeToCheck(now, force))
				{
					if (GetParameter())
					{
						stale_ = false;
						backoff_ = 0;
					}
					else
					{
						backoff_ = Math.Min(backoff_ + 1, 5);
					}

					lastCheck_ = now;
				}
			}

			checkedOnce_ = true;

			return !stale_;
		}

		private bool TimeToCheck(float now, bool force)
		{
			if (lastCheck_ < 0 || force)
				return true;

			var d = (now - lastCheck_);
			if (d >= (interval_ + backoff_))
				return true;

			return false;
		}

		protected abstract bool CheckIfDead();
		protected abstract bool GetParameter();

		protected string DeadString()
		{
			var now = Cue.Instance.Sys.RealtimeSinceStartup;
			var elapsed = now - lastCheck_;
			return $"s={stale_},e={elapsed:0.00},bo={backoff_},i={interval_:0.00}";
		}
	}


	abstract class BasicParameterRO<NativeType, StorableType> : ParameterChecker
		where StorableType : JSONStorableParam
	{
		protected StorableType param_ = null;
		protected NativeType dummyValue_;

		public BasicParameterRO(IAtom a, string s, string name)
			: base(a as VamAtom, s, name)
		{
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
			if (Check())
			{
				try
				{
					return DoGetValue();
				}
				catch (Exception e)
				{
					Log.Error(
						$"can't get value" +
						$"for '{storableID_}' '{paramName_}': " +
						e.ToString());

					param_ = null;
					MakeStale();
				}
			}

			return dummyValue_;
		}

		protected NativeType GetDefaultValue()
		{
			if (Check())
			{
				try
				{
					return DoGetDefaultValue();
				}
				catch (Exception e)
				{
					Log.Error($"can't get default value, {e}");
					param_ = null;
					MakeStale();
				}
			}

			return dummyValue_;
		}

		protected override bool CheckIfDead()
		{
			if (atom_ == null)
				return false;

			if (param_ != null)
			{
				if (param_.storable == null)
				{
					Log.Info("param is dead");
					param_ = null;
					return true;
				}
			}

			return false;
		}

		protected override bool GetParameter()
		{
			if (atom_ == null)
				return false;

			param_ = DoGetParameter();

			if (param_ == null)
			{
				if (!checkedOnce_)
					Log.Verbose($"param not found");
			}
			else
			{
				Log.Verbose($"found param");
			}

			return (param_ != null);
		}

		public override string ToString()
		{
			if (param_ == null)
			{
				if (checkedOnce_)
					return $"(dead)";
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

			if (Check())
			{
				try
				{
					DoSetValue(v);
				}
				catch (Exception e)
				{
					Log.Error($"can't set val, {e}");
					param_ = null;
					MakeStale();
				}
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
			return Parameters.GetBool(
				atom_.Atom, storableID_, paramName_, storableNamesCache_);
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
			return Parameters.GetBool(
				atom_.Atom, storableID_, paramName_, storableNamesCache_);
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
				if (Check())
				{
					try
					{
						return param_.min;
					}
					catch (Exception e)
					{
						Log.Error($"can't get minimum value, {e}");
						param_ = null;
						MakeStale();
					}
				}

				return dummyValue_;
			}
		}

		public float Maximum
		{
			get
			{
				if (Check())
				{
					try
					{
						return param_.max;
					}
					catch (Exception e)
					{
						Log.Error($"can't get maximum value, {e}");
						param_ = null;
						MakeStale();
					}
				}

				return dummyValue_;
			}
		}

		protected override JSONStorableFloat DoGetParameter()
		{
			return Parameters.GetFloat(
				atom_.Atom, storableID_, paramName_, storableNamesCache_);
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
				if (Check())
				{
					try
					{
						return param_.min;
					}
					catch (Exception e)
					{
						Log.Error($"can't get minimum value, {e}");
						param_ = null;
						MakeStale();
					}
				}

				return dummyValue_;
			}
		}

		public float Maximum
		{
			get
			{
				if (Check())
				{
					try
					{
						return param_.max;
					}
					catch (Exception e)
					{
						Log.Error($"can't get maximum value, {e}");
						param_ = null;
						MakeStale();
					}
				}

				return dummyValue_;
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
			return Parameters.GetFloat(
				atom_.Atom, storableID_, paramName_, storableNamesCache_);
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
			return Parameters.GetStringChooser(
				atom_.Atom, storableID_, paramName_, storableNamesCache_);
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
			return Parameters.GetStringChooser(
				atom_.Atom, storableID_, paramName_, storableNamesCache_);
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
			return Parameters.GetString(
				atom_.Atom, storableID_, paramName_, storableNamesCache_);
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
			return Parameters.GetString(
				atom_.Atom, storableID_, paramName_, storableNamesCache_);
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
			return Parameters.GetColor(
				atom_.Atom, storableID_, paramName_, storableNamesCache_);
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
			return Parameters.GetColor(
				atom_.Atom, storableID_, paramName_, storableNamesCache_);
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
		protected JSONStorableAction param_ = null;

		public ActionParameter(IObject o, string s, string name)
			: this(o.Atom as VamAtom, s, name)
		{
		}

		public ActionParameter(VamAtom a, string s, string name)
			: base(a, s, name)
		{
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
				Log.Error($"can't fire action, {e}");
				param_ = null;
				MakeStale();
			}
		}

		protected override bool CheckIfDead()
		{
			if (atom_ == null)
				return false;

			if (param_ != null)
			{
				if (param_.storable == null)
				{
					Log.Info($"action param is dead");

					param_ = null;
					return true;
				}
			}

			return false;
		}

		protected override bool GetParameter()
		{
			param_ = Parameters.GetAction(
				atom_.Atom, storableID_, paramName_, storableNamesCache_);

			return (param_ != null);
		}
	}
}
