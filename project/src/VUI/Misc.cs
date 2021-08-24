namespace VUI
{
	class Spacer : Panel
	{
		public override string TypeName { get { return "Spacer"; } }

		private int size_;

		public Spacer(int size = 0)
		{
			size_ = size;
		}

		protected override Size DoGetPreferredSize(float maxWidth, float maxHeight)
		{
			return new Size(size_, size_);
		}

		protected override Size DoGetMinimumSize()
		{
			return new Size(size_, size_);
		}
	}


	class HorizontalStretch : Panel
	{
		public override string TypeName { get { return "HorizontalStretch"; } }

		public HorizontalStretch()
		{
		}

		protected override Size DoGetPreferredSize(float maxWidth, float maxHeight)
		{
			return new Size(maxWidth, DontCare);
		}

		protected override Size DoGetMinimumSize()
		{
			return new Size(0, 0);
		}
	}


	class VerticalStretch : Panel
	{
		public override string TypeName { get { return "VerticalStretch"; } }

		public VerticalStretch()
		{
		}

		protected override Size DoGetPreferredSize(float maxWidth, float maxHeight)
		{
			return new Size(DontCare, maxHeight);
		}

		protected override Size DoGetMinimumSize()
		{
			return new Size(0, 0);
		}
	}
}
