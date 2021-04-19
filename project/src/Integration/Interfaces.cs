namespace Cue
{
	class GazeSettings
	{
		public const int LookAtDisabled = 0;
		public const int LookAtTarget = 1;
		public const int LookAtPlayer = 2;
	}


	interface IPlayer
	{
		bool Playing { get; }
		bool Play(IAnimation a, int flags);
		void Stop(bool rewind);
		void FixedUpdate(float s);
		void Update(float s);
	}

	interface IAnimation
	{
		int Sex { get; set; }
	}

	abstract class BasicAnimation : IAnimation
	{
		private int sex_ = Sexes.Any;

		public int Sex
		{
			get
			{
				return sex_;
			}

			set
			{
				sex_ = value;
			}
		}
	}


	interface IBreather
	{
		float Intensity { get; set; }
		float Speed { get; set; }
	}

	interface IGazer
	{
		int LookAt { get; set; }
		Vector3 Target { get; set; }
		void LookInFront();
		void Update(float s);
	}

	interface ISpeaker
	{
		void Say(string s);
	}

	interface IKisser
	{
		void Update(float s);
	}

	interface IHandjob
	{
		bool Active { get; set; }
		Person Target { get; set; }
	}

	interface IClothing
	{
		bool GenitalsVisible { get; set; }
		bool BreastsVisible { get; set; }
		void OnPluginState(bool b);
		void Dump();
	}
}
