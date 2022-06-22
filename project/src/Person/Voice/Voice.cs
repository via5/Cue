namespace Cue
{
	public class Voice
	{
		private Person person_;
		private IVoice provider_;

		public Voice(Person p, IVoice provider)
		{
			person_ = p;
			provider_ = provider;
		}

		public IVoice Provider
		{
			get
			{
				return provider_;
			}

			set
			{
				provider_?.Destroy();

				provider_ = value;
				provider_.Init(person_);
			}
		}

		public void Init()
		{
			provider_.Init(person_);
		}

		public void Update(float s)
		{
			provider_.Update(s);
		}


		public bool MouthEnabled
		{
			get { return provider_.MouthEnabled; }
			set { provider_.MouthEnabled = value; }
		}

		public float Intensity
		{
			get { return provider_.Intensity; }
			set { provider_.Intensity = value; }
		}

		public string Warning
		{
			get { return provider_.Warning; }
		}

		public void StartOrgasm()
		{
			provider_.StartOrgasm();
		}

		public void StopOrgasm()
		{
			provider_.StopOrgasm();
		}

		public string[] Debug()
		{
			return provider_.Debug();
		}
	}
}
