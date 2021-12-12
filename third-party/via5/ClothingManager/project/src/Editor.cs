using UnityEngine;

namespace ClothingManager
{
	class Preview
	{
		private readonly Transform root_;
		private GameObject node_ = null;
		private GameObject box_ = null;
		private Material material_ = null;
		private Renderer renderer_ = null;

		public Preview(Transform root)
		{
			root_ = root;
		}

		public void Update(BoxCollider c)
		{
			if (node_ == null)
				Create();

			node_.transform.position = c.transform.parent.position;
			node_.transform.localRotation = c.transform.rotation;

			box_.transform.localScale = c.size;
			box_.transform.localPosition = c.center;
		}

		private void Create()
		{
			Log.Verbose("creating node");

			node_ = new GameObject();
			node_.transform.SetParent(root_, false);

			box_ = GameObject.CreatePrimitive(PrimitiveType.Cube);
			box_.transform.SetParent(node_.transform, false);

			renderer_ = box_.GetComponent<Renderer>();
			renderer_.enabled = true;

			material_ = new Material(Shader.Find("Battlehub/RTGizmos/Handles"));
			material_.color = new Color(0, 0, 1, 0.1f);
			renderer_.material = material_;

			foreach (var cc in box_.GetComponentsInChildren<Collider>())
			{
				cc.enabled = false;
				Object.Destroy(cc);
			}
		}

		public void Destroy()
		{
			if (node_ != null)
			{
				Object.Destroy(node_);
				node_ = null;
			}
		}
	}


	class Editor
	{
		private const string RootName = "via5.clothingmanager.editor";

		private bool enabled_ = false;
		private GameObject root_ = null;
		private VamClothingItem ci_ = null;
		private int side_ = Sides.Both;
		private Preview left_ = null;
		private Preview right_ = null;

		public Editor()
		{
		}

		public bool Enabled
		{
			get
			{
				return enabled_;
			}

			set
			{
				if (enabled_ != value)
				{
					enabled_ = value;

					if (root_ != null)
						root_.SetActive(enabled_);
				}
			}
		}

		public void Select(VamClothingItem item, int side)
		{
			ci_ = item;
			side_ = side;
			Log.Verbose($"editor: {ci_?.Uid}");
		}

		private Transform GetRoot()
		{
			if (root_ == null)
			{
				root_ = new GameObject(RootName);
				root_.transform.SetParent(ClothingManager.Instance.Root, false);
			}

			return root_.transform;
		}

		public void Update()
		{
			if (!enabled_)
				return;

			if ((ci_?.HasCollider(Sides.Left) ?? false) && Sides.IsLeft(side_))
			{
				if (left_ == null)
					left_ = new Preview(GetRoot());

				left_.Update(ci_.Daz.colliderLeft);
			}
			else
			{
				if (left_ != null)
				{
					left_.Destroy();
					left_ = null;
				}
			}

			if ((ci_?.HasCollider(Sides.Right) ?? false) && Sides.IsRight(side_))
			{
				if (right_ == null)
					right_ = new Preview(GetRoot());

				right_.Update(ci_.Daz.colliderRight);
			}
			else
			{
				if (right_ != null)
				{
					right_.Destroy();
					right_ = null;
				}
			}
		}
	}
}
