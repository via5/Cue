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


	class Controls
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

			if (SuperController.singleton.gameMode == SuperController.GameMode.Play)
			{
				if (Cue.Instance.Hud.IsHovered(Input.mousePosition))
					return;

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

			for (int i = 0; i < controls_.Count; ++i)
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
}
