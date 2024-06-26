﻿using MVR.FileManagementSecure;
using SimpleJSON;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VUI
{
	class CustomInputField : InputField
	{
		private static Vector2 NoPos = new Vector2(float.MinValue, float.MinValue);
		private const float DoubleClickTime = 0.5f;

		public delegate void EventCallback(PointerEventData data);
		public event EventCallback PointerDown, PointerUp, Click, DoubleClick, TripleClick;
		public event EventCallback Focused, Blurred;

		public delegate void KeyCallback();
		public event KeyCallback UpArrow, DownArrow;

		private TextBox parent_ = null;
		private bool selected_ = false;
		private float lastClick_ = 0;
		private int clickCount_ = 0;
		private Vector2 lastClickPos_ = NoPos;

		public Logger Log
		{
			get { return parent_?.Log ?? Logger.Global; }
		}

		public TextBox Parent
		{
			set { parent_ = value; }
		}

		protected override void LateUpdate()
		{
			base.LateUpdate();

			if (isFocused != selected_)
			{
				if (isFocused)
				{
					selected_ = true;
					Focused?.Invoke(null);
				}
				else
				{
					selected_ = false;
					Blurred?.Invoke(null);
				}
			}

			if (Input.GetKeyDown(KeyCode.DownArrow))
				DownArrow?.Invoke();
			else if (Input.GetKeyDown(KeyCode.UpArrow))
				UpArrow?.Invoke();
		}

		public override void OnPointerDown(PointerEventData data)
		{
			try
			{
				HandleOnPointerDown(data);
			}
			catch (Exception e)
			{
				Log.ErrorST(e.ToString());
			}
		}

		public override void OnPointerUp(PointerEventData data)
		{
			try
			{
				HandleOnPointerUp(data);
			}
			catch (Exception e)
			{
				Log.ErrorST(e.ToString());
			}
		}

		public override void OnDeselect(BaseEventData data)
		{
			try
			{
				HandleOnDeselect(data);
			}
			catch (Exception e)
			{
				Log.ErrorST(e.ToString());
			}
		}

		public int CaretPosition(Vector2 pos)
		{
			return GetCharacterIndexFromPosition(pos);
		}

		public void SelectAllText()
		{
			SelectAll();
		}

		private void HandleOnPointerDown(PointerEventData data)
		{
			base.OnPointerDown(data);
			PointerDown?.Invoke(data);

			if (!selected_)
			{
				Focused?.Invoke(data);
				selected_ = true;
			}

			++clickCount_;

			if (clickCount_ == 1)
			{
				lastClick_ = Time.unscaledTime;
				lastClickPos_ = data.position;
			}
			else if (clickCount_ == 2)
			{
				var timeDiff = Time.unscaledTime - lastClick_;
				var posDiff = Vector2.Distance(data.position, lastClickPos_);

				if (timeDiff <= DoubleClickTime && posDiff <= 4)
				{
					DoubleClick?.Invoke(data);
				}
				else
				{
					lastClick_ = Time.unscaledTime;
					lastClickPos_ = data.position;
					clickCount_ = 1;
				}
			}
			else if (clickCount_ == 3)
			{
				var timeDiff = Time.unscaledTime - lastClick_;
				var posDiff = Vector2.Distance(data.position, lastClickPos_);

				if (timeDiff <= DoubleClickTime && posDiff <= 4)
				{
					TripleClick?.Invoke(data);
					lastClick_ = 0;
					lastClickPos_ = NoPos;
					clickCount_ = 0;
				}
				else
				{
					lastClick_ = Time.unscaledTime;
					lastClickPos_ = data.position;
					clickCount_ = 1;
				}
			}
		}

		private void HandleOnPointerUp(PointerEventData data)
		{
			base.OnPointerUp(data);

			PointerUp?.Invoke(data);

			if (clickCount_ == 1)
				Click?.Invoke(data);
		}

		private void HandleOnDeselect(BaseEventData data)
		{
			bool invoke = selected_;

			selected_ = false;
			base.OnDeselect(data);

			if (invoke)
				Blurred?.Invoke(null);
		}
	}


	class AutoComplete
	{
		private const int Max = 30;

		public delegate void ChangedHandler();
		public event ChangedHandler Changed;

		private readonly TextBox tb_;
		private Panel panel_ = null;
		private readonly List<string> list_ = new List<string>();
		private ListView<string> listView_ = null;
		private bool ignore_ = false;
		private bool enabled_ = false;
		private string file_ = null;
		private float height_ = 500;
		private bool completeString_ = false;

		public AutoComplete(TextBox tb)
		{
			tb_ = tb;
		}

		public Widget Widget
		{
			get { return panel_; }
		}

		public bool Visible
		{
			get { return panel_?.Visible ?? false; }
		}

		public float Height
		{
			get { return height_; }
			set { height_ = value; }
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
					if (!enabled_)
						Hide();
				}
			}
		}

		public string File
		{
			get
			{
				return file_;
			}

			set
			{
				if (file_ != value)
				{
					file_ = value;
					if (file_ == "")
						file_ = null;

					if (!string.IsNullOrEmpty(file_))
						Reload();
				}
			}
		}

		public bool CompleteString
		{
			get { return completeString_; }
			set { completeString_ = value; }
		}

		public void Add(string s)
		{
			if (!enabled_)
				return;

			if (ignore_) return;

			s = s.Trim();
			if (string.IsNullOrEmpty(s))
				return;

			try
			{
				ignore_ = true;

				// move to top
				int i = list_.IndexOf(s);
				if (i >= 0 && i < list_.Count)
					list_.RemoveAt(i);

				list_.Insert(0, s);

				while (list_.Count > Max)
					list_.RemoveAt(list_.Count - 1);

				ItemsChanged();
			}
			finally
			{
				ignore_ = false;
			}
		}

		public void Remove(string s)
		{
			if (!enabled_)
				return;

			if (ignore_) return;

			s = s.Trim();
			if (string.IsNullOrEmpty(s) || list_.IndexOf(s) == -1)
				return;

			try
			{
				ignore_ = true;
				list_.Remove(s);
				ItemsChanged();
			}
			finally
			{
				ignore_ = false;
			}
		}

		public string Complete(string s)
		{
			if (string.IsNullOrEmpty(s))
				return null;

			for (int i = 0; i < list_.Count; ++i)
			{
				if (list_[i].StartsWith(s, StringComparison.OrdinalIgnoreCase))
				{
					SelectQuiet(i);
					return list_[i];
				}
			}

			return null;
		}

		public string Next()
		{
			if (listView_.Count == 0)
				return "";

			if (listView_.SelectedIndex < (listView_.Count - 1))
				SelectQuiet(listView_.SelectedIndex + 1);

			return listView_.Selected;
		}

		public string Previous()
		{
			if (listView_.Count == 0)
				return "";

			if (listView_.SelectedIndex > 0)
				SelectQuiet(listView_.SelectedIndex - 1);

			return listView_.Selected;
		}

		private void SelectQuiet(int i)
		{
			try
			{
				ignore_ = true;
				listView_?.Select(i);
			}
			finally
			{
				ignore_ = false;
			}
		}

		private void ItemsChanged()
		{
			if (listView_ != null)
				listView_.SetItems(list_);

			if (list_.Count == 0)
				Hide();

			Save();
			Changed?.Invoke();
		}

		public void Show()
		{
			if (!enabled_ || Visible)
				return;

			if (list_.Count == 0)
				return;

			if (panel_ == null)
			{
				listView_ = new ListView<string>();
				listView_.SelectionChanged += OnSelection;
				listView_.ItemRightClicked += OnRightClick;
				listView_.SetItems(list_);

				panel_ = new Panel(new BorderLayout());
				panel_.Add(listView_, BorderLayout.Center);
				panel_.BackgroundColor = Style.Theme.BackgroundColor;
				panel_.Clickthrough = false;

				tb_.GetRoot().FloatingPanel.Add(panel_);
				listView_.Events.Blur += OnBlur;
			}

			var r = tb_.AbsoluteClientBounds;
			r.Top += r.Height;
			r.Bottom = r.Top + height_;

			panel_.SetBounds(r);
			panel_.Visible = true;
			panel_.BringToTop();
			panel_.DoLayout();

			listView_.ScrollToTop();

			panel_.GetRoot().SetOpenedPopup(listView_);
		}

		public void Hide()
		{
			if (panel_ != null)
			{
				panel_.Visible = false;
				panel_.GetRoot().SetOpenedPopup(null);
			}
		}

		private void Save()
		{
			if (file_ == null)
				return;

			var j = new JSONClass();

			var a = new JSONArray();
			foreach (var s in list_)
				a.Add(new JSONData(s));

			j.Add("autocomplete", a);

			SuperController.singleton.SaveJSON(j, file_);
		}

		public void Reload()
		{
			if (file_ == null)
				return;

			if (!FileManagerSecure.FileExists(file_))
				return;

			var j = SuperController.singleton.LoadJSON(file_)?.AsObject;
			if (j == null)
				return;

			if (j.HasKey("autocomplete"))
			{
				var a = j["autocomplete"].AsArray;
				if (a == null)
					return;

				list_.Clear();

				foreach (JSONNode n in a)
					list_.Add(n.Value);

				if (listView_ != null)
					listView_.SetItems(list_);
			}
		}

		private void OnSelection(string s)
		{
			if (!enabled_)
				return;

			if (ignore_) return;

			if (string.IsNullOrEmpty(s))
				return;

			Add(s);
			tb_.Text = s;
			tb_.Blur();

			Hide();
		}

		private void OnRightClick(string s)
		{
			if (!enabled_)
				return;

			if (ignore_) return;

			if (string.IsNullOrEmpty(s))
				return;

			Remove(s);
		}

		private void OnBlur(FocusEvent e)
		{
			Hide();
		}
	}


	class TextBox : Widget
	{
		public override string TypeName { get { return "TextBox"; } }

		public struct Polishing
		{
			public Color disabledTextColor;
			public Color backgroundColor, disabledBackgroundColor;
			public Color selectionBackgroundColor;
			public Color placeholderTextColor, disabledPlaceholderTextColor;

			public static Polishing Default
			{
				get
				{
					var p = new Polishing();

					p.disabledTextColor = Style.Theme.DisabledEditableTextColor;
					p.backgroundColor = Style.Theme.EditableBackgroundColor;
					p.disabledBackgroundColor = Style.Theme.DisabledEditableBackgroundColor;
					p.selectionBackgroundColor = Style.Theme.EditableSelectionBackgroundColor;
					p.placeholderTextColor = Style.Theme.PlaceholderTextColor;
					p.disabledPlaceholderTextColor = Style.Theme.DisabledPlaceholderTextColor;

					return p;
				}
			}
		}

		public class Validation
		{
			public string text;
			public bool valid;
		}

		public delegate void ValidateCallback(Validation v);
		public delegate void StringCallback(string s);

		public event ValidateCallback Validate;

		// after changes are committed
		public event StringCallback Edited;

		// for each character
		public event StringCallback Changed;

		// on enter, after Edited is fired
		public event StringCallback Submitted;

		// on escape
		public event Callback Cancelled;


		private string text_ = "";
		private string oldText_ = null;
		private string placeholder_ = "";
		private GameObject field_ = null;
		private Text fieldText_ = null;
		private CustomInputField input_ = null;
		private bool ignore_ = false;
		private bool ignoreAcForCompletion_ = false;
		private bool ignoreAcForAdd_ = false;
		private Insets textMargins_ = Insets.Zero;
		private Polishing polishing_ = Polishing.Default;
		private AutoComplete ac_;

		public TextBox(string t = "", string placeholder = "", StringCallback edited = null)
		{
			text_ = t;
			placeholder_ = placeholder;
			ac_ = new AutoComplete(this);

			TextColor = Style.Theme.EditableTextColor;
			Borders = new Insets(1);
			Cursor = Glue.CursorProvider.Beam;

			if (edited != null)
				Edited += edited;

			Events.Blur += OnBlur;
		}

		public TextBox(StringCallback edited)
			: this("", "", edited)
		{
		}

		public CustomInputField InputField
		{
			get { return input_; }
		}

		public string Text
		{
			get
			{
				return text_;
			}

			set
			{
				if (text_ != value)
				{
					try
					{
						ignoreAcForAdd_ = true;
						text_ = value;

						if (input_ != null)
						{
							input_.text = value;
							OnEdited(text_);
						}
					}
					finally
					{
						ignoreAcForAdd_ = false;
					}
				}
			}
		}

		public string Placeholder
		{
			get
			{
				return placeholder_;
			}

			set
			{
				SetPlaceholder(value);
			}
		}

		public AutoComplete AutoComplete
		{
			get { return ac_; }
		}

		public Insets TextMargins
		{
			get
			{
				return textMargins_;
			}

			set
			{
				textMargins_ = value;
				UpdateTextRect();
			}
		}

		public Color DisabledTextColor
		{
			get
			{
				return polishing_.disabledTextColor;
			}

			set
			{
				if (polishing_.disabledTextColor != value)
				{
					polishing_.disabledTextColor = value;
					Polish();
				}
			}
		}

		public Color BackgroundColor
		{
			get
			{
				return polishing_.backgroundColor;
			}

			set
			{
				if (polishing_.backgroundColor != value)
				{
					polishing_.backgroundColor = value;
					Polish();
				}
			}
		}

		public Color DisabledBackgroundColor
		{
			get
			{
				return polishing_.disabledBackgroundColor;
			}

			set
			{
				if (polishing_.disabledBackgroundColor != value)
				{
					polishing_.disabledBackgroundColor = value;
					Polish();
				}
			}
		}

		public Color SelectionBackgroundColor
		{
			get
			{
				return polishing_.selectionBackgroundColor;
			}

			set
			{
				if (polishing_.selectionBackgroundColor != value)
				{
					polishing_.selectionBackgroundColor = value;
					Polish();
				}
			}
		}

		public Color PlaceholderTextColor
		{
			get
			{
				return polishing_.placeholderTextColor;
			}

			set
			{
				if (polishing_.placeholderTextColor != value)
				{
					polishing_.placeholderTextColor = value;
					Polish();
				}
			}
		}

		public Color DisabledPlaceholderTextColor
		{
			get
			{
				return polishing_.disabledPlaceholderTextColor;
			}

			set
			{
				if (polishing_.disabledPlaceholderTextColor != value)
				{
					polishing_.disabledPlaceholderTextColor = value;
					Polish();
				}
			}
		}

		public void Blur()
		{
			input_.DeactivateInputField();
		}

		protected override void DoFocus()
		{
			input_?.ActivateInputField();
		}

		protected override void DoCreate()
		{
			field_ = new GameObject("TextBoxInputField");
			field_.transform.SetParent(WidgetObject.transform, false);
			field_.AddComponent<RectTransform>();

			fieldText_ = field_.AddComponent<Text>();
			fieldText_.alignment = TextAnchor.MiddleLeft;

			input_ = field_.AddComponent<CustomInputField>();
			input_.Parent = this;
			input_.PointerDown += OnMouseDown;
			input_.Focused += OnFocused;
			input_.DoubleClick += OnDoubleClick;
			input_.TripleClick += OnTripleClick;
			input_.DownArrow += OnDownArrow;
			input_.UpArrow += OnUpArrow;
			input_.textComponent = fieldText_;
			input_.text = text_;
			input_.onEndEdit.AddListener(OnEdited);
			input_.onValueChanged.AddListener(OnValueChanged);
			input_.lineType = CustomInputField.LineType.SingleLine;

			var image = WidgetObject.AddComponent<UnityEngine.UI.Image>();
			image.raycastTarget = false;

			if (placeholder_ != "")
				CreatePlaceholder();

			Style.Setup(this, polishing_);
			UpdateTextRect();
		}

		private void SetPlaceholder(string s)
		{
			if (placeholder_ != s)
			{
				placeholder_ = s;

				if (input_ != null && placeholder_ != "")
				{
					CreatePlaceholder();
					input_.placeholder.GetComponent<Text>().text = placeholder_;
				}
			}
		}

		private void CreatePlaceholder()
		{
			if (input_.placeholder == null)
			{
				var go = new GameObject("TextBoxPlaceholder");
				go.transform.SetParent(input_.transform.parent, false);

				var text = go.AddComponent<Text>();
				text.supportRichText = false;
				text.horizontalOverflow = HorizontalWrapMode.Overflow;
				text.raycastTarget = false;

				var rt = text.rectTransform;
				rt.anchorMin = new Vector2(0, 0);
				rt.anchorMax = new Vector2(1, 1);
				rt.offsetMin = new Vector2(5, 0);
				rt.offsetMax = new Vector2(0, -5);

				input_.placeholder = text;
				input_.placeholder.GetComponent<Text>().text = placeholder_;

				Style.SetupPlaceholder(this, polishing_);
			}
		}

		private void UpdateTextRect()
		{
			if (input_ != null)
			{
				var rt = input_.GetComponent<RectTransform>();

				rt.anchorMin = new Vector2(0, 0);
				rt.anchorMax = new Vector2(1, 1);

				rt.offsetMin = new Vector2(textMargins_.Left + 5, textMargins_.Top);
				rt.offsetMax = new Vector2(-textMargins_.Right, -textMargins_.Bottom);
			}
		}

		protected override void DoSetEnabled(bool b)
		{
			input_.interactable = b;
		}

		protected override void DoPolish()
		{
			base.DoPolish();
			Style.Polish(this, polishing_);
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			var tl = TextLength(text_);
			var w = tl + Style.Metrics.TextBoxHorizontalPadding;

			return new Size(
				Math.Max(w, Style.Metrics.TextBoxPreferredSize.Width),
				Style.Metrics.TextBoxPreferredSize.Height);
		}

		protected override Size DoGetMinimumSize()
		{
			return Style.Metrics.TextBoxMinimumSize;
		}

		protected override void DoSetRender(bool b)
		{
			WidgetObject.gameObject.SetActive(b);
		}

		private void OnMouseDown(PointerEventData data)
		{
			try
			{
				Focus();
			}
			catch (Exception e)
			{
				Log.ErrorST(e.ToString());
			}
		}

		private void OnFocused(PointerEventData data)
		{
			if (data != null)
			{
				Vector2 pos;

				RectTransformUtility.ScreenPointToLocalPointInRectangle(
					input_.GetComponent<RectTransform>(), data.position,
					data.pressEventCamera, out pos);

				int c = input_.CaretPosition(pos);

				var old = input_.selectionColor;
				input_.selectionColor = new Color(0, 0, 0, 0);

				TimerManager.Instance.CreateTimer(Timer.Immediate, () =>
				{
					input_.caretPosition = c;
					input_.selectionAnchorPosition = c;
					input_.selectionFocusPosition = c;
					input_.selectionColor = old;
					input_.ForceLabelUpdate();
				});
			}

			ac_.Show();
		}

		private void OnDoubleClick(PointerEventData data)
		{
			Vector2 pos;

			RectTransformUtility.ScreenPointToLocalPointInRectangle(
				input_.GetComponent<RectTransform>(), data.position,
				data.pressEventCamera, out pos);

			int caret = input_.CaretPosition(pos);
			var range = Utilities.WordRange(input_.text, caret);

			input_.caretPosition = range[1];
			input_.selectionAnchorPosition = range[0];
			input_.selectionFocusPosition = range[1];
			input_.ForceLabelUpdate();
		}

		private void OnTripleClick(PointerEventData data)
		{
			input_.SelectAllText();
		}

		private void OnDownArrow()
		{
			if (ac_.Enabled)
			{
				try
				{
					ignoreAcForCompletion_ = true;
					Text = ac_.Next();
					SelectAll();
				}
				finally
				{
					ignoreAcForCompletion_ = false;
				}
			}
		}

		private void OnUpArrow()
		{
			if (ac_.Enabled)
			{
				try
				{
					ignoreAcForCompletion_ = true;
					Text = ac_.Previous();
					SelectAll();
				}
				finally
				{
					ignoreAcForCompletion_ = false;
				}
			}
		}

		private void OnValueChanged(string s)
		{
			if (Validate != null)
				return;

			try
			{
				if (ignore_)
					return;

				text_ = s;

				if (ac_.CompleteString)
				{
					if (ac_.Enabled && !ignoreAcForCompletion_)
					{
						if (!Input.GetKey(KeyCode.Backspace) && !Input.GetKey(KeyCode.Delete))
						{
							var c = ac_.Complete(s);

							if (c != null)
							{
								bool resetIgnore = (ignore_ == false);

								try
								{
									ignore_ = true;
									text_ = c;
									input_.text = c;
									Select(s.Length, text_.Length);
								}
								finally
								{
									if (resetIgnore)
										ignore_ = false;
								}
							}
						}
					}
				}

				Changed?.Invoke(s);
			}
			catch (Exception e)
			{
				Log.ErrorST(e.ToString());
			}
		}

		public void Select(int begin, int end)
		{
			input_.selectionAnchorPosition = begin;
			input_.selectionFocusPosition = end;
		}

		public void SelectAll()
		{
			input_.SelectAllText();
		}

		private void OnEdited(string s)
		{
			try
			{
				if (ignore_)
					return;

				if (input_.wasCanceled)
				{
					Cancelled?.Invoke();
					ac_.Hide();
					return;
				}

				if (Validate != null)
				{
					var v = new Validation();
					v.text = s;
					v.valid = true;

					Validate(v);

					if (!v.valid)
					{
						input_.text = text_;
						return;
					}

					text_ = v.text;
					input_.text = text_;
				}
				else
				{
					text_ = s;
				}

				if (oldText_ != text_)
				{
					Edited?.Invoke(text_);

					if (!ac_.Visible && !ignoreAcForAdd_)
						ac_.Add(text_);

					oldText_ = text_;
				}

				if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
				{
					Submitted?.Invoke(text_);
					ac_.Add(text_);
					ac_.Hide();
				}
			}
			catch (Exception e)
			{
				Log.ErrorST(e.ToString());
			}
		}

		private void OnBlur(FocusEvent e)
		{
			if (!ac_.Enabled || !ac_.Visible || !e.Other.HasParent(ac_.Widget))
			{
				if (ac_.Enabled && ac_.Visible)
					ac_.Add(text_);

				ac_.Hide();
			}
		}
	}


	class FloatTextBox : TextBox
	{
		public delegate void FloatCallback(float f);

		// after changes are committed
		public event FloatCallback FloatEdited;

		// for each character
		public event FloatCallback FloatChanged;

		// on enter, after Edited is fired
		public event FloatCallback FloatSubmitted;



		public FloatTextBox(string t = "", string placeholder = "", FloatCallback edited = null)
			: base(t, placeholder)
		{
			if (edited != null)
				FloatEdited += edited;

			Edited += (s) =>
			{
				float f;
				if (float.TryParse(s, out f))
					FloatEdited?.Invoke(f);
			};

			Changed += (s) =>
			{
				float f;
				if (float.TryParse(s, out f))
					FloatChanged?.Invoke(f);
			};

			Submitted += (s) =>
			{
				float f;
				if (float.TryParse(s, out f))
					FloatSubmitted?.Invoke(f);
			};

			Validate += (v) =>
			{
				float f;

				if (float.TryParse(v.text, out f))
				{
					v.text = $"{f:0.00}";
					v.valid = true;
				}
				else
				{
					v.valid = false;
				}
			};
		}

		public FloatTextBox(FloatCallback edited)
			: this("", "", edited)
		{
		}
	}


	class SearchBox : Panel
	{
		public delegate void Handler(string s);
		public event Handler Changed;

		private TextBox box_;
		private ToolButton clear_;

		public SearchBox(string placeholder)
		{
			Layout = new BorderLayout();

			box_ = new TextBox("", placeholder);
			box_.TextMargins = new Insets(0, 0, 44, 0);
			box_.Changed += (s) => Changed?.Invoke(s);

			var clearPanel = new Panel(new HorizontalFlow(0, Align.VCenterRight));
			clear_ = new ToolButton("X");
			clear_.Margins = new Insets(0, 2, 3, 1);
			clear_.MaximumSize = new Size(35, 35);
			clear_.Clicked += () => box_.Text = "";

			clearPanel.Add(clear_);

			Add(box_, BorderLayout.Center);
			Add(clearPanel, BorderLayout.Center);
		}

		public string Text
		{
			get
			{
				return box_?.Text ?? "";
			}

			set
			{
				if (box_ != null)
					box_.Text = value;
			}
		}

		public TextBox TextBox
		{
			get { return box_; }
		}

		public AutoComplete AutoComplete
		{
			get { return box_.AutoComplete; }
		}
	}
}
