namespace Cue
{
	class ZappedEvent : BasicEvent
	{
		class ZapZone
		{
			class Source
			{
				public int personIndex;
				public float tentativeElapsed = 0;
				public float inactiveElapsed = 10000;
				public bool active = false;

				public Source(int i)
				{
					personIndex = i;
				}

				public bool CheckActive(float s, Person self, Person other, ZoneTypes zone)
				{
					if (active)
					{
						if (!self.Body.Zone(zone).Sources[other.PersonIndex].Active)
						{
							active = false;
							inactiveElapsed = 0;
						}
					}
					else
					{
						if (self.Body.Zone(zone).Sources[other.PersonIndex].Active)
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
							inactiveElapsed += s;
						}
					}

					return false;
				}

				public string DebugLine(Person self, Person other, ZoneTypes zone)
				{
					var ps = self.Personality;

					return
						$"from={Cue.Instance.ActivePersons[personIndex].ID} " +
						$"tent={tentativeElapsed:0.00}/{ps.Get(PS.ZappedTentativeTime)} " +
						$"inact={inactiveElapsed:0.00}/{ps.Get(PS.ZappedCooldown)} " +
						$"active={active} " +
						$"trig={self.Body.Zone(zone).Sources[other.PersonIndex].Active}";
				}
			}

			private Person person_;
			private string name_;
			private ZoneTypes zone_;
			private Source[] sources_;

			public ZapZone(Person p, string n, ZoneTypes zone)
			{
				person_ = p;
				name_ = n;
				zone_ = zone;

				sources_ = new Source[Cue.Instance.ActivePersons.Length];
				for (int i = 0; i < sources_.Length; ++i)
					sources_[i] = new Source(i);
			}

			public string Name
			{
				get { return name_; }
			}

			public void Debug(DebugLines debug)
			{
				for (int i = 0; i < sources_.Length; ++i)
				{
					var p = Cue.Instance.ActivePersons[i];
					if (p == person_)
						continue;

					debug.Add(ZoneTypes.ToString(zone_), sources_[i].DebugLine(person_, p, zone_));
				}
			}

			public void Update(float s)
			{
				var ps = person_.Personality;

				for (int i = 0; i < Cue.Instance.ActivePersons.Length; ++i)
				{
					var p = Cue.Instance.ActivePersons[i];
					if (p == person_)
						continue;

					if (sources_[i].CheckActive(s, person_, p, zone_))
					{
						person_.Body.Zapped(
							p, zone_, GetExcitementValue(person_, p),
							ps.Get(PS.ZappedTime));

						break;
					}
				}
			}

			private float GetExcitementValue(Person self, Person other)
			{
				var ps = self.Personality;

				if (other.IsPlayer)
				{
					if (zone_ ==SS.Genitals)
						return ps.Get(PS.ZappedByPlayerGenitalsExcitement);
					else if (zone_ == SS.Breasts)
						return ps.Get(PS.ZappedByPlayerBreastsExcitement);
					else if (zone_ == SS.Penetration)
						return ps.Get(PS.ZappedByPlayerPenetrationExcitement);
					else if (zone_ == SS.Mouth)
						return ps.Get(PS.ZappedByPlayerMouthExcitement);
				}
				else
				{
					if (zone_ == SS.Genitals)
						return ps.Get(PS.ZappedByOtherGenitalsExcitement);
					else if (zone_ == SS.Breasts)
						return ps.Get(PS.ZappedByOtherBreastsExcitement);
					else if (zone_ == SS.Penetration)
						return ps.Get(PS.ZappedByOtherPenetrationExcitement);
					else if (zone_ == SS.Mouth)
						return ps.Get(PS.ZappedByOtherMouthExcitement);
				}

				self.Log.Error($"zap source: bad zone {zone_}");
				return 0;
			}
		}


		private ZapZone[] zones_ = null;


		public ZappedEvent()
			: base("zapped")
		{
		}

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

		public override void Debug(DebugLines debug)
		{
			for (int i = 0; i < zones_.Length; ++i)
				zones_[i].Debug(debug);
		}

		public override void Update(float s)
		{
			for (int i = 0; i < zones_.Length; ++i)
				zones_[i].Update(s);
		}
	}
}
