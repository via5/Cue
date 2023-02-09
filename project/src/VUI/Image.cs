using UnityEngine;
using UnityEngine.UI;

namespace VUI
{
	class Image : Widget
	{
		public override string TypeName { get { return "Image"; } }

		private RawImage raw_ = null;
		private Texture tex_ = null;
		private static Material emptyMat_ = null;

		public Texture Texture
		{
			set
			{
				tex_ = value;
				UpdateTexture();
			}
		}

		protected override GameObject CreateGameObject()
		{
			return new GameObject();
		}

		protected override void DoCreate()
		{
			raw_ = WidgetObject.AddComponent<RawImage>();

			if (emptyMat_ == null)
			{
				emptyMat_ = new Material(raw_.material);
				emptyMat_.mainTexture = Texture2D.blackTexture;
			}

			raw_.material = emptyMat_;
			UpdateTexture();
		}

		private void UpdateTexture()
		{
			if (raw_ != null)
				raw_.texture = tex_;
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			if (tex_ == null)
				return new Size(40, 40);

			return new Size(tex_.width, tex_.height);
		}
	}
}
