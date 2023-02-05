using SimpleJSON;
using System;
using System.Collections.Generic;

namespace Cue
{
	public class PersonOptions
	{
		public const int Orgasm = 0;
		public const int HandjobLeft = 1;
		public const int HandjobRight = 2;
		public const int LeftFinger = 3;
		public const int RightFinger = 4;
		public const int Head = 5;
		public const int Thrust = 6;
		public const int Trib = 7;
		public const int Kiss = 8;
		public const int LeftHandOnBreast = 9;
		public const int RightHandOnBreast = 10;


		public class AnimationOptions
		{
			private readonly int type_;
			private readonly string name_;
			private readonly string key_;
			private CustomTrigger triggerOn_, triggerOff_;
			private bool play_ = true;
			private readonly Sys.IActionParameter action_;

			public AnimationOptions(Person p, int type, string name, Action f = null)
			{
				type_ = type;
				name_ = name;
				key_ = name.ToLower();
				triggerOn_ = new CustomTrigger($"{name}.on");
				triggerOff_ = new CustomTrigger($"{name}.on");

				if (f == null)
					action_ = null;
				else
					action_ = Cue.Instance.Sys.RegisterActionParameter($"{p.ID}.{name}", f);
			}

			public void Load(JSONClass o)
			{
				if (o.HasKey(key_))
				{
					var ao = o[key_].AsObject;

					J.OptBool(ao, "play", ref play_);

					if (ao.HasKey("triggerOn"))
						triggerOn_ = CustomTrigger.FromJSON(ao["triggerOn"].AsObject);

					if (ao.HasKey("triggerOff"))
						triggerOff_ = CustomTrigger.FromJSON(ao["triggerOff"].AsObject);
				}
			}

			public void Save(JSONClass o)
			{
				var ao = new JSONClass();

				ao.Add("play", new JSONData(play_));
				ao.Add("triggerOn", triggerOn_.ToJSON());
				ao.Add("triggerOff", triggerOff_.ToJSON());

				o[key_] = ao;
			}

			public int Type
			{
				get { return type_; }
			}

			public string Name
			{
				get { return name_; }
			}

			public CustomTrigger TriggerOn
			{
				get { return triggerOn_; }
			}

			public CustomTrigger TriggerOff
			{
				get { return triggerOff_; }
			}

			public void Trigger(bool on)
			{
				if (on)
					triggerOn_.Fire();
				else
					triggerOff_.Fire();
			}

			public bool Play
			{
				get
				{
					return play_;
				}

				set
				{
					if (play_ != value)
					{
						play_ = value;
						OnChange();
					}
				}
			}

			private void OnChange()
			{
				Cue.Instance.Save();
			}
		}


		private readonly Person person_;
		private float maxExcitement_ = 1.0f;
		private List<AnimationOptions> anims_ = new List<AnimationOptions>();
		private bool idlePose_ = true;
		private bool excitedPose_ = true;

		public PersonOptions(Person p)
		{
			person_ = p;

			anims_.Add(new AnimationOptions(p, Orgasm, "Orgasm", p.Mood.ForceOrgasm));
			anims_.Add(new AnimationOptions(p, HandjobLeft, "Left HJ"));
			anims_.Add(new AnimationOptions(p, HandjobRight, "Right HJ"));
			anims_.Add(new AnimationOptions(p, LeftFinger, "Left Finger"));
			anims_.Add(new AnimationOptions(p, RightFinger, "Right Finger"));
			anims_.Add(new AnimationOptions(p, Head, "Head"));
			anims_.Add(new AnimationOptions(p, Thrust, "Thrust"));
			anims_.Add(new AnimationOptions(p, Trib, "Trib"));
			anims_.Add(new AnimationOptions(p, Kiss, "Kiss"));
			anims_.Add(new AnimationOptions(p, LeftHandOnBreast, "Left hand on breast"));
			anims_.Add(new AnimationOptions(p, RightHandOnBreast, "Right hand on breast"));
		}

		public void Load(JSONClass o)
		{
			J.OptFloat(o, "maxExcitement", ref maxExcitement_);
			J.OptBool(o, "idlePose", ref idlePose_);
			J.OptBool(o, "excitedPose", ref excitedPose_);

			foreach (var a in anims_)
				a.Load(o);
		}

		public void Save(JSONClass o)
		{
			o.Add("maxExcitement", new JSONData(maxExcitement_));
			o.Add("idlePose", new JSONData(idlePose_));
			o.Add("excitedPose", new JSONData(excitedPose_));

			foreach (var a in anims_)
				a.Save(o);
		}

		public float MaxExcitement
		{
			get { return maxExcitement_; }
			set { maxExcitement_ = value; OnChange(); }
		}

		public bool IdlePose
		{
			get { return idlePose_; }
			set { idlePose_ = value; OnChange(); }
		}

		public bool ExcitedPose
		{
			get { return excitedPose_; }
			set { excitedPose_ = value; OnChange(); }
		}

		public List<AnimationOptions> GetAnimationOptions()
		{
			return anims_;
		}

		public AnimationOptions GetAnimationOption(int type)
		{
			for (int i = 0; i < anims_.Count; ++i)
			{
				if (anims_[i].Type == type)
					return anims_[i];
			}

			return null;
		}

		public void Trigger(int type, bool on)
		{
			var a = GetAnimationOption(type);
			if (a == null)
				return;

			if (on)
				a.TriggerOn.Fire();
			else
				a.TriggerOff.Fire();
		}

		private void OnChange()
		{
			Cue.Instance.Save();
		}
	}


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
		private PersonOptions options_;

		private ISpeaker speech_;
		private IClothing clothing_;
		private IHoming homing_;

		private bool hasBody_ = false;
		private bool loadPose_ = true;


		public Person(int objectIndex, int personIndex, Sys.IAtom atom)
			: base(objectIndex, atom)
		{
			personIndex_ = personIndex;

			body_ = new Body(this);
			SetPersonality(Resources.Personalities.Clone(Resources.DefaultPersonality, this), false);
			animator_ = new Animator(this);
			excitement_ = new Excitement(this);
			gaze_ = new Gaze(this);
			mood_ = new Mood(this);
			ai_ = new PersonAI(this);

			expression_ = new ExpressionManager(this);
			status_ = new PersonStatus(this);
			options_ = new PersonOptions(this);

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
			Atom.SetBodyDamping(BodyDamping.Normal);
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
			else
			{
				if (loadPose_)
					personality_.Pose.Set(this);
			}

			Options.Load(r);
		}

		public override JSONNode ToJSON()
		{
			var o = base.ToJSON().AsObject;

			o.Add("id", ID);
			o.Add("loadPose", new JSONData(loadPose_));

			Options.Save(o);

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

		public void SetPersonality(Personality p, bool canLoadPose = true)
		{
			if (personality_ != null)
				personality_.Destroy();

			personality_ = p;
			personality_.Init();

			voice_?.Destroy();
			voice_ = personality_.CreateVoice();
			voice_.Init(this);

			if (loadPose_ && canLoadPose)
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

		public PersonOptions Options
		{
			get { return options_; }
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

		public override void UpdatePaused(float s)
		{
			base.UpdatePaused(s);

			Instrumentation.Start(I.AI);
			{
				ai_.UpdatePaused(s);
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
