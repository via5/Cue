using System;
using System.Collections;
using System.Collections.Generic;

namespace Cue.W
{
	interface ISys
	{
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

	interface ILog
	{
		void Clear();
		void Verbose(string s);
		void Info(string s);
		void Error(string s);
	}

	class NavStates
	{
		public const int None = 0;
		public const int Moving = 1;
		public const int TurningLeft = 2;
		public const int TurningRight = 3;
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
		void TeleportTo(Vector3 p, float bearing);

		bool NavEnabled { get; set; }
		bool NavPaused { get; set; }
		int NavState { get; }
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
