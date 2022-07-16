namespace Cue
{
	class GazeZapped : BasicGazeEvent
	{
		private bool active_ = false;
		private float gazeDuration_ = -1;

		public GazeZapped(Person p)
			: base(p, I.GazeZapped)
		{
		}

		protected override int DoCheck(int flags)
		{
			var ps = person_.Personality;

			if (active_)
			{
				Person other = null;
				float otherIntensity = 0;
				bool self = false;

				for (int i = 0; i < Cue.Instance.ActivePersons.Length; ++i)
				{
					var p = Cue.Instance.ActivePersons[i];
					if (p.Body.Zap.Intensity <= 0)
						continue;

					if (p == person_)
					{
						// self was zapped
						self = true;
						break;
					}
					else
					{
						if (p.Body.Zap.Source != person_)
						{
							if (p.Body.Zap.Intensity > otherIntensity)
							{
								// other was zapped
								other = p;
								otherIntensity = p.Body.Zap.Intensity;
							}
						}
					}
				}

				if (self)
				{
					DoSelfZapped();
					active_ = true;
				}
				else if (other != null)
				{
					DoOtherZapped(other);
					active_ = true;
				}
				else
				{
					active_ = false;
					gazeDuration_ = -1;
				}

				if (active_)
					person_.Gaze.Gazer.Duration = gazeDuration_;
			}

			return Continue;
		}

		protected override bool DoHasEmergency(float s)
		{
			active_ = false;

			// find anybody zapped, including self
			for (int i = 0; i < Cue.Instance.ActivePersons.Length; ++i)
			{
				var p = Cue.Instance.ActivePersons[i];

				if (p.Body.Zap.Intensity > 0 && p.Body.Zap.Source != person_)
				{
					active_ = true;
					gazeDuration_ = -1;
					break;
				}
			}

			return active_;
		}

		private void DoSelfZapped()
		{
			var ps = person_.Personality;
			var z = person_.Body.Zap;

			// look at person zapping
			float w = GetEyesWeight(z.Source.IsPlayer, z.Zone) * z.Intensity;
			if (w >= 0)
			{
				targets_.SetWeight(
					z.Source, BP.Eyes, w, $"self zapped by {z.Source}");
			}

			// look at body part being zapped
			var targetPart = person_.Body.Zone(z.Zone).MainBodyPart;
			if (targetPart != null)
			{
				w = GetTargetWeight(z.Source.IsPlayer, z.Zone) * z.Intensity;
				if (w >= 0)
				{
					targets_.SetWeight(
						person_, targetPart.Type, w,
						$"self zapped by {z.Source}");
				}
			}

			// look up
			if (z.Source.IsPlayer)
			{
				targets_.SetAboveWeight(
					ps.Get(PS.ZappedByPlayerLookUpWeight) * z.Intensity,
					$"self zapped by {z.Source}");
			}
			else
			{
				targets_.SetAboveWeight(
					ps.Get(PS.ZappedByOtherLookUpWeight) * z.Intensity,
					$"self zapped by {z.Source}");
			}

			if (z.Source.IsPlayer)
				SetGazeDuration(PS.ZappedByPlayerGazeDuration, z.Intensity);
			else
				SetGazeDuration(PS.ZappedByOtherGazeDuration, z.Intensity);
		}

		private void DoOtherZapped(Person other)
		{
			var ps = person_.Personality;
			var z = other.Body.Zap;

			// look at person being zapped
			targets_.SetWeight(
				other, BP.Eyes, ps.Get(PS.OtherZappedEyesWeight) * z.Intensity,
				$"other {other} zapped by {z.Source}");

			// look at body part being zapped
			var targetPart = other.Body.Zone(z.Zone).MainBodyPart;
			if (targetPart != null)
			{
				targets_.SetWeight(
					other, targetPart.Type,
					ps.Get(PS.OtherZappedTargetWeight) * z.Intensity,
					$"other {other} zapped by {z.Source}");
			}

			// look at person zapping
			targets_.SetWeight(
				z.Source, BP.Eyes,
				ps.Get(PS.OtherZappedSourceWeight) * z.Intensity,
				$"{z.Source} is zapping {other}");

			SetGazeDuration(PS.OtherZappedGazeDuration, z.Intensity);
		}

		private void SetGazeDuration(PS.DurationIndex di, float intensity)
		{
			if (gazeDuration_ < 0)
			{
				var d = person_.Personality.GetDuration(di);
				d.Reset(intensity);
				gazeDuration_ = d.Current;
				person_.Log.Verbose($"new gaze duration {gazeDuration_}");
			}
		}

		private float GetEyesWeight(bool player, ZoneType zone)
		{
			var ps = person_.Personality;

			if (player)
			{
				if (zone == SS.Genitals)
					return ps.Get(PS.ZappedByPlayerGenitalsEyesWeight);
				else if (zone == SS.Breasts)
					return ps.Get(PS.ZappedByPlayerBreastsEyesWeight);
				else if (zone == SS.Penetration)
					return ps.Get(PS.ZappedByPlayerPenetrationEyesWeight);
				else if (zone == SS.Mouth)
					return ps.Get(PS.ZappedByPlayerMouthEyesWeight);
			}
			else
			{
				if (zone == SS.Genitals)
					return ps.Get(PS.ZappedByOtherGenitalsEyesWeight);
				else if (zone == SS.Breasts)
					return ps.Get(PS.ZappedByOtherBreastsEyesWeight);
				else if (zone == SS.Penetration)
					return ps.Get(PS.ZappedByOtherPenetrationEyesWeight);
				else if (zone == SS.Mouth)
					return ps.Get(PS.ZappedByOtherMouthEyesWeight);
			}

			return -1;
		}

		private float GetTargetWeight(bool player, ZoneType zone)
		{
			var ps = person_.Personality;

			if (player)
			{
				if (zone == SS.Genitals)
					return ps.Get(PS.ZappedByPlayerGenitalsTargetWeight);
				else if (zone == SS.Breasts)
					return ps.Get(PS.ZappedByPlayerBreastsTargetWeight);
				else if (zone == SS.Penetration)
					return ps.Get(PS.ZappedByPlayerPenetrationTargetWeight);
			}
			else
			{
				if (zone == SS.Genitals)
					return ps.Get(PS.ZappedByOtherGenitalsTargetWeight);
				else if (zone == SS.Breasts)
					return ps.Get(PS.ZappedByOtherBreastsTargetWeight);
				else if (zone == SS.Penetration)
					return ps.Get(PS.ZappedByOtherPenetrationTargetWeight);
			}

			return -1;
		}

		public override string ToString()
		{
			return "zapped";
		}
	}
}
