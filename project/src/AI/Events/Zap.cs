using System;

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

				public bool CheckActive(float s, Person self, Person other, ZoneType zone)
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
							inactiveElapsed = Math.Min(
								inactiveElapsed + s,
								self.Personality.Get(PS.ZappedCooldown));
						}
					}

					return false;
				}

				public string DebugLine(Person self, Person other, ZoneType zone)
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
			private ZoneType zone_;
			private Source[] sources_;

			public ZapZone(Person p, string n, ZoneType zone)
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

					debug.Add(ZoneType.ToString(zone_), sources_[i].DebugLine(person_, p, zone_));
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
						person_.Body.Zapped(p, zone_);
						break;
					}
				}
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
