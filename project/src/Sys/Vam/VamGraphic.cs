using UnityEngine;

namespace Cue.W
{
	class VamBoxGraphic : IBoxGraphic
	{
		public const int Layer = 21;

		private GameObject object_ = null;
		private Material material_ = null;
		private Renderer renderer_ = null;

		public VamBoxGraphic(UnityEngine.Vector3 pos)
		{
			object_ = GameObject.CreatePrimitive(PrimitiveType.Cube);
			object_.transform.SetParent(SuperController.singleton.transform.root, false);
			object_.layer = Layer;
			object_.GetComponent<Renderer>().enabled = false;

			material_ = new Material(Shader.Find("Battlehub/RTGizmos/Handles"));
			material_.color = new Color(0, 0, 1, 0.5f);
			material_.SetFloat("_Offset", 1f);
			material_.SetFloat("_MinAlpha", 1f);

			renderer_ = object_.GetComponent<Renderer>();
			renderer_.material = material_;

			object_.transform.localScale =
				new UnityEngine.Vector3(0.5f, 0.05f, 0.5f);

			object_.transform.position = pos;
		}

		public Vector3 Position
		{
			get
			{
				return Vector3.FromUnity(object_.transform.position);
			}

			set
			{
				object_.transform.position = Vector3.ToUnity(value);
			}
		}

		public bool Visible
		{
			get { return renderer_.enabled; }
			set { renderer_.enabled = value; }
		}

		public Color Color
		{
			get { return material_.color; }
			set { material_.color = value; }
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
}
