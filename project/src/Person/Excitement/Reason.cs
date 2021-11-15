namespace Cue
{
	public class ExcitementReason
	{
		public const int Mouth = 0;
		public const int Breasts = 1;
		public const int Genitals = 2;
		public const int Penetration = 3;
		public const int OtherSex = 4;
		public const int Count = 5;

		private int type_;
		private string name_;
		private float value_ = 0;
		private float rate_ = 0;
		private float specificSensitivityModifier_ = 0;
		private float globalSensitivityRate_ = 0;
		private float sensitivityMax_ = 0;

		private static string[] names_ = new string[]
		{
			"mouth", "breasts", "genitals", "penetration", "others"
		};

		public ExcitementReason(int type)
		{
			type_ = type;
			name_ = ToString(type);
		}

		private static string ToString(int type)
		{
			return names_[type];
		}

		public string Name
		{
			get { return name_; }
		}

		public bool Physical
		{
			get
			{
				// todo
				return (type_ != OtherSex);
			}
		}

		public float Value
		{
			get { return value_; }
			set { value_ = value; }
		}

		public float Rate
		{
			get { return rate_; }
			set { rate_ = value; }
		}

		public float SpecificSensitivityModifier
		{
			get { return specificSensitivityModifier_; }
			set { specificSensitivityModifier_ = value; }
		}

		public float GlobalSensitivityRate
		{
			get { return globalSensitivityRate_; }
			set { globalSensitivityRate_ = value; }
		}

		public float SensitivityMax
		{
			get { return sensitivityMax_; }
			set { sensitivityMax_ = value; }
		}

		public override string ToString()
		{
			if (rate_ == 0)
				return "0";

			return
				$"{value_:0.00}=" +
				$"{rate_:0.000000}";
		}
	}
}
