using System;
using System.Collections.Generic;

namespace Cue.Sys.Vam
{
	class MorphValueHijack
	{
		private static readonly bool Enabled = true;

		private const float ValuePingOffset = 0.012345678f;
		private const float ValuePongOffset = 0.087654321f;
		private const float ValueUninstallOffset = 0.045612378f;

		private static Logger log_ = null;

		public static Logger Log
		{
			get
			{
				if (log_ == null)
					log_ = new Logger(Logger.Sys, "hijacker");

				return log_;
			}
		}

		private static float ValuePing(DAZMorph m)
		{
			if (m.min < m.max)
				return m.min + ValuePingOffset;
			else
				return m.max + ValuePingOffset;
		}

		private static float ValuePong(DAZMorph m)
		{
			if (m.min < m.max)
				return m.min + ValuePongOffset;
			else
				return m.max + ValuePongOffset;
		}

		private static float ValueUninstall(DAZMorph m)
		{
			if (m.min < m.max)
				return m.min + ValueUninstallOffset;
			else
				return m.max + ValueUninstallOffset;
		}

		public static void Install(MorphInfo m)
		{
			if (!Enabled)
				return;

			Log.Verbose($"{m.ID}: installing (tk={CueMain.Instance.Token})");

			bool hadOld = DoUninstall(m);
			DoInstall(m);

			if (hadOld)
				Log.Verbose($"hijacked {m.ID} (replaced old)");
			else
				Log.Verbose($"hijacked {m.ID} (new)");
		}

		public static bool Uninstall(MorphInfo m)
		{
			if (!Enabled)
				return false;

			bool found = DoUninstall(m);

			if (found)
				Log.Verbose($"restored {m.ID}");

			return found;
		}

		private static void DoInstall(MorphInfo m)
		{
			var st = m.DAZMorph?.jsonFloat;
			if (st == null)
			{
				Log.Error($"{m.ID}: can't hijack, no jsonFloat");
				return;
			}

			var old = st.setCallbackFunction;
			st.setCallbackFunction = (v) => Callback(m, v, st, old);

			Log.Verbose($"{m.ID}: installed (tk={CueMain.Instance.Token})");
		}

		private static bool DoUninstall(MorphInfo mi)
		{
			var m = mi?.DAZMorph;
			if (m?.jsonFloat == null)
			{
				Log.Error($"{m.uid}: can't restore, no jsonFloat");
				return false;
			}

			var oldValue = m.jsonFloat.val;

			Log.Verbose($"{m.uid}: trying to uninstall, old value {oldValue} (tk={CueMain.Instance.Token})");

			Log.Verbose($"{m.uid}: pinging (tk={CueMain.Instance.Token})");

			bool hadOld = false;
			m.jsonFloat.val = ValuePing(m);

			try
			{
				if (m.jsonFloat.val == ValuePong(m))
				{
					Log.Verbose($"{m.uid}: pong received, uninstalling (tk={CueMain.Instance.Token})");
					hadOld = true;
					m.jsonFloat.val = ValueUninstall(m);
				}
				else
				{
					Log.Verbose($"{m.uid}: no answer ({m.jsonFloat.val}), not installed (tk={CueMain.Instance.Token})");
				}
			}
			finally
			{
				Log.Verbose($"{m.uid}: restoring old value {oldValue} (tk={CueMain.Instance.Token})");
				m.jsonFloat.val = oldValue;
			}

			return hadOld;
		}

		private static void Callback(MorphInfo mi, float f, JSONStorableFloat st, JSONStorableFloat.SetFloatCallback old)
		{
			var m = mi?.DAZMorph;

			if (f == ValuePing(m))
			{
				Log.Verbose($"{m.uid}: ping received, sending pong (tk={CueMain.Instance.Token})");
				st.valNoCallback = ValuePong(m);
				return;
			}
			else if (f == ValueUninstall(m))
			{
				Log.Verbose($"{m.uid}: uninstall received, uninstalling (tk={CueMain.Instance.Token})");
				st.setCallbackFunction = old;
				Log.Verbose($"{m.uid}: uninstalled (tk={CueMain.Instance.Token})");
			}
			else
			{
				if (MorphInfo.SetFromCue || SuperController.singleton.freezeAnimation)
					old(f);
				else
					mi.SetFromHijacker(f);
			}
		}
	}


	class MorphInfo
	{
		// per second
		public const float MaxChangeSpeed = 8;

		private VamAtom atom_;
		private string id_;
		private DAZMorph m_;
		private List<MorphInfo> subMorphs_ = new List<MorphInfo>();
		private float multiplier_ = 1;
		private int eyesIndex_ = -1;
		private float eyesClosed_ = Morph.NoEyesClosed;

		private int lastSetFrame_ = -1;
		private float morphValueOnLastSet_ = -1;
		private float targetOnLastSet_ = -1;
		private bool limiterEnabled_ = true;

		private static bool setFromCue_ = false;

		public MorphInfo(VamAtom atom, string morphId, DAZMorph m, float eyesClosed)
		{
			atom_ = atom;
			id_ = morphId;
			m_ = m;

			FindSubMorphs();

			if (m_ != null && subMorphs_.Count == 0)
				MorphValueHijack.Install(this);

			if (morphId == "PHMEyesClosedR" || morphId == "PHMEyesClosedL")
			{
				eyesIndex_ = atom_.AddEyesClosed();
				eyesClosed_ = 1;
			}
			else if (Morph.HasEyesClosed(eyesClosed))
			{
				eyesIndex_ = atom_.AddEyesClosed();
				eyesClosed_ = eyesClosed;
			}
		}

		public static bool SetFromCue
		{
			get { return setFromCue_; }
		}

		public DAZMorph DAZMorph
		{
			get { return m_; }
		}

		public string ID
		{
			get { return id_; }
		}

		public float Value
		{
			get { return m_?.morphValue ?? -1; }
		}

		public float DefaultValue
		{
			get { return m_?.startValue ?? 0; }
		}

		public bool LimiterEnabled
		{
			set { limiterEnabled_ = value; }
		}

		public void AddToMap(Dictionary<string, MorphInfo> map)
		{
			if (m_ == null)
				return;

			if (subMorphs_.Count > 0)
			{
				foreach (var m in subMorphs_)
					m.AddToMap(map);
			}
			else
			{
				if (!map.ContainsKey(m_.uid))
					map.Add(m_.uid, this);
			}
		}

		public void OnPluginState(bool b)
		{
			if (m_ == null)
				return;

			if (subMorphs_.Count > 0)
			{
				foreach (var m in subMorphs_)
					m.OnPluginState(b);
			}
			else
			{
				if (b)
					MorphValueHijack.Install(this);
				else
					MorphValueHijack.Uninstall(this);
			}
		}

		public void SetFromHijacker(float f)
		{
			float maxDelta = UnityEngine.Time.fixedDeltaTime * MaxChangeSpeed;

			if (lastSetFrame_ == -1 || (Cue.Instance.Frame - lastSetFrame_) > 200)
				maxDelta = 10000;

			DoSet(f, maxDelta);
		}

		public void Set(float f)
		{
			float maxDelta;

			if (limiterEnabled_)
				maxDelta = UnityEngine.Time.fixedDeltaTime * MaxChangeSpeed;
			else
				maxDelta = 10000;

			DoSet(f, maxDelta);
		}

		public void DoSet(float f, float maxDelta)
		{
			if (m_ == null)
				return;

			bool doSet;

			if (lastSetFrame_ == Cue.Instance.Frame)
			{
				var d = Math.Abs(f - m_.startValue);
				var lastD = Math.Abs(targetOnLastSet_ - m_.startValue);

				doSet = (d > lastD);
			}
			else
			{
				doSet = true;
				morphValueOnLastSet_ = m_.morphValue;
			}

			if (doSet)
			{
				targetOnLastSet_ = f;

				float v;

				if (f > morphValueOnLastSet_)
					v = Math.Min(morphValueOnLastSet_ + maxDelta, f);
				else
					v = Math.Max(morphValueOnLastSet_ - maxDelta, f);

				SetMorphValue(v);
				lastSetFrame_ = Cue.Instance.Frame;
			}

			SetSubMorphs(f, maxDelta);
		}

		public void Reset()
		{
			if (m_ != null)
			{
				try
				{
					setFromCue_ = true;
					m_.morphValue = m_.startValue + 1;
				}
				finally
				{
					setFromCue_ = false;
				}

				SetMorphValue(m_.startValue);
				ResetSubMorphs();
			}
		}

		public override string ToString()
		{
			string s = id_ + " ";

			if (m_ == null)
				s += "notfound";
			else
				s += $"v={m_.morphValue:0.00} sub={subMorphs_.Count != 0}";

			if (eyesIndex_ >= 0)
				s += $" eyes={eyesIndex_}:{eyesClosed_:0.00}";
			else
				s += $" eyes=no";

			return s;
		}

		private void SetMorphValue(float f)
		{
			try
			{
				setFromCue_ = true;

				m_.morphValue = f;

				if (eyesIndex_ >= 0)
					atom_.SetEyesClosed(eyesIndex_, f / eyesClosed_);
			}
			finally
			{
				setFromCue_ = false;
			}
		}

		private void FindSubMorphs()
		{
			if (m_ != null && (m_.deltas == null || m_.deltas.Length == 0))
			{
				foreach (var sm in m_.formulas)
				{
					if (sm.targetType == DAZMorphFormulaTargetType.MorphValue)
					{
						var smm = VamMorphManager.Instance.Get(atom_, sm.target, eyesClosed_, m_.morphBank);
						smm.multiplier_ = sm.multiplier;
						subMorphs_.Add(smm);
					}
					else
					{
						subMorphs_.Clear();
						break;
					}
				}

				if (subMorphs_.Count > 0)
					m_.Reset();
			}
		}

		private void SetSubMorphs(float f, float maxDelta)
		{
			for (int i = 0; i < subMorphs_.Count; ++i)
			{
				float smf = f * subMorphs_[i].multiplier_;
				subMorphs_[i].DoSet(smf, maxDelta);
			}
		}

		private void ResetSubMorphs()
		{
			for (int i = 0; i < subMorphs_.Count; ++i)
				subMorphs_[i].Reset();
		}
	}


	class VamMorphManager
	{
		private static VamMorphManager instance_ = new VamMorphManager();

		private Dictionary<string, MorphInfo> map_ =
			new Dictionary<string, MorphInfo>();

		private Logger log_ = new Logger(Logger.Sys, "morphManager");

		public static VamMorphManager Instance
		{
			get { return instance_; }
		}

		public Logger Log
		{
			get { return log_; }
		}

		public MorphInfo[] GetAll()
		{
			var map = new Dictionary<string, MorphInfo>();

			foreach (var kv in map_)
				kv.Value.AddToMap(map);

			var a = new MorphInfo[map.Count];
			map.Values.CopyTo(a, 0);
			return a;
		}

		public MorphInfo Get(VamAtom atom, string morphId, float eyesClosed, DAZMorphBank bank = null)
		{
			string key = atom.ID + "/" + morphId;

			MorphInfo mi;
			if (map_.TryGetValue(key, out mi))
				return mi;

			DAZMorph m;

			if (bank == null)
				m = U.FindMorph(atom.Atom, morphId);
			else
				m = bank.GetMorph(morphId);

			if (m == null)
			{
				Log.Error($"{atom.ID}: morph '{morphId}' not found");
			}
			else if (m.hasBoneModificationFormulas || m.hasBoneRotationFormulas)
			{
				Log.Warning($"{atom.ID}: morph '{morphId}' has bone morphs");
			}

			mi = new MorphInfo(atom, morphId, m, eyesClosed);
			map_.Add(key, mi);

			return mi;
		}

		public void OnPluginState(bool b)
		{
			foreach (var kv in map_)
				kv.Value.OnPluginState(b);
		}
	}


	class VamMorph : IMorph
	{
		private VamAtom atom_;
		private string name_;
		private float eyesClosed_;
		private MorphInfo morph_ = null;
		private bool inited_ = false;

		public VamMorph(VamAtom a, string name, float eyesClosed = Morph.NoEyesClosed)
		{
			atom_ = a;
			name_ = name;
			eyesClosed_ = eyesClosed;
		}

		public bool Valid
		{
			get
			{
				GetMorph();
				return (morph_ != null && morph_.DAZMorph != null);
			}
		}


		public MorphInfo MorphInfo
		{
			get { return morph_; }
		}

		public string Name
		{
			get { return name_; }
		}

		public float Value
		{
			get
			{
				GetMorph();
				return morph_?.Value ?? 0;
			}

			set
			{
				GetMorph();
				if (morph_ != null)
					morph_.Set(value);
			}
		}

		public float DefaultValue
		{
			get
			{
				GetMorph();
				return morph_?.DefaultValue ?? 0;
			}
		}

		public bool LimiterEnabled
		{
			set
			{
				GetMorph();
				if (morph_ != null)
					morph_.LimiterEnabled = value;
			}
		}

		public void Reset()
		{
			GetMorph();
			morph_?.Reset();
		}

		private void GetMorph()
		{
			if (inited_)
				return;

			morph_ = VamMorphManager.Instance.Get(atom_, name_, eyesClosed_);
			if (morph_ == null)
				atom_.Log.Error($"no morph '{name_}'");
			else if (morph_.DAZMorph == null)
				atom_.Log.Verbose($"morph '{name_}' has no dazmorph");

			inited_ = true;
		}

		public override string ToString()
		{
			return $"{morph_}";
		}

		public void Dump()
		{
			Cue.Instance.Log.Info($"morph {name_}:");

			GetMorph();
			var m = morph_.DAZMorph;

			if (m == null)
			{
				Cue.Instance.Log.Info($"  - not found");
				return;
			}


			if (m.deltas != null)
				Cue.Instance.Log.Info(" - has deltas");

			Cue.Instance.Log.Info(" - formulas:");

			foreach (var sm in m.formulas)
			{
				if (sm.targetType == DAZMorphFormulaTargetType.MorphValue)
				{
					var smm = VamMorphManager.Instance.Get(atom_, sm.target, eyesClosed_, m.morphBank);
					Cue.Instance.Log.Info($"    - {smm.ID} mult={sm.multiplier}");
				}
			}
		}
	}
}
