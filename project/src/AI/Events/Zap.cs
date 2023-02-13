using System;

namespace Cue
{
	class ZappedEvent : BasicEvent<EmptyEventData>
	{
		class ZapZone
		{
			class Source
			{
				public int sourceIndex;
				public float tentativeElapsed = 0;
				public float inactiveElapsed = 10000;
				public bool active = false;

				public Source(int sourceIndex)
				{
					this.sourceIndex = sourceIndex;
				}

				public bool CheckActive(float s, Person self, ZoneType zone)
				{
					if (active)
					{
						if (!self.Body.Zone(zone).Sources[sourceIndex].Active)
						{
							active = false;
							inactiveElapsed = 0;
						}
					}
					else
					{
						if (self.Body.Zone(zone).Sources[sourceIndex].Active)
						{
							if (inactiveElapsed >= self.Personality.Get(PS.ZappedCooldown))
							{
								tentativeElapsed += s;
								if (tentativeElapsed >= self.Personality.Get(PS.ZappedTentativeTime))
								{
									tentativeElapsed = 0;
									active = true;
									return true;
								}
							}
							else
							{
								inactiveElapsed = 0;
							}
						}
						else
						{
							inactiveElapsed = Math.Min(
								inactiveElapsed + s,
								self.Personality.Get(PS.ZappedCooldown));
						}
					}

					return false;
				}

				public string DebugLine(Person self, ZoneType zone)
				{
					var ps = self.Personality;

					string from;

					if (sourceIndex >= 0 && sourceIndex < Cue.Instance.ActivePersons.Length)
						from = Cue.Instance.ActivePersons[sourceIndex].ID;
					else if (sourceIndex == self.Body.Zone(SS.Penetration).ToySourceIndex)
						from = "toy";
					else if (sourceIndex == self.Body.Zone(SS.Penetration).ExternalSourceIndex)
						from = "external";
					else
						from = "?";

					return
						$"src={from} " +
						$"tent={tentativeElapsed:0.00}/{ps.Get(PS.ZappedTentativeTime)} " +
						$"inact={inactiveElapsed:0.00}/{ps.Get(PS.ZappedCooldown)} " +
						$"A={active} " +
						$"T={self.Body.Zone(zone).Sources[sourceIndex].Active}";
				}
			}

			private Person person_;
			private string name_;
			private ZoneType zone_;
			private Source[] sources_;

			public ZapZone(Person p, string n, ZoneType zone)
			{
				person_ = p;
				name_ = n;
				zone_ = zone;

				sources_ = new Source[Cue.Instance.ActivePersons.Length + 2];
				for (int i = 0; i < sources_.Length; ++i)
					sources_[i] = new Source(i);

				sources_[ToySourceIndex] = new Source(ToySourceIndex);
				sources_[ExternalSourceIndex] = new Source(ExternalSourceIndex);
			}

			private int ToySourceIndex
			{
				get
				{
					return person_.Body.Zone(SS.Penetration).ToySourceIndex;
				}
			}

			private int ExternalSourceIndex
			{
				get
				{
					return person_.Body.Zone(SS.Penetration).ToySourceIndex;
				}
			}

			public string Name
			{
				get { return name_; }
			}

			public void Debug(DebugLines debug)
			{
				for (int i = 0; i < sources_.Length; ++i)
				{
					if (i == person_.PersonIndex)
						continue;

					debug.Add(ZoneType.ToString(zone_), sources_[i].DebugLine(person_, zone_));
				}
			}

			public void Update(float s)
			{
				for (int i=0; i<sources_.Length;++i)
				{
					if (i == person_.PersonIndex)
						continue;

					Person source = null;

					if (i >= 0 && i < Cue.Instance.ActivePersons.Length)
						source = Cue.Instance.ActivePersons[i];

					if (sources_[i].CheckActive(s, person_, zone_))
					{
						person_.Body.Zapped(source, zone_);
						return;
					}
				}
			}
		}


		private ZapZone[] zones_ = null;


		public ZappedEvent()
			: base("Zap")
		{
		}

		public override bool Active
		{
			get { return false; }
			set { }
		}

		public override bool CanToggle { get { return false; } }
		public override bool CanDisable { get { return false; } }

		protected override void DoInit()
		{
			base.DoInit();

			zones_ = new ZapZone[]
			{
				new ZapZone(person_, "genitals", SS.Genitals),
				new ZapZone(person_, "breasts", SS.Breasts),
				new ZapZone(person_, "penetration", SS.Penetration),
				new ZapZone(person_, "mouth", SS.Mouth)
			};
		}

		protected override void DoDebug(DebugLines debug)
		{
			for (int i = 0; i < zones_.Length; ++i)
				zones_[i].Debug(debug);
		}

		protected override void DoUpdate(float s)
		{
			if (!Enabled)
				return;

			for (int i = 0; i < zones_.Length; ++i)
				zones_[i].Update(s);
		}
	}
}
