using System;

namespace Cue
{
	class MacGruberBreather : IBreather
	{
		private Person person_;
		private W.VamFloatParameter intensity_;
		private float lastIntensity_ = 0;

		public MacGruberBreather(Person p)
		{
			person_ = p;
			intensity_ = new W.VamFloatParameter(p, "MacGruber.Breathing", "Intensity");
		}

		public float Intensity
		{
			get
			{
				lastIntensity_ = intensity_.GetValue();
				return lastIntensity_;
			}

			set
			{
				intensity_.SetValue(value);
				lastIntensity_ = value;
			}
		}

		// not supported
		public float Speed
		{
			get { return 0; }
			set { }
		}

		public override string ToString()
		{
			return $"MacGruber: intensity={lastIntensity_:0.000} speed=n/a";
		}
	}


	class MacGruberOrgasmer : IOrgasmer
	{
		private Person person_ = null;
		private W.VamActionParameter action_;

		public MacGruberOrgasmer(Person p)
		{
			person_ = p;
			action_ = new W.VamActionParameter(p, "MacGruber.Breathing", "QueueOrgasm");
		}

		public void Orgasm()
		{
			action_.Fire();
		}
	}


	abstract class BasicGazer : IGazer
	{
		protected Person person_ = null;

		public abstract bool Blink { get; set; }

		protected BasicGazer(Person p)
		{
			person_ = p;
		}

		public abstract void LookAt(IObject o);
		public abstract void LookAt(Vector3 p);
		public abstract void LookInFront();
		public abstract void LookAtNothing();

		public abstract void Update(float s);
	}


	class MacGruberGaze : BasicGazer
	{
		private W.VamBoolParameter toggle_;
		private W.VamBoolParameter lookatTarget_;
		private VamEyes eyes_ = null;
		private IObject object_ = null;

		public MacGruberGaze(Person p)
			: base(p)
		{
			eyes_ = new VamEyes(p);
			toggle_ = new W.VamBoolParameter(p, "MacGruber.Gaze", "enabled");
			lookatTarget_ = new W.VamBoolParameter(p, "MacGruber.Gaze", "LookAt EyeTarget");
		}

		public override bool Blink
		{
			get { return eyes_.Blink; }
			set { eyes_.Blink = value; }
		}

		public override void LookAt(IObject o)
		{
			object_ = o;
			toggle_.SetValue(true);
			lookatTarget_.SetValue(true);
			eyes_.LookAt = GazeSettings.LookAtTarget;
			eyes_.Target = object_.Atom.HeadPosition;
		}

		public override void LookAt(Vector3 p)
		{
			object_ = null;
			toggle_.SetValue(true);
			lookatTarget_.SetValue(true);
			eyes_.LookAt = GazeSettings.LookAtTarget;
			eyes_.Target = p;
		}

		public override void LookInFront()
		{
			object_ = null;
			toggle_.SetValue(false);
			lookatTarget_.SetValue(false);
			eyes_.LookAt = GazeSettings.LookAtDisabled;
			eyes_.Target =
				person_.HeadPosition +
				Vector3.Rotate(new Vector3(0, 0, 1), person_.Bearing);
		}

		public override void LookAtNothing()
		{
			object_ = null;
			toggle_.SetValue(false);
			lookatTarget_.SetValue(false);
			eyes_.LookAt = GazeSettings.LookAtDisabled;
		}

		public override void Update(float s)
		{
			if (object_ != null)
				eyes_.Target = object_.Atom.HeadPosition;

			eyes_.Update(s);
		}

		public override string ToString()
		{
			return
				$"MacGruber: " +
				$"o={object_} gaze={toggle_.GetValue()} " +
				$"gazeTarget={lookatTarget_.GetValue()}" +
				$"eyes=${eyes_}";
		}
	}
}
