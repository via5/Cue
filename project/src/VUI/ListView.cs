using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VUI
{
	// needs an interface because components cannot be generics
	//
	interface IListView
	{
		void OnItemActivatedInternal();
	}


	// added to the list viewport
	//
	class ListViewComponent : MonoBehaviour
	{
		public IListView List = null;
	}


	// added to the item prefab
	//
	class ListViewItem : MonoBehaviour, IPointerClickHandler
	{
		public void OnPointerClick(PointerEventData eventData)
		{
			if (eventData.button == PointerEventData.InputButton.Left)
			{
				if (eventData.clickCount == 2)
				{
					var lists = GetComponentsInParent<ListViewComponent>();
					if (lists.Length == 0)
					{
						Glue.LogError("ListViewItem: no ListViewComponent in parents");
						return;
					}

					var list = lists[0];
					if (list.List == null)
						Glue.LogError("ListViewItem: parent list is null");
					else
						list.List.OnItemActivatedInternal();
				}
			}
		}
	}


	class ListView<ItemType> : TypedList<ItemType>, IListView
		where ItemType : class
	{
		public override string TypeName { get { return "ListView"; } }

		public event ItemCallback ItemActivated;
		public event IndexCallback ItemIndexActivated;

		public ListView(List<ItemType> items = null)
			: this(items, null)
		{
		}

		public ListView(ItemCallback selectionChanged)
			: this(null, selectionChanged)
		{
		}

		public ListView(List<ItemType> items, ItemCallback selectionChanged)
			: base(items, selectionChanged)
		{
			Borders = new Insets(2);
		}

		protected override GameObject CreateGameObject()
		{
			return UnityEngine.Object.Instantiate(
				Glue.PluginManager.configurableScrollablePopupPrefab)
					.gameObject;
		}

		protected override void DoCreate()
		{
			base.DoCreate();

			Popup.popup.alwaysOpen = true;
			Popup.popup.topButton.gameObject.SetActive(false);
			Popup.popup.backgroundImage.gameObject.SetActive(false);
			Popup.popup.onValueChangeHandlers += (string s) => { GetRoot().SetFocus(this); };
			Popup.popup.topBottomBuffer = 3;

			AddItemComponents();

			// the pivot is initially (0.5, 1.0), which doesn't work for what
			// happens in UpdateBounds()
			Popup.popup.popupPanel.pivot = new Vector2(0.5f, 0.5f);

			// when the popupPanel is first created, it's a child of Popup,
			// but this changes the first time the list is visible on screen:
			// the popupPanel becomes a direct child of WidgetObject and
			// Popup is nowhere to be seen, it's removed from the hierarchy
			//
			// so this might be useless
			var rt = Popup.GetComponent<RectTransform>();
			rt.offsetMin = new Vector2(0, 0);
			rt.offsetMax = new Vector2(0, 0);
			rt.anchorMin = new Vector2(0, 0);
			rt.anchorMax = new Vector2(1, 1);
			rt.anchoredPosition = new Vector2(0, 0);

			Style.Setup(this);
		}

		private void AddItemComponents()
		{
			// adding ListViewItem component to prefab
			var go = Popup.popup.popupButtonPrefab?.gameObject;
			if (go == null)
			{
				Glue.LogError("ListView: prefab object null");
			}
			else
			{
				var item = go.AddComponent<ListViewItem>();
				if (item == null)
					Glue.LogError("ListView: can't add ListViewItem component");
			}

			// adding the component on this doesn't work, GetComponentsInParent()
			// can't find it, but it's fine when in the viewport
			var viewport = Utilities.FindChildRecursive(WidgetObject, "Viewport");
			if (viewport == null)
			{
				Glue.LogError("ListView: no viewport");
			}
			else
			{
				var c = viewport.AddComponent<ListViewComponent>();
				if (c == null)
					Glue.LogError("ListView: can't add component");
				else
					c.List = this;
			}
		}

		protected override void DoPolish()
		{
			base.DoPolish();
			Style.Polish(this);
		}

		public override void UpdateBounds()
		{
			base.UpdateBounds();
			Utilities.SetRectTransform(Popup.popup.popupPanel, ClientBounds);
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			return new Size(300, 200);
		}

		public void OnItemActivatedInternal()
		{
			var s = Selected;
			if (s == null)
			{
				Glue.LogError("selected null");
			}
			else
			{
				ItemActivated?.Invoke(s);
				ItemIndexActivated?.Invoke(SelectedIndex);
			}
		}
	}
}
