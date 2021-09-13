#if MOCK

using SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;

namespace Cue.Sys.Mock
{
	class MockGraphic : IGraphic
	{
		public bool Visible { get; set; }
		public Vector3 Position { get; set; }
		public Quaternion Rotation { get; set; }
		public Vector3 Size { get; set; }
		public Color Color { get; set; }
		public bool Collision { get; set; }

		public void Destroy()
		{
		}
	}


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

		public JSONClass GetConfig()
		{
			return null;
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

		public Vector3 CameraPosition
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

		public bool HasUI
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

		public string Fps
		{
			get { return "fps"; }
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

		public void Update(float s)
		{
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
			return File.ReadAllText(path);
		}

		private string RootPath()
		{
			string exeDir;
			var exe = System.Reflection.Assembly.GetEntryAssembly()?.Location;

			if (exe == null)
			{
				exeDir = AppDomain.CurrentDomain?.BaseDirectory;

				if (exeDir == null)
					exeDir = Directory.GetCurrentDirectory();
			}
			else
			{
				exeDir = Path.GetDirectoryName(exe);
			}

			return Path.GetFullPath(Path.Combine(exeDir, "../../.."));
		}

		private string ResPath()
		{
			return Path.Combine(RootPath(), "res");
		}

		public string GetResourcePath(string path)
		{
			return Path.Combine(ResPath(), path);
		}

		public IObjectCreator CreateObjectCreator(string name, string type, JSONClass opts)
		{
			return null;
		}

		public IGraphic CreateBoxGraphic(string name, Box box, Color c)
		{
			return CreateBoxGraphic(name, box.center, box.size, c);
		}

		public IGraphic CreateBoxGraphic(string name, Vector3 pos, Vector3 size, Color c)
		{
			return new MockGraphic();
		}

		public IGraphic CreateSphereGraphic(string name, Vector3 pos, float radius, Color c)
		{
			return new MockGraphic();
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

		public bool MenuUp { get { return false; } }
		public bool MenuDown { get { return false; } }
		public bool MenuLeft { get { return false; } }
		public bool MenuRight { get { return false; } }
		public bool MenuSelect { get { return false; } }

		public bool Move { get { return false; } }
		public Vector3 MoveDirection { get { return Vector3.Zero; } }

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

		public bool Visible
		{
			get { return true; }
			set { }
		}

		public bool IsPerson
		{
			get { return true; }
		}

		public bool IsMale
		{
			get { return true; }
		}

		public bool Selected
		{
			get { return false; }
		}

		public IClothing Clothing
		{
			get { return null; }
		}

		public IBody Body
		{
			get { return null; }
		}

		public IHair Hair
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

		public Quaternion Rotation
		{
			get { return Quaternion.Zero; }
			set { }
		}

		public bool Collisions
		{
			get { return false; }
			set { }
		}

		public bool Physics
		{
			get { return false; }
			set { }
		}

		public bool Hidden
		{
			get { return false; }
			set { }
		}

		public float Scale
		{
			get { return 1; }
			set { }
		}

		public void Init()
		{
		}

		public void Destroy()
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

		public void SetParentLink(IBodyPart bp)
		{
		}

		public void SetBodyDamping(int e)
		{
		}

		public void OnPluginState(bool b)
		{
		}

		public void Update(float s)
		{
		}

		public IMorph GetMorph(string id)
		{
			return null;
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

#endif
