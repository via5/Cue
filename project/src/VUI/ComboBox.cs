using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VUI
{
	class ComboBoxList<ItemType> : TypedList<ItemType>
		where ItemType : class
	{
		public override string TypeName { get { return "ComboBoxList"; } }

		public event Callback Opened;

		private GameObject arrowObject_ = null;
		private WidgetBorderGraphics borders_ = null;
		private TextBox filter_ = null;
		private bool filterable_ = false;
		private bool accuratePreferredSize_ = true;
		private int popupHeight_ = -1, popupWidth_ = -1;


		public ComboBoxList(List<ItemType> items = null)
			: this(items, null)
		{
		}

		public ComboBoxList(ItemCallback selectionChanged)
			: this(null, selectionChanged)
		{
		}

		public ComboBoxList(List<ItemType> items, ItemCallback selectionChanged)
			: base(items, selectionChanged)
		{
			Events.PointerDown += OnPointerDown;
		}

		public GameObject Arrow
		{
			get { return arrowObject_; }
		}

		public bool Filterable
		{
			get { return filterable_; }
			set { filterable_ = value; }
		}

		public bool AccuratePreferredSize
		{
			get { return accuratePreferredSize_; }
			set { accuratePreferredSize_ = value; }
		}

		public int PopupHeight
		{
			get
			{
				return popupHeight_;
			}

			set
			{
				popupHeight_ = value;

				if (Popup != null)
					Popup.popupPanelHeight = value;
			}
		}

		public int PopupWidth
		{
			get
			{
				return popupWidth_;
			}

			set
			{
				popupWidth_ = value;

				if (Popup?.popup?.popupPanel  != null)
				{
					var sd = Popup.popup.popupPanel.sizeDelta;
					sd.x = popupWidth_;
					Popup.popup.popupPanel.sizeDelta = sd;
				}
			}
		}

		public override void SetItems(ItemType[] items, ItemType sel = null)
		{
			base.SetItems(items, sel);

			if (SelectedIndex == -1 && Count > 0)
				Select(0);
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			if (!accuratePreferredSize_)
				return new Size(maxWidth, maxHeight);

			float widest = 0;

			foreach (var i in InternalItems)
				widest = Math.Max(widest, TextLength(i.Text) + 50);

			return new Size(Math.Max(200, widest), 40);
		}

		protected override void DoPostCreate()
		{
			if (filterable_)
			{
				filter_.Create();
				filter_.MainObject.transform.SetParent(FilterParent(), false);
			}
		}

		protected override GameObject CreateGameObject()
		{
			return UnityEngine.Object.Instantiate(
				Glue.PluginManager.configurableFilterablePopupPrefab).gameObject;
		}

		private IEnumerator CoSetPosition()
		{
			yield return new WaitForEndOfFrame();
			//yield return new WaitForEndOfFrame();

			Popup.popup.popupPanel.offsetMin = new Vector2(
				0, Popup.popup.popupPanel.offsetMin.y);
		}

		protected override void DoCreate()
		{
			base.DoCreate();

			if (filterable_)
			{
				filter_ = new TextBox("", "Filter");
				filter_.FocusFlags = Root.FocusKeepPopup;
				filter_.Changed += OnFilterChanged;
			}
			else
			{
				Popup.popup.useFiltering = false;
			}

			if (popupHeight_ >= 0)
				Popup.popupPanelHeight = popupHeight_;

			if (popupWidth_ >= 0)
			{
				var sd = Popup.popup.popupPanel.sizeDelta;
				sd.x = popupWidth_;
				Popup.popup.popupPanel.sizeDelta = sd;
			}

			Popup.popup.onOpenPopupHandlers += () =>
			{
				var rt2 = borders_.gameObject.GetComponent<RectTransform>();

				Utilities.SetRectTransform(rt2, new Rectangle(
					0, 0, new Size(
					Popup.popup.popupPanel.rect.width,
					Popup.popup.popupPanel.rect.height)));

				if (popupWidth_ >= 0)
				{
					//var sd = Popup.popup.popupPanel.sizeDelta;
					//sd.x = popupWidth_;
					//Popup.popup.popupPanel.sizeDelta = sd;

					SuperController.singleton.StartCoroutine(CoSetPosition());
				}
			};

			var h = Popup.popup.topButton.gameObject.AddComponent<MouseHandler>();
			h.Clicked += (data) =>
			{
				try
				{
					if (Popup.popup.visible)
						OnOpen();
				}
				catch (Exception e)
				{
					Log.ErrorST(e.ToString());
				}
			};

			arrowObject_ = new GameObject("ComboBoxArrow");
			arrowObject_.transform.SetParent(WidgetObject.transform, false);
			arrowObject_.AddComponent<RectTransform>();
			arrowObject_.AddComponent<LayoutElement>();

			var arrowText = arrowObject_.AddComponent<Text>();
			arrowText.alignment = TextAnchor.MiddleRight;
			arrowText.raycastTarget = false;
			arrowText.text = Utilities.DownArrow;

			var go = new GameObject("ComboBoxBorders");
			go.transform.SetParent(Popup.popup.popupPanel.transform, false);

			borders_ = go.AddComponent<WidgetBorderGraphics>();
			borders_.Borders = new Insets(2);
			borders_.Color = BorderColor;

			var text = Popup.popup.topButton?.GetComponentInChildren<Text>();
			if (text != null)
				text.alignment = TextAnchor.MiddleLeft;

			Style.Setup(this);
			FixBackgroundColor();
		}

		protected override void DoPolish()
		{
			base.DoPolish();
			Style.Polish(this);
			FixBackgroundColor();
		}

		private void FixBackgroundColor()
		{
			// there's something weird with the combobox panel background color,
			// it seems to have some transparency applied that can't be removed
			//
			// this applies ComboBoxBackgroundColor to the background, which is
			// slightly lighter than the default background
			//
			// it's not perfect, but it's close enough

			var panel = Popup.popup.popupPanel;
			var images = panel.GetComponentsInChildren<UnityEngine.UI.Image>();

			foreach (var bg in images)
				bg.color = Style.Theme.ComboBoxBackgroundColor;
		}

		protected override void AfterUpdateBounds()
		{
			var rect = arrowObject_.GetComponent<RectTransform>();
			rect.offsetMin = new Vector2(0, 0);
			rect.offsetMax = new Vector2(Bounds.Width - 10, Bounds.Height);
			rect.anchorMin = new Vector2(0, 0);
			rect.anchorMax = new Vector2(0, 0);
			rect.anchoredPosition = new Vector2(Bounds.Width / 2, Bounds.Height / 2);
		}

		protected virtual void OnOpen()
		{
			Focus();
			GetRoot().SetOpenedPopup(this, Popup.popup);
			Utilities.BringToTop(Popup.popup.popupPanel);

			UpdateFilterBounds();

			if (filterable_)
			{
				// the popup hasn't processed the event yet and it will steal
				// focus when it does, so focus the filter after that
				TimerManager.Instance.CreateTimer(Timer.Immediate, () =>
				{
					filter_.Focus();
				});
			}

			Opened?.Invoke();
		}

		Transform FilterParent()
		{
			return Popup.popup.popupPanel;
		}

		private void UpdateFilterBounds()
		{
			if (!filterable_)
				return;

			var parent = FilterParent();
			if (parent == null)
			{
				Log.Error("ComboBox: no filter parent");
				return;
			}

			var rt = parent.GetComponent<RectTransform>();
			var r = rt.rect;
			var h = filter_.GetRealPreferredSize(DontCare, DontCare).Height;

			filter_.SetBounds(Rectangle.FromPoints(
				0, r.height, r.width, r.height + h));

			filter_.UpdateBounds();
		}

		private void OnFilterChanged(string s)
		{
			Popup.popup.filter = s;
		}

		private void OnPointerDown(PointerEvent e)
		{
			e.Bubble = false;
		}
	}


	class ComboBox<ItemType> : Widget
		where ItemType : class
	{
		public override string TypeName { get { return "ComboBox"; } }

		public event Callback Opened;

		public delegate void ItemCallback(ItemType item);
		public event ItemCallback SelectionChanged;

		public delegate void IndexCallback(int index);
		public event IndexCallback SelectionIndexChanged;

		private readonly Panel buttons_ = null;
		private readonly CustomButton up_ = null;
		private readonly CustomButton down_ = null;
		private ComboBoxList<ItemType> list_;
		private bool nav_ = false;


		public ComboBox(List<ItemType> items = null)
			: this(items, null)
		{
		}

		public ComboBox(IndexCallback selectionChanged)
			: this(null, null)
		{
			if (selectionChanged != null)
				SelectionIndexChanged += selectionChanged;
		}

		public ComboBox(ItemCallback selectionChanged)
			: this(null, selectionChanged)
		{
		}

		public ComboBox(ItemType[] items, IndexCallback selectionChanged)
			: this(new List<ItemType>(items), null, selectionChanged)
		{
		}

		public ComboBox(
			List<ItemType> items,
			ItemCallback selectionChanged,
			IndexCallback selectionIndexChanged = null)
		{
			buttons_ = new Panel(new VerticalFlow());

			up_ = new CustomButton(Utilities.UpArrow, OnUp);
			up_.FontSize = Style.Theme.ComboBoxNavTextSize;
			down_ = new CustomButton(Utilities.DownArrow, OnDown);
			down_.FontSize = Style.Theme.ComboBoxNavTextSize;
			list_ = new ComboBoxList<ItemType>(items);

			up_.MinimumSize = new Size(20, 20);
			down_.MinimumSize = new Size(20, 20);

			buttons_.Visible = false;
			buttons_.Add(up_);
			buttons_.Add(down_);

			Layout = new BorderLayout(3);
			Add(buttons_, BorderLayout.Left);
			Add(list_, BorderLayout.Center);

			list_.Opened += () => OnOpened();
			list_.SelectionChanged += (item) => SelectionChanged?.Invoke(item);
			list_.SelectionIndexChanged += (index) => SelectionIndexChanged?.Invoke(index);

			if (selectionChanged != null)
				SelectionChanged += selectionChanged;

			if (selectionIndexChanged != null)
				SelectionIndexChanged += selectionIndexChanged;
		}

		public ComboBox(List<ItemType> items, ItemType sel, ItemCallback selectionChanged)
			: this(items, selectionChanged)
		{
			Select(sel);
		}

		public ComboBoxList<ItemType> List
		{
			get { return list_; }
		}

		private void OnOpened()
		{
			Opened?.Invoke();
		}

		public bool NavButtons
		{
			get
			{
				return nav_;
			}

			set
			{
				nav_ = value;
				buttons_.Visible = value;
			}
		}

		public bool Filterable
		{
			get { return list_.Filterable; }
			set { list_.Filterable = value; }
		}

		public bool AccuratePreferredSize
		{
			get { return list_.AccuratePreferredSize; }
			set { list_.AccuratePreferredSize = value; }
		}

		public int PopupHeight
		{
			get { return list_.PopupHeight; }
			set { list_.PopupHeight = value; }
		}

		public int PopupWidth
		{
			get { return list_.PopupWidth; }
			set { list_.PopupWidth = value; }
		}

		public void AddItem(ItemType i, bool select=false)
		{
			list_.AddItem(i, select);
		}

		public void RemoveItem(ItemType item)
		{
			list_.RemoveItem(item);
		}

		public void RemoveItemAt(int index)
		{
			list_.RemoveItemAt(index);
		}

		public void SetItemAt(int index, ItemType item)
		{
			list_.SetItemAt(index, item);
		}

		public List<ItemType> Items
		{
			get { return list_.Items; }
		}

		public ItemType At(int index)
		{
			return list_.At(index);
		}

		public int Count
		{
			get { return list_.Count; }
		}

		public void Clear()
		{
			list_.Clear();
		}

		public void SetItems(ItemType[] items, ItemType sel = null)
		{
			list_.SetItems(items, sel);

			if (SelectedIndex == -1 && Count > 0)
				Select(0);
		}

		public void SetItems(List<ItemType> items, ItemType sel = null)
		{
			SetItems(items.ToArray(), sel);
		}

		public void UpdateItemsText()
		{
			list_.UpdateItemsText();
		}

		public void UpdateItemText(int index)
		{
			list_.UpdateItemText(index);
		}

		public void UpdateItemText(ItemType item)
		{
			list_.UpdateItemText(item);
		}

		public int IndexOf(ItemType item)
		{
			return list_.IndexOf(item);
		}

		public void Select(ItemType item)
		{
			list_.Select(item);
		}

		public void Select(int i)
		{
			list_.Select(i);
		}

		public ItemType Selected
		{
			get { return list_.Selected; }
		}

		public int SelectedIndex
		{
			get { return list_.SelectedIndex; }
		}

		protected override void DoSetEnabled(bool b)
		{
			base.DoSetEnabled(b);
			list_.Enabled = b;
		}

		private void OnUp()
		{
			if (SelectedIndex > 0)
				Select(SelectedIndex - 1);
		}

		private void OnDown()
		{
			if (SelectedIndex < (Count - 1))
				Select(SelectedIndex + 1);
		}
	}
}
