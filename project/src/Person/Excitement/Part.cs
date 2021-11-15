namespace Cue
{
	class ExcitementBodyPart
	{
		private float value_ = 0;
		private float specificModifier_ = 0;
		private float fromPenisValue_ = 0;

		public float Value
		{
			get { return value_; }
			set { value_ = value; }
		}

		public float SpecificModifier
		{
			get { return specificModifier_; }
			set { specificModifier_ = value; }
		}

		public float FromPenisValue
		{
			get { return fromPenisValue_; }
			set { fromPenisValue_ = value; }
		}

		public void Clear()
		{
			specificModifier_ = 0;
			fromPenisValue_ = 0;
		}
	}
}
