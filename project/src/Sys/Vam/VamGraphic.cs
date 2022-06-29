using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cue.Sys.Vam
{
	abstract class VamBasicGraphic : IGraphic
	{
		public const int Layer = 21;

		public abstract bool Visible { get; set; }
		public abstract Vector3 Position { get; set; }
		public abstract Quaternion Rotation { get; set; }
		public abstract Vector3 Size { get; set; }
		public abstract Color Color { get; set; }
		public abstract bool Collision { get; set; }

		public abstract void Destroy();
	}

	abstract class VamObjectGraphic : VamBasicGraphic
	{
		protected GameObject object_ = null;

		protected VamObjectGraphic()
		{
		}

		protected void SetObject(GameObject o, string name)
		{
			object_ = o;
			object_.name = name;
			object_.transform.SetParent(Cue.Instance.VamSys.RootTransform, false);
			object_.layer = Layer;
		}

		public override Vector3 Position
		{
			get { return U.FromUnity(object_.transform.localPosition); }
			set { object_.transform.localPosition = U.ToUnity(value); }
		}

		public override Quaternion Rotation
		{
			get { return U.FromUnity(object_.transform.localRotation); }
			set { object_.transform.localRotation = U.ToUnity(value); }
		}

		public override Vector3 Size
		{
			get { return U.FromUnity(object_.transform.localScale); }
			set { object_.transform.localScale = U.ToUnity(value); }
		}

		public GameObject Object
		{
			get { return object_; }
		}

		public Transform Transform
		{
			get { return object_.transform; }
		}

		public override void Destroy()
		{
			if (object_ == null)
				return;

			UnityEngine.Object.Destroy(object_);
		}
	}

	abstract class VamPrimitiveGraphic : VamObjectGraphic
	{
		protected Material material_ = null;
		protected Renderer renderer_ = null;
		private Color color_;

		protected VamPrimitiveGraphic(string name, PrimitiveType type, Color c)
		{
			color_ = c;

			var o = GameObject.CreatePrimitive(type);
			SetObject(o, name);

			renderer_ = Transform.GetComponent<Renderer>();
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

		public override bool Collision
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

		public override bool Visible
		{
			get { return renderer_.enabled; }
			set { renderer_.enabled = value; }
		}

		public override Color Color
		{
			get { return color_; }
			set { color_ = value; SetMaterial(); }
		}
	}


	class VamBoxGraphic : VamPrimitiveGraphic
	{
		public VamBoxGraphic(string name, Vector3 pos, Vector3 size, Color c)
			: base(name, PrimitiveType.Cube, c)
		{
			object_.transform.localScale = U.ToUnity(size);
			object_.transform.position = U.ToUnity(pos);
		}
	}


	class VamSphereGraphic : VamPrimitiveGraphic
	{
		public VamSphereGraphic(string name, Vector3 pos, float radius, Color c)
			: base(name, PrimitiveType.Sphere, c)
		{
			object_.transform.localScale =
				new UnityEngine.Vector3(radius, radius, radius);

			object_.transform.position = U.ToUnity(pos);
		}
	}


	class VamCapsuleGraphic : VamPrimitiveGraphic
	{
		public VamCapsuleGraphic(string name, Color c)
			: base(name, PrimitiveType.Capsule, c)
		{
		}
	}


	class VamLineGraphic : VamBasicGraphic
	{
		private IGraphic fromBox_, toBox_;
		private IGraphic line_;

		public VamLineGraphic(string name, Color c)
		{
			fromBox_ = Cue.Instance.Sys.CreateBoxGraphic(
				$"{name}.lineFromBox",
				Vector3.Zero, new Vector3(0.005f, 0.005f, 0.005f),
				c);

			toBox_ = Cue.Instance.Sys.CreateBoxGraphic(
				$"{name}.lineToBox",
				Vector3.Zero, new Vector3(0.005f, 0.005f, 0.005f),
				c);

			line_ = Cue.Instance.Sys.CreateBoxGraphic(
				$"{name}.line",
				Vector3.Zero, Vector3.Zero,
				c);
		}

		public override bool Visible
		{
			get
			{
				return fromBox_.Visible;
			}

			set
			{
				fromBox_.Visible = value;
				toBox_.Visible = value;
				line_.Visible = value;
			}
		}

		public override Vector3 Position
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}
		public override Quaternion Rotation
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public override Vector3 Size
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public override Color Color
		{
			get
			{
				return fromBox_.Color;
			}

			set
			{
				fromBox_.Color = value;
				toBox_.Color = value;
				line_.Color = value;
			}
		}

		public override bool Collision
		{
			get
			{
				return fromBox_.Collision;
			}

			set
			{
				fromBox_.Collision = value;
				toBox_.Collision = value;
				line_.Collision = value;
			}
		}

		public void Set(Vector3 from, Vector3 to)
		{
			fromBox_.Position = from;
			toBox_.Position = to;

			line_.Position = from + (to - from) / 2;
			line_.Size = new Vector3(0.0005f, 0.0005f, Vector3.Distance(from, to));
			line_.Rotation = U.FromUnity(UnityEngine.Quaternion.LookRotation(
				U.ToUnity(to) - U.ToUnity(from)));
		}

		public override void Destroy()
		{
			if (fromBox_ != null)
			{
				fromBox_.Destroy();
				fromBox_ = null;
			}

			if (toBox_ != null)
			{
				toBox_.Destroy();
				toBox_ = null;
			}

			if (line_ != null)
			{
				line_.Destroy();
				line_ = null;
			}
		}
	}


	class VamTextGraphic : VamObjectGraphic
	{
		private TextMesh text_;

		public VamTextGraphic(string name, string text, Color c)
		{
			var o = new GameObject();
			SetObject(o, name);

			text_ = Object.AddComponent<TextMesh>();
			text_.text = text;
			text_.fontSize = 30;
			text_.transform.localScale = new UnityEngine.Vector3(-0.002f, 0.002f, 0.002f);
		}

		public override bool Visible
		{
			get { return Object.activeInHierarchy; }
			set { Object.SetActive(value); }
		}

		public override Color Color
		{
			get { return U.FromUnity(text_.color); }
			set { text_.color = U.ToUnity(value); }
		}

		public override bool Collision
		{
			get { return false; }
			set { }
		}
	}


	public class VamDebugRenderer
	{
		private static string MakeDebugName(string name)
		{
			return $"cue!DebugRender.{name}";
		}

		public interface IDebugRender
		{
			void Destroy();
			bool Update(float s);
		}


		abstract class BoxRender : IDebugRender
		{
			private IGraphic g_;

			public BoxRender(string name, Vector3 scale)
			{
				g_ = Cue.Instance.Sys.CreateBoxGraphic(
					MakeDebugName(name),
					Vector3.Zero, scale,
					new Color(1, 1, 1, 0.1f));
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

					DoUpdate(g_);

					return true;
				}
				catch (Exception)
				{
					return false;
				}
			}

			protected abstract void DoUpdate(IGraphic g);
		}


		class DistanceRender : IDebugRender
		{
			private Vector3 from_, to_;
			private VamLineGraphic line_;
			private VamTextGraphic text_;

			public DistanceRender(Vector3 from, Vector3 to)
			{
				from_ = from;
				to_ = to;

				line_ = new VamLineGraphic(
					MakeDebugName("line"), new Color(1, 0, 1, 0.1f));

				text_ = new VamTextGraphic(
					MakeDebugName("distance"),
					$"{Vector3.Distance(from, to):0.0000}",
					new Color(1, 1, 1, 1));
			}

			public void Destroy()
			{
				if (line_ != null)
				{
					line_.Destroy();
					line_ = null;
				}

				if (text_ != null)
				{
					text_.Destroy();
					text_ = null;
				}
			}

			public bool Update(float s)
			{
				line_.Set(from_, to_);

				text_.Position = to_;
				text_.Transform.LookAt(Camera.main.transform.position);

				return true;
			}
		}


		class TransformRender : BoxRender
		{
			private Transform t_;

			public TransformRender(Transform t, Vector3 scale)
				: base(t.name, scale)
			{
				t_ = t;
			}

			protected override void DoUpdate(IGraphic g)
			{
				g.Position = U.FromUnity(t_.position);
				g.Rotation = U.FromUnity(t_.rotation);
			}
		}


		class PointRender : BoxRender
		{
			private Vector3 p_;

			public PointRender(Vector3 p, Vector3 scale)
				: base("point", scale)
			{
				p_ = p;
			}

			protected override void DoUpdate(IGraphic g)
			{
				g.Position = p_;
			}
		}


		class BodyPartRender : BoxRender
		{
			private IBodyPart bp_;

			public BodyPartRender(IBodyPart bp)
				: base(BodyPartType.ToString(bp.Type), new Vector3(0.005f, 0.005f, 0.005f))
			{
				bp_ = bp;
			}

			protected override void DoUpdate(IGraphic g)
			{
				g.Position = bp_.Position;
				g.Rotation = bp_.Rotation;
			}
		}



		class ColliderRender : IDebugRender
		{
			private CapsuleCollider cc_;
			private SphereCollider sc_;
			private Transform parent_;
			private VamPrimitiveGraphic g_;

			public ColliderRender(Collider c)
			{
				cc_ = c as CapsuleCollider;
				sc_ = c as SphereCollider;

				if (cc_ == null && sc_ == null)
					Cue.LogError($"collider {c} not a capsule or sphere collider");

				parent_ = new GameObject().transform;
				parent_.SetParent(Cue.Instance.VamSys.RootTransform, false);

				if (cc_ != null)
				{
					g_ = Cue.Instance.Sys.CreateCapsuleGraphic(
						MakeDebugName(c.transform.name),
						new Color(0, 0, 1, 0.1f)) as VamPrimitiveGraphic;
				}
				else if (sc_ != null)
				{
					g_ = Cue.Instance.Sys.CreateSphereGraphic(
						MakeDebugName(c.transform.name),
						Vector3.Zero, 0,
						new Color(0, 0, 1, 0.1f)) as VamPrimitiveGraphic;
				}

				if (g_ != null)
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

					if (cc_ != null)
					{
						float size = cc_.radius * 2;
						float height = cc_.height / 2;

						parent_.position = cc_.transform.position;
						parent_.rotation = cc_.transform.rotation;

						g_.Size = new Vector3(size, height, size);

						if (cc_.direction == 0)
							g_.Rotation = U.FromUnity(UnityEngine.Quaternion.AngleAxis(90, UnityEngine.Vector3.forward));
						else if (cc_.direction == 2)
							g_.Rotation = U.FromUnity(UnityEngine.Quaternion.AngleAxis(90, UnityEngine.Vector3.right));

						g_.Position = U.FromUnity(cc_.center);
					}
					else if (sc_ != null)
					{
						parent_.position = sc_.transform.position;
						parent_.rotation = sc_.transform.rotation;

						g_.Size = new Vector3(sc_.radius * 2, sc_.radius * 2, sc_.radius * 2);
						g_.Position = U.FromUnity(sc_.center);
					}

					return true;
				}
				catch (Exception)
				{
					return false;
				}
			}
		}


		private List<IDebugRender> renders_ = null;

		public IDebugRender AddRender(Vector3 p)
		{
			return AddRender(new PointRender(p, new Vector3(0.005f, 0.005f, 0.005f)));
		}

		public IDebugRender AddRender(Vector3 from, Vector3 to)
		{
			return AddRender(new DistanceRender(from, to));
		}

		public IDebugRender AddRender(Transform t)
		{
			return AddRender(t, new Vector3(0.1f, 0.1f, 0.1f));
		}

		public IDebugRender AddRender(Transform t, Vector3 scale)
		{
			return AddRender(new TransformRender(t, scale));
		}

		public IDebugRender AddRender(Collider c)
		{
			return AddRender(new ColliderRender(c));
		}

		public IDebugRender AddRender(IBodyPart bp)
		{
			return AddRender(new BodyPartRender(bp));
		}

		public void RemoveRender(IDebugRender r)
		{
			if (r != null)
			{
				r.Destroy();
				renders_.Remove(r);
			}
		}

		private IDebugRender AddRender(IDebugRender r)
		{
			if (renders_ == null)
				renders_ = new List<IDebugRender>();

			renders_.Add(r);
			return r;
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
