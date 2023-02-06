using SimpleJSON;
using System;
using System.Collections.Generic;

namespace Cue
{
	public interface ICustomMenuItem
	{
		CustomMenuItems Parent { get; set; }
		VUI.Panel CreateConfigWidget();
		VUI.Panel CreateMenuWidget();
		void Activate();
		void Remove();
		void MoveUp();
		void MoveDown();
		JSONNode ToJSON();
	}


	public abstract class BasicCustomMenuItem : ICustomMenuItem
	{
		private CustomMenuItems parent_ = null;

		public static ICustomMenuItem FromJSON(JSONClass n)
		{
			try
			{
				var type = J.OptString(n, "type", "button");

				if (type == "button")
					return CustomButtonItem.FromJSON(n);
				else if (type == "toggle")
					return CustomToggleItem.FromJSON(n);
				else
					throw new LoadFailed($"unknown custom item type {type}");
			}
			catch (Exception e)
			{
				Logger.Global.Error("failed to load custom menu, " + e.ToString());
				return null;
			}
		}

		public CustomMenuItems Parent
		{
			get { return parent_; }
			set { parent_ = value; }
		}

		public VUI.Panel CreateConfigWidget()
		{
			var p = new VUI.Panel(new VUI.BorderLayout());

			var left = new VUI.Panel(new VUI.HorizontalFlow(10));
			DoCreateConfigWidget(left);
			p.Add(left, VUI.BorderLayout.Left);

			var right = new VUI.Panel(new VUI.HorizontalFlow(10));
			right.Add(new VUI.ToolButton("X", Remove));
			right.Add(new VUI.ToolButton("\x2191", MoveUp));
			right.Add(new VUI.ToolButton("\x2193", MoveDown));
			p.Add(right, VUI.BorderLayout.Right);

			return p;
		}

		public VUI.Panel CreateMenuWidget()
		{
			var p = new VUI.Panel(new VUI.HorizontalFlow(10));
			DoCreateMenuWidget(p);
			return p;
		}

		public abstract void Activate();

		public void Remove()
		{
			Parent.RemoveItem(this);
		}

		public void MoveUp()
		{
			Parent.MoveItemUp(this);
		}

		public void MoveDown()
		{
			Parent.MoveItemDown(this);
		}

		public JSONNode ToJSON()
		{
			var o = new JSONClass();
			DoToJSON(o);
			return o;
		}

		protected abstract void DoCreateConfigWidget(VUI.Panel p);
		protected abstract void DoCreateMenuWidget(VUI.Panel p);
		protected abstract void DoToJSON(JSONClass o);
	}


	public class CustomButtonItem : BasicCustomMenuItem
	{
		private string caption_;
		private Sys.IActionTrigger trigger_;

		private CustomButtonItem(string caption, Sys.IActionTrigger trigger)
		{
			caption_ = caption;
			trigger_ = trigger;
		}

		public CustomButtonItem(string caption)
			: this(caption, Cue.Instance.Sys.CreateActionTrigger())
		{
		}

		public static new CustomButtonItem FromJSON(JSONClass o)
		{
			var c = J.ReqString(o, "caption");

			if (!o.HasKey("trigger"))
				throw new LoadFailed("missing trigger");

			var t = Cue.Instance.Sys.LoadActionTrigger(o["trigger"].AsObject);
			if (t == null)
				throw new LoadFailed("failed to create trigger");

			return new CustomButtonItem(c, t);
		}

		public string Caption
		{
			get
			{
				return caption_;
			}

			set
			{
				caption_ = value;
				trigger_.Name = value;
				Parent?.FireTriggersChanged();
			}
		}

		public Sys.IActionTrigger Trigger
		{
			get { return trigger_; }
		}

		public override void Activate()
		{
			trigger_?.Fire();
		}

		public void Edit(Action onDone = null)
		{
			trigger_?.Edit(onDone);
		}

		public override string ToString()
		{
			return $"CustomButtonItem.[{Caption}]";
		}

		protected override void DoCreateConfigWidget(VUI.Panel p)
		{
			var c = p.Add(new VUI.TextBox(Caption, "Button name"));
			c.Edited += OnCaption;
			p.Add(new VUI.Button("Edit actions...", OnEditTrigger));
		}

		protected override void DoCreateMenuWidget(VUI.Panel p)
		{
			p.Add(new VUI.Button(Caption, () =>
			{
				Activate();
			}));
		}

		protected override void DoToJSON(JSONClass o)
		{
			o["type"] = "button";
			o["caption"] = caption_;
			o["trigger"] = trigger_.ToJSON();
		}

		private void OnEditTrigger()
		{
			Trigger.Edit(() => Parent.FireTriggersChanged());
		}

		private void OnCaption(string s)
		{
			Caption = s;
		}
	}


	public class CustomToggleItem : BasicCustomMenuItem
	{
		private string caption_;
		private bool value_ = false;
		private Sys.IActionTrigger triggerOn_, triggerOff_;

		public CustomToggleItem(string caption)
			: this(caption, null, null)
		{
		}

		private CustomToggleItem(
			string caption,
			Sys.IActionTrigger triggerOn, Sys.IActionTrigger triggerOff)
		{
			caption_ = caption;
			triggerOn_ = triggerOn ?? Cue.Instance.Sys.CreateActionTrigger();
			triggerOff_ = triggerOff ?? Cue.Instance.Sys.CreateActionTrigger();
		}

		public static new CustomToggleItem FromJSON(JSONClass n)
		{
			try
			{
				var c = J.ReqString(n, "caption");

				if (!n.HasKey("triggerOn"))
					throw new LoadFailed("missing triggerOn");

				if (!n.HasKey("triggerOff"))
					throw new LoadFailed("missing triggerOff");

				var on = Cue.Instance.Sys.LoadActionTrigger(n["triggerOn"].AsObject);
				if (on == null)
					throw new LoadFailed("failed to create triggerOn");

				var off = Cue.Instance.Sys.LoadActionTrigger(n["triggerOff"].AsObject);
				if (off == null)
					throw new LoadFailed("failed to create triggerOff");

				return new CustomToggleItem(c, on, off);
			}
			catch (Exception e)
			{
				Logger.Global.Error("failed to load custom menu, " + e.ToString());
				return null;
			}
		}

		public string Caption
		{
			get
			{
				return caption_;
			}

			set
			{
				caption_ = value;
				triggerOn_.Name = value + ".on";
				triggerOff_.Name = value + ".off";
				Parent?.FireTriggersChanged();
			}
		}

		public Sys.IActionTrigger TriggerOn
		{
			get { return triggerOn_; }
		}

		public Sys.IActionTrigger TriggerOff
		{
			get { return triggerOff_; }
		}

		public override void Activate()
		{
			SetValue(!value_);
		}

		public void SetValue(bool b)
		{
			if (b)
			{
				value_ = true;
				triggerOn_?.Fire();
			}
			else
			{
				value_ = false;
				triggerOff_?.Fire();
			}
		}

		public override string ToString()
		{
			return $"CustomButtonItem.[{Caption}]";
		}

		protected override void DoCreateConfigWidget(VUI.Panel p)
		{
			var c = p.Add(new VUI.TextBox(Caption, "Toggle name"));
			c.Edited += OnCaption;
			p.Add(new VUI.Button("Edit On actions...", OnEditTriggerOn));
			p.Add(new VUI.Button("Edit Off actions...", OnEditTriggerOff));
		}

		protected override void DoCreateMenuWidget(VUI.Panel p)
		{
			p.Add(new VUI.CheckBox(Caption, (b) =>
			{
				SetValue(b);
			}));
		}

		protected override void DoToJSON(JSONClass o)
		{
			o["type"] = "toggle";
			o["caption"] = caption_;
			o["triggerOn"] = triggerOn_.ToJSON();
			o["triggerOff"] = triggerOff_.ToJSON();
		}

		private void OnEditTriggerOn()
		{
			TriggerOn.Edit(() => Parent.FireTriggersChanged());
		}

		private void OnEditTriggerOff()
		{
			TriggerOff.Edit(() => Parent.FireTriggersChanged());
		}

		private void OnCaption(string s)
		{
			Caption = s;
		}
	}


	public class CustomMenuItems
	{
		public delegate void Handler();
		public event Handler Changed;
		private List<ICustomMenuItem> items_ = new List<ICustomMenuItem>();

		public CustomMenuItems()
		{
		}

		public ICustomMenuItem[] Items
		{
			get { return items_.ToArray(); }
		}

		public void Save(JSONArray a)
		{
			if (items_.Count > 0)
			{
				foreach (var m in items_)
					a.Add(m.ToJSON());
			}
		}

		public void Load(JSONArray a)
		{
			items_.Clear();

			foreach (var mo in a.Childs)
			{
				var m = BasicCustomMenuItem.FromJSON(mo.AsObject);
				if (m != null)
					Add(m);
			}

			OnTriggersChanged();
		}

		public void AddCustomItem(ICustomMenuItem item)
		{
			Add(item);
			OnTriggersChanged();
		}

		private void Add(ICustomMenuItem t)
		{
			items_.Add(t);
			t.Parent = this;
		}

		public void RemoveItem(ICustomMenuItem m)
		{
			if (!items_.Contains(m))
			{
				Logger.Global.Error($"custom menu '{m}' not found");
				return;
			}

			m.Parent = null;
			items_.Remove(m);

			OnTriggersChanged();
		}

		public void MoveItemUp(ICustomMenuItem item)
		{
			int i = IndexOfItem(item);
			if (i < 0 || i >= items_.Count)
				return;

			if (i > 0)
			{
				items_.RemoveAt(i);
				items_.Insert(i - 1, item);
				FireTriggersChanged();
			}
		}

		public void MoveItemDown(ICustomMenuItem item)
		{
			int i = IndexOfItem(item);
			if (i < 0 || i >= items_.Count)
				return;

			if ((i + 1) < items_.Count)
			{
				items_.RemoveAt(i);
				items_.Insert(i + 1, item);
				FireTriggersChanged();
			}
		}

		public int IndexOfItem(ICustomMenuItem item)
		{
			return items_.IndexOf(item);
		}

		public void FireTriggersChanged()
		{
			OnTriggersChanged();
		}

		private void OnTriggersChanged()
		{
			Cue.Instance.SaveLater();
			Changed?.Invoke();
		}
	}
}
