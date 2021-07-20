namespace Cue
{
	interface IEnumValues
	{
		string GetSlidingDurationName(int i);
		int GetSlidingDurationCount();

		string GetBoolName(int i);
		int GetBoolCount();

		string GetFloatName(int i);
		int GetFloatCount();

		string GetStringName(int i);
		int GetStringCount();
	}


	class EnumValueManager
	{
		private IEnumValues values_;
		private SlidingDuration[] slidingDurations_;
		private bool[] bools_;
		private float[] floats_;
		private string[] strings_;

		public EnumValueManager(IEnumValues e)
		{
			values_ = e;
			slidingDurations_ = new SlidingDuration[e.GetSlidingDurationCount()];
			bools_ = new bool[e.GetBoolCount()];
			floats_ = new float[e.GetFloatCount()];
			strings_ = new string[e.GetStringCount()];

			for (int i = 0; i < slidingDurations_.Length; i++)
				slidingDurations_[i] = new SlidingDuration();
		}

		public IEnumValues Values
		{
			get { return values_; }
		}

		public void CopyFrom(EnumValueManager v)
		{
			for (int i = 0; i < v.slidingDurations_.Length; ++i)
				slidingDurations_[i] = new SlidingDuration(v.slidingDurations_[i]);

			for (int i = 0; i < v.bools_.Length; ++i)
				bools_[i] = v.bools_[i];

			for (int i = 0; i < v.floats_.Length; ++i)
				floats_[i] = v.floats_[i];

			for (int i = 0; i < v.strings_.Length; ++i)
				strings_[i] = v.strings_[i];
		}

		//public void Set(SlidingDuration[] sds, bool[] bs, float[] fs, string[] ss)
		//{
		//	slidingDurations_ = sds;
		//	bools_ = bs;
		//	floats_ = fs;
		//	strings_ = ss;
		//}
		//
		public virtual SlidingDuration GetSlidingDuration(int i)
		{
			return slidingDurations_[i];
		}

		public virtual void SetSlidingDuration(int i, SlidingDuration d)
		{
			slidingDurations_[i] = d;
		}

		public bool GetBool(int i)
		{
			return bools_[i];
		}

		public void SetBool(int i, bool b)
		{
			bools_[i] = b;
		}

		public virtual float Get(int i)
		{
			return floats_[i];
		}

		public virtual void Set(int i, float f)
		{
			floats_[i] = f;
		}

		public virtual string GetString(int i)
		{
			return strings_[i];
		}

		public virtual void SetString(int i, string s)
		{
			strings_[i] = s;
		}
	}
}
