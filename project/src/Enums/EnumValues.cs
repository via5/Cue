namespace Cue
{
	interface IEnumValues
	{
		string[] GetAllNames();

		string GetDurationName(BasicEnumValues.DurationIndex i);
		int GetDurationCount();
		BasicEnumValues.DurationIndex[] GetDurationIndexes();

		string GetBoolName(BasicEnumValues.BoolIndex i);
		int GetBoolCount();
		BasicEnumValues.BoolIndex[] GetBoolIndexes();

		string GetFloatName(BasicEnumValues.FloatIndex i);
		int GetFloatCount();
		BasicEnumValues.FloatIndex[] GetFloatIndexes();

		string GetStringName(BasicEnumValues.StringIndex i);
		int GetStringCount();
		BasicEnumValues.StringIndex[] GetStringIndexes();
	}


	abstract class BasicEnumValues : IEnumValues
	{
		public struct DurationIndex
		{
			public int index;

			public DurationIndex(int index)
			{
				this.index = index;
			}
		}

		public struct BoolIndex
		{
			public int index;

			public BoolIndex(int index)
			{
				this.index = index;
			}
		}

		public struct FloatIndex
		{
			private static FloatIndex none_ = new FloatIndex(-100);
			public int index;

			public FloatIndex(int index)
			{
				this.index = index;
			}

			static public FloatIndex None
			{
				get { return none_; }
			}
		}

		public struct StringIndex
		{
			public int index;

			public StringIndex(int index)
			{
				this.index = index;
			}
		}


		public abstract string[] GetAllNames();

		public abstract string GetDurationName(DurationIndex i);
		public abstract int GetDurationCount();

		public BasicEnumValues.DurationIndex[] GetDurationIndexes()
		{
			var indexes = new BasicEnumValues.DurationIndex[GetDurationCount()];
			for (int i = 0; i < indexes.Length; ++i)
				indexes[i] = new BasicEnumValues.DurationIndex(i);
			return indexes;
		}

		public abstract string GetBoolName(BoolIndex i);
		public abstract int GetBoolCount();
		public BasicEnumValues.BoolIndex[] GetBoolIndexes()
		{
			var indexes = new BasicEnumValues.BoolIndex[GetBoolCount()];
			for (int i = 0; i < indexes.Length; ++i)
				indexes[i] = new BasicEnumValues.BoolIndex(i);
			return indexes;
		}

		public abstract string GetFloatName(FloatIndex i);
		public abstract int GetFloatCount();
		public BasicEnumValues.FloatIndex[] GetFloatIndexes()
		{
			var indexes = new BasicEnumValues.FloatIndex[GetFloatCount()];
			for (int i = 0; i < indexes.Length; ++i)
				indexes[i] = new BasicEnumValues.FloatIndex(i);
			return indexes;
		}

		public abstract string GetStringName(StringIndex i);
		public abstract int GetStringCount();
		public BasicEnumValues.StringIndex[] GetStringIndexes()
		{
			var indexes = new BasicEnumValues.StringIndex[GetStringCount()];
			for (int i = 0; i < indexes.Length; ++i)
				indexes[i] = new BasicEnumValues.StringIndex(i);
			return indexes;
		}
	}


	class EnumValueManager
	{
		private IEnumValues values_;
		private Duration[] durations_;
		private bool[] bools_;
		private float[] floats_;
		private string[] strings_;

		public EnumValueManager(IEnumValues e)
		{
			values_ = e;
			durations_ = new Duration[e.GetDurationCount()];
			bools_ = new bool[e.GetBoolCount()];
			floats_ = new float[e.GetFloatCount()];
			strings_ = new string[e.GetStringCount()];

			for (int i = 0; i < durations_.Length; i++)
				durations_[i] = new Duration();
		}

		public IEnumValues Values
		{
			get { return values_; }
		}

		public void CopyFrom(EnumValueManager v)
		{
			for (int i = 0; i < v.durations_.Length; ++i)
				durations_[i] = v.durations_[i].Clone();

			for (int i = 0; i < v.bools_.Length; ++i)
				bools_[i] = v.bools_[i];

			for (int i = 0; i < v.floats_.Length; ++i)
				floats_[i] = v.floats_[i];

			for (int i = 0; i < v.strings_.Length; ++i)
				strings_[i] = v.strings_[i];
		}

		public virtual Duration GetDuration(BasicEnumValues.DurationIndex i)
		{
			return durations_[i.index];
		}

		public virtual void SetDuration(BasicEnumValues.DurationIndex i, Duration d)
		{
			durations_[i.index] = d;
		}

		public bool GetBool(BasicEnumValues.BoolIndex i)
		{
			return bools_[i.index];
		}

		public void SetBool(BasicEnumValues.BoolIndex i, bool b)
		{
			bools_[i.index] = b;
		}

		public virtual float Get(BasicEnumValues.FloatIndex i)
		{
			return floats_[i.index];
		}

		public virtual void Set(BasicEnumValues.FloatIndex i, float f)
		{
			floats_[i.index] = f;
		}

		public virtual string GetString(BasicEnumValues.StringIndex i)
		{
			return strings_[i.index];
		}

		public virtual void SetString(BasicEnumValues.StringIndex i, string s)
		{
			strings_[i.index] = s;
		}
	}
}
