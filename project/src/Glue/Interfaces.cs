namespace Cue.W
{
	interface ISys
	{
		ITime Time { get; }
		ILog Log { get; }
		IAtom GetAtom(string id);
		IAtom ContainingAtom { get; }
		bool Paused { get; }
	}

	interface ITime
	{
		float deltaTime { get; }
	}

	interface ILog
	{
		void Verbose(string s);
		void Info(string s);
		void Error(string s);
	}

	interface IAtom
	{
		bool IsPerson { get; }
		Vector3 Position { get; set; }
		Vector3 Direction { get; set; }
	}
}
