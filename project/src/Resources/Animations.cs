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
				LoadBuiltin();

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

			int type = Animation.TypeFromString(o["animation"]);
			if (type == Animation.NoType)
				return null;

			int from = PersonState.None;
			int to = PersonState.None;
			int state = PersonState.None;

			switch (type)
			{
				case Animation.WalkType:
				case Animation.TurnLeftType:
				case Animation.TurnRightType:
				{
					break;
				}

				case Animation.TransitionType:
				{
					if (!o.HasKey("from"))
					{
						log_.Error("transition animation missing 'from");
						return null;
					}

					if (!o.HasKey("to"))
					{
						log_.Error("transition animation missing 'from");
						return null;
					}

					from = PersonState.StateFromString(o["from"]);
					to = PersonState.StateFromString(o["to"]);

					break;
				}

				case Animation.SexType:
				case Animation.IdleType:
				{
					if (!o.HasKey("state"))
					{
						log_.Error("sex animation missing 'state'");
						return null;
					}

					state = PersonState.StateFromString(o["state"]);
					break;
				}
			}

			int ms = MovementStyles.Any;
			if (o.HasKey("sex"))
				ms = MovementStyles.FromString(o["sex"]);
			else if (o.HasKey("style"))
				ms = MovementStyles.FromString(o["style"]);

			return new Animation(type, from, to, state, ms, a);
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
			else
				log_.Error($"unknown animation type '{type}'");

			return a;
		}

		private void LoadBuiltin()
		{
			foreach (var a in Proc.BuiltinAnimations.Get())
			{
				if (a != null)
					Add(a);
			}
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

		public List<Animation> GetAll(int type, int style)
		{
			if (type == Animation.NoType)
				return anims_;

			var list = new List<Animation>();

			for (int i = 0; i < anims_.Count; ++i)
			{
				if (anims_[i].Type == type)
				{
					if (MovementStyles.Match(anims_[i].MovementStyle, style))
						list.Add(anims_[i]);
				}
			}

			return list;
		}

		public List<Animation> GetAllIdles(int state, int style)
		{
			var list = new List<Animation>();

			for (int i = 0; i < anims_.Count; ++i)
			{
				if (anims_[i].Type == Animation.IdleType)
				{
					if (anims_[i].State == state)
					{
						if (MovementStyles.Match(anims_[i].MovementStyle, style))
							list.Add(anims_[i]);
					}
				}
			}

			return list;
		}

		public Animation GetAnyTransition(int from, int to, int style)
		{
			for (int i = 0; i < anims_.Count; ++i)
			{
				if (anims_[i].Type == Animation.TransitionType)
				{
					if (anims_[i].TransitionFrom == from &&
						anims_[i].TransitionTo == to)
{
						if (MovementStyles.Match(anims_[i].MovementStyle, style))
							return anims_[i];
					}
				}
			}

			return null;
		}

		public Animation GetAnySex(int state, int style)
		{
			for (int i = 0; i < anims_.Count; ++i)
			{
				if (anims_[i].Type == Animation.SexType)
				{
					if (anims_[i].State == state || anims_[i].State == PersonState.None)
					{
						if (MovementStyles.Match(anims_[i].MovementStyle, style))
							return anims_[i];
					}
				}
			}

			return null;
		}

		private void Add(Animation a)
		{
			log_.Info(a.ToString());
			anims_.Add(a);
		}
	}
}
