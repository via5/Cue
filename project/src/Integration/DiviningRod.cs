namespace Cue.ToumeiHitsuji
{
	class DiviningRod : IHoming
	{
		private const string PluginName = "ToumeiHitsuji.DiviningRod";
		private const float WarningCheckInterval = 1;
		private const float Delay = 2;

		struct Parameters
		{
			public Sys.Vam.BoolParameter female;
			public Sys.Vam.BoolParameter male;
			public Sys.Vam.FloatParameter distance;
		};

		class Part
		{
			public bool active, needsDelay, inDelay;
			public float elapsed;
			public Sys.Vam.BoolParameter param;

			public Part(Sys.Vam.BoolParameter p, bool needsDelayy)
			{
				needsDelay = needsDelayy;
				active = false;
				inDelay = false;
				elapsed = 0;
				param = p;
			}
		};

		private Person person_;
		private Parameters p_;
		private string warning_ = "";
		private float warningCheck_ = 0;
		private Part genitals_;
		private Part mouth_;
		private Part leftHand_;
		private Part rightHand_;


		public DiviningRod(Person p)
		{
			person_ = p;
		}

		public void Init()
		{
			// hands need a delay or the cwbj plugin has trouble getting to the
			// center, it can stay on the side of the body

			genitals_ = new Part(GBP("Vagina Enabled"), false);
			mouth_ = new Part(GBP("Mouth Enabled"), false);
			leftHand_ = new Part(GBP("Left Hand Enabled"), true);
			rightHand_ = new Part(GBP("Right Hand Enabled"), true);

			p_.female = GBP("Female Enabled");
			p_.male = GBP("Male Enabled");
			p_.distance = GFP("Min Target Distance [cm]");

			p_.female.Value = true;
			p_.male.Value = true;
			p_.distance.Value = 15;

			Mouth = false;
			LeftHand = false;
			RightHand = false;

			// genitals are always active to allow positioning
			Set(genitals_, true);
		}

		public void Update(float s)
		{
			warningCheck_ += s;
			if (warningCheck_ >= WarningCheckInterval)
			{
				warningCheck_ = 0;
				CheckWarning();
			}

			Check(mouth_, s);
			Check(genitals_, s);
			Check(leftHand_, s);
			Check(rightHand_, s);
		}

		private void Set(Part p, bool b)
		{
			if (b)
			{
				p.active = true;

				if (p.needsDelay)
				{
					p.inDelay = true;
					p.elapsed = 0;
					p.param.Value = false;
				}
				else
				{
					p.inDelay = false;
					p.param.Value = true;
				}
			}
			else
			{
				p.active = false;
				p.param.Value = false;
			}
		}

		private void Check(Part p, float s)
		{
			if (p.active && p.inDelay)
			{
				p.elapsed += s;
				if (p.elapsed >= Delay)
				{
					p.param.Value = true;
					p.inDelay = false;
				}
			}
		}

		public bool Genitals
		{
			get { return true; }
			set { }
		}

		public bool Mouth
		{
			get { return mouth_.active; }
			set { Set(mouth_, value); }
		}

		public bool LeftHand
		{
			get
			{
				return leftHand_.active;
			}

			set
			{
				if (Cue.Instance.Options.DiviningRodLeftHand)
					Set(leftHand_, value);
			}
		}

		public bool RightHand
		{
			get
			{
				return rightHand_.active;
			}

			set
			{
				if (Cue.Instance.Options.DiviningRodRightHand)
					Set(rightHand_, value);
			}
		}

		public string Warning
		{
			get
			{
				return warning_;
			}
		}

		private bool CanBeUsed()
		{
			return person_.Body.HasPenis && !person_.Body.Strapon;
		}

		public override string ToString()
		{
			string s = "DiviningRod";

			if (CanBeUsed())
				s += $": active, g={genitals_.param} m={mouth_.param} lh={leftHand_.param} rh={rightHand_.param}";
			else
				s += ": inactive";

			return s;
		}


		private void CheckWarning()
		{
			if (!CanBeUsed())
			{
				warning_ = "";
				return;
			}

			if (mouth_.param.Check())
				warning_ = "";
			else
				warning_ = "DiviningRod missing";
		}

		private Sys.Vam.BoolParameter GBP(string name)
		{
			return new Sys.Vam.BoolParameter(person_, PluginName, name);
		}

		private Sys.Vam.FloatParameter GFP(string name)
		{
			return new Sys.Vam.FloatParameter(person_, PluginName, name);
		}
	}
}
