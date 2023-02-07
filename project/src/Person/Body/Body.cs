using System;
using System.Collections.Generic;

namespace Cue
{
	static class BodyDamping
	{
		public const int Normal = 0;
		public const int SexReceiver = 1;

		public static string ToString(int i)
		{
			switch (i)
			{
				case Normal:
					return "normal";

				case SexReceiver:
					return "sex receiver";

				default:
					return $"?{i}";
			}
		}
	}


	public class Body
	{
		public class ZapInfo
		{
			private Person source_ = null;
			private ZoneType zone_ = SS.None;
			private float maxIntensity_ = 0;
			private float time_ = 0;
			private float elapsed_ = 0;

			public void Set(Person source, ZoneType zone, float maxIntensity, float time)
			{
				source_ = source;
				zone_ = zone;
				maxIntensity_ = maxIntensity;
				time_ = time;
				elapsed_ = 0;
			}

			public void Update(float s)
			{
				elapsed_ += s;

				if (elapsed_ >= time_)
					Reset();
			}

			public void Reset()
			{
				source_ = null;
				maxIntensity_ = 0;
				time_ = 0;
				elapsed_ = 0;
			}

			public Person Source
			{
				get { return source_; }
			}

			public ZoneType Zone
			{
				get { return zone_; }
			}

			public float Intensity
			{
				get
				{
					if (time_ > 0)
						return maxIntensity_ * (1 - (elapsed_ / time_));
					else
						return 0;
				}
			}

			public bool Active
			{
				get { return (zone_ != SS.None); }
			}

			public string DebugLine(Person self)
			{
				if (zone_ == SS.None)
					return "no";

				string s;

				if (source_ == null)
					s = "null";
				else
					s = source_.ToString();

				return
					$"{s} on {self.Body.Zone(zone_)} at {Intensity:0.00}, " +
					$"max={maxIntensity_:0.00} time={time_:0.00}";
			}
		}

		public const int CloseDelay = 2;
		private const float MaxMorphs = 1.2f;
		private const float MaxEyeMorphs = 1.0f;

		private Person person_;
		private Logger log_;
		private readonly BodyPart[] all_;
		private Hand leftHand_, rightHand_;
		private DampedFloat temperature_;
		private float[] morphsRemaining_ = new float[BP.Count];
		private ErogenousZones zones_;
		private ZapInfo zap_ = new ZapInfo();
		private ForceableBool breathing_ = new ForceableBool(true);
		private DampedFloat air_ = new DampedFloat();
		private int damping_ = BodyDamping.Normal;

		public Body(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.Object, p, $"body");
			temperature_ = new DampedFloat();
			zones_ = new ErogenousZones(p);

			air_.Target = 1;
			air_.SetValue(1);

			var parts = p.Atom.Body.GetBodyParts();
			var all = new List<BodyPart>();

			foreach (BodyPartType i in BodyPartType.Values)
			{
				if (parts[i.Int] != null && parts[i.Int].Type != i)
					Log.Error($"mismatched body part type {parts[i.Int].Type} {i.Int}");

				all.Add(new BodyPart(person_, i, parts[i.Int]));
			}

			all_ = all.ToArray();

			leftHand_ = new Hand(p, "left", p.Atom.Body.GetLeftHand(), BP.LeftHand);
			rightHand_ = new Hand(p, "right", p.Atom.Body.GetRightHand(), BP.RightHand);
		}

		public Logger Log
		{
			get { return log_; }
		}

		public void Init()
		{
			zones_.Init();
		}

		public BodyPart[] Parts
		{
			get { return all_; }
		}

		public bool Exists
		{
			get { return person_.Atom.Body.Exists; }
		}

		public bool HasPenis
		{
			get { return Get(BP.Penis).Exists; }
		}

		public bool PenisSensitive
		{
			get { return (HasPenis && Get(BP.Penis).IsPhysical); }
		}

		public int Damping
		{
			get { return damping_; }
		}

		public float Scale
		{
			get { return person_.Atom.Scale; }
		}

		public BodyPartType GenitalsBodyPart
		{
			get { return (HasPenis ? BP.Penis : BP.Vagina); }
		}

		public bool Strapon
		{
			get { return person_.Atom.Body.Strapon; }
			set { person_.Atom.Body.Strapon = value; }
		}

		public float Temperature
		{
			get { return temperature_.Target; }
		}

		public DampedFloat DampedTemperature
		{
			get { return temperature_; }
		}

		public bool Breathing
		{
			get { return breathing_.Value; }
			set { breathing_.Value = value; }
		}

		public ForceableBool BreathingBool
		{
			get { return breathing_; }
			set { breathing_ = value; }
		}

		public DampedFloat DampedAir
		{
			get { return air_; }
		}

		public Hand LeftHand
		{
			get { return leftHand_; }
		}

		public Hand RightHand
		{
			get { return rightHand_; }
		}

		public ZapInfo Zap
		{
			get { return zap_; }
		}

		public ErogenousZones Zones
		{
			get { return zones_; }
		}

		public BodyPart Get(BodyPartType type)
		{
			if (type.Int < 0 || type.Int >= all_.Length)
			{
				Log.Error($"bad part type {type}");
				return null;
			}

			return all_[type.Int];
		}

		public BodyPart FindBodyPart(Sys.IBodyPart sys)
		{
			for (int i = 0; i < all_.Length; ++i)
			{
				if (all_[i].Sys == sys)
					return all_[i];
			}

			return null;
		}

		public static BodyPart ResolveBodyPart(Sys.IBodyPartRegion region)
		{
			var p = Cue.Instance.PersonForAtom(region?.BodyPart?.Atom);
			if (p == null)
				return null;

			var bp = p.Body.FindBodyPart(region?.BodyPart);
			if (bp == null)
				return null;

			return bp;
		}

		public ErogenousZone Zone(ZoneType i)
		{
			return zones_.Get(i);
		}

		public void SetDamping(int i)
		{
			damping_ = i;
			person_.Atom.SetBodyDamping(i);
		}

		public static void SetSexDamping()
		{
			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (p.AI.GetEvent<ThrustEvent>().Active ||
					p.AI.GetEvent<TribEvent>().Active)
				{
					p.Body.SetDamping(BodyDamping.Normal);
				}
				else
				{
					bool found = false;

					foreach (var pp in Cue.Instance.ActivePersons)
					{
						if (pp == p)
							continue;

						var thrust = pp.AI.GetEvent<ThrustEvent>();
						if (thrust.Active && thrust.Receiver == p)
						{
							p.Body.SetDamping(BodyDamping.SexReceiver);
							found = true;
							break;
						}

						var trib = pp.AI.GetEvent<TribEvent>();
						if (trib.Active && trib.Receiver == p)
						{
							p.Body.SetDamping(BodyDamping.SexReceiver);
							found = true;
							break;
						}
					}

					if (!found)
						p.Body.SetDamping(BodyDamping.Normal);
				}
			}
		}

		public void Slapped(float speed)
		{
			person_.Expression.Slapped(speed);
		}

		public void Zapped(Person source, ZoneType zone)
		{
			Zapped(
				source, zone,
				GetZappedIntensity(source, zone),
				person_.Personality.Get(PS.ZappedTime));
		}

		public void Zapped(Person source, ZoneType zone, float intensity, float time)
		{
			intensity = U.Clamp(intensity, 0, person_.Options.MaxExcitement);

			if (person_.Personality.GetBool(PS.ZappedEnabled))
			{
				zap_.Set(source, zone, intensity, time);
				Log.Verbose($"zapped: {zap_.DebugLine(person_)}");
			}
		}

		private float GetZappedIntensity(Person other, ZoneType zone)
		{
			var ps = person_.Personality;

			if (other != null && other.IsPlayer)
			{
				if (zone == SS.Genitals)
					return ps.Get(PS.ZappedByPlayerGenitalsExcitement);
				else if (zone == SS.Breasts)
					return ps.Get(PS.ZappedByPlayerBreastsExcitement);
				else if (zone == SS.Penetration)
					return ps.Get(PS.ZappedByPlayerPenetrationExcitement);
				else if (zone == SS.Mouth)
					return ps.Get(PS.ZappedByPlayerMouthExcitement);
			}
			else
			{
				if (zone == SS.Genitals)
					return ps.Get(PS.ZappedByOtherGenitalsExcitement);
				else if (zone == SS.Breasts)
					return ps.Get(PS.ZappedByOtherBreastsExcitement);
				else if (zone == SS.Penetration)
					return ps.Get(PS.ZappedByOtherPenetrationExcitement);
				else if (zone == SS.Mouth)
					return ps.Get(PS.ZappedByOtherMouthExcitement);
			}

			Log.Error($"zap source: bad zone {zone}");
			return 0;
		}

		public void Update(float s)
		{
			Instrumentation.Start(I.BodyZap);
			{
				zap_.Update(s);
			}
			Instrumentation.End();


			Instrumentation.Start(I.BodyParts);
			{
				for (int i = 0; i < all_.Length; ++i)
					all_[i].Update(s);
			}
			Instrumentation.End();


			Instrumentation.Start(I.BodyZones);
			{
				zones_.Update(s);
			}
			Instrumentation.End();

			bool updateBody = false;

			Instrumentation.Start(I.BodyVoice);
			{
				air_.DownRate = 1.0f / person_.Personality.Get(PS.ChokedAirDownTime);
				air_.UpRate = 1.0f / person_.Personality.Get(PS.ChokedAirUpTime);

				if (Breathing)
					air_.Target = 1;
				else
					air_.Target = 0;

				if (air_.Update(s))
					updateBody = true;
			}
			Instrumentation.End();

			Instrumentation.Start(I.BodyTemperature);
			{
				var ps = person_.Personality;

				temperature_.UpRate = person_.Mood.Get(MoodType.Excited) * ps.Get(PS.TemperatureExcitementRate);
				temperature_.DownRate = ps.Get(PS.TemperatureDecayRate);

				temperature_.Target = U.Clamp(
					person_.Mood.Get(MoodType.Excited) / ps.Get(PS.TemperatureExcitementMax),
					0, 1);
				if (temperature_.Update(s))
					updateBody = true;

				if (updateBody)
				{
					person_.Atom.Body.FlushMag = ps.Get(PS.FlushRedMag);
					person_.Atom.Body.Sweat = temperature_.Value * ps.Get(PS.MaxSweat);
					person_.Atom.Body.Flush = MakeFlush();
					person_.Atom.Hair.Loose = temperature_.Value;
				}
			}
			Instrumentation.End();
		}

		private float MakeFlush()
		{
			var ps = person_.Personality;

			float temp = temperature_.Value * ps.Get(PS.MaxFlush);
			float air = (1.0f - air_.Value) * ps.Get(PS.MaxChokedFlush);

			float f = Math.Max(temp, air);

			return U.Clamp(f, 0, 1);
		}

		public void DebugAllLocks(List<string> list)
		{
			list.Clear();

			for (int i = 0; i < all_.Length; ++i)
				all_[i].Locker.DebugAllLocks(list);
		}

		public Box GetUpperBodyBox()
		{
			Vector3 topPos = person_.EyeInterest + new Vector3(0, 0.2f, 0);
			Vector3 bottomPos;

			var hips = Get(BP.Hips);

			// this happens for the camera pseudo-person
			if (hips.Exists)
				bottomPos = hips.Position;
			else
				bottomPos = topPos - new Vector3(0, 0.5f, 0);

			return new Box(
				bottomPos + (topPos - bottomPos) / 2,
				new Vector3(0.5f, (topPos - bottomPos).Y, 0.5f));
		}

		public void ResetMorphLimits()
		{
			for (int i = 0; i < morphsRemaining_.Length; ++i)
				morphsRemaining_[i] = MaxMorphs;

			morphsRemaining_[BP.Eyes.Int] = MaxEyeMorphs;
		}

		public float UseMorphs(BodyPartType[] bodyParts, float use)
		{
			if (bodyParts == null || bodyParts.Length == 0)
				return use;

			float smallestAv = float.MaxValue;

			for (int i = 0; i < bodyParts.Length; ++i)
			{
				var bp = bodyParts[i];
				if (bp != BP.None && morphsRemaining_[bp.Int] < smallestAv)
					smallestAv = morphsRemaining_[bp.Int];
			}

			float av = Math.Min(use, smallestAv);

			for (int i = 0; i < bodyParts.Length; ++i)
			{
				var bp = bodyParts[i];
				if (bp != BP.None)
					morphsRemaining_[bp.Int] -= av;
			}

			return av;
		}
	}
}
