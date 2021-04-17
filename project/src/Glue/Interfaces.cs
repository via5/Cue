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
		string GetResourcePath(string path);
		void ReloadPlugin();
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
		int Sex { get; }

		Vector3 Position { get; set; }
		Vector3 Direction { get; set; }
		Vector3 HeadPosition { get; }

		void SetDefaultControls();

		void OnPluginState(bool b);

		void Update(float s);

		bool NavEnabled { get; set; }
		bool NavPaused { get; set; }
		bool NavActive { get; }
		void NavTo(Vector3 v, float bearing);
		void NavStop();
	}

	interface INav
	{
		void Update();
		List<Vector3> Calculate(Vector3 from, Vector3 to);
		bool Render { get; set; }
	}
}
