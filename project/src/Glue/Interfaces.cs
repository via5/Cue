using System.Collections.Generic;

namespace Cue.W
{
	interface ISys
	{
		ITime Time { get; }
		ILog Log { get; }
		IAtom GetAtom(string id);
		IAtom ContainingAtom { get; }
		INav Nav { get; }
		bool Paused { get; }
		void OnPluginState(bool b);
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

	interface INav
	{
		void AddBox(float x, float z, float w, float h);
		List<Vector3> Calculate(Vector3 from, Vector3 to);
	}
}
