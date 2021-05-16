namespace Cue
{
	class Gaze
	{
		private Person person_;
		private IEyes eyes_;
		private IGazer gazer_;
		private IGazeLookat lookat_;
		private IObject avoid_ = null;

		public Gaze(Person p)
		{
			person_ = p;
			eyes_ = Integration.CreateEyes(p);
			gazer_ = Integration.CreateGazer(p);
			lookat_ = new LookatNothing(p);
		}

		public IEyes Eyes { get { return eyes_; } }
		public IGazer Gazer { get { return gazer_; } }
		public IObject Avoid { get { return avoid_; } }

		public void Update(float s)
		{
			avoid_ = person_.Personality.GazeAvoid();

			DetermineLookat();

			lookat_.Update(person_, s);

			if (lookat_.HasPosition)
			{
				eyes_.LookAt(lookat_.Position);
			}
			else
			{
				// ??
			}

			gazer_.Enabled = lookat_.EnableGaze;
			gazer_.Duration = person_.Personality.GazeDuration;

			eyes_.Update(s);
			gazer_.Update(s);
		}

		public void DetermineLookat()
		{
			if (person_.Body.Get(BodyParts.Head).Grabbed)
			{
				if (Cue.Instance.Player != null)
				{
					if (lookat_ is LookatObject)
						(lookat_ as LookatObject).Set(Cue.Instance.Player, false);
					else
						lookat_ = new LookatObject(person_, Cue.Instance.Player, false);

					return;
				}
			}


			if (person_.Kisser.Active)
			{
				var t = person_.Kisser.Target;
				if (t != null)
				{
					if (t != avoid_)
					{
						if (lookat_ is LookatObject)
							(lookat_ as LookatObject).Set(t, false);
						else
							lookat_ = new LookatObject(person_, t, false);

						return;
					}
				}
			}


			if (person_.Blowjob.Active)
			{
				var t = person_.Blowjob.Target;

				if (t != null)
				{
					if (t != avoid_)
					{
						var parts = new int[]
						{
							BodyParts.Genitals, BodyParts.Eyes
						};

						if (lookat_ is LookatParts)
							(lookat_ as LookatParts).Set(t, parts, false);
						else
							lookat_ = new LookatParts(person_, t, parts, false);

						return;
					}
				}
			}


			if (person_.Handjob.Active)
			{
				if (!(lookat_ is LookatRandom))
					lookat_ = new LookatRandom(person_, true);

				return;
			}


			if (person_.HasTarget)
			{
				if (person_.MoveTarget != null)
				{
					if (lookat_ is LookatObject)
						(lookat_ as LookatObject).Set(person_.MoveTarget, true);
					else
						lookat_ = new LookatObject(person_, person_.MoveTarget, true);
				}
				else
				{
					if (!(lookat_ is LookatFront))
						lookat_ = new LookatFront(person_);
				}

				return;
			}


			if (person_.Body.PlayerIsClose)
			{
				if (avoid_ != Cue.Instance.Player)
				{
					if (lookat_ is LookatObject)
						(lookat_ as LookatObject).Set(Cue.Instance.Player, true);
					else
						lookat_ = new LookatObject(person_, Cue.Instance.Player, true);

					return;
				}
			}


			if (!(lookat_ is LookatRandom))
				lookat_ = new LookatRandom(person_, true);
		}


		public override string ToString()
		{
			return lookat_.ToString();
		}
	}
}
