using UnityEngine;

namespace Cue.W
{
	abstract class VamGraphic : IGraphic
	{
		public const int Layer = 21;

		protected GameObject object_ = null;
		protected Material material_ = null;
		protected Renderer renderer_ = null;
		private Color color_;

		protected VamGraphic(PrimitiveType type, Color c)
		{
			color_ = c;

			object_ = GameObject.CreatePrimitive(type);
			object_.transform.SetParent(SuperController.singleton.transform.root, false);
			object_.layer = Layer;

			renderer_ = object_.GetComponent<Renderer>();
			renderer_.enabled = false;

			SetMaterial();
		}

		private void SetMaterial()
		{
			if (material_ == null)
			{
				if (color_.a == 1)
					material_ = CreateOpaqueMaterial();
				else
					material_ = CreateTransparentMaterial();
			}
			else if (material_.color.a != color_.a)
			{
				if (material_.color.a == 1 && color_.a != 1)
				{
					// was opaque, now transparent
					if (material_)
						Object.Destroy(material_);

					material_ = CreateTransparentMaterial();
				}
				else if (material_.color.a != 1 && color_.a == 1)
				{
					// was transparent, now opaque
					if (material_)
						Object.Destroy(material_);

					material_ = CreateOpaqueMaterial();
				}
			}

			material_.color = W.VamU.ToUnity(color_);
			renderer_.material = material_;
		}

		private Material CreateTransparentMaterial()
		{
			return new Material(Shader.Find("Battlehub/RTGizmos/Handles"));
		}

		private Material CreateOpaqueMaterial()
		{
			return new Material(Shader.Find("Diffuse"));
		}

		public bool Collision
		{
			get
			{
				foreach (var c in object_.GetComponentsInChildren<Collider>())
					return c.enabled;

				return false;
			}

			set
			{
				foreach (var c in object_.GetComponentsInChildren<Collider>())
					c.enabled = value;
			}
		}

		public Vector3 Position
		{
			get
			{
				return W.VamU.FromUnity(object_.transform.position);
			}

			set
			{
				object_.transform.position = W.VamU.ToUnity(value);
			}
		}

		public bool Visible
		{
			get { return renderer_.enabled; }
			set { renderer_.enabled = value; }
		}

		public Color Color
		{
			get { return color_; }
			set { color_ = value; SetMaterial(); }
		}

		public Transform Transform
		{
			get { return object_.transform; }
		}

		public void Destroy()
		{
			if (object_ == null)
				return;

			Object.Destroy(object_);
		}
	}


	class VamBoxGraphic : VamGraphic
	{
		public VamBoxGraphic(Vector3 pos, Color c)
			: base(PrimitiveType.Cube, c)
		{
			object_.transform.localScale =
				new UnityEngine.Vector3(0.5f, 0.05f, 0.5f);

			object_.transform.position = W.VamU.ToUnity(pos);
		}
	}


	class VamSphereGraphic : VamGraphic
	{
		public VamSphereGraphic(Vector3 pos, float radius, Color c)
			: base(PrimitiveType.Sphere, c)
		{
			object_.transform.localScale =
				new UnityEngine.Vector3(radius, radius, radius);

			object_.transform.position = W.VamU.ToUnity(pos);
		}
	}
}
