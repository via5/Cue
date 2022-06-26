using System;
using System.Collections.Generic;

namespace Cue
{
	public class Body
	{
		public class ZapInfo
		{
			private Person source_ = null;
			private int zone_ = SS.None;
			private float maxIntensity_ = 0;
			private float time_ = 0;
			private float elapsed_ = 0;

			public void Set(Person source, int zone, float maxIntensity, float time)
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

			public int Zone
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

			public string DebugLine(Person self)
			{
				if (source_ == null)
					return "no";

				return
					$"{source_} on {self.Body.Zone(zone_)} at {Intensity}, " +
					$"max={maxIntensity_:0.00} time={time_:0.00}";
			}
		}

		public const int CloseDelay = 2;
		private const float MaxMorphs = 1.2f;

		private Person person_;
		private Logger log_;
		private readonly BodyPart[] all_;
		private Hand leftHand_, rightHand_;
		private DampedFloat temperature_;
		private float[] morphsRemaining_ = new float[BP.Count];
		private ErogenousZones zones_;
		private ZapInfo zap_ = new ZapInfo();


		public Body(Person p)
		{
			person_ = p;
			log_ = new Logger(Logger.Object, p, $"body");
			temperature_ = new DampedFloat();
			zones_ = new ErogenousZones(p);

			var parts = p.Atom.Body.GetBodyParts();
			var all = new List<BodyPart>();

			for (int i = 0; i < BP.Count; ++i)
			{
				if (parts[i] != null && parts[i].Type != i)
					Log.Error($"mismatched body part type {parts[i].Type} {i}");

				all.Add(new BodyPart(person_, i, parts[i]));
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

		public int GenitalsBodyPart
		{
			get { return (HasPenis ? BP.Penis : BP.Labia); }
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

		public BodyPart Get(int type)
		{
			if (type < 0 || type >= all_.Length)
			{
				Log.Error($"bad part type {type}");
				return null;
			}

			return all_[type];
		}

		public ErogenousZone Zone(int i)
		{
			return zones_.Get(i);
		}

		public void Slapped(float speed)
		{
			person_.Expression.Slapped(speed);
		}

		public void Zapped(Person source, int zone, float intensity, float time)
		{
			if (person_.Personality.GetBool(PS.ZappedEnabled))
			{
				zap_.Set(source, zone, intensity, time);
				Log.Info($"zapped: {zap_.DebugLine(person_)}");
			}
		}

		public void Update(float s)
		{
			zap_.Update(s);

			for (int i = 0; i < all_.Length; ++i)
				all_[i].Update(s);

			zones_.Update(s);

			var ps = person_.Personality;

			temperature_.UpRate = person_.Mood.Get(Moods.Excited) * ps.Get(PS.TemperatureExcitementRate);
			temperature_.DownRate = ps.Get(PS.TemperatureDecayRate);

			temperature_.Target = U.Clamp(
				person_.Mood.Get(Moods.Excited) / ps.Get(PS.TemperatureExcitementMax),
				0, 1);

			if (temperature_.Update(s))
			{
				person_.Atom.Body.Sweat = temperature_.Value * ps.Get(PS.MaxSweat);
				person_.Atom.Body.Flush = temperature_.Value * ps.Get(PS.MaxFlush);
				person_.Atom.Hair.Loose = temperature_.Value;
			}

			person_.Voice.MaxIntensity = person_.Mood.MovementEnergy;
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
		}

		public float UseMorphs(int[] bodyParts, float use)
		{
			if (bodyParts == null || bodyParts.Length == 0)
				return use;

			float smallestAv = float.MaxValue;

			for (int i = 0; i < bodyParts.Length; ++i)
			{
				var bp = bodyParts[i];
				if (bp != BP.None && morphsRemaining_[bp] < smallestAv)
					smallestAv = morphsRemaining_[bp];
			}

			float av = Math.Min(use, smallestAv);

			for (int i = 0; i < bodyParts.Length; ++i)
			{
				var bp = bodyParts[i];
				if (bp != BP.None)
					morphsRemaining_[bp] -= av;
			}

			return av;
		}
	}
}
