namespace VUI
{
	class TitleBar : Panel
	{
		private readonly Window w_;
		private readonly Label title_;

		public TitleBar(Window w, string title = "")
		{
			w_ = w;

			title_ = new Label(title, Align.VCenterCenter);
			title_.FontStyle = UnityEngine.FontStyle.Bold;

			var buttons = new Panel(new HorizontalFlow(5));
			buttons.Margins = new Insets(10);

			var x = new ToolButton("X", OnClose);
			buttons.Add(x);

			Layout = new BorderLayout();
			BackgroundColor = Style.Theme.DialogTitleBackgroundColor;
			Padding = new Insets(10, 0, 0, 0);

			Add(title_, BorderLayout.Center);
			Add(buttons, BorderLayout.Right);
		}

		public string Text
		{
			get { return title_.Text; }
			set { title_.Text = value; }
		}

		private void OnClose()
		{
			w_.Close();
		}
	}


	class Window : Panel
	{
		public delegate void Handler();
		public event Handler CloseRequest;

		public override string TypeName { get { return "Window"; } }

		private readonly TitleBar tb_;
		private readonly Panel content_;

		public Window(string title = "")
		{
			tb_ = new TitleBar(this, title);
			content_ = new Panel();

			BackgroundColor = Style.Theme.BackgroundColor;
			Layout = new BorderLayout();
			Borders = new Insets(1);

			Add(tb_, BorderLayout.Top);
			Add(content_, BorderLayout.Center);
		}

		public string Title
		{
			get { return tb_.Text; }
			set { tb_.Text = value; }
		}

		public virtual Widget ContentPanel
		{
			get { return content_; }
		}

		public void Close()
		{
			CloseRequest?.Invoke();
		}
	}


	class Dialog : Window
	{
		protected const float WidthAdjust = 0.8f;

		public override string TypeName { get { return "Dialog"; } }

		public delegate void CloseHandler(int result);
		public event CloseHandler Closed;

		private readonly Root root_;
		private int result_ = -1;

		public Dialog(Root r, string title)
			: base(title)
		{
			root_ = r;
			MinimumSize = new Size(600, 200);
			MaximumSize = r.Bounds.Size * WidthAdjust;
		}

		public int Result
		{
			get { return result_; }
		}

		public override Root GetRoot()
		{
			return root_;
		}

		public virtual void RunDialog(CloseHandler h = null)
		{
			root_.OverlayVisible = true;

			var ps = GetRealPreferredSize(root_.Bounds.Width * WidthAdjust, root_.Bounds.Height);
			SetBounds(new Rectangle(
				root_.Bounds.Center.X - (ps.Width / 2),
				root_.Bounds.Center.Y - (ps.Height / 2),
				ps));

			DoLayout();
			BringToTop();

			if (h != null)
				Closed += h;
		}

		public void CloseDialog(int result=-1)
		{
			result_ = result;
			root_.OverlayVisible = false;
			Destroy();

			Closed?.Invoke(result_);
		}

		protected override Size DoGetMinimumSize()
		{
			return new Size(600, 200);
		}
	}


	static class Buttons
	{
		public const int OK     = 0x01;
		public const int Cancel = 0x02;
		public const int Yes    = 0x04;
		public const int No     = 0x08;
		public const int Close  = 0x10;
		public const int Apply  = 0x20;
	}


	class ButtonBox : Panel
	{
		public override string TypeName { get { return "ButtonBox"; } }

		public delegate void ButtonCallback(int id);
		public event ButtonCallback ButtonClicked;

		public ButtonBox(int buttons)
		{
			Layout = new HorizontalFlow(10, Align.Right);

			AddButton(buttons, Buttons.OK, S("OK"));
			AddButton(buttons, Buttons.Cancel, S("Cancel"));
			AddButton(buttons, Buttons.Yes, S("Yes"));
			AddButton(buttons, Buttons.No, S("No"));
			AddButton(buttons, Buttons.Apply, S("Apply"));
			AddButton(buttons, Buttons.Close, S("Close"));

			Borders = new Insets(0, 1, 0, 0);
			Padding = new Insets(0, 20, 0, 0);
		}

		private void AddButton(int buttons, int id, string text)
		{
			if (!Bits.IsSet(buttons, id))
				return;

			Add(new Button(text, () => OnButton(id)));
		}

		private void OnButton(int id)
		{
			ButtonClicked?.Invoke(id);
		}
	}



	class DialogWithButtons : Dialog
	{
		public override string TypeName { get { return "DialogWithButtons"; } }

		private readonly ButtonBox buttons_;
		private readonly Panel center_;

		public DialogWithButtons(Root r, int buttons, string title)
			: base(r, title)
		{
			buttons_ = new ButtonBox(buttons);
			buttons_.ButtonClicked += OnButtonClicked;

			center_ = new Panel(new BorderLayout());

			base.ContentPanel.Layout = new BorderLayout();
			base.ContentPanel.Add(center_, BorderLayout.Center);
			base.ContentPanel.Add(buttons_, BorderLayout.Bottom);

			center_.Margins = new Insets(0, 0, 0, 20);
		}

		public override Widget ContentPanel
		{
			get { return center_; }
		}

		private void OnButtonClicked(int id)
		{
			CloseDialog(id);
		}
	}


	class MessageDialog : DialogWithButtons
	{
		public override string TypeName { get { return "MessageDialog"; } }

		public MessageDialog(Root r, int buttons, string title, string text)
			: base(r, buttons, title)
		{
			ContentPanel.Add(new Label(text, VUI.Align.TopLeft), BorderLayout.Center);
		}
	}


	class InputDialog : DialogWithButtons
	{
		public override string TypeName { get { return "InputDialog"; } }

		public delegate void TextHandler(string value);

		private readonly TextBox textbox_;

		public InputDialog(
			Root r, string title, string text, string initialValue)
				: base(r, Buttons.OK | Buttons.Cancel, title)
		{
			textbox_ = new TextBox(initialValue);
			textbox_.Submitted += OnSubmit;
			textbox_.Cancelled += OnCancelled;

			ContentPanel.Layout = new VerticalFlow(10);
			ContentPanel.Add(new Label(text));
			ContentPanel.Add(textbox_);

			Created += () =>
			{
				textbox_.Focus();
			};
		}

		public string Text
		{
			get
			{
				return textbox_.Text;
			}
		}

		static public void GetInput(
			Root r, string title, string text, string initialValue,
			TextHandler h)
		{
			var d = new InputDialog(r, title, text, initialValue);

			d.RunDialog((button) =>
			{
				if (button != Buttons.OK)
					return;

				h(d.Text);
			});
		}

		private void OnSubmit(string s)
		{
			CloseDialog(Buttons.OK);
		}

		private void OnCancelled()
		{
			CloseDialog(Buttons.Cancel);
		}
	}


	class TaskDialog : Dialog
	{
		private const int ButtonPadding = 20;

		public override string TypeName { get { return "TaskDialog"; } }

		VUI.Label lb;

		public TaskDialog(Root r, string title, string mainText, string secondaryText="")
			: base(r, title)
		{
			ContentPanel.Layout = new VerticalFlow(10);

			lb = ContentPanel.Add(new Label(mainText, Label.Wrap));

			if (secondaryText != "")
				ContentPanel.Add(new Label(secondaryText));

			ContentPanel.Add(new Spacer(20));
		}

		public void AddButton(int id, string text, string description="")
		{
			var s = text;
			if (description != "")
				text += "\n" + description;

			var b = new Button(s);
			b.Alignment = Align.VCenterLeft;
			b.Padding = new Insets(ButtonPadding);
			b.FontStyle = UnityEngine.FontStyle.Bold;

			b.Clicked += () =>
			{
				CloseDialog(id);
			};

			ContentPanel.Add(b);
		}
	}
}
