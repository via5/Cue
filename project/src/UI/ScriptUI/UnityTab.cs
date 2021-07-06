using System.Collections.Generic;

namespace Cue
{
	class UnityTab : Tab
	{
		class UnityObject
		{
			public UnityEngine.Transform t;
			public bool expanded = false;
			public int indent;

			public UnityObject(UnityEngine.Transform t, int indent)
			{
				this.t = t;
				this.indent = indent;
			}

			public override string ToString()
			{
				string s = new string(' ', indent * 4);

				if (expanded)
					s += "- ";
				else
					s += "+ ";

				s += t.name;

				return s;
			}
		}

		private VUI.Button refresh_ = new VUI.Button("Refresh");
		private VUI.ListView<UnityObject> objects_ = new VUI.ListView<UnityObject>();
		private List<UnityObject> items_ = new List<UnityObject>();

		public UnityTab()
			: base("Unity")
		{
			Layout = new VUI.BorderLayout();
			Add(refresh_, VUI.BorderLayout.Top);
			Add(objects_, VUI.BorderLayout.Center);

			Refresh();

			refresh_.Clicked += Refresh;
			objects_.ItemIndexActivated += OnActivated;
		}

		public override void Update(float s)
		{
		}

		private void Refresh()
		{
			items_.Clear();
			items_.Add(new UnityObject(SuperController.singleton.transform.root, 0));

			objects_.SetItems(items_);
		}

		private void OnActivated(int i)
		{
			var o = objects_.At(i);

			if (o.expanded)
			{
			}
			else
			{
				o.expanded = true;
				var newItems = new List<UnityObject>();

				foreach (UnityEngine.Transform c in o.t)
					newItems.Add(new UnityObject(c, o.indent + 1));

				U.NatSort(newItems);
				items_.InsertRange(i + 1, newItems);
				objects_.SetItems(items_);
			}
		}
	}
}
