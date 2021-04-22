using System.Collections.Generic;
using UnityEngine;

namespace Cue
{
	class ObjectControls
	{
		private IObject object_;
		private GameObject control_ = null;
		private Material material_ = null;
		private bool hovered_ = false;

		public ObjectControls(Transform parent, IObject o)
		{
			object_ = o;

			control_ = GameObject.CreatePrimitive(PrimitiveType.Cube);
			control_.transform.SetParent(parent, false);
			control_.layer = Controls.Layer;
			control_.GetComponent<Renderer>().enabled = false;

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

		public IObject Object
		{
			get { return object_; }
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

		public bool Visible
		{
			set
			{
				control_.GetComponent<Renderer>().enabled = value;
			}
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
				material_.color = new Color(0, 1, 0, 0.3f);
			else if (object_.Slots.AnyLocked)
				material_.color = new Color(1, 0, 0, 0.1f);
			else
				material_.color = new Color(0, 0, 1, 0.1f);
		}
	}


	interface IControls
	{
		void Create();
		void Destroy();
		void Update();
		IObject Find(Transform t);
		bool Visible { get; set; }
	}


	class MockControls : IControls
	{
		public void Create()
		{
		}

		public void Destroy()
		{
		}

		public void Update()
		{
		}

		public IObject Find(Transform t)
		{
			return null;
		}

		public bool Visible
		{
			get { return false; }
			set { }
		}
	}


	class Controls : IControls
	{
		public const int Layer = 21;
		private bool visible_ = false;
		private GameObject root_ = null;
		private List<ObjectControls> controls_ = new List<ObjectControls>();

		public Controls()
		{
			Cue.Instance.HoveredChanged += OnHoveredChanged;

			root_ = new GameObject();
			root_.transform.SetParent(SuperController.singleton.transform.root, false);

			for (int i = 0; i < 32; ++i)
				Physics.IgnoreLayerCollision(i, Layer);
		}

		public bool Visible
		{
			get
			{
				return visible_;
			}

			set
			{
				if (visible_ != value)
				{
					visible_ = value;

					foreach (var c in controls_)
						c.Visible = value;
				}
			}
		}

		public void Create()
		{
			foreach (var o in Cue.Instance.Objects)
				controls_.Add(new ObjectControls(root_.transform, o));
		}

		public void Destroy()
		{
			foreach (var c in controls_)
				c.Destroy();

			controls_.Clear();
		}

		public void Update()
		{
			if (!visible_)
				return;

			for (int i = 0; i < controls_.Count; ++i)
				controls_[i].Update();
		}

		public IObject Find(Transform t)
		{
			for (int i = 0; i < controls_.Count; ++i)
			{
				if (controls_[i].Is(t))
					return controls_[i].Object;
			}

			return null;
		}

		private void OnHoveredChanged(IObject o)
		{
			for (int i = 0; i < controls_.Count; ++i)
				controls_[i].Hovered = (controls_[i].Object == o);
		}
	}
}
