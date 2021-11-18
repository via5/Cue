using SimpleJSON;
using System;
using System.Collections.Generic;

namespace Cue
{
	public class PersonOptions
	{
		private bool canKiss_ = true;

		public PersonOptions(Person p)
		{
		}

		public bool CanKiss
		{
			get { return canKiss_; }
			set { canKiss_ = value; }
		}
	}


	public class Person : BasicObject
	{
		public delegate void Callback();
		public event Callback PersonalityChanged;

		private readonly int personIndex_;

		private PersonOptions options_;
		private Personality personality_;
		private Animator animator_;
		private Excitement excitement_;
		private Body body_;
		private Gaze gaze_;
		private Mood mood_;
		private IAI ai_ = null;
		private ExpressionManager expression_;
		private PersonStatus status_;

		private IBreather breathing_;
		private IOrgasmer orgasmer_;
		private ISpeaker speech_;
		private IClothing clothing_;

		private bool hasBody_ = false;


		public Person(int objectIndex, int personIndex, Sys.IAtom atom)
			: base(objectIndex, atom)
		{
			personIndex_ = personIndex;

			options_ = new PersonOptions(this);
			personality_ = Resources.Personalities.Clone(Resources.DefaultPersonality, this);
			animator_ = new Animator(this);
			excitement_ = new Excitement(this);
			body_ = new Body(this);
			gaze_ = new Gaze(this);
			mood_ = new Mood(this);
			ai_ = new PersonAI(this);

			expression_ = new ExpressionManager(this);
			status_ = new PersonStatus(this);

			breathing_ = Integration.CreateBreather(this);
			orgasmer_ = Integration.CreateOrgasmer(this);
			speech_ = Integration.CreateSpeaker(this);
			clothing_ = Integration.CreateClothing(this);

			Atom.SetDefaultControls("init");
		}

		public void Init()
		{
			Personality.Init();
			Excitement.Init();
			Body.Init();
			Gaze.Init();

			hasBody_ = body_.Exists;

			if (IsPlayer)
				AI.EventsEnabled = false;

			AI.Init();

			Atom.Init();
			Atom.SetBodyDamping(Sys.BodyDamping.Normal);
		}

		public override void Load(JSONClass r)
		{
			base.Load(r);

			if (r.HasKey("personality"))
			{
				var po = r["personality"].AsObject;

				if (po.HasKey("name"))
				{
					var p = Resources.Personalities.Clone(po["name"], this);
					if (p != null)
						personality_ = p;
				}

				personality_.Load(po);
			}
		}

		public override JSONNode ToJSON()
		{
			var o = base.ToJSON();

			var p = personality_.ToJSON();
			if (p.Count > 0)
				o.Add("personality", p);

			if (o.Count > 0)
				o.Add("id", ID);

			return o;
		}

		public int PersonIndex
		{
			get { return personIndex_; }
		}

		public PersonOptions Options { get { return options_; } }
		public Animator Animator { get { return animator_; } }
		public Excitement Excitement { get { return excitement_; } }
		public Body Body { get { return body_; } }
		public Gaze Gaze { get { return gaze_; } }
		public Mood Mood { get { return mood_; } }
		public IAI AI { get { return ai_; } }
		public IClothing Clothing { get { return clothing_; } }

		public IBreather Breathing { get { return breathing_; } }
		public IOrgasmer Orgasmer { get { return orgasmer_; } }
		public ISpeaker Speech { get { return speech_; } }

		public int MovementStyle
		{
			get
			{
				if (Atom.IsMale)
					return MovementStyles.Masculine;
				else
					return MovementStyles.Feminine;
			}
		}

		public override Vector3 EyeInterest
		{
			get
			{
				return body_.Get(BP.Eyes)?.Position ?? base.EyeInterest;
			}
		}

		public Personality Personality
		{
			get
			{
				return personality_;
			}

			set
			{
				personality_ = value;
				personality_.Init();
				PersonalityChanged?.Invoke();
			}
		}

		public ExpressionManager Expression
		{
			get { return expression_; }
		}

		public PersonStatus Status
		{
			get { return status_; }
		}

		public bool IsInteresting
		{
			get
			{
				if (IsPlayer)
					return true;
				else
					return Body.Exists;
			}
		}

		public bool Grabbed
		{
			get { return Atom.Grabbed; }
		}

		public override void FixedUpdate(float s)
		{
			base.FixedUpdate(s);

			if (hasBody_)
			{
				body_.ResetMorphLimits();
				animator_.FixedUpdate(s);
			}

			ai_.FixedUpdate(s);

			if (hasBody_)
				expression_.FixedUpdate(s);
		}

		public override void Update(float s)
		{
			base.Update(s);

			I.Start(I.UpdatePersonAnimator);
			{
				if (hasBody_)
					animator_.Update(s);
			}
			I.End();


			I.Start(I.UpdatePersonBody);
			{
				if (hasBody_)
					body_.Update(s);
			}
			I.End();


			I.Start(I.UpdatePersonStatus);
			{
				if (hasBody_)
					status_.Update(s);
			}
			I.End();


			I.Start(I.UpdatePersonExcitement);
			{
				if (!IsPlayer)
					excitement_.Update(s);
			}
			I.End();


			I.Start(I.UpdatePersonGaze);
			{
				if (!IsPlayer && hasBody_)
					gaze_.Update(s);
			}
			I.End();


			I.Start(I.UpdatePersonMood);
			{
				if (!IsPlayer)
					mood_.Update(s);
			}
			I.End();


			I.Start(I.UpdatePersonAI);
			{
				ai_.Update(s);
			}
			I.End();
		}

		public override void OnPluginState(bool b)
		{
			base.OnPluginState(b);

			animator_.OnPluginState(b);
			ai_.OnPluginState(b);
			expression_.OnPluginState(b);
		}

		public virtual void Say(string s)
		{
			speech_.Say(s);
		}
	}
}
