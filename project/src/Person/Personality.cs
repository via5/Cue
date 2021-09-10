using System.Collections.Generic;

namespace Cue
{
	class Voice
	{
		public class Dataset
		{
			private string name_;
			private float pitch_;

			public Dataset(string name, float pitch)
			{
				name_ = name;
				pitch_ = pitch;
			}

			public Dataset(Dataset d)
			{
				name_ = d.name_;
				pitch_ = d.pitch_;
			}

			public string Name
			{
				get { return name_; }
			}

			public float GetPitch(Person p)
			{
				if (pitch_ < 0)
				{
					float neutral = p.Physiology.Get(PE.NeutralVoicePitch);
					float scale = p.Atom.Body.Scale;
					SetPitch(neutral + (1 - scale));
				}

				return pitch_;
			}

			public void SetPitch(float f)
			{
				pitch_ = U.Clamp(f, 0, 1);
			}
		}

		public class DatasetForIntensity
		{
			public Dataset dataset;
			public float intensityMin;
			public float intensityMax;

			public DatasetForIntensity(Dataset ds, float intensityMin, float intensityMax)
			{
				this.dataset = ds;
				this.intensityMin = intensityMin;
				this.intensityMax = intensityMax;
			}

			public DatasetForIntensity(DatasetForIntensity d)
			{
				dataset = new Dataset(d.dataset);
				intensityMin = d.intensityMin;
				intensityMax = d.intensityMax;
			}
		}

		private DatasetForIntensity[] datasets_ = new DatasetForIntensity[0];
		private Dataset orgasm_ = new Dataset("", -1);
		private Dataset dummy_ = new Dataset("", -1);
		private bool warned_ = false;

		public void Set(List<DatasetForIntensity> dss, Dataset orgasm)
		{
			if (dss != null)
				datasets_ = dss.ToArray();

			if (orgasm != null)
				orgasm_ = orgasm;
		}

		public void CopyFrom(Voice v)
		{
			datasets_ = new DatasetForIntensity[v.datasets_.Length];
			for (int i = 0; i < datasets_.Length; ++i)
				datasets_[i] = new DatasetForIntensity(v.datasets_[i]);

			orgasm_ = new Dataset(v.orgasm_);
		}

		public DatasetForIntensity[] Datasets
		{
			get { return datasets_; }
		}

		public Dataset OrgasmDataset
		{
			get { return orgasm_; }
		}

		public float GetNormalPitch(Person p)
		{
			return GetDatasetForIntensity(0).GetPitch(p);
		}

		public void SetPitchForAll(float f)
		{
			foreach (var d in datasets_)
				d.dataset.SetPitch(f);

			orgasm_.SetPitch(f);
		}

		public Dataset GetDatasetForIntensity(float e)
		{
			for (int i = 0; i < datasets_.Length; ++i)
			{
				if (e >= datasets_[i].intensityMin &&
					e <= datasets_[i].intensityMax)
				{
					return datasets_[i].dataset;
				}
			}

			if (!warned_)
			{
				Cue.LogError(
					$"personality missing voice for excitement " +
					$"{e}");

				warned_ = true;
			}

			if (datasets_.Length == 0)
				return dummy_;
			else
				return datasets_[0].dataset;
		}
	}


	class Personality
	{
		public const int IdleState = 0;
		public const int CloseState = 1;
		public const int StateCount = 2;


		public class ExpressionMaximum
		{
			public int type = -1;
			public float maximum = 0;

			public ExpressionMaximum(int type)
			{
				this.type = type;
			}

			public ExpressionMaximum(ExpressionMaximum o)
			{
				type = o.type;
				maximum = o.maximum;
			}
		}


		public class State : EnumValueManager
		{
			public readonly string name;

			private ExpressionMaximum[] maximums_;
			private Voice voice_ = new Voice();

			public State(int stateIndex)
				: base(new PSE())
			{
				name = StateToString(stateIndex);

				maximums_ = new ExpressionMaximum[Expressions.Count];
				for (int i = 0; i < maximums_.Length; i++)
					maximums_[i] = new ExpressionMaximum(i);
			}

			public ExpressionMaximum[] Maximums
			{
				get{ return maximums_; }
			}

			public void SetMaximum(int type, float max)
			{
				maximums_[type].maximum = max;
			}

			public Voice Voice
			{
				get { return voice_; }
			}

			public void CopyFrom(State s)
			{
				base.CopyFrom(s);

				for (int i = 0; i < s.maximums_.Length; ++i)
					maximums_[i] = new ExpressionMaximum(s.maximums_[i]);

				voice_.CopyFrom(s.voice_);
			}
		}


		private readonly string name_;
		private Person person_;

		private bool wasClose_ = false;
		private bool inited_ = false;
		private bool forcedClose_ = false;
		private bool isCloseForced_ = false;

		private State[] states_ = new State[StateCount];

		public Personality(string name, Person p = null)
		{
			name_ = name;
			person_ = p;

			for (int i = 0; i < StateCount; ++i)
				states_[i] = new State(i);
		}

		public Personality Clone(string newName, Person p)
		{
			var ps = new Personality(newName ?? name_, p);

			for (int si = 0; si < states_.Length; ++si)
				ps.states_[si].CopyFrom(states_[si]);

			return ps;
		}

		public string Name
		{
			get { return name_; }
		}

		public State[] States
		{
			get { return states_; }
		}

		public State GetState(int i)
		{
			return states_[i];
		}

		public Voice Voice
		{
			get { return CurrentState.Voice; }
		}

		private State CurrentState
		{
			get
			{
				if (wasClose_)
					return states_[CloseState];
				else
					return states_[IdleState];
			}
		}

		public Pair<float, float> LookAtRandomInterval
		{
			get
			{
				return new Pair<float, float>(
					Get(PSE.GazeRandomIntervalMinimum),
					Get(PSE.GazeRandomIntervalMaximum));
			}
		}

		public float GazeDuration
		{
			get { return GetSlidingDuration(PSE.GazeDuration).Current; }
		}

		public void Set(State[] ss)
		{
			states_ = ss;
		}

		public SlidingDuration GetSlidingDuration(int i)
		{
			return CurrentState.GetSlidingDuration(i);
		}

		public bool GetBool(int i)
		{
			return CurrentState.GetBool(i);
		}

		public float Get(int i)
		{
			return CurrentState.Get(i);
		}

		public string GetString(int i)
		{
			return CurrentState.GetString(i);
		}

		public virtual void Update(float s)
		{
			if (!inited_)
			{
				Init();
				inited_ = true;
			}

			bool close = false;

			if (Cue.Instance.Player != null)
			{
				close =
					person_.Body.InsidePersonalSpace(Cue.Instance.Player) ||
					person_.Kisser.Target == Cue.Instance.Player;
			}

			if (close != wasClose_)
			{
				person_.Log.Info("Personality: " + (close ? "now close" : "now far"));
				DoSetClose(close);
				wasClose_ = close;
			}


			// todo
			Cue.Assert(PSE.SlidingDurationCount == 1);
			GetSlidingDuration(PSE.GazeDuration).WindowMagnitude = person_.Mood.GazeEnergy;
			GetSlidingDuration(PSE.GazeDuration).Update(s);
		}

		public override string ToString()
		{
			return $"{Name}";
		}

		public static string StateToString(int s)
		{
			if (s < 0 || s >= StateCount)
				return $"?{s}";

			return StateNames[s];
		}

		public static string[] StateNames
		{
			get { return new string[] { "idleState", "closeState" }; }
		}


		private void Init()
		{
			SetClose(false);
		}

		private void SetClose(bool b)
		{
			State s;

			if (b)
				s = states_[CloseState];
			else
				s = states_[IdleState];

			foreach (var m in s.Maximums)
				person_.Expression.SetMaximum(m.type, m.maximum);

			// todo
			person_.Expression.SetIntensity(Expressions.Common, 1);
			person_.Expression.SetIntensity(Expressions.Happy, 1);
			person_.Expression.SetIntensity(Expressions.Mischievous, 1);
			person_.Expression.SetIntensity(Expressions.Angry, 1);
		}

		public void ForceSetClose(bool enabled, bool close)
		{
			isCloseForced_ = enabled;
			forcedClose_ = close;
			DoSetClose(close);
		}

		private void DoSetClose(bool b)
		{
			if (isCloseForced_)
				SetClose(forcedClose_);
			else
				SetClose(b);
		}
	}
}
