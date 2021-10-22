using SimpleJSON;
using System;
using System.Collections.Generic;

namespace Cue
{
	class PersonOptions
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


	class Person : BasicObject
	{
		private readonly int personIndex_;

		private PersonOptions options_;
		private Animator animator_;
		private Excitement excitement_;
		private Body body_;
		private Gaze gaze_;
		private Physiology physiology_;
		private Mood mood_;
		private IAI ai_ = null;
		private Personality personality_;

		private IBreather breathing_;
		private IOrgasmer orgasmer_;
		private ISpeaker speech_;
		private IKisser kisser_;
		private IHandjob handjob_;
		private IBlowjob blowjob_;
		private IClothing clothing_;


		public Person(int objectIndex, int personIndex, Sys.IAtom atom)
			: base(objectIndex, atom)
		{
			personIndex_ = personIndex;

			options_ = new PersonOptions(this);
			animator_ = new Animator(this);
			excitement_ = new Excitement(this);
			body_ = new Body(this);
			gaze_ = new Gaze(this);
			physiology_ = Resources.Physiologies.Clone(Resources.DefaultPhysiology, this);
			mood_ = new Mood(this);
			ai_ = new PersonAI(this);

			Personality = Resources.Personalities.Clone(Resources.DefaultPersonality, this);

			breathing_ = Integration.CreateBreather(this);
			orgasmer_ = Integration.CreateOrgasmer(this);
			speech_ = Integration.CreateSpeaker(this);
			kisser_ = Integration.CreateKisser(this);
			handjob_ = Integration.CreateHandjob(this);
			blowjob_ = Integration.CreateBlowjob(this);
			clothing_ = Integration.CreateClothing(this);

			Atom.SetDefaultControls("init");
		}

		public void Init()
		{
			Body.Init();
			gaze_.Init();

			if (IsPlayer)
				AI.EventsEnabled = false;

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
		public Physiology Physiology { get { return physiology_; } }
		public Mood Mood { get { return mood_; } }
		public IAI AI { get { return ai_; } }
		public IClothing Clothing { get { return clothing_; } }

		public IBreather Breathing { get { return breathing_; } }
		public IOrgasmer Orgasmer { get { return orgasmer_; } }
		public ISpeaker Speech { get { return speech_; } }
		public IKisser Kisser { get { return kisser_; } }
		public IHandjob Handjob { get { return handjob_; } }
		public IBlowjob Blowjob { get { return blowjob_; } }

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
			get { return personality_; }
			set { personality_ = value; }
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

			animator_.FixedUpdate(s);
			ai_.FixedUpdate(s);
		}

		private void UpdateGaze(float s)
		{
			if (!IsPlayer)
				gaze_.Update(s);
		}

		public override void Update(float s)
		{
			base.Update(s);

			body_.ResetMorphLimits();

			I.Start(I.UpdatePersonAnimator);
			{
				animator_.Update(s);
			}
			I.End();


			I.Start(I.UpdatePersonGaze);
			{
				UpdateGaze(s);
			}
			I.End();


			I.Start(I.UpdatePersonEvents);
			{
				Kisser.Update(s);
				Handjob.Update(s);
				Blowjob.Update(s);
			}
			I.End();


			if (!IsPlayer)
			{
				I.Start(I.UpdatePersonExcitement);
				{
					excitement_.Update(s);
				}
				I.End();


				I.Start(I.UpdatePersonPersonality);
				{
					personality_.Update(s);
				}
				I.End();


				I.Start(I.UpdatePersonMood);
				{
					mood_.Update(s);
				}
				I.End();


				I.Start(I.UpdatePersonBody);
				{
					body_.Update(s);
				}
				I.End();
			}


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
			Kisser.OnPluginState(b);
			Handjob.OnPluginState(b);
			Blowjob.OnPluginState(b);

			ai_.OnPluginState(b);
		}

		public virtual void Say(string s)
		{
			speech_.Say(s);
		}
	}
}
