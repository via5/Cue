using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VUI
{
	// needs an interface because components cannot be generics
	//
	interface IListView
	{
		Logger Log { get; }
		void OnItemActivatedInternal();
		void OnItemRightClickedInternal();
		void SetHoveredInternal(ListViewItem from, ListViewItem hovered);
	}


	// added to the list viewport
	//
	class ListViewComponent : MonoBehaviour
	{
		public IListView List = null;
	}


	// added to the item prefab
	//
	class ListViewItem : MonoBehaviour,
		IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
	{
		public Logger Log
		{
			get
			{
				var lists = GetComponentsInParent<ListViewComponent>();
				if (lists == null || lists.Length == 0)
					return Logger.Global;
				else
					return lists[0]?.List?.Log ?? Logger.Global;
			}
		}

		private IListView GetListView()
		{
			var lists = GetComponentsInParent<ListViewComponent>();
			if (lists.Length == 0)
			{
				Log.Error("ListViewItem: no ListViewComponent in parents");
				return null;
			}

			return lists[0]?.List;
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			try
			{
				GetListView()?.SetHoveredInternal(this, this);
			}
			catch (Exception e)
			{
				Log.Error("exception in ListViewItem.OnPointerEnter");
				Log.Error(e.ToString());
			}
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			try
			{
				GetListView()?.SetHoveredInternal(this, null);
			}
			catch (Exception e)
			{
				Log.Error("exception in ListViewItem.OnPointerExit");
				Log.Error(e.ToString());
			}
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			try
			{
				if (eventData.button == PointerEventData.InputButton.Left)
				{
					if (eventData.clickCount == 2)
						GetListView()?.OnItemActivatedInternal();
				}
				else if (eventData.button == PointerEventData.InputButton.Right)
				{
					if (eventData.clickCount == 1)
						GetListView()?.OnItemRightClickedInternal();
				}
			}
			catch (Exception e)
			{
				Log.Error("exception in ListViewItem.OnPointerClick");
				Log.Error(e.ToString());
			}
		}
	}


	class ListView<ItemType> : TypedList<ItemType>, IListView
		where ItemType : class
	{
		public override string TypeName { get { return "ListView"; } }

		public event ItemCallback ItemActivated;
		public event IndexCallback ItemIndexActivated;

		public event ItemCallback ItemRightClicked;
		public event IndexCallback ItemIndexRightClicked;

		private ListViewItem hovered_ = null;


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
			Events.PointerDown += OnPointerDown;
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
			Popup.popup.onValueChangeHandlers += (string s) => { Focus(); };
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
				Log.Error("ListView: prefab object null");
			}
			else
			{
				var item = go.AddComponent<ListViewItem>();
				if (item == null)
					Log.Error("ListView: can't add ListViewItem component");
			}

			// adding the component on this doesn't work, GetComponentsInParent()
			// can't find it, but it's fine when in the viewport
			var viewport = Utilities.FindChildRecursive(WidgetObject, "Viewport");
			if (viewport == null)
			{
				Log.Error("ListView: no viewport");
			}
			else
			{
				var c = viewport.AddComponent<ListViewComponent>();
				if (c == null)
					Log.Error("ListView: can't add component");
				else
					c.List = this;
			}
		}

		protected override void DoPolish()
		{
			base.DoPolish();
			Style.Polish(this);
		}

		protected override void AfterUpdateBounds()
		{
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
				Log.Error("selected null");
			}
			else
			{
				ItemActivated?.Invoke(s);
				ItemIndexActivated?.Invoke(SelectedIndex);
			}
		}

		public void OnItemRightClickedInternal()
		{
			var s = hovered_;
			if (s == null)
			{
				Log.Error("right clicked null");
			}
			else if (hovered_ != null)
			{
				if (ItemRightClicked != null || ItemIndexRightClicked != null)
				{
					int i = GetListViewItemIndex(hovered_);

					if (i >= 0 && i < Count)
					{
						ItemRightClicked?.Invoke(Items[i]);
						ItemIndexRightClicked?.Invoke(i);
					}
				}
			}
		}

		private int GetListViewItemIndex(ListViewItem item)
		{
			int i = 0;

			foreach (Transform t in item.transform.parent)
			{
				if (t == item.transform)
					return i;

				++i;
			}

			return -1;
		}

		public void SetHoveredInternal(ListViewItem from, ListViewItem hovered)
		{
			if (hovered == null)
			{
				if (hovered_ == from)
					hovered_ = null;
			}
			else
			{
				hovered_ = hovered;
			}
		}

		private void OnPointerDown(PointerEvent e)
		{
			e.Bubble = false;
		}
	}
}
