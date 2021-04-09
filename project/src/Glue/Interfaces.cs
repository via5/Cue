using System;
using System.Collections;
using System.Collections.Generic;

namespace Cue.W
{
	interface ISys
	{
		ITime Time { get; }
		ILog Log { get; }
		IAtom GetAtom(string id);
		List<IAtom> GetAtoms(bool alsoOff=false);
		IAtom ContainingAtom { get; }
		INav Nav { get; }
		bool Paused { get; }
		void OnPluginState(bool b);
		void OnReady(Action f);
		string ReadFileIntoString(string path);
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
		string ID { get; }
		bool IsPerson { get; }
		Vector3 Position { get; set; }
		Vector3 Direction { get; set; }
		Vector3 HeadPosition { get; }
	}

	interface INav
	{
		void Update();
		List<Vector3> Calculate(Vector3 from, Vector3 to);
		bool Render { get; set; }
	}
}
