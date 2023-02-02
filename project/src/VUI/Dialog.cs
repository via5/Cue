namespace VUI
{
	class Dialog : Panel
	{
		protected const float WidthAdjust = 0.8f;

		public override string TypeName { get { return "Dialog"; } }

		public delegate void CloseHandler(int result);
		public event CloseHandler Closed;

		private readonly Root root_;
		private readonly Label title_;
		private readonly Panel content_;
		private int result_ = -1;

		public Dialog(Root r, string title)
		{
			root_ = r;
			title_ = new Label(title, Label.AlignCenter | Label.AlignVCenter);
			content_ = new Panel();

			BackgroundColor = Style.Theme.BackgroundColor;
			Layout = new BorderLayout();
			Borders = new Insets(1);

			content_.Margins = new Insets(10, 20, 10, 10);
			title_.BackgroundColor = Style.Theme.DialogTitleBackgroundColor;
			title_.Padding = new Insets(5, 5, 0, 10);

			MinimumSize = new Size(600, 200);
			MaximumSize = r.Bounds.Size * WidthAdjust;

			Add(title_, BorderLayout.Top);
			Add(content_, BorderLayout.Center);
		}

		public virtual Widget ContentPanel
		{
			get { return content_; }
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


	class ButtonBox : Panel
	{
		public override string TypeName { get { return "ButtonBox"; } }

		public delegate void ButtonCallback(int id);
		public event ButtonCallback ButtonClicked;

		// sync with MessageDialog
		public const int OK     = 0x01;
		public const int Cancel = 0x02;
		public const int Yes    = 0x04;
		public const int No     = 0x08;
		public const int Close  = 0x10;
		public const int Apply  = 0x20;

		public ButtonBox(int buttons)
		{
			Layout = new HorizontalFlow(10, HorizontalFlow.AlignRight);

			AddButton(buttons, OK, S("OK"));
			AddButton(buttons, Cancel, S("Cancel"));
			AddButton(buttons, Yes, S("Yes"));
			AddButton(buttons, No, S("No"));
			AddButton(buttons, Apply, S("Apply"));
			AddButton(buttons, Close, S("Close"));

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

		// sync with ButtonBox
		public const int OK     = 0x01;
		public const int Cancel = 0x02;
		public const int Yes    = 0x04;
		public const int No     = 0x08;
		public const int Close  = 0x10;
		public const int Apply  = 0x20;

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
			ContentPanel.Add(
				new Label(text, Label.AlignLeft | Label.AlignTop),
				BorderLayout.Center);
		}
	}


	class InputDialog : DialogWithButtons
	{
		public override string TypeName { get { return "InputDialog"; } }

		public delegate void TextHandler(string value);

		private readonly TextBox textbox_;

		public InputDialog(
			Root r, string title, string text, string initialValue)
				: base(r, OK | Cancel, title)
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
				if (button != OK)
					return;

				h(d.Text);
			});
		}

		private void OnSubmit(string s)
		{
			CloseDialog(OK);
		}

		private void OnCancelled()
		{
			CloseDialog(Cancel);
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
			b.Alignment = Label.AlignLeft | Label.AlignVCenter;
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
