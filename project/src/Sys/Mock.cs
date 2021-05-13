using System;
using System.Collections.Generic;

namespace Cue.W
{
	class MockSys : ISys
	{
		private static MockSys instance_ = null;
		private readonly MockNav nav_ = new MockNav();
		private readonly MockInput input_ = new MockInput();

		public MockSys()
		{
			instance_ = this;
		}

		static public MockSys Instance
		{
			get { return instance_; }
		}

		public void Tick()
		{
		}

		public void ClearLog()
		{
		}

		public void Log(string s, int level)
		{
			foreach (var line in s.Split('\n'))
				Console.WriteLine("[" + LogLevels.ToShortString(level) + "] " + s);
		}

		public INav Nav
		{
			get { return nav_; }
		}

		public IInput Input
		{
			get { return input_; }
		}

		public IAtom ContainingAtom
		{
			get { return null; }
		}

		public Vector3 Camera
		{
			get { return Vector3.Zero; }
		}

		public IAtom GetAtom(string id)
		{
			return new MockAtom(id);
		}

		public List<IAtom> GetAtoms(bool alsoOff)
		{
			return new List<IAtom>();
		}

		public bool Paused
		{
			get { return false; }
		}

		public bool IsVR
		{
			get { return false; }
		}

		public bool IsPlayMode
		{
			get { return true; }
		}

		public float DeltaTime
		{
			get { return 0; }
		}

		public float RealtimeSinceStartup
		{
			get { return 0; }
		}

		public Vector3 InteractiveLeftHandPosition
		{
			get { return Vector3.Zero; }
		}

		public Vector3 InteractiveRightHandPosition
		{
			get { return Vector3.Zero; }
		}

		public int RandomInt(int first, int last)
		{
			return 0;
		}

		public float RandomFloat(float first, float last)
		{
			return 0;
		}

		public void OnPluginState(bool b)
		{
		}

		public void HardReset()
		{
		}

		public void ReloadPlugin()
		{
		}

		public void OnReady(Action f)
		{
			f?.Invoke();
		}

		public string ReadFileIntoString(string path)
		{
			return "";
		}

		public string GetResourcePath(string path)
		{
			return path;
		}

		public VUI.Root CreateHud(Vector3 offset, Point pos, Size size)
		{
			return null;
		}

		public VUI.Root CreateAttached(bool left, Vector3 offset, Point pos, Size size)
		{
			return null;
		}

		public VUI.Root Create2D(float topOffset, Size size)
		{
			return null;
		}

		public VUI.Root CreateScriptUI()
		{
			return null;
		}

		public IGraphic CreateBoxGraphic(string name, Vector3 pos, Vector3 size, Color c)
		{
			return null;
		}

		public IGraphic CreateSphereGraphic(string name, Vector3 pos, float radius, Color c)
		{
			return null;
		}
	}

	class MockInput : IInput
	{
		public bool HardReset { get { return false; } }
		public bool ReloadPlugin { get { return false; } }

		public bool ShowLeftMenu { get { return true; } }
		public bool LeftAction { get { return false; } }

		public bool ShowRightMenu { get { return true; } }
		public bool RightAction { get { return false; } }

		public bool Select { get { return false; } }
		public bool Action { get { return false; } }
		public bool ToggleControls { get { return false; } }

		public void Update(float s)
		{
		}

		public HoveredInfo GetLeftHovered()
		{
			return HoveredInfo.None;
		}

		public HoveredInfo GetRightHovered()
		{
			return HoveredInfo.None;
		}

		public HoveredInfo GetMouseHovered()
		{
			return HoveredInfo.None;
		}
	}


	class MockAtom : IAtom
	{
		private string id_;

		public MockAtom(string id)
		{
			id_ = id;
		}

		public string ID
		{
			get { return id_; }
		}

		public bool IsPerson
		{
			get { return true; }
		}

		public int Sex
		{
			get { return Sexes.Any; }
		}

		public bool Selected
		{
			get { return false; }
		}

		public IClothing Clothing
		{
			get { return null; }
		}

		public bool Teleporting
		{
			get { return false; }
		}

		public bool Possessed
		{
			get { return false; }
		}

		public Vector3 Position
		{
			get { return Vector3.Zero; }
			set { }
		}

		public Vector3 Direction
		{
			get { return Vector3.Zero; }
			set { }
		}

		public void Init()
		{
		}

		public void Say(string s)
		{
			Cue.LogInfo(id_ + " says '" + s + "'");
		}

		public List<IBodyPart> GetBodyParts()
		{
			return new List<IBodyPart>();
		}

		public void SetDefaultControls(string why)
		{
		}

		public void OnPluginState(bool b)
		{
		}

		public void Update(float s)
		{
		}

		public void TeleportTo(Vector3 v, float bearing)
		{
		}

		public bool NavEnabled
		{
			get { return false; }
			set { }
		}

		public bool NavPaused
		{
			get { return false; }
			set { }
		}

		public int NavState
		{
			get { return NavStates.None; }
		}

		public void NavTo(Vector3 v, float bearing, float stoppingDistance)
		{
		}

		public void NavStop(string why)
		{
		}
	}

	class MockNav : INav
	{
		public void Update()
		{
		}

		public List<Vector3> Calculate(Vector3 from, Vector3 to)
		{
			return new List<Vector3>();
		}

		public bool Render
		{
			get { return false; }
			set { }
		}
	}
}
