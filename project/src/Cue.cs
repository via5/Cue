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
		private List<ObjectControls> controls_ = new List<ObjectControls>();
		private Transform root_ = null;
		private GameObject fullPanel_ = null;
		private GameObject rootPanel_ = null;
		private VUI.Root vroot_ = null;

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

		int i = 0;

		public void Update()
		{
			if (!enabled_)
				return;

			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;

			bool b = Physics.Raycast(ray, out hit, float.MaxValue, 1 << Layer);

			for (int i = 0; i < controls_.Count; ++i)
				controls_[i].Hovered = (b && controls_[i].Is(hit.transform));

			++i;
			if (i == 10)
			{
				vroot_ = new VUI.Root(rootPanel_.transform);
				vroot_.ContentPanel.Layout = new VUI.BorderLayout();
				vroot_.ContentPanel.Add(new VUI.Label("tesT"), VUI.BorderLayout.Top);
			}

			if (vroot_ != null)
				vroot_.DoLayoutIfNeeded();
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
			{
				fullPanel_ = new GameObject();
				fullPanel_.transform.SetParent(root_, false);
				var canvas = fullPanel_.AddComponent<Canvas>();
				var cr = fullPanel_.AddComponent<CanvasRenderer>();
				var bg = fullPanel_.AddComponent<Image>();
				var rt = fullPanel_.AddComponent<RectTransform>();
				if (rt == null)
					rt = fullPanel_.GetComponent<RectTransform>();

				canvas.renderMode = RenderMode.ScreenSpaceOverlay;

				//VUI.Utilities.SetRectTransform(rt, new VUI.Rectangle(
				//	-1f, -0.5f, new VUI.Size(0.5f, 0.1f)));

				rt.offsetMin = new Vector2(2000, 2000);
				rt.offsetMax = new Vector2(2000f, 2000);
				rt.anchorMin = new Vector2(0, 0);
				rt.anchorMax = new Vector2(1, 1);
				rt.anchoredPosition = new Vector2(0.5f, -0.5f);
				bg.color = new Color(1, 1, 1, 0);


				rootPanel_ = new GameObject();
				rootPanel_.transform.SetParent(fullPanel_.transform, false);
				bg = rootPanel_.AddComponent<Image>();
				rt = rootPanel_.AddComponent<RectTransform>();
				if (rt == null)
					rt = rootPanel_.GetComponent<RectTransform>();

				bg.color = new Color(1, 0, 0, 0.5f);

				rt.offsetMin = new Vector2(-100, 0f);
				rt.offsetMax = new Vector2(100, 100);
				rt.anchorMin = new Vector2(0.5f, 1);
				rt.anchorMax = new Vector2(0.5f, 1);
				rt.anchoredPosition = new Vector2(
					(rt.offsetMax.x - rt.offsetMin.x) / 2,
					-(rt.offsetMax.y - rt.offsetMin.y) / 2);
			}
			else
			{
				UnityEngine.Object.Destroy(fullPanel_);
				fullPanel_ = null;
			}
		}
	}



	class Cue : MVRScript
	{
		private static Cue instance_ = null;
		private W.ISys sys_ = null;
		private BasicObject player_ = null;
		private readonly List<Person> persons_ = new List<Person>();
		private readonly List<IObject> objects_ = new List<IObject>();
		private UI.IUI ui_ = null;
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
					ui_ = new UI.MockUI();
				}
				else
				{
					sys_ = new W.VamSys(this);
					ui_ = new UI.VamUI();
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

				ui_.Init();
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
				ui_.Update();
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
