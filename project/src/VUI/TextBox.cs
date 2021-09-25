using System;
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
		public event EventCallback Down, Up, Click, DoubleClick, TripleClick;
		public event EventCallback Focused;


		private bool selected_ = false;
		private float lastClick_ = 0;
		private int clickCount_ = 0;
		private Vector2 lastClickPos_ = NoPos;

		public override void OnPointerDown(PointerEventData data)
		{
			try
			{
				HandleOnPointerDown(data);
			}
			catch (Exception e)
			{
				Glue.LogErrorST(e.ToString());
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
				Glue.LogErrorST(e.ToString());
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
				Glue.LogErrorST(e.ToString());
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
			Down?.Invoke(data);

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

			Up?.Invoke(data);

			if (clickCount_ == 1)
				Click?.Invoke(data);
		}

		private void HandleOnDeselect(BaseEventData data)
		{
			selected_ = false;
			base.OnDeselect(data);
		}
	}

	class TextBox : Widget
	{
		public override string TypeName { get { return "TextBox"; } }

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
		private string placeholder_ = "";
		private CustomInputField input_ = null;
		private bool ignore_ = false;
		private int focusflags_ = Root.FocusDefault;
		private Insets textMargins_ = Insets.Zero;

		public TextBox(string t = "", string placeholder = "")
		{
			text_ = t;
			placeholder_ = placeholder;
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
				text_ = value;

				if (input_ != null)
					input_.text = value;
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
				placeholder_ = value;

				if (input_ != null)
					input_.placeholder.GetComponent<Text>().text = placeholder_;
			}
		}

		public int FocusFlags
		{
			get { return focusflags_; }
			set { focusflags_ = value; }
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

		protected override void DoFocus()
		{
			input_.ActivateInputField();
		}

		protected override GameObject CreateGameObject()
		{
			var go = base.CreateGameObject();

			return go;
		}

		protected override void DoCreate()
		{
			var field = new GameObject("TextBoxInputField");
			field.transform.SetParent(WidgetObject.transform, false);
			field.AddComponent<RectTransform>();

			var text = field.AddComponent<Text>();
			text.color = Style.Theme.TextColor;
			text.fontSize = Style.Theme.DefaultFontSize;
			text.font = Style.Theme.DefaultFont;
			text.alignment = TextAnchor.MiddleLeft;

			input_ = field.AddComponent<CustomInputField>();
			input_.Down += OnMouseDown;
			input_.Focused += OnFocused;
			input_.DoubleClick += OnDoubleClick;
			input_.TripleClick += OnTripleClick;
			input_.textComponent = text;
			input_.text = text_;
			input_.onEndEdit.AddListener(OnEdited);
			input_.onValueChanged.AddListener(OnValueChanged);
			input_.lineType = CustomInputField.LineType.SingleLine;

			var image = WidgetObject.AddComponent<Image>();
			image.raycastTarget = false;

			// placeholder
			var go = new GameObject("TextBoxPlaceholder");
			go.transform.SetParent(input_.transform.parent, false);

			text = go.AddComponent<Text>();
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

			Style.Setup(this);
			UpdateTextRect();
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
			Style.Polish(this);
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			return new Size(
				Root.TextLength(Font, FontSize, text_) + 20, 40);
		}

		protected override Size DoGetMinimumSize()
		{
			return new Size(100, DontCare);
		}

		protected override void DoSetRender(bool b)
		{
			WidgetObject.gameObject.SetActive(b);
		}

		private void OnMouseDown(PointerEventData data)
		{
			try
			{
				GetRoot().SetFocus(this, focusflags_);
			}
			catch (Exception e)
			{
				Glue.LogErrorST(e.ToString());
			}
		}

		private void OnFocused(PointerEventData data)
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

		private void OnValueChanged(string s)
		{
			if (Validate != null)
				return;

			try
			{
				if (ignore_)
					return;

				text_ = s;
				Changed?.Invoke(s);
			}
			catch (Exception e)
			{
				Glue.LogErrorST(e.ToString());
			}
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

				Edited?.Invoke(text_);

				if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
					Submitted?.Invoke(text_);
			}
			catch (Exception e)
			{
				Glue.LogErrorST(e.ToString());
			}
		}
	}
}
