using System;
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
		IInput Input { get; }
		bool Paused { get; }
		void OnPluginState(bool b);
		void OnReady(Action f);
		string ReadFileIntoString(string path);
		string GetResourcePath(string path);
		void HardReset();
		void ReloadPlugin();
		bool IsPlayMode { get; }
		bool IsVR { get; }
		float RealtimeSinceStartup { get; }
		int RandomInt(int first, int last);
		float RandomFloat(float first, float last);
		VUI.Root CreateHud(Vector3 offset, Point pos, Size size);
		VUI.Root CreateAttached(Vector3 offset, Point pos, Size size);
		VUI.Root Create2D(float topOffset, Size size);
		VUI.Root CreateScriptUI();
		IBoxGraphic CreateBoxGraphic(Vector3 pos);
	}

	interface IInput
	{
		bool HardReset { get; }
		bool ReloadPlugin { get; }
		bool ToggleMenu { get; }
		bool ToggleControls { get; }
		bool Select { get; }
		bool Action { get; }

		void Update();
		IObject GetHovered();
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

	interface ITriggers
	{
		bool Lip { get; }
		bool Mouth { get; }
		bool LeftBreast { get; }
		bool RightBreast { get; }
		bool Labia { get; }
		bool Vagina { get; }
		bool DeepVagina { get; }
		bool DeeperVagina { get; }
	}

	interface IAtom
	{
		string ID { get; }
		bool IsPerson { get; }
		int Sex { get; }
		ITriggers Triggers { get; }

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

	interface IBoxGraphic
	{
		bool Visible { get; set; }
		Vector3 Position { get; set; }
		Color Color { get; set; }
		void Destroy();
	}
}
