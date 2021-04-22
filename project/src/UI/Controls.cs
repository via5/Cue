using System.Collections.Generic;
using UnityEngine;

namespace Cue
{
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
			control_.layer = Controls.Layer;

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
				material_.color = new Color(0, 1, 0, 0.3f);
			else if (object_.Slots.AnyLocked)
				material_.color = new Color(1, 0, 0, 0.1f);
			else
				material_.color = new Color(0, 0, 1, 0.1f);
		}
	}


	interface IControls
	{
		void Create(List<IObject> objects);
		void Update();
		IObject Find(Transform t);
		bool Enabled { get; set; }
	}


	class MockControls : IControls
	{
		public void Create(List<IObject> objects)
		{
		}

		public void Update()
		{
		}

		public IObject Find(Transform t)
		{
			return null;
		}

		public bool Enabled
		{
			get { return false; }
			set { }
		}
	}


	class Controls : IControls
	{
		public const int Layer = 21;
		private bool enabled_ = true;
		private List<ObjectControls> controls_ = new List<ObjectControls>();

		public bool Enabled
		{
			get { return enabled_; }
			set { enabled_ = value; Check(); }
		}

		public void Create(List<IObject> objects)
		{
			Cue.Instance.HoveredChanged += OnHoveredChanged;

			foreach (var o in objects)
				controls_.Add(new ObjectControls(o));

			for (int i = 0; i < 32; ++i)
				Physics.IgnoreLayerCollision(i, Layer);

			Check();
		}

		public void Update()
		{
			if (!enabled_)
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
}
