using SimpleJSON;

namespace Cue
{
	class Resources
	{
		private static AnimationResources animations_ = new AnimationResources();
		private static ClothingResources clothing_ = new ClothingResources();
		private static ObjectResources objects_ = new ObjectResources();
		private static PhysiologyResources physiologies_ = new PhysiologyResources();
		private static PersonalityResources personalities_ = new PersonalityResources();

		public static void LoadAll()
		{
			animations_.Load();
			clothing_.Load();
			objects_.Load();
			physiologies_.Load();
			personalities_.Load();
		}

		public static AnimationResources Animations
		{
			get { return animations_; }
		}

		public static ClothingResources Clothing
		{
			get { return clothing_; }
		}

		public static ObjectResources Objects
		{
			get { return objects_; }
		}

		public static PhysiologyResources Physiologies
		{
			get { return physiologies_; }
		}

		public static PersonalityResources Personalities
		{
			get { return personalities_; }
		}


		public static void LoadEnumValues(
			EnumValueManager v, JSONClass o, bool inherited)
		{
			for (int i = 0; i < v.Values.GetSlidingDurationCount(); ++i)
			{
				string key = v.Values.GetSlidingDurationName(i);

				if (inherited)
				{
					if (o.HasKey(key))
						v.SetSlidingDuration(i, SlidingDuration.FromJSON(o, key, false));
				}
				else
				{
					v.SetSlidingDuration(i, SlidingDuration.FromJSON(o, key, true));
				}
			}


			for (int i = 0; i < v.Values.GetBoolCount();  ++i)
			{
				string key = v.Values.GetBoolName(i);

				if (inherited)
				{
					bool b = false;
					if (J.OptBool(o, key, ref b))
						v.SetBool(i, b);
				}
				else
				{
					v.SetBool(i, J.ReqBool(o, key));
				}
			}


			for (int i = 0; i < v.Values.GetFloatCount(); ++i)
			{
				string key = v.Values.GetFloatName(i);

				if (inherited)
				{
					float f = 0;
					if (J.OptFloat(o, key, ref f))
						v.Set(i, f);
				}
				else
				{
					v.Set(i, J.ReqFloat(o, key));
				}
			}


			for (int i = 0; i < v.Values.GetStringCount(); ++i)
			{
				string key = v.Values.GetStringName(i);

				if (inherited)
				{
					string s = "";
					if (J.OptString(o, key, ref s))
						v.SetString(i, s);
				}
				else
				{
					v.SetString(i, J.ReqString(o, key));
				}
			}
		}
	}
}
