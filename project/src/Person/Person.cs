﻿using SimpleJSON;
using System;
using System.Collections.Generic;

namespace Cue
{
	public class Person : BasicObject
	{
		public delegate void Callback();
		public event Callback PersonalityChanged;

		private readonly int personIndex_;

		private Personality personality_;
		private Animator animator_;
		private Excitement excitement_;
		private Body body_;
		private Voice voice_;
		private Gaze gaze_;
		private Mood mood_;
		private IAI ai_ = null;
		private ExpressionManager expression_;
		private PersonStatus status_;

		private ISpeaker speech_;
		private IClothing clothing_;
		private IHoming homing_;

		private bool hasBody_ = false;
		private bool loadPose_ = true;


		public Person(int objectIndex, int personIndex, Sys.IAtom atom)
			: base(objectIndex, atom)
		{
			personIndex_ = personIndex;

			SetPersonality(Resources.Personalities.Clone(Resources.DefaultPersonality, this));
			animator_ = new Animator(this);
			excitement_ = new Excitement(this);
			body_ = new Body(this);
			gaze_ = new Gaze(this);
			mood_ = new Mood(this);
			ai_ = new PersonAI(this);

			expression_ = new ExpressionManager(this);
			status_ = new PersonStatus(this);

			speech_ = Integration.CreateSpeaker(this);
			clothing_ = Integration.CreateClothing(this);
			homing_ = Integration.CreateHoming(this);
		}

		public void Init()
		{
			Personality.Init();
			Excitement.Init();
			Body.Init();
			Voice.Init(this);
			Gaze.Init();
			Homing.Init();

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

			loadPose_ = J.OptBool(r, "loadPose", true);

			if (r.HasKey("personality"))
			{
				var po = r["personality"].AsObject;

				if (po.HasKey("name"))
				{
					var p = Resources.Personalities.Clone(po["name"], this);
					if (p != null)
						SetPersonality(p);
				}

				personality_.Load(po);
			}

			if (r.HasKey("maxExcitement"))
				Mood.MaxExcitement = J.ReqFloat(r, "maxExcitement");
		}

		public override JSONNode ToJSON()
		{
			var o = base.ToJSON();

			o.Add("id", ID);
			o.Add("loadPose", new JSONData(loadPose_));
			o.Add("maxExcitement", new JSONData(Mood.MaxExcitement));

			var p = personality_.ToJSON();
			if (p.Count > 0)
				o.Add("personality", p);

			return o;
		}

		public int PersonIndex
		{
			get { return personIndex_; }
		}

		public Animator Animator { get { return animator_; } }
		public Excitement Excitement { get { return excitement_; } }
		public Body Body { get { return body_; } }
		public Voice Voice { get { return voice_; } }
		public Gaze Gaze { get { return gaze_; } }
		public Mood Mood { get { return mood_; } }
		public IAI AI { get { return ai_; } }
		public IClothing Clothing { get { return clothing_; } }

		public ISpeaker Speech { get { return speech_; } }
		public IHoming Homing { get { return homing_; } }

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
		}

		public bool LoadPose
		{
			get { return loadPose_; }
			set { loadPose_ = value; }
		}

		public void SetPersonality(Personality p)
		{
			if (personality_ != null)
				personality_.Destroy();

			personality_ = p;
			personality_.Init();

			voice_?.Destroy();
			voice_ = personality_.CreateVoice();
			voice_.Init(this);

			if (loadPose_)
				personality_.Pose.Set(this);

			PersonalityChanged?.Invoke();
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
				if (Cue.Instance.Options.IgnoreCamera)
					return Body.Exists;

				return true;
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
				Instrumentation.Start(I.FUBody);
				{
					body_.ResetMorphLimits();
				}
				Instrumentation.End();

				Instrumentation.Start(I.FUAnimator);
				{
					animator_.FixedUpdate(s);
				}
				Instrumentation.End();
			}

			Instrumentation.Start(I.FUAI);
			{
				ai_.FixedUpdate(s);
			}
			Instrumentation.End();


			if (hasBody_)
			{
				Instrumentation.Start(I.FUExpressions);
				{
					expression_.FixedUpdate(s);
				}
				Instrumentation.End();
			}
		}

		public override void Update(float s)
		{
			base.Update(s);

			Instrumentation.Start(I.Animator);
			{
				if (hasBody_)
					animator_.Update(s);
			}
			Instrumentation.End();


			Instrumentation.Start(I.Body);
			{
				if (hasBody_)
					body_.Update(s);
			}
			Instrumentation.End();


			Instrumentation.Start(I.Status);
			{
				if (hasBody_)
					status_.Update(s);
			}
			Instrumentation.End();


			Instrumentation.Start(I.Excitement);
			{
				if (!IsPlayer)
					excitement_.Update(s);
			}
			Instrumentation.End();


			Instrumentation.Start(I.Gaze);
			{
				if (hasBody_)
					gaze_.Update(s);
			}
			Instrumentation.End();


			Instrumentation.Start(I.Voice);
			{
				if (hasBody_)
					voice_.Update(s);
			}
			Instrumentation.End();


			Instrumentation.Start(I.Mood);
			{
				mood_.Update(s);
			}
			Instrumentation.End();


			Instrumentation.Start(I.Homing);
			{
				if (hasBody_)
					homing_.Update(s);
			}
			Instrumentation.End();


			Instrumentation.Start(I.AI);
			{
				ai_.Update(s);
			}
			Instrumentation.End();
		}

		public override void OnPluginState(bool b)
		{
			base.OnPluginState(b);

			animator_.OnPluginState(b);
			ai_.OnPluginState(b);
			expression_.OnPluginState(b);
			gaze_.OnPluginState(b);
		}

		public virtual void Say(string s)
		{
			speech_.Say(s);
		}
	}
}
