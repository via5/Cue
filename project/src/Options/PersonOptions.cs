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
		private List<AnimationOptions> anims_ = new List<AnimationOptions>();

		private readonly FloatOption maxExcitement_;
		private readonly BoolOption idlePose_;
		private readonly BoolOption excitedPose_;
		private readonly FloatOption sweatMultiplier_;
		private readonly FloatOption flushMultiplier_;
		private readonly FloatOption hairLooseMultiplier_;

		private readonly Dictionary<string, BoolOption> bools_ =
			new Dictionary<string, BoolOption>();

		private readonly Dictionary<string, FloatOption> floats_ =
			new Dictionary<string, FloatOption>();


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

			maxExcitement_ = AddFloat("maxExcitement", 1.0f, 0.0f, 1.0f);
			idlePose_ = AddBool("idlePose", true);
			excitedPose_ = AddBool("excitedPose", true);

			sweatMultiplier_ = AddFloat("sweatMultiplier", 1.0f, 0.0f, 1.0f);
			flushMultiplier_ = AddFloat("flushMultiplier", 1.0f, 0.0f, 5.0f);
			hairLooseMultiplier_ = AddFloat("hairLooseMultiplier", 1.0f, 0.0f, 5.0f);
		}

		private BoolOption AddBool(string name, bool init, string paramName = null)
		{
			if (paramName == null)
				paramName = name;

			var o = new BoolOption($"{person_.ID}.{paramName}", init);
			o.Changed += (b) => OnChanged();
			bools_.Add(name, o);
			return o;
		}

		private FloatOption AddFloat(string name, float init, float min, float max, string paramName = null)
		{
			if (paramName == null)
				paramName = name;

			var o = new FloatOption($"{person_.ID}.{paramName}", init, min, max);
			o.Changed += (f) => OnChanged();
			floats_.Add(name, o);
			return o;
		}

		public void Load(JSONClass o)
		{
			foreach (var bo in bools_)
			{
				bool b = false;
				if (J.OptBool(o, bo.Key, ref b))
					bo.Value.Value = b;
			}

			foreach (var fo in floats_)
			{
				float f = 0;
				if (J.OptFloat(o, fo.Key, ref f))
					fo.Value.Value = f;
			}

			foreach (var a in anims_)
				a.Load(o);
		}

		public void Save(JSONClass o)
		{
			foreach (var bo in bools_)
				o[bo.Key] = new JSONData(bo.Value.Value);

			foreach (var fo in floats_)
				o[fo.Key] = new JSONData(fo.Value.Value);

			foreach (var a in anims_)
				a.Save(o);
		}

		public float MaxExcitement
		{
			get { return maxExcitement_.Value; }
			set { maxExcitement_.Value = value; }
		}

		public FloatOption MaxExcitementOption
		{
			get { return maxExcitement_; }
		}

		public bool IdlePose
		{
			get { return idlePose_.Value; }
			set { idlePose_.Value = value; }
		}

		public bool ExcitedPose
		{
			get { return excitedPose_.Value; }
			set { excitedPose_.Value = value; }
		}

		public float SweatMultiplier
		{
			get { return sweatMultiplier_.Value; }
			set { sweatMultiplier_.Value = value; }
		}

		public FloatOption SweatMultiplierOption
		{
			get { return sweatMultiplier_; }
		}

		public float FlushMultiplier
		{
			get { return flushMultiplier_.Value; }
			set { flushMultiplier_.Value = value; }
		}

		public FloatOption FlushMultiplierOption
		{
			get { return flushMultiplier_; }
		}

		public float HairLooseMultiplier
		{
			get { return hairLooseMultiplier_.Value; }
			set { hairLooseMultiplier_.Value = value; }
		}

		public FloatOption HairLooseMultiplierOption
		{
			get { return hairLooseMultiplier_; }
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

		private void OnChanged()
		{
			Cue.Instance.Save();
		}
	}
}
