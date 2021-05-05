namespace Cue
{
	class PersonState
	{
		public const int None = 0;
		public const int Standing = 1;
		public const int Walking = 2;
		public const int Sitting = 3;
		public const int Kneeling = 4;
		public const int SittingStraddling = 5;

		private Person self_;
		private int current_ = Standing;
		private int next_ = None;

		public PersonState(Person self)
		{
			self_ = self;
		}

		public int Current
		{
			get { return current_; }
		}

		public int Next
		{
			get { return next_; }
		}

		public bool IsUpright
		{
			get
			{
				return
					(current_ == Standing || current_ == Walking) &&
					 (next_ == None);
			}
		}

		public bool Is(int type)
		{
			return current_ == type || next_ == type;
		}

		public bool IsCurrently(int type)
		{
			return current_ == type;
		}

		public bool Transitioning
		{
			get { return next_ != None; }
		}

		public void Set(int state)
		{
			if (current_ == state)
				return;

			string before = ToString();

			current_ = state;
			next_ = None;

			string after = ToString();

			Cue.LogInfo(
				self_.ID + ": " +
				"state changed from " + before + " to " + after);
		}

		public bool StartTransition(int next)
		{
			if (next_ == next)
				return false;

			next_ = next;
			Cue.LogInfo(self_.ID + ": new transition, " + ToString());
			return true;
		}

		public void CancelTransition()
		{
			if (next_ != None)
			{
				Cue.LogInfo($"{self_.ID}: cancelling transition {StateToString(next_)}");
				next_ = None;
			}
		}

		public void FinishTransition()
		{
			if (next_ == None)
				return;

			string before = StateToString(current_);

			current_ = next_;
			next_ = None;

			string after = StateToString(current_);

			Cue.LogInfo(
				self_.ID + ": " +
				"transition finished from " + before + " to " + after);
		}

		public override string ToString()
		{
			string s = StateToString(current_);

			if (next_ != None)
				s += "->" + StateToString(next_);

			return s;
		}

		private static string[] GetNames()
		{
			return new string[]
			{
				"none", "standing", "walking", "sitting", "kneeling",
				"sittingstraddling"
			};
		}

		public static string StateToString(int state)
		{
			var names = GetNames();

			if (state < 0 || state >= names.Length)
				return "?" + state.ToString();

			return names[state];
		}

		public static int StateFromString(string os)
		{
			var names = GetNames();
			var s = os.ToLower();

			for (int i = 0; i < names.Length; ++i)
			{
				if (names[i] == s)
					return i;
			}

			Cue.LogError($"unknown person state '{os}'");
			return None;
		}
	}
}
