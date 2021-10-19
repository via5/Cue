using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cue.Sys.Vam
{
	abstract class VamGraphic : IGraphic
	{
		public const int Layer = 21;

		protected GameObject object_ = null;
		protected Material material_ = null;
		protected Renderer renderer_ = null;
		private Color color_;

		protected VamGraphic(string name, PrimitiveType type, Color c)
		{
			color_ = c;

			object_ = GameObject.CreatePrimitive(type);
			object_.name = name;
			object_.transform.SetParent(Cue.Instance.VamSys.RootTransform, false);
			object_.layer = Layer;

			renderer_ = object_.GetComponent<Renderer>();
			renderer_.enabled = true;

			SetMaterial();
			Collision = false;
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
						UnityEngine.Object.Destroy(material_);

					material_ = CreateTransparentMaterial();
				}
				else if (material_.color.a != 1 && color_.a == 1)
				{
					// was transparent, now opaque
					if (material_)
						UnityEngine.Object.Destroy(material_);

					material_ = CreateOpaqueMaterial();
				}
			}

			material_.color = U.ToUnity(color_);
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
			get { return U.FromUnity(object_.transform.localPosition); }
			set { object_.transform.localPosition = U.ToUnity(value); }
		}

		public Quaternion Rotation
		{
			get { return U.FromUnity(object_.transform.localRotation); }
			set { object_.transform.localRotation = U.ToUnity(value); }
		}

		public Vector3 Size
		{
			get { return U.FromUnity(object_.transform.localScale); }
			set { object_.transform.localScale = U.ToUnity(value); }
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

			UnityEngine.Object.Destroy(object_);
		}
	}


	class VamBoxGraphic : VamGraphic
	{
		public VamBoxGraphic(string name, Vector3 pos, Vector3 size, Color c)
			: base(name, PrimitiveType.Cube, c)
		{
			object_.transform.localScale = U.ToUnity(size);
			object_.transform.position = U.ToUnity(pos);
		}
	}


	class VamSphereGraphic : VamGraphic
	{
		public VamSphereGraphic(string name, Vector3 pos, float radius, Color c)
			: base(name, PrimitiveType.Sphere, c)
		{
			object_.transform.localScale =
				new UnityEngine.Vector3(radius, radius, radius);

			object_.transform.position = U.ToUnity(pos);
		}
	}


	class VamCapsuleGraphic : VamGraphic
	{
		public VamCapsuleGraphic(string name, Color c)
			: base(name, PrimitiveType.Capsule, c)
		{
		}
	}



	class VamDebugRenderer
	{
		interface IRender
		{
			void Destroy();
			bool Update(float s);
		}


		class BoxRender : IRender
		{
			private Transform t_;
			private IGraphic g_;

			public BoxRender(Transform t, Vector3 scale)
			{
				t_ = t;
				g_ = Cue.Instance.Sys.CreateBoxGraphic(
					$"cue!DebugRender.{t.name}",
					Vector3.Zero, scale,
					new Color(0, 0, 1, 0.1f));
			}

			public void Destroy()
			{
				if (g_ != null)
				{
					g_.Destroy();
					g_ = null;
				}
			}

			public bool Update(float s)
			{
				try
				{
					if (g_ == null)
						return false;

					g_.Position = U.FromUnity(t_.position);
					g_.Rotation = U.FromUnity(t_.rotation);

					return true;
				}
				catch (Exception)
				{
					return false;
				}
			}
		}


		class ColliderRender : IRender
		{
			private CapsuleCollider cc_;
			private Transform parent_;
			private VamGraphic g_;

			public ColliderRender(Collider c)
			{
				cc_ = c as CapsuleCollider;

				parent_ = new GameObject().transform;
				parent_.SetParent(Cue.Instance.VamSys.RootTransform, false);

				g_ = Cue.Instance.Sys.CreateCapsuleGraphic(
					$"cue!DebugRender.{c.transform.name}",
					new Color(0, 0, 1, 0.1f)) as VamGraphic;

				g_.Transform.SetParent(parent_, false);
			}

			public void Destroy()
			{
				if (g_ != null)
				{
					g_.Destroy();
					g_ = null;
				}
			}

			public bool Update(float s)
			{
				try
				{
					if (g_ == null)
						return false;

					float size = cc_.radius * 2;
					float height = cc_.height / 2;

					parent_.position = cc_.transform.parent.position;
					parent_.rotation = cc_.transform.parent.rotation;

					g_.Size = new Vector3(size, height, size);

					if (cc_.direction == 0)
						g_.Rotation = U.FromUnity(UnityEngine.Quaternion.AngleAxis(90, UnityEngine.Vector3.forward));
					else if (cc_.direction == 2)
						g_.Rotation = U.FromUnity(UnityEngine.Quaternion.AngleAxis(90, UnityEngine.Vector3.right));

					g_.Position = U.FromUnity(cc_.center);

					return true;
				}
				catch (Exception)
				{
					return false;
				}
			}
		}


		private List<IRender> renders_ = null;

		public void AddRender(Transform t)
		{
			AddRender(t, new Vector3(0.1f, 0.1f, 0.1f));
		}

		public void AddRender(Transform t, Vector3 scale)
		{
			AddRender(new BoxRender(t, scale));
		}

		public void AddRender(Collider c)
		{
			AddRender(new ColliderRender(c));
		}

		private void AddRender(IRender r)
		{
			if (renders_ == null)
				renders_ = new List<IRender>();

			renders_.Add(r);
		}

		public void Update(float s)
		{
			if (renders_ != null)
			{
				int i = 0;
				while (i < renders_.Count)
				{
					if (renders_[i].Update(s))
					{
						++i;
					}
					else
					{
						renders_[i].Destroy();
						renders_.RemoveAt(i);
					}
				}
			}
		}
	}
}
