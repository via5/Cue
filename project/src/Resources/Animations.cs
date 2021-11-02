using SimpleJSON;
using System;
using System.Collections.Generic;

namespace Cue
{
	class AnimationResources
	{
		private Logger log_;
		private readonly List<Animation> anims_ = new List<Animation>();

		public AnimationResources()
		{
			log_ = new Logger(Logger.Resources, "AnimRes");
		}

		public bool Load()
		{
			try
			{
				anims_.Clear();
				LoadFromFile();
				return true;
			}
			catch (Exception e)
			{
				log_.Error("failed to load animations, " + e.ToString());
				return false;
			}
		}

		private void LoadFromFile()
		{
			var meta = Cue.Instance.Sys.GetResourcePath("animations.json");
			var doc = JSON.Parse(Cue.Instance.Sys.ReadFileIntoString(meta));

			if (doc == null)
			{
				log_.Error("failed to parse animations");
				return;
			}

			foreach (var an in doc.AsObject["animations"].AsArray.Childs)
			{
				var a = ParseAnimation(an.AsObject);

				if (a != null)
					Add(a);
			}
		}

		private Animation ParseAnimation(JSONClass o)
		{
			IAnimation a = CreateIntegrationAnimation(o);
			if (a == null)
				return null;

			if (o.HasKey("enabled") && !o["enabled"].AsBool)
				return null;

			if (!o.HasKey("animation"))
			{
				log_.Error("object missing 'animation'");
				return null;
			}

			int type = Animations.FromString(o["animation"].Value);
			if (type == Animations.None)
			{
				log_.Error($"bad animation type '{o["animation"].Value}'");
				return null;
			}

			int ms = MovementStyles.Any;
			if (o.HasKey("sex"))
				ms = MovementStyles.FromString(o["sex"]);
			else if (o.HasKey("style"))
				ms = MovementStyles.FromString(o["style"]);

			return new Animation(type, ms, a);
		}

		private IAnimation CreateIntegrationAnimation(JSONClass o)
		{
			if (!o.HasKey("type"))
			{
				log_.Error("object missing 'type'");
				return null;
			}

			string type = o["type"];

			JSONClass options;

			if (o.HasKey("options"))
				options = o["options"].AsObject;
			else
				options = new JSONClass();

			IAnimation a = null;

			if (type == "bvh")
				a = BVH.Animation.Create(options);
			else if (type == "timeline")
				a = TimelineAnimation.Create(options);
			else if (type == "synergy")
				a = SynergyAnimation.Create(options);
			else if (type == "proc")
				a = Proc.ProcAnimation.Create(options);
			else if (type == "internal")
				a = BuiltinAnimations.Get(options["name"].Value);
			else
				log_.Error($"unknown animation type '{type}'");

			return a;
		}

		public Animation GetAny(int type, int style)
		{
			for (int i = 0; i < anims_.Count; ++i)
			{
				if (anims_[i].Type == type)
				{
					if (MovementStyles.Match(anims_[i].MovementStyle, style))
						return anims_[i];
				}
			}

			return null;
		}

		public List<Animation> GetAll()
		{
			return GetAll(Animations.None, MovementStyles.Any);
		}

		public List<Animation> GetAll(int type, int style)
		{
			if (type == Animations.None && style == MovementStyles.Any)
				return anims_;

			var list = new List<Animation>();

			for (int i = 0; i < anims_.Count; ++i)
			{
				if (anims_[i].Type == type)
				{
					if (style == MovementStyles.Any ||
						MovementStyles.Match(anims_[i].MovementStyle, style))
					{
						list.Add(anims_[i]);
					}
				}
			}

			return list;
		}

		private void Add(Animation a)
		{
			log_.Info(a.ToString());
			anims_.Add(a);
		}
	}
}
