using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VUI
{
	class Metrics
	{
		public float CursorHeight
		{
			get { return 45; }
		}

		public float TooltipDelay
		{
			get { return 0.5f; }
		}

		public float MaxTooltipWidth
		{
			get { return 1000; }
		}

		public float TooltipBorderOffset
		{
			get { return 10; }
		}

		public Size ButtonMinimumSize
		{
			get { return new Size(150, 40); }
		}

		public Size TextBoxMinimumSize
		{
			get { return new Size(100, 40); }
		}

		public Size TextBoxPreferredSize
		{
			get { return new Size(200, 40); }
		}

		public int TextBoxHorizontalPadding
		{
			get { return 20; }
		}

		public Size SliderMinimumSize
		{
			get { return new Size(150, 40); }
		}

		public Size TabButtonMinimumSize
		{
			get { return new Size(120, 40); }
		}

		public Size ButtonPadding
		{
			get { return new Size(20, 0); }
		}

		public Size SelectedTabPadding
		{
			get { return new Size(0, 5); }
		}

		// space between toggle and label
		//
		public float ToggleLabelSpacing
		{
			get { return 5; }
		}

		// height of a tree item
		//
		public float TreeItemHeight
		{
			get { return 35; }
		}

		// vertical space between items
		//
		public float TreeItemSpacing
		{
			get { return 2; }
		}

		// padding to the left of all items
		//
		public float TreeViewLeftMargin
		{
			get { return 10; }
		}

		// padding to the top of the tree
		//
		public float TreeViewTopMargin
		{
			get { return 0; }
		}

		// width of one indent for a tree item
		//
		public float TreeIndentSize
		{
			get { return 25; }
		}

		// width of the + button
		//
		public float TreeToggleWidth
		{
			get { return 30; }
		}

		// width of the checkbox
		//
		public float TreeCheckBoxWidth
		{
			get { return 30; }
		}

		// width of the icon
		//
		public float TreeIconWidth
		{
			get { return 30; }
		}

		// space between the + button and label
		//
		public float TreeToggleSpacing
		{
			get { return 5; }
		}

		// space between the checkbox and label
		//
		public float TreeCheckboxSpacing
		{
			get { return 10; }
		}

		// space between the checkbox and the icon
		//
		public float TreeIconSpacing
		{
			get { return 10; }
		}

		// width of the bar
		//
		public float ScrollBarWidth
		{
			get { return 40; }
		}

		// max distance from handle, resets the scroll position if it's exceeded
		//
		public float ScrollBarMaxDragDistance
		{
			get { return 175; }
		}

		// width/height of the splitter handle
		//
		public float SplitterHandleSize
		{
			get { return 10; }
		}
	}


	class Theme
	{
		private Font defaultFont_ = null;
		private Font monospaceFont_ = null;

		private Font GetFont(string name)
		{
			var f = Resources.GetBuiltinResource<Font>(name);
			if (f != null)
				return f;

			f = Resources.GetBuiltinResource<Font>(name + ".ttf");
			if (f != null)
				return f;

			return Font.CreateDynamicFontFromOSFont(name, 24);
		}

		public Font DefaultFont
		{
			get
			{
				if (defaultFont_ == null)
					defaultFont_ = GetFont("Arial");

				return defaultFont_;
			}
		}

		public Font MonospaceFont
		{
			get
			{
				if (monospaceFont_ == null)
					monospaceFont_ = GetFont("Consolas");

				return monospaceFont_;
			}
		}

		public int DefaultFontSize
		{
			get { return 28; }
		}


		public Color BorderColor
		{
			get { return new Color(0.5f, 0.5f, 0.5f); }
		}

		public Color TextColor
		{
			get { return new Color(0.84f, 0.84f, 0.84f); }
		}

		public Color DisabledTextColor
		{
			get { return new Color(0.3f, 0.3f, 0.3f); }
		}

		public Color EditableTextColor
		{
			get { return TextColor; }
		}

		public Color DisabledEditableTextColor
		{
			get { return new Color(0.6f, 0.6f, 0.6f); }
		}

		public Color PlaceholderTextColor
		{
			get { return new Color(0.5f, 0.5f, 0.5f); }
		}

		public Color DisabledPlaceholderTextColor
		{
			get { return new Color(0.3f, 0.3f, 0.3f); }
		}

		public Color EditableBackgroundColor
		{
			get { return new Color(0, 0, 0, 0); }
		}

		public Color DisabledEditableBackgroundColor
		{
			get { return new Color(0, 0, 0, 0); }
		}

		public Color EditableSelectionBackgroundColor
		{
			get { return new Color(0.6f, 0.6f, 0.6f); }
		}

		public Color BackgroundColor
		{
			get { return new Color(0.12f, 0.12f, 0.12f); }
		}

		public Color ComboBoxBackgroundColor
		{
			// see ComboBox.FixBackgroundColor()
			get { return new Color(0.146f, 0.146f, 0.146f); }
		}

		public Color ScrollBarBackgroundColor
		{
			get { return new Color(0.10f, 0.10f, 0.10f); }
		}

		public Color OverlayBackgroundColor
		{
			get { return new Color(0, 0, 0, 0.8f); }
		}

		public Color ButtonBackgroundColor
		{
			get { return new Color(0.25f, 0.25f, 0.25f); }
		}

		public Color SliderBackgroundColor
		{
			get { return new Color(0, 0, 0, 0); }
		}

		public Color DisabledButtonBackgroundColor
		{
			get { return new Color(0.20f, 0.20f, 0.20f); }
		}

		public Color HighlightBackgroundColor
		{
			get { return new Color(0.30f, 0.30f, 0.30f); }
		}

		public Color SelectionBackgroundColor
		{
			get { return new Color(0.08f, 0.13f, 0.32f); }
		}

		public Color ActiveOverlayColor
		{
			get { return new Color(0, 0, 0, 0.5f); }
		}

		public int SliderTextSize
		{
			get { return 20; }
		}

		public int ComboBoxNavTextSize
		{
			get { return 12; }
		}

		public Color DialogTitleBackgroundColor
		{
			get { return new Color(0.05f, 0.05f, 0.05f); }
		}

		public Color SelectedTabBackgroundColor
		{
			get { return new Color(0.3f, 0.3f, 0.3f); }
		}

		public Color SelectedTabTextColor
		{
			get { return TextColor; }
		}

		public Color TabBackgroundColor
		{
			get { return ButtonBackgroundColor; }
		}

		public Color TabTextColor
		{
			get { return TextColor; }
		}

		public Color SplitterHandleBackgroundColor
		{
			get { return new Color(0.15f, 0.15f, 0.15f); }
		}

		public Color TreeViewToggleBorderColor
		{
			get { return new Color(0.25f, 0.25f, 0.25f); }
		}
	}


	class Style
	{
		private static readonly Logger log_ = new Logger("vui.style");
		private static readonly Theme theme_ = new Theme();
		private static readonly Metrics metrics_ = new Metrics();

		private class Info
		{
			private bool setFont_ = true;
			private bool enabled_ = true;
			private Font font_ = null;
			private FontStyle fontStyle_ = FontStyle.Normal;
			private int fontSize_ = -1;
			private Color textColor_ = Theme.TextColor;

			public Info(Widget w, bool setFont = true)
				: this(setFont, w.Enabled, w.Font, w.FontStyle, w.FontSize, w.TextColor)
			{
			}

			public Info(
				bool setFont, bool enabled,
				Font font, FontStyle fontStyle, int fontSize,
				Color textColor)
			{
				setFont_ = setFont;
				enabled_ = enabled;
				font_ = font;
				fontStyle_ = fontStyle;
				fontSize_ = fontSize;
				textColor_ = textColor;
			}

			public bool SetFont
			{
				get { return setFont_; }
			}

			public bool Enabled
			{
				get { return enabled_; }
			}

			public Font Font
			{
				get { return font_ ?? theme_.DefaultFont; }
			}

			public FontStyle FontStyle
			{
				get { return fontStyle_; }
			}

			public int FontSize
			{
				get { return fontSize_ < 0 ? theme_.DefaultFontSize : fontSize_; }
			}

			public Color TextColor
			{
				get { return textColor_; }
			}

			public Info WithFontSize(int size)
			{
				return new Info(setFont_, enabled_, font_, fontStyle_, size, textColor_);
			}

			public Info WithSetFont(bool b)
			{
				return new Info(b, enabled_, font_, fontStyle_, fontSize_, textColor_);
			}
		}


		public static Logger Log
		{
			get { return log_; }
		}

		public static Theme Theme
		{
			get { return theme_; }
		}

		public static Metrics Metrics
		{
			get { return metrics_; }
		}


		private static void ForComponent<T>(Component o, Action<T> f)
		{
			if (o == null)
			{
				Log.ErrorST("ForComponent null");
				return;
			}

			ForComponent<T>(o.gameObject, f);
		}

		private static void ForComponent<T>(GameObject o, Action<T> f)
		{
			if (o == null)
			{
				Log.ErrorST("ForComponent null");
				return;
			}

			var c = o.GetComponent<T>();

			if (c == null)
			{
				Log.ErrorST(
					"component " + typeof(T).ToString() + " not found " +
					"in " + o.name);

				return;
			}

			f(c);
		}

		private static void ForComponents<T>(GameObject o, Action<T> f)
		{
			if (o == null)
			{
				Log.ErrorST("ForComponents null");
				return;
			}

			var cs = o.GetComponentsInChildren<T>();

			if (cs.Length == 0)
			{
				Log.ErrorST(
					"component " + typeof(T).ToString() + " not found " +
					"in " + o.name);

				return;
			}

			for (int i=0; i<cs.Length;++i)
				f(cs[i]);
		}

		private static void ForComponentInChildren<T>(Component o, Action<T> f)
		{
			if (o == null)
			{
				Log.ErrorST("ForComponentInChildren null");
				return;
			}

			ForComponentInChildren<T>(o.gameObject, f);
		}

		private static void ForComponentInChildren<T>(GameObject o, Action<T> f)
		{
			if (o == null)
			{
				Log.ErrorST("ForComponentInChildren null");
				return;
			}

			var c = o.GetComponentInChildren<T>();

			if (c == null)
			{
				Log.ErrorST(
					"component " + typeof(T).ToString() + " not found in " +
					"children of " + o.name);

				return;
			}

			f(c);
		}

		private static void ForChildRecursive(Component parent, string name, Action<GameObject> f)
		{
			if (parent == null)
			{
				Log.ErrorST("ForChildRecursive null");
				return;
			}

			var c = Utilities.FindChildRecursive(parent, name);

			if (c == null)
			{
				Log.ErrorST(
					"child " + name + " not found in " + parent.name);

				return;
			}

			f(c);
		}

		private static void ForChildRecursiveOpt(Component parent, string name, Action<GameObject> f)
		{
			if (parent == null)
				return;

			var c = Utilities.FindChildRecursive(parent, name);
			if (c == null)
				return;

			f(c);
		}

		private static GameObject RequireChildRecursive(Component parent, string name)
		{
			if (parent == null)
				throw new Exception("RequireChildRecursive parent null");

			return RequireChildRecursive(parent.gameObject, name);
		}

		private static GameObject RequireChildRecursive(GameObject parent, string name)
		{
			if (parent == null)
				throw new Exception("RequireChildRecursive parent null");

			var child = Utilities.FindChildRecursive(parent, name);
			if (child == null)
				throw new Exception("child " + name + " not found in " + parent.name);

			return child;
		}


		public static void ClampScrollView(GameObject scrollView)
		{
			ForComponent<ScrollRect>(scrollView, (sr) =>
			{
				sr.movementType = ScrollRect.MovementType.Clamped;
			});
		}

		public static void UnclampScrollView(GameObject scrollView)
		{
			ForComponent<ScrollRect>(scrollView, (sr) =>
			{
				sr.movementType = ScrollRect.MovementType.Elastic;
			});
		}


		// this used to remember the background color because SetupRoot()
		// changed it, but that's gone now, so it doesn't remember anything
		public class RootRestore
		{
		}


		public static RootRestore SetupRoot(Transform t)
		{
			RootRestore rr = new RootRestore();

			if (t.GetComponent<MVRScriptUI>() != null)
			{
				ForChildRecursive(t, "Scroll View", (scrollView) =>
				{
					// clamp the whole script ui
					ClampScrollView(scrollView);
				});
			}

			return rr;
		}

		public static void RevertRoot(Transform t, RootRestore rr)
		{
			if (t.GetComponent<MVRScriptUI>() != null)
			{
				ForChildRecursive(t, "Scroll View", (scrollView) =>
				{
					// unclamp the whole script ui
					UnclampScrollView(scrollView);
				});
			}
		}


		public static void Setup(ColorPicker e)
		{
			Adjust(e);
			Polish(e);
		}

		public static void Adjust(ColorPicker e)
		{
			ForComponent<UIDynamicColorPicker>(e.WidgetObject, (picker) =>
			{
				Adjust(picker, new Info(e));
			});
		}

		public static void Polish(ColorPicker e)
		{
			ForComponent<UIDynamicColorPicker>(e.WidgetObject, (picker) =>
			{
				Polish(picker, new Info(e));
			});
		}


		public static void Setup<T>(BasicSlider<T> e)
		{
			Adjust(e);
			Polish(e);
		}

		public static void Adjust<T>(BasicSlider<T> e)
		{
			ForComponent<UIDynamicSlider>(e.WidgetObject, (slider) =>
			{
				Adjust(slider, new Info(e));
			});
		}

		public static void Polish<T>(BasicSlider<T> e)
		{
			ForComponent<UIDynamicSlider>(e.WidgetObject, (slider) =>
			{
				Polish(slider, new Info(e));
			});
		}


		public static void Setup(CheckBox e)
		{
			Adjust(e);
			Polish(e);
		}

		public static void Adjust(CheckBox e)
		{
			ForComponent<UIDynamicToggle>(e.WidgetObject, (toggle) =>
			{
				Adjust(toggle, new Info(e));
			});
		}

		public static void Polish(CheckBox e)
		{
			ForComponent<UIDynamicToggle>(e.WidgetObject, (toggle) =>
			{
				Polish(toggle, new Info(e));
			});
		}


		public static void Setup<ItemType>(ComboBoxList<ItemType> cb)
			where ItemType : class
		{
			Adjust(cb);
			Polish(cb);
		}

		public static void Adjust<ItemType>(ComboBoxList<ItemType> cb)
			where ItemType : class
		{
			ForComponent<UIDynamicPopup>(cb.WidgetObject, (popup) =>
			{
				Adjust(popup, new Info(cb));
			});


			var labelTextParent = cb.Popup?.popup?.labelText?.transform?.parent;

			if (labelTextParent == null)
			{
				Log.Error("ComboBox has no labelText parent");
			}
			else
			{
				ForComponent<RectTransform>(labelTextParent.gameObject, (rt) =>
				{
					rt.offsetMin = new Vector2(rt.offsetMin.x, rt.offsetMin.y);
					rt.offsetMax = new Vector2(rt.offsetMax.x, rt.offsetMax.y);
					rt.anchorMin = new Vector2(0, 1);
					rt.anchorMax = new Vector2(0, 1);
					rt.anchoredPosition = new Vector2(
						rt.offsetMin.x + (rt.offsetMax.x - rt.offsetMin.x) / 2,
						rt.offsetMin.y + (rt.offsetMax.y - rt.offsetMin.y) / 2);
				});
			}


			if (cb.Popup?.popup?.topButton == null)
			{
				Log.Error("ComboBox has no topButton");
			}
			else
			{
				// topButton is the actual combobox the user clicks to open the
				// popup

				// make it take the exact size as the parent, it normally has
				// an offset all around it
				ForComponent<RectTransform>(cb.Popup.popup.topButton.gameObject, (rt) =>
				{
					rt.offsetMin = new Vector2(rt.offsetMin.x - 12, rt.offsetMin.y - 6);
					rt.offsetMax = new Vector2(rt.offsetMax.x + 8, rt.offsetMax.y + 6);
					rt.anchoredPosition = new Vector2(
						rt.offsetMin.x + (rt.offsetMax.x - rt.offsetMin.x) / 2,
						rt.offsetMin.y + (rt.offsetMax.y - rt.offsetMin.y) / 2);
				});

				ForComponentInChildren<Text>(cb.Popup.popup.topButton, (text) =>
				{
					// avoid overlap with arrow
					text.rectTransform.offsetMax = new Vector2(
						text.rectTransform.offsetMax.x - 25,
						text.rectTransform.offsetMax.y);
				});
			}


			if (cb.Popup.popup.popupPanel == null)
			{
				Log.Error("ComboBox has no popupPanel");
			}
			else
			{
				var rt = cb.Popup.popup.popupPanel;
				rt.offsetMin = new Vector2(rt.offsetMin.x - 10, rt.offsetMin.y);
				rt.offsetMax = new Vector2(rt.offsetMax.x + 5, rt.offsetMax.y - 5);
			}
		}

		public static void Polish<ItemType>(ComboBoxList<ItemType> cb)
			where ItemType : class
		{
			ForComponent<UIDynamicPopup>(cb.WidgetObject, (popup) =>
			{
				Polish(popup, new Info(cb));
			});

			ForComponent<Text>(cb.Arrow, (text) =>
			{
				if (cb.Enabled)
					text.color = cb.TextColor;
				else
					text.color = theme_.DisabledTextColor;

				text.fontSize = (cb.FontSize > 0 ? cb.FontSize :theme_.DefaultFontSize);
				text.fontStyle = cb.FontStyle;
				text.font = cb.Font ? cb.Font : theme_.DefaultFont;
			});
		}


		public static void Setup<ItemType>(ListView<ItemType> list)
			where ItemType : class
		{
			Adjust(list);
			Polish(list);
		}

		public static void Adjust<ItemType>(ListView<ItemType> list)
			where ItemType : class
		{
			ForComponent<UIDynamicPopup>(list.WidgetObject, (popup) =>
			{
				Adjust(popup, new Info(list));
			});
		}

		public static void Polish<ItemType>(ListView<ItemType> list)
			where ItemType : class
		{
			ForComponent<UIDynamicPopup>(list.WidgetObject, (popup) =>
			{
				Polish(popup, new Info(list));
			});
		}

		public static void Setup(Button b, Button.Polishing p)
		{
			Adjust(b);
			Polish(b, p);
		}

		public static void Adjust(Button b)
		{
			ForComponent<UIDynamicButton>(b.WidgetObject, (button) =>
			{
				Adjust(button, new Info(b));
			});
		}

		public static void Polish(Button b, Button.Polishing p)
		{
			ForComponent<UIDynamicButton>(b.WidgetObject, (button) =>
			{
				Polish(button, p, new Info(b));
			});
		}


		public static void Setup(Label e)
		{
			Adjust(e);
			Polish(e);
		}

		public static void Adjust(Label e)
		{
			// also includes ellipsis, if any
			ForComponents<Text>(e.MainObject, (text) =>
			{
				Adjust(text, new Info(e));
			});
		}

		public static void Polish(Label e)
		{
			// also includes ellipsis, if any
			ForComponents<Text>(e.MainObject, (text) =>
			{
				Polish(text, new Info(e));
			});
		}


		public static void Setup(TextBox e)
		{
			Adjust(e);
			Polish(e);
		}

		public static void SetupPlaceholder(TextBox e)
		{
			if (e.InputField.placeholder != null)
			{
				var info = new Info(e);

				AdjustPlaceholder(e.InputField, info);
				PolishPlaceholder(e.InputField, info);
			}
		}

		public static void Adjust(TextBox e)
		{
			var input = e.InputField;
			if (input == null)
				Log.Error("TextBox has no InputField");
			else
				Adjust(input, new Info(e));
		}

		public static void Polish(TextBox e)
		{
			// textbox background
			ForComponentInChildren<UnityEngine.UI.Image>(e.WidgetObject, (bg) =>
			{
				if (e.Enabled)
					bg.color = theme_.EditableBackgroundColor;
				else
					bg.color = theme_.DisabledEditableBackgroundColor;
			});

			var input = e.InputField;
			if (input == null)
				Log.Error("TextBox has no InputField");
			else
				Polish(input, new Info(e));
		}


		private static void Adjust(UIDynamicToggle e, Info info)
		{
			var bg = e.transform.Find("Background");
			var bgRT = bg.GetComponent<RectTransform>();
			var cm = bg.Find("Checkmark");
			var cmRT = cm.GetComponent<RectTransform>();
			var label = e.transform.Find("Label");
			var labelRT = label.GetComponent<RectTransform>();

			// size of the toggle
			{
				var rt = bgRT;

				rt.offsetMin = new Vector2(0, rt.offsetMin.y - 8);
				rt.offsetMax = new Vector2(rt.offsetMax.x - 16, 0);
				rt.anchorMin = new Vector2(0, 1);
				rt.anchorMax = new Vector2(0, 1);
				rt.anchoredPosition = new Vector2(
					rt.offsetMin.x + (rt.offsetMax.x - rt.offsetMin.x) / 2,
					rt.offsetMin.y + (rt.offsetMax.y - rt.offsetMin.y) / 2);
			}

			// size of the checkmark within the toggle
			{
				var rt = cmRT;

				rt.offsetMin = new Vector2(0, rt.offsetMin.y - 10);
				rt.offsetMax = new Vector2(rt.offsetMax.x + 10, 0);
				rt.anchorMin = new Vector2(0, 1);
				rt.anchorMax = new Vector2(0, 1);
				rt.anchoredPosition = new Vector2(
					rt.offsetMin.x + (rt.offsetMax.x - rt.offsetMin.x) / 2,
					rt.offsetMin.y + (rt.offsetMax.y - rt.offsetMin.y) / 2);
			}

			// spacing between toggle and label
			{
				var rt = labelRT;

				rt.offsetMin = new Vector2(
					bgRT.rect.width + Metrics.ToggleLabelSpacing,
					rt.offsetMin.y);

				rt.anchoredPosition = new Vector2(
					rt.offsetMin.x + (rt.offsetMax.x - rt.offsetMin.x) / 2,
					rt.offsetMin.y + (rt.offsetMax.y - rt.offsetMin.y) / 2);
			}

			e.labelText.alignment = TextAnchor.MiddleLeft;

			Adjust(e.labelText, info);
		}

		private static void Polish(UIDynamicToggle e, Info info)
		{
			// background color of the whole widget
			e.backgroundImage.color = new Color(0, 0, 0, 0);

			if (info.Enabled)
			{
				// color of the text on the toggle
				e.textColor = info.TextColor;
			}
			else
			{
				e.textColor = theme_.DisabledTextColor;
			}

			// there doesn't seem to be any way to change the checkmark color,
			// so the box will have to stay white for now

			Polish(e.labelText, info);
		}

		private static void Adjust(UnityEngine.UI.Button e, Info info)
		{
			// no-op
		}

		private static void Polish(UnityEngine.UI.Button e, Button.Polishing p, Info info)
		{
			ForComponent<UnityEngine.UI.Image>(e, (i) =>
			{
				i.color = Color.white;
			});

			ForComponentInChildren<UIStyleText>(e, (st) =>
			{
				if (info.Enabled)
					st.color = info.TextColor;
				else
					st.color = p.disabledTextColor;

				if (info.SetFont)
				{
					st.fontSize = info.FontSize;
					st.fontStyle = info.FontStyle;
				}

				st.UpdateStyle();
			});

			ForComponent<UIStyleButton>(e, (sb) =>
			{
				sb.normalColor = p.backgroundColor;
				sb.highlightedColor = p.highlightBackgroundColor;
				sb.pressedColor = p.highlightBackgroundColor;
				sb.disabledColor = p.disabledBackgroundColor;
				sb.UpdateStyle();
			});
		}


		private static void Adjust(UIDynamicButton e, Info info)
		{
			Adjust(e.button, info);
		}

		private static void Polish(UIDynamicButton e, Button.Polishing p, Info info)
		{
			Polish(e.button, p, info);

			if (e.buttonText == null)
			{
				Log.Error("UIDynamicButton has no buttonText");
			}
			else
			{
				e.buttonText.font = info.Font;
				e.buttonText.fontSize = info.FontSize;
				e.buttonText.fontStyle = info.FontStyle;
			}
		}


		private static void Adjust(UIDynamicPopup e, Info info)
		{
			// popups normally have a label on the left side and this controls
			// the offset of the popup button; since the label is removed, this
			// must be 0 so the popup button is left aligned
			e.labelWidth = 0;

			if (e.popup == null)
			{
				Log.Error("UIDynamicPopup has no popup");
			}
			else
			{
				// the top and bottom padding in the list, this looks roughly
				// equivalent to what's on the left and right
				e.popup.topBottomBuffer = 3;

				Adjust(e.popup, info);
			}
		}

		private static void Polish(UIDynamicPopup e, Info info)
		{
			if (e.popup == null)
				Log.Error("UIDynamicPopup has no popup");
			else
				Polish(e.popup, info);
		}


		private static void Adjust(Text e, Info info)
		{
			// no-op
		}

		private static void Polish(Text e, Info info)
		{
			if (info.Enabled)
				e.color = info.TextColor;
			else
				e.color = Theme.DisabledTextColor;

			if (info.SetFont)
			{
				e.fontSize = info.FontSize;
				e.fontStyle = info.FontStyle;
				e.font = info.Font;
			}
		}


		private static void Adjust(InputField input, Info info)
		{
			// field
			input.caretWidth = 2;

			// placeholder
			if (input.placeholder != null)
				AdjustPlaceholder(input, info);
		}

		private static void Polish(InputField input, Info info)
		{
			// textbox text
			var text = input.textComponent;
			if (text == null)
			{
				Log.Error("InputField has no textComponent");
			}
			else
			{
				if (info.Enabled)
					text.color = theme_.EditableTextColor;
				else
					text.color = theme_.DisabledEditableTextColor;

				if (info.SetFont)
				{
					text.fontSize = info.FontSize;
					text.fontStyle = info.FontStyle;
					text.font = info.Font;
				}
			}

			// field
			input.selectionColor = theme_.EditableSelectionBackgroundColor;

			// placeholder
			if (input.placeholder != null)
				PolishPlaceholder(input, info);
		}

		private static void AdjustPlaceholder(InputField input, Info info)
		{
			// no-op
		}

		private static void PolishPlaceholder(InputField input, Info info)
		{
			ForComponent<Text>(input.placeholder, (ph) =>
			{
				if (info.Enabled)
					ph.color = theme_.PlaceholderTextColor;
				else
					ph.color = theme_.DisabledPlaceholderTextColor;

				if (info.SetFont)
				{
					ph.font = info.Font;
					ph.fontSize = info.FontSize;
					ph.fontStyle = FontStyle.Italic;
				}
			});
		}


		private static void Adjust(UIPopup e, Info info)
		{
			try
			{
				var scrollView = RequireChildRecursive(e, "Scroll View");
				var viewport = RequireChildRecursive(e, "Viewport");
				var scrollbar = RequireChildRecursive(e, "Scrollbar Vertical");

				ClampScrollView(scrollView);

				// topButton is the actual combobox the user clicks to open the
				// popup
				if (e.topButton == null)
				{
					Log.Error("UIPopup has no topButton");
				}
				else
				{
					Adjust(e.topButton, info);
				}

				// popupButtonPrefab is the prefab used to create items in the
				// popup
				if (e.popupButtonPrefab == null)
				{
					Log.Error("UIPopup has no popupButtonPrefab");
				}
				else
				{
					ForComponent<UnityEngine.UI.Button>(e.popupButtonPrefab, (prefab) =>
					{
						Adjust(prefab, info);
					});
				}

				// there's some empty space at the bottom of the list, remove it
				// by changing the bottom offset of both the viewport and vertical
				// scrollbar; the scrollbar is also one pixel too far to the right
				ForComponent<RectTransform>(viewport, (rt) =>
				{
					rt.offsetMin = new Vector2(rt.offsetMin.x, 0);
				});

				ForComponent<RectTransform>(scrollbar, (rt) =>
				{
					rt.offsetMin = new Vector2(rt.offsetMin.x - 1, 0);
					rt.offsetMax = new Vector2(rt.offsetMax.x - 1, rt.offsetMax.y);
				});

				// for filterable popups, hide the filter, there's a custom textbox
				// already visible
				if (e.filterField != null)
					e.filterField.gameObject.SetActive(false);
			}
			catch (Exception)
			{
				// eat it
			}
		}

		private static void Polish(UIPopup e, Info info)
		{
			try
			{
				var scrollView = RequireChildRecursive(e, "Scroll View");
				var viewport = RequireChildRecursive(e, "Viewport");
				var scrollbar = RequireChildRecursive(e, "Scrollbar Vertical");
				var scrollbarHandle = RequireChildRecursive(scrollbar, "Handle");

				// background color for items in the popup; to have items be the
				// same color as the background of the popup, this must be
				// transparent instead of BackgroundColor because darker borders
				// would be added automatically and they can't be configured
				e.normalColor = new Color(0, 0, 0, 0);

				// background color for a selected item inside the popup
				e.selectColor = theme_.SelectionBackgroundColor;

				// background color of the popup, behind the items
				if (e.popupPanel == null)
				{
					Log.Error("UIPopup has no popupPanel");
				}
				else
				{
					ForComponent<UnityEngine.UI.Image>(e.popupPanel, (bg) =>
					{
						bg.color = new Color(0, 0, 0, 0);
					});
				}

				// background color of the scroll view inside the popup; this must
				// be transparent for the background color set above to appear
				// correctly
				ForComponent<UnityEngine.UI.Image>(scrollView, (bg) =>
				{
					bg.color = new Color(0, 0, 0, 0);
				});

				// topButton is the actual combobox the user clicks to open the
				// popup
				if (e.topButton == null)
					Log.Error("UIPopup has no topButton");
				else
					Polish(e.topButton, Button.Polishing.Default, info);

				// popupButtonPrefab is the prefab used to create items in the
				// popup
				if (e.popupButtonPrefab == null)
				{
					Log.Error("UIPopup has no popupButtonPrefab");
				}
				else
				{
					ForComponent<UnityEngine.UI.Button>(e.popupButtonPrefab, (prefab) =>
					{
						Polish(prefab, Button.Polishing.Default, info);
					});

					ForComponentInChildren<UnityEngine.UI.Text>(e.popupButtonPrefab, (prefab) =>
					{
						Polish(prefab, info);
					});
				}

				// scrollbar background color
				ForComponent<UnityEngine.UI.Image>(scrollbar, (bg) =>
				{
					bg.color = theme_.SliderBackgroundColor;
				});

				// scrollbar handle color
				ForComponent<UnityEngine.UI.Image>(scrollbarHandle, (i) =>
				{
					i.color = theme_.ButtonBackgroundColor;
				});

				// child items, this is necessary in case the enabled state has
				// changed but the popup has already been opened
				var itemPolish = Button.Polishing.Default;
				foreach (var item in viewport.GetComponentsInChildren<UIPopupButton>())
				{
					ForComponentInChildren<UIStyleText>(item, (st) =>
					{
						if (info.Enabled)
							st.color = info.TextColor;
						else
							st.color = itemPolish.disabledTextColor;

						st.UpdateStyle();
					});
				}
			}
			catch (Exception)
			{
				// eat it
			}
		}

		private static void Adjust(UIDynamicColorPicker picker, Info info)
		{
			if (picker.colorPicker == null)
				Log.Error("UIDynamicColorPicker has no colorPicker");
			else
				Adjust(picker.colorPicker, info);

			// buttons at the bottom
			var buttons = new List<string>()
			{
				"DefaultValueButton",
				"CopyToClipboardButton",
				"PasteFromClipboardButton"
			};

			foreach (var name in buttons)
			{
				ForChildRecursive(picker, name, (c) =>
				{
					ForComponent<UnityEngine.UI.Button>(c, (button) =>
					{
						Adjust(button, info);
					});
				});
			}


			// make the sliders on the right a bit smaller
			int offset = 25;

			{
				// shrink the right column with the sliders
				var right = Utilities.FindChildRecursive(picker, "RightColumn");
				var rt = right.GetComponent<RectTransform>();
				rt.offsetMin = new Vector2(rt.offsetMin.x + offset, rt.offsetMin.y);
			}

			{
				// move the hue slider to the right
				var rt = picker.colorPicker.hueImage.transform.parent.GetComponent<RectTransform>();
				rt.offsetMin = new Vector2(rt.offsetMin.x + offset, rt.offsetMin.y);
				rt.offsetMax = new Vector2(rt.offsetMax.x + offset, rt.offsetMax.y);
			}

			{
				// expand the saturation image to use the new space
				var rt = picker.colorPicker.saturationImage.GetComponent<RectTransform>();
				rt.offsetMax = new Vector2(rt.offsetMax.x + offset, rt.offsetMax.y - 5);
			}
		}

		private static void Polish(UIDynamicColorPicker picker, Info info)
		{
			// background
			ForComponent<UnityEngine.UI.Image>(picker, (bg) =>
			{
				bg.color = new Color(0, 0, 0, 0);
			});


			// label on top
			if (picker.labelText == null)
			{
				Log.Error("UIDynamicColorPicker has no labelText");
			}
			else
			{
				if (info.Enabled)
					picker.labelText.color = info.TextColor;
				else
					picker.labelText.color = Theme.DisabledTextColor;

				picker.labelText.alignment = TextAnchor.MiddleLeft;
			}


			if (picker.colorPicker == null)
				Log.Error("UIDynamicColorPicker has no colorPicker");
			else
				Polish(picker.colorPicker, info);


			// buttons at the bottom
			var buttons = new List<string>()
			{
				"DefaultValueButton",
				"CopyToClipboardButton",
				"PasteFromClipboardButton"
			};

			foreach (var name in buttons)
			{
				ForChildRecursive(picker, name, (c) =>
				{
					ForComponent<UnityEngine.UI.Button>(c, (button) =>
					{
						Polish(
							button, Button.Polishing.Default,
							info.WithSetFont(false));
					});
				});
			}
		}

		private static List<UnityEngine.UI.Slider> GetPickerSliders(
			HSVColorPicker picker)
		{
			var sliders = new List<UnityEngine.UI.Slider>();


			if (picker.redSlider == null)
				Log.Error("HSVColorPIcker has no redSlider");
			else
				sliders.Add(picker.redSlider);

			if (picker.greenSlider == null)
				Log.Error("HSVColorPIcker has no greenSlider");
			else
				sliders.Add(picker.greenSlider);

			if (picker.blueSlider == null)
				Log.Error("HSVColorPIcker has no blueSlider");
			else
				sliders.Add(picker.blueSlider);


			return sliders;
		}

		private static void Adjust(HSVColorPicker picker, Info info)
		{
			foreach (var slider in GetPickerSliders(picker))
			{
				// sliders are actually in a parent that has the panel, label,
				// input and slider
				var parent = slider?.transform?.parent;

				if (parent == null)
				{
					Log.Error("color picker slider " + slider.name + " has no parent");
				}
				else
				{
					ForChildRecursive(parent, "Text", (textObject) =>
					{
						ForComponent<Text>(textObject, (text) =>
						{
							var rt = text.GetComponent<RectTransform>();
							rt.offsetMin = new Vector2(rt.offsetMin.x - 10, rt.offsetMin.y);

							Adjust(text, info.WithFontSize(theme_.SliderTextSize));
						});
					});

					ForChildRecursive(parent, "InputField", (inputObject) =>
					{
						ForComponent<InputField>(inputObject, (input) =>
						{
							var rt = input.GetComponent<RectTransform>();
							rt.offsetMax = new Vector2(rt.offsetMax.x + 10, rt.offsetMax.y);
						});
					});

					ForComponent<RectTransform>(slider, (rt) =>
					{
						rt.offsetMin = new Vector2(rt.offsetMin.x - 10, rt.offsetMin.y + 10);
						rt.offsetMax = new Vector2(rt.offsetMax.x + 10, rt.offsetMax.y);
					});
				}

				// adjust the slider itself
				Adjust(slider, info);
			}


			Action<UnityEngine.UI.Slider, int> moveSlider = (slider, yDelta) =>
			{
				var parent = slider?.transform?.parent;
				if (parent == null)
					return;

				ForComponent<RectTransform>(parent, (rt) =>
				{
					rt.offsetMin = new Vector2(rt.offsetMin.x, rt.offsetMin.y + yDelta);
					rt.offsetMax = new Vector2(rt.offsetMax.x, rt.offsetMax.y + yDelta);
				});
			};


			// moving all the sliders down to make space for the color
			// sample at the top
			moveSlider(picker.blueSlider, -10);
			moveSlider(picker.greenSlider, -30);
			moveSlider(picker.redSlider, -50);

			if (picker.colorSample != null)
			{
				ForComponent<RectTransform>(picker.colorSample, (rt) =>
				{
					rt.offsetMin = new Vector2(rt.offsetMin.x, rt.offsetMin.y - 50);
				});
			}
		}

		private static void Polish(HSVColorPicker picker, Info info)
		{
			foreach (var slider in GetPickerSliders(picker))
			{
				// sliders are actually in a parent that has the panel, label,
				// input and slider
				var parent = slider?.transform?.parent;

				if (parent == null)
				{
					Log.Error("color picker slider " + slider.name + " has no parent");
				}
				else
				{
					ForChildRecursive(parent, "Panel", (panel) =>
					{
						ForComponent<UnityEngine.UI.Image>(panel, (bg) =>
						{
							bg.color = new Color(0, 0, 0, 0);
						});
					});

					ForChildRecursive(parent, "Text", (textObject) =>
					{
						ForComponent<Text>(textObject, (text) =>
						{
							Polish(text, new Info(
								false, info.Enabled, info.Font, info.FontStyle,
								theme_.SliderTextSize, info.TextColor));
						});
					});

					ForChildRecursive(parent, "InputField", (input) =>
					{
						ForComponent<InputField>(input, (field) =>
						{
							// that input doesn't seem to get styled properly, can't
							// get the background color to change, so just change the
							// text color
							//Polish(input, font, fontSize, false);

							if (field.textComponent == null)
							{
								Log.Error("InputField has no textComponent");
							}
							else
							{
								field.textComponent.color = info.TextColor;
								field.textComponent.fontSize = theme_.SliderTextSize;
							}
						});
					});
				}

				// polish the slider itself
				Polish(slider, info);
			}
		}

		private static void Setup(UIDynamicSlider e, Info info)
		{
			Adjust(e, info);
			Polish(e, info);
		}

		private static void Adjust(UIDynamicSlider e, Info info)
		{
			Adjust(e.slider, info);
		}

		private static void Polish(UIDynamicSlider e, Info info)
		{
			Polish(e.slider, info);
		}


		private static void Setup(UnityEngine.UI.Slider e, Info info)
		{
			Adjust(e, info);
			Polish(e, info);
		}

		private static void Adjust(UnityEngine.UI.Slider e, Info info)
		{
			ForChildRecursive(e, "Fill", (fill) =>
			{
				ForComponent<RectTransform>(fill, (rt) =>
				{
					rt.offsetMin = new Vector2(rt.offsetMin.x - 4, rt.offsetMin.y);
				});
			});
		}

		private static void Polish(UnityEngine.UI.Slider e, Info info)
		{
			// slider background color
			ForComponent<UnityEngine.UI.Image>(e, (bg) =>
			{
				bg.color = theme_.SliderBackgroundColor;
			});

			ForComponent<UIStyleSlider>(e, (ss) =>
			{
				ss.normalColor = theme_.ButtonBackgroundColor;
				ss.highlightedColor = theme_.HighlightBackgroundColor;
				ss.pressedColor = theme_.HighlightBackgroundColor;
				ss.UpdateStyle();
			});

			ForChildRecursive(e, "Fill", (fill) =>
			{
				ForComponent<UnityEngine.UI.Image>(fill, (bg) =>
				{
					bg.color = new Color(0, 0, 0, 0);
				});
			});
		}
	}
}
