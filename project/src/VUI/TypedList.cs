using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VUI
{
	class TypedList<ItemType> : Widget
		where ItemType : class
	{
		public override string TypeName { get { return "TypedList"; } }

		protected class Item
		{
			private ItemType object_;

			public Item(ItemType o)
			{
				object_ = o;
			}

			public ItemType Object
			{
				get { return object_; }
				set { object_ = value; }
			}

			public string Text
			{
				get
				{
					return object_?.ToString() ?? "";
				}
			}
		}

		public delegate void ItemCallback(ItemType item);
		public event ItemCallback SelectionChanged;

		public delegate void IndexCallback(int index);
		public event IndexCallback SelectionIndexChanged;

		private readonly List<Item> items_ = new List<Item>();
		private int selection_ = -1;
		private bool updatingChoices_ = false;

		private UIDynamicPopup popup_ = null;


		public TypedList(List<ItemType> items, ItemCallback selectionChanged)
		{
			if (items != null)
				SetItems(items.ToArray());

			if (selectionChanged != null)
				SelectionChanged += selectionChanged;
		}

		public void ScrollToTop()
		{
			var sr = popup_.popup.popupPanel.GetComponentInChildren<ScrollRect>();
			if (sr != null)
				sr.verticalNormalizedPosition = 1;
		}

		public void AddItem(ItemType i, bool select = false)
		{
			InsertItem(i, -1, select);
		}

		public void InsertItem(ItemType item, int index, bool select = false)
		{
			InsertItemNoUpdate(new Item(item), index);
			UpdateChoices();

			if (select)
				Select(item);
		}

		private bool ItemsEqual(ItemType a, ItemType b)
		{
			return EqualityComparer<ItemType>.Default.Equals(a, b);
		}

		public void RemoveItem(ItemType item)
		{
			int itemIndex = -1;

			for (int i = 0; i < items_.Count; ++i)
			{
				if (ItemsEqual(items_[i].Object, item))
				{
					itemIndex = i;
					break;
				}
			}

			if (itemIndex == -1)
				return;

			RemoveItemAt(itemIndex);
		}

		public void RemoveItemAt(int index)
		{
			items_.RemoveAt(index);
			UpdateChoices();

			if (items_.Count == 0)
				Select(-1);
			else if (selection_ >= items_.Count)
				Select(items_.Count - 1);
			else if (selection_ > index)
				Select(selection_ - 1);
			else
				Select(selection_);
		}

		public void SetItemAt(int index, ItemType item)
		{
			if (index < 0 || index >= items_.Count)
				return;

			items_[index].Object = item;
			UpdateItemText(index);
		}

		public List<ItemType> Items
		{
			get
			{
				var list = new List<ItemType>();

				foreach (var i in items_)
					list.Add(i.Object);

				return list;
			}
		}

		public ItemType At(int index)
		{
			if (index < 0 || index >= items_.Count)
				return null;
			else
				return items_[index].Object;
		}

		public int Count
		{
			get { return items_.Count; }
		}

		public void Clear()
		{
			SetItems(new ItemType[0]);
		}

		public void SetItems(List<ItemType> items, ItemType sel = null)
		{
			SetItems(items.ToArray(), sel);
		}

		public virtual void SetItems(ItemType[] items, ItemType sel = null)
		{
			items_.Clear();

			int selIndex = -1;

			for (int i = 0; i < items.Length; ++i)
			{
				if (ItemsEqual(items[i], sel))
					selIndex = i;

				InsertItemNoUpdate(new Item(items[i]), -1);
			}

			UpdateChoices();
			Select(selIndex);
		}

		public void UpdateItemsText()
		{
			UpdateChoices();
			UpdateLabel();
		}

		public void UpdateItemText(int index)
		{
			if (index < 0 || index >= items_.Count)
				return;

			popup_.popup.setDisplayPopupValue(index, items_[index].Text);

			if (index == selection_)
				UpdateLabel();
		}

		public void UpdateItemText(ItemType item)
		{
			int i = IndexOf(item);
			if (i != -1)
				UpdateItemText(i);
		}

		public virtual int IndexOf(ItemType item)
		{
			for (int i = 0; i < items_.Count; ++i)
			{
				if (ItemsEqual(items_[i].Object, item))
					return i;
			}

			return -1;
		}

		public void Select(ItemType item)
		{
			Select(IndexOf(item));
		}

		public void Select(int i)
		{
			if (i < 0 || i >= items_.Count)
				i = -1;

			selection_ = i;
			UpdateLabel();
			SelectionChanged?.Invoke(Selected);
			SelectionIndexChanged?.Invoke(selection_);
		}

		public ItemType Selected
		{
			get
			{
				if (selection_ < 0 || selection_ >= items_.Count)
					return null;
				else
					return items_[selection_].Object;
			}
		}

		public int SelectedIndex
		{
			get { return selection_; }
		}

		protected override void DoCreate()
		{
			popup_ = WidgetObject.GetComponent<UIDynamicPopup>();
			popup_.popup.showSlider = false;
			popup_.popup.useDifferentDisplayValues = true;
			popup_.popup.labelText.gameObject.SetActive(false);
			popup_.popup.onValueChangeHandlers += OnSelectionChanged;

			var text = popup_.popup.popupButtonPrefab.GetComponentInChildren<Text>();
			if (text != null)
			{
				text.alignment = TextAnchor.MiddleLeft;
				text.rectTransform.offsetMin = new Vector2(
					text.rectTransform.offsetMin.x + 10,
					text.rectTransform.offsetMin.y);
			}

			var rt = popup_.popup.popupButtonPrefab;
			rt.offsetMin = new Vector2(rt.offsetMin.x - 3, rt.offsetMin.y);
			rt.offsetMax = new Vector2(rt.offsetMax.x + 5, rt.offsetMax.y - 15);

			UpdateChoices();
			UpdateLabel();
		}

		protected override void DoSetEnabled(bool b)
		{
			base.DoSetEnabled(b);
			popup_.popup.topButton.interactable = b;
		}

		protected List<Item> InternalItems
		{
			get { return items_; }
		}

		public UIDynamicPopup Popup
		{
			get { return popup_; }
		}

		private void UpdateChoices()
		{
			if (popup_ == null)
				return;

			try
			{
				updatingChoices_ = true;

				popup_.popup.numPopupValues = items_.Count;
				for (int i = 0; i < items_.Count; ++i)
				{
					var item = items_[i];
					popup_.popup.setDisplayPopupValue(i, item.Text);
					popup_.popup.setPopupValue(i, item.GetHashCode().ToString());
				}
			}
			finally
			{
				updatingChoices_ = false;
			}
		}

		protected void UpdateLabel()
		{
			if (popup_ == null)
				return;

			var visible = popup_.popup.visible;

			popup_.popup.currentValueNoCallback = "";
			if (selection_ != -1)
			{
				popup_.popup.currentValueNoCallback =
					items_[selection_].GetHashCode().ToString();
			}

			popup_.popup.visible = visible;
		}

		private void InsertItemNoUpdate(Item i, int index)
		{
			if (index < 0)
				items_.Add(i);
			else
				items_.Insert(index, i);
		}

		private void OnSelectionChanged(string s)
		{
			if (updatingChoices_)
				return;

			try
			{
				int sel = -1;

				for (int i = 0; i < items_.Count; ++i)
				{
					if (items_[i].GetHashCode().ToString() == s)
					{
						sel = i;
						break;
					}
				}

				if (sel == -1)
					Log.Error("combobox: selected item '" + s + "' not found");

				Select(sel);
			}
			catch (Exception e)
			{
				Log.ErrorST(e.ToString());
			}
		}
	}
}
