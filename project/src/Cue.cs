﻿using GPUTools.Physics.Scripts.Behaviours;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
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

			public IObject Object
			{
				get { return object_; }
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
				set { hovered_ = value; }
			}

			public void Update()
			{
				UpdateColor();
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
				else if (object_.LockedBy != null)
					material_.color = new Color(1, 0, 0, 0.5f);
				else
					material_.color = new Color(0, 0, 1, 0.5f);
			}
		}

		private bool enabled_ = true;
		private List<ObjectControls> controls_ = new List<ObjectControls>();

		public bool Enabled
		{
			get { return enabled_; }
			set { enabled_ = value; Check();  }
		}

		public void Create(List<IObject> objects)
		{
			foreach (var o in objects)
				controls_.Add(new ObjectControls(o));

			Check();
		}

		public void Update()
		{
			if (!enabled_)
				return;

			for (int i = 0; i < controls_.Count; ++i)
				controls_[i].Update();

			if (SuperController.singleton.gameMode == SuperController.GameMode.Play)
			{
				CheckHovered();

				if (Input.GetMouseButtonUp(0))
				{
					Cue.Instance.Select(Cue.Instance.Hovered);
				}

				if (Input.GetMouseButtonUp(1))
				{
					var p = Cue.Instance.Selected as Person;
					if (p == null)
						return;

					var o = Cue.Instance.Hovered;
					if (o == null)
						return;

					p.InteractWith(o);
				}
			}
		}

		private void CheckHovered()
		{
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

			IObject sel = HitObject(ray);
			if (sel == null)
				sel = HitPerson(ray);

			Cue.Instance.Hover(sel);

			for (int i=0; i<controls_.Count; ++i)
				controls_[i].Hovered = (controls_[i].Object == sel);
		}

		private IObject HitObject(Ray ray)
		{
			RaycastHit hit;
			bool b = Physics.Raycast(ray, out hit, float.MaxValue, 1 << Layer);

			if (!b)
				return null;

			for (int i = 0; i < controls_.Count; ++i)
			{
				if (controls_[i].Is(hit.transform))
					return controls_[i].Object;
			}

			return null;
		}

		private IObject HitPerson(Ray ray)
		{
			var a = HitAtom(ray);
			if (a == null)
				return null;

			var ps = Cue.Instance.Persons;

			for (int i = 0; i < ps.Count; ++i)
			{
				if (((W.VamAtom)ps[i].Atom).Atom == a)
					return ps[i];
			}

			//if (((W.VamAtom)Cue.Instance.Player.Atom).Atom == a)
			//	return Cue.Instance.Player;

			return null;
		}

		private Atom HitAtom(Ray ray)
		{
			RaycastHit hit;
			bool b = Physics.Raycast(ray, out hit, float.MaxValue, 0x24000100);

			if (!b)
				return null;

			var fc = hit.transform.GetComponent<FreeControllerV3>();

			if (fc != null)
				return fc.containingAtom;

			var bone = hit.transform.GetComponent<DAZBone>();
			if (bone != null)
				return bone.containingAtom;

			var rb = hit.transform.GetComponent<Rigidbody>();
			var p = rb.transform;

			while (p != null)
			{
				var a = p.GetComponent<Atom>();
				if (a != null)
					return a;

				p = p.parent;
			}

			return null;
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
		}
	}



	class Cue : MVRScript
	{
		public delegate void ObjectHandler(IObject o);
		public event ObjectHandler SelectionChanged;
		public event ObjectHandler HoveredChanged;

		private static Cue instance_ = null;
		private W.ISys sys_ = null;
		//private Person player_ = null;
		private readonly List<Person> persons_ = new List<Person>();
		private readonly List<IObject> objects_ = new List<IObject>();
		private readonly List<IObject> allObjects_ = new List<IObject>();
		private UI.ScriptUI sui_ = null;
		private Hud hud_ = new Hud();
		private Controls controls_ = new Controls();
		private bool paused_ = false;

		private IObject hovered_ = null;
		private IObject sel_ = null;
		private int frame_ = 0;

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

		public List<IObject> AllObjects
		{
			get { return allObjects_; }
		}

		public List<IObject> Objects
		{
			get { return objects_; }
		}

		public List<Person> Persons
		{
			get { return persons_; }
		}

		//public Person Player
		//{
		//	get { return player_; }
		//}

		public IObject Selected
		{
			get { return sel_; }
		}

		public IObject Hovered
		{
			get { return hovered_; }
		}

		public void Select(IObject o)
		{
			if (sel_ != o)
			{
				sel_ = o;
				SelectionChanged?.Invoke(sel_);

				foreach (var p in persons_)
				{
					if (p == o)
					{
						p.Gaze.LookAt = GazeSettings.LookAtDisabled;
					}
					else if (o is Person)
					{
						p.Gaze.LookAt = GazeSettings.LookAtTarget;
						p.Gaze.Target = ((Person)o).HeadPosition;
					}
				}
			}
		}

		public void Hover(IObject o)
		{
			if (hovered_ != o)
			{
				hovered_ = o;
				HoveredChanged?.Invoke(hovered_);
			}
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
				frame_ = 0;

				var re = new Regex(@"cue!([a-zA-Z]+)#?.*");

				foreach (var a in sys_.GetAtoms())
				{
					if (a.IsPerson)
					{
						persons_.Add(new Person(a));
						((W.VamAtom)a).Atom.collisionEnabled = false;
					}
					else
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
				}

				if (sui_ != null)
					sui_.Init();

				sys_.Nav.Update();

				//player_ = new Person(sys_.GetAtom("Player"));
				((W.VamAtom)persons_[0].Atom).Atom.mainController.MoveControl(new UnityEngine.Vector3(0, 0, 0));
				((W.VamAtom)persons_[1].Atom).Atom.mainController.MoveControl(new UnityEngine.Vector3(1.7f, 0, 0));
				((W.VamAtom)persons_[2].Atom).Atom.mainController.MoveControl(new UnityEngine.Vector3(0, 0, 1.7f));
				//persons_[0].AI = new PersonAI();

				allObjects_.AddRange(objects_);

				foreach (var p in persons_)
					allObjects_.Add(p);

				//allObjects_.Add(player_);

				Resources.Animations.Load();

				//VUI.Utilities.DumpComponentsAndUp(SuperController.singleton.errorLogPanel);


				//for (int i = 0; i < persons_.Count; ++i)
				//	persons_[i].OnPluginState(true);
				//
				controls_.Create(objects_);
				OnEnable();
			});
		}

		public void ReloadPlugin()
		{
			foreach (var pui in UITransform.parent.GetComponentsInChildren<MVRPluginUI>())
			{
				if (pui.urlText.text.Contains("Cue.cslist"))
				{
					LogError("reloading");
					pui.reloadButton.onClick.Invoke();
				}
			}
		}

		public void Update()
		{
			U.Safe(() =>
			{
				++frame_;

				if (frame_ == 5)
				{
					foreach (var p in persons_)
						((W.VamAtom)p.Atom).Atom.collisionEnabled = true;
				}

				if (sys_.Paused != paused_)
				{
					paused_ = sys_.Paused;
					for (int i = 0; i < allObjects_.Count; ++i)
						allObjects_[i].SetPaused(sys_.Paused);
				}

				if (!sys_.Paused)
				{
					for (int i = 0; i < allObjects_.Count; ++i)
						allObjects_[i].Update(sys_.Time.deltaTime);
				}

				controls_.Update();
				hud_.Update();

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

				for (int i = 0; i < allObjects_.Count; ++i)
					allObjects_[i].FixedUpdate(sys_.Time.deltaTime);
			});
		}

		public void OnEnable()
		{
			U.Safe(() =>
			{
				sys_.OnPluginState(true);
				controls_.Enabled = true;
				hud_.Create(SuperController.singleton.mainMenuUI.root);

				for (int i = 0; i < allObjects_.Count; ++i)
					allObjects_[i].OnPluginState(true);
			});
		}

		public void OnDisable()
		{
			U.Safe(() =>
			{
				sys_.OnPluginState(false);
				controls_.Enabled = false;
				hud_.Destroy();

				for (int i = 0; i < allObjects_.Count; ++i)
					allObjects_[i].OnPluginState(false);
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
