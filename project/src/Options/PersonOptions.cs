using SimpleJSON;
using System;
using System.Collections.Generic;

namespace Cue
{
	public class PersonOptions
	{
		public class AnimationOptions
		{
			private readonly AnimationType type_;
			private readonly string name_;
			private readonly string key_;
			private CustomButtonItem triggerOn_, triggerOff_;
			private bool play_ = true;
			private readonly Sys.IActionParameter action_;

			public AnimationOptions(Person p, AnimationType type, string name, Action f = null)
			{
				type_ = type;
				name_ = name;
				key_ = name.ToLower();
				triggerOn_ = new CustomButtonItem($"{name}.on");
				triggerOff_ = new CustomButtonItem($"{name}.on");

				if (f == null || !p.Body.Exists)
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
						triggerOn_ = CustomButtonItem.FromJSON(ao["triggerOn"].AsObject);

					if (ao.HasKey("triggerOff"))
						triggerOff_ = CustomButtonItem.FromJSON(ao["triggerOff"].AsObject);
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

			public AnimationType Type
			{
				get { return type_; }
			}

			public string Name
			{
				get { return name_; }
			}

			public CustomButtonItem TriggerOn
			{
				get { return triggerOn_; }
			}

			public CustomButtonItem TriggerOff
			{
				get { return triggerOff_; }
			}

			public void Trigger(bool on)
			{
				if (on)
					triggerOn_.Activate();
				else
					triggerOff_.Activate();
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

		private Sys.IFloatParameter maxExcitementParam_;
		private Sys.IBoolParameter idlePoseParam_;
		private Sys.IBoolParameter excitedPoseParam_;

		public PersonOptions(Person p)
		{
			person_ = p;

			anims_.Add(new AnimationOptions(p, AnimationType.Orgasm, "Orgasm", p.Mood.ForceOrgasm));
			anims_.Add(new AnimationOptions(p, AnimationType.HandjobLeft, "Left HJ"));
			anims_.Add(new AnimationOptions(p, AnimationType.HandjobRight, "Right HJ"));
			anims_.Add(new AnimationOptions(p, AnimationType.LeftFinger, "Left Finger"));
			anims_.Add(new AnimationOptions(p, AnimationType.RightFinger, "Right Finger"));
			anims_.Add(new AnimationOptions(p, AnimationType.Blowjob, "Head"));
			anims_.Add(new AnimationOptions(p, AnimationType.Thrust, "Thrust"));
			anims_.Add(new AnimationOptions(p, AnimationType.Trib, "Trib"));
			anims_.Add(new AnimationOptions(p, AnimationType.Kiss, "Kiss"));
			anims_.Add(new AnimationOptions(p, AnimationType.LeftHandOnBreast, "Left hand on breast"));
			anims_.Add(new AnimationOptions(p, AnimationType.RightHandOnBreast, "Right hand on breast"));
			anims_.Add(new AnimationOptions(p, AnimationType.LeftHandOnChest, "Left hand on chest"));
			anims_.Add(new AnimationOptions(p, AnimationType.RightHandOnChest, "Right hand on chest"));
		}

		public void Init()
		{
			if (person_.Body.Exists)
			{
				maxExcitementParam_ = Cue.Instance.Sys.RegisterFloatParameter(
					$"{person_.ID}.MaxExcitement", OnMaxExcitementParam, maxExcitement_,
					0, 1);

				idlePoseParam_ = Cue.Instance.Sys.RegisterBoolParameter(
					$"{person_.ID}.IdlePose", OnIdlePoseParam, idlePose_);

				excitedPoseParam_ = Cue.Instance.Sys.RegisterBoolParameter(
					$"{person_.ID}.ExcitedPose", OnExcitedPoseParam, excitedPose_);
			}
		}

		private void OnMaxExcitementParam(float f)
		{
			MaxExcitement = U.Clamp(f, 0, 1);
		}

		private void OnIdlePoseParam(bool b)
		{
			IdlePose = b;
		}

		private void OnExcitedPoseParam(bool b)
		{
			ExcitedPose = b;
		}

		public void Load(JSONClass o)
		{
			J.OptFloat(o, "maxExcitement", ref maxExcitement_);
			J.OptBool(o, "idlePose", ref idlePose_);
			J.OptBool(o, "excitedPose", ref excitedPose_);

			foreach (var a in anims_)
				a.Load(o);

			if (maxExcitementParam_ != null)
				maxExcitementParam_.Value = maxExcitement_;

			if (idlePoseParam_ != null)
				idlePoseParam_.Value = idlePose_;

			if (excitedPoseParam_ != null)
				excitedPoseParam_.Value = excitedPose_;
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
			get
			{
				return maxExcitement_;
			}

			set
			{
				if (maxExcitement_ != value)
				{
					maxExcitement_ = value;

					if (maxExcitementParam_ != null)
						maxExcitementParam_.Value = maxExcitement_;

					OnChange();
				}
			}
		}

		public bool IdlePose
		{
			get
			{
				return idlePose_;
			}

			set
			{
				if (idlePose_ != value)
				{
					idlePose_ = value;

					if (idlePoseParam_ != null)
						idlePoseParam_.Value = idlePose_;

					OnChange();
				}
			}
		}

		public bool ExcitedPose
		{
			get
			{
				return excitedPose_;
			}

			set
			{
				if (excitedPose_ != value)
				{
					excitedPose_ = value;

					if (excitedPoseParam_ != null)
						excitedPoseParam_.Value = excitedPose_;

					OnChange();
				}
			}
		}

		public List<AnimationOptions> GetAnimationOptions()
		{
			return anims_;
		}

		public AnimationOptions GetAnimationOption(AnimationType type)
		{
			for (int i = 0; i < anims_.Count; ++i)
			{
				if (anims_[i].Type == type)
					return anims_[i];
			}

			return null;
		}

		public void Trigger(AnimationType type, bool on)
		{
			var a = GetAnimationOption(type);
			if (a == null)
				return;

			if (on)
				a.TriggerOn.Activate();
			else
				a.TriggerOff.Activate();
		}

		private void OnChange()
		{
			Cue.Instance.Save();
		}
	}
}
