using System;
using System.Collections.Generic;

namespace Cue.Sys.Vam
{
	class MorphValueHijack
	{
		private const float ValuePingOffset = 0.012345678f;
		private const float ValuePongOffset = 0.087654321f;
		private const float ValueUninstallOffset = 0.045612378f;

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
			Cue.LogVerbose($"{m.ID}: installing (tk={CueMain.Instance.Token})");

			bool hadOld = DoUninstall(m);
			DoInstall(m);

			if (hadOld)
				Cue.LogVerbose($"hijacked {m.ID} (replaced old)");
			else
				Cue.LogVerbose($"hijacked {m.ID} (new)");
		}

		public static bool Uninstall(MorphInfo m)
		{
			bool found = DoUninstall(m);

			if (found)
				Cue.LogVerbose($"restored {m.ID}");

			return found;
		}

		private static void DoInstall(MorphInfo m)
		{
			var st = m.DAZMorph?.jsonFloat;
			if (st == null)
			{
				Cue.LogError($"{m.ID}: can't hijack, no jsonFloat");
				return;
			}

			var old = st.setCallbackFunction;
			st.setCallbackFunction = (v) => Callback(m, v, st, old);

			Cue.LogVerbose($"{m.ID}: installed (tk={CueMain.Instance.Token})");
		}

		private static bool DoUninstall(MorphInfo mi)
		{
			var m = mi?.DAZMorph;
			if (m?.jsonFloat == null)
			{
				Cue.LogError($"{m.uid}: can't restore, no jsonFloat");
				return false;
			}

			var oldValue = m.jsonFloat.val;

			Cue.LogVerbose($"{m.uid}: trying to uninstall, old value {oldValue} (tk={CueMain.Instance.Token})");

			Cue.LogVerbose($"{m.uid}: pinging (tk={CueMain.Instance.Token})");

			bool hadOld = false;
			m.jsonFloat.val = ValuePing(m);

			try
			{
				if (m.jsonFloat.val == ValuePong(m))
				{
					Cue.LogVerbose($"{m.uid}: pong received, uninstalling (tk={CueMain.Instance.Token})");
					hadOld = true;
					m.jsonFloat.val = ValueUninstall(m);
				}
				else
				{
					Cue.LogVerbose($"{m.uid}: no answer ({m.jsonFloat.val}), not installed (tk={CueMain.Instance.Token})");
				}
			}
			finally
			{
				Cue.LogVerbose($"{m.uid}: restoring old value {oldValue} (tk={CueMain.Instance.Token})");
				m.jsonFloat.val = oldValue;
			}

			return hadOld;
		}

		private static void Callback(MorphInfo mi, float f, JSONStorableFloat st, JSONStorableFloat.SetFloatCallback old)
		{
			var m = mi?.DAZMorph;

			if (f == ValuePing(m))
			{
				Cue.LogVerbose($"{m.uid}: ping received, sending pong (tk={CueMain.Instance.Token})");
				st.valNoCallback = ValuePong(m);
				return;
			}
			else if (f == ValueUninstall(m))
			{
				Cue.LogVerbose($"{m.uid}: uninstall received, uninstalling (tk={CueMain.Instance.Token})");
				st.setCallbackFunction = old;
				Cue.LogVerbose($"{m.uid}: uninstalled (tk={CueMain.Instance.Token})");
			}
			else
			{
				if (MorphInfo.SetFromCue || SuperController.singleton.freezeAnimation)
					old(f);
				else
					mi.Set(f);
			}
		}
	}


	class MorphInfo
	{
		private VamAtom atom_;
		private string id_;
		private DAZMorph m_;
		private List<MorphInfo> subMorphs_ = new List<MorphInfo>();
		private float multiplier_ = 1;

		private int lastSetFrame_ = -1;
		private float morphValueOnLastSet_ = -1;
		private float targetOnLastSet_ = -1;

		private static bool setFromCue_ = false;

		public MorphInfo(VamAtom atom, string morphId, DAZMorph m)
		{
			atom_ = atom;
			id_ = morphId;
			m_ = m;

			FindSubMorphs();

			if (m_ != null && subMorphs_.Count == 0)
				MorphValueHijack.Install(this);
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

		public void Set(float f)
		{
			if (m_ == null)
				return;

			if (subMorphs_.Count > 0)
			{
				SetSubMorphs(f);
				return;
			}


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

				if (f > morphValueOnLastSet_)
					SetMorphValue(Math.Min(morphValueOnLastSet_ + 0.02f, f));
				else
					SetMorphValue(Math.Max(morphValueOnLastSet_ - 0.02f, f));

				lastSetFrame_ = Cue.Instance.Frame;
			}
		}

		public void Reset()
		{
			if (m_ != null)
				SetMorphValue(m_.startValue);
		}

		public override string ToString()
		{
			string s = id_ + " ";

			if (m_ == null)
				s += "notfound";
			else
				s += $"v={m_.morphValue:0.00} sub={subMorphs_.Count != 0}";

			return s;
		}

		private void SetMorphValue(float f)
		{
			try
			{
				setFromCue_ = true;
				m_.morphValue = f;
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
						var smm = VamMorphManager.Instance.Get(atom_, sm.target, m_.morphBank);
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

		private void SetSubMorphs(float f)
		{
			for (int i = 0; i < subMorphs_.Count; ++i)
			{
				float smf = f * subMorphs_[i].multiplier_;
				subMorphs_[i].Set(smf);
			}
		}
	}


	class VamMorphManager
	{
		private static VamMorphManager instance_ = new VamMorphManager();

		private Dictionary<string, MorphInfo> map_ =
			new Dictionary<string, MorphInfo>();

		public static VamMorphManager Instance
		{
			get { return instance_; }
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

		public MorphInfo Get(VamAtom atom, string morphId, DAZMorphBank bank = null)
		{
			string key = atom.ID + "/" + morphId;

			MorphInfo mi;
			if (map_.TryGetValue(key, out mi))
				return mi;

			DAZMorph m;

			if (bank == null)
				m = Cue.Instance.VamSys.FindMorph(atom.Atom, morphId);
			else
				m = bank.GetMorph(morphId);

			if (m == null)
				Cue.LogError($"{atom.ID}: morph '{morphId}' not found");

			mi = new MorphInfo(atom, morphId, m);
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
		private MorphInfo morph_ = null;
		private bool inited_ = false;

		public VamMorph(VamAtom a, string name)
		{
			atom_ = a;
			name_ = name;
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

		public void Reset()
		{
			GetMorph();
			morph_?.Reset();
		}

		private void GetMorph()
		{
			if (inited_)
				return;

			morph_ = VamMorphManager.Instance.Get(atom_, name_);
			if (morph_ == null)
				atom_.Log.Error($"no morph '{name_}'");

			inited_ = true;
		}

		public override string ToString()
		{
			return $"{morph_}";
		}
	}
}
