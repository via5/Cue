using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace Cue
{
	class Controls
	{
		public const int Layer = 21;

		class ObjectControls
		{
			private IObject object_ = null;
			private GameObject control_ = null;
			private Material material_ = null;
			private bool hovered_ = false;

			public ObjectControls(IObject o)
			{
				object_ = o;
			}

			public void Create()
			{
				if (control_ != null)
					return;

				control_ = GameObject.CreatePrimitive(PrimitiveType.Cube);
				control_.layer = Layer;

				foreach (var collider in control_.GetComponents<Collider>())
				{
					//collider.enabled = false;
					//UnityEngine.Object.Destroy(collider);
				}

				material_ = new Material(Shader.Find("Battlehub/RTGizmos/Handles"));

				material_.color = new Color(0, 0, 1, 0.5f);
				material_.SetFloat("_Offset", 1f);
				material_.SetFloat("_MinAlpha", 1f);

				var r = control_.GetComponent<Renderer>();
				r.material = material_;

				control_.transform.localScale =
					new UnityEngine.Vector3(0.5f, 0.05f, 0.5f);

				control_.transform.position = Vector3.ToUnity(object_.Position);
				UpdateColor();
			}

			public bool Is(Transform t)
			{
				return (control_.transform == t);
			}

			public bool Hovered
			{
				get { return hovered_; }
				set { hovered_ = value; UpdateColor(); }
			}

			public void Destroy()
			{
				if (control_ == null)
					return;

				UnityEngine.Object.Destroy(control_);
				control_ = null;
			}

			private void UpdateColor()
			{
				if (hovered_)
					material_.color = new Color(0, 1, 0, 0.5f);
				else
					material_.color = new Color(0, 0, 1, 0.5f);
			}
		}

		private bool enabled_ = true;
		private Hud hud_ = new Hud();
		private List<ObjectControls> controls_ = new List<ObjectControls>();
		private Transform root_ = null;

		public bool Enabled
		{
			get { return enabled_; }
			set { enabled_ = value; Check();  }
		}

		public void Create(Transform r, List<IObject> objects)
		{
			root_ = r;

			foreach (var o in objects)
				controls_.Add(new ObjectControls(o));

			Check();
		}

		public void Update()
		{
			if (!enabled_)
				return;

			CheckHovered();
			hud_.Update();
		}

		private void CheckHovered()
		{
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

			RaycastHit hit;
			bool b = Physics.Raycast(ray, out hit, float.MaxValue, 1 << Layer);

			for (int i = 0; i < controls_.Count; ++i)
				controls_[i].Hovered = (b && controls_[i].Is(hit.transform));
		}

		private void Check()
		{
			foreach (var c in controls_)
			{
				if (enabled_)
					c.Create();
				else
					c.Destroy();
			}

			if (enabled_)
				hud_.Create(root_);
			else
				hud_.Destroy();
		}
	}



	class Cue : MVRScript
	{
		private static Cue instance_ = null;
		private W.ISys sys_ = null;
		private BasicObject player_ = null;
		private readonly List<Person> persons_ = new List<Person>();
		private readonly List<IObject> objects_ = new List<IObject>();
		private UI.ScriptUI sui_ = null;
		private Controls controls_ = new Controls();

		public Cue()
		{
			instance_ = this;
		}

		public static Cue Instance
		{
			get { return instance_; }
		}

		public W.ISys Sys
		{
			get { return sys_; }
		}

		public List<IObject> Objects
		{
			get { return objects_; }
		}

		public List<Person> Persons
		{
			get { return persons_; }
		}

		public override void Init()
		{
			base.Init();

			try
			{
				if (W.MockSys.Instance != null)
				{
					sys_ = W.MockSys.Instance;
				}
				else
				{
					sys_ = new W.VamSys(this);
					sui_ = new UI.ScriptUI();
				}

				sys_.OnReady(DoInit);
			}
			catch (Exception e)
			{
				LogError(e.Message);
			}
		}

		private void DoInit()
		{
			U.Safe(() =>
			{
				var re = new Regex(@"cue!([a-zA-Z]+)#?.*");

				foreach (var a in sys_.GetAtoms())
				{
					var m = re.Match(a.ID);

					if (m != null && m.Success)
					{
						string type = m.Groups[1].Value;
						//LogError("found " + a.ID + " " + type);

						BasicObject o = new BasicObject(a);
						var s = new Slot();

						if (type == "sit")
							o.SitSlot = s;
						if (type == "stand")
							o.StandSlot = s;

						objects_.Add(o);
					}
				}

				if (sui_ != null)
					sui_.Init();

				sys_.Nav.Update();

				persons_.Add(new Person(sys_.GetAtom("Person")));
				player_ = new BasicObject(sys_.GetAtom("Player"));

				for (int i = 0; i < persons_.Count; ++i)
					persons_[i].OnPluginState(true);

				player_.OnPluginState(true);

				controls_.Create(SuperController.singleton.mainMenuUI.root, objects_);
			});
		}

		//float ss = 0;

		public void Update()
		{
			//ss += Time.deltaTime;
			//if (ss > 2)
			//	controls_.Enabled = false;

			U.Safe(() =>
			{
				if (!sys_.Paused)
				{
					for (int i = 0; i < persons_.Count; ++i)
						persons_[i].Update(sys_.Time.deltaTime);
				}

				controls_.Update();

				if (sui_ != null)
					sui_.Update();
			});
		}

		public void FixedUpdate()
		{
			U.Safe(() =>
			{
				if (sys_.Paused)
					return;

				for (int i = 0; i < persons_.Count; ++i)
					persons_[i].FixedUpdate(sys_.Time.deltaTime);
			});
		}

		public void OnEnable()
		{
			U.Safe(() =>
			{
				sys_.OnPluginState(true);
				controls_.Enabled = true;

				for (int i = 0; i < persons_.Count; ++i)
					persons_[i].OnPluginState(true);
			});
		}

		public void OnDisable()
		{
			U.Safe(() =>
			{
				sys_.OnPluginState(false);
				controls_.Enabled = false;

				for (int i = 0; i < persons_.Count; ++i)
					persons_[i].OnPluginState(false);
			});
		}

		static public void LogVerbose(string s)
		{
			//Instance.sys_.Log.Verbose(s);
		}

		static public void LogInfo(string s)
		{
			Instance.sys_.Log.Info(s);
		}

		static public void LogWarning(string s)
		{
			Instance.sys_.Log.Error(s);
		}

		static public void LogError(string s)
		{
			Instance.sys_.Log.Error(s);
		}
	}
}
