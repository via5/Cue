using System.Collections.Generic;

namespace Cue
{
	class PersonBodyTab : Tab
	{
		private readonly Person person_;
		private VUI.Tabs tabsWidget_ = new VUI.Tabs();
		private List<Tab> tabs_ = new List<Tab>();

		public PersonBodyTab(Person person)
		{
			person_ = person;

			tabs_.Add(new PersonBodyPartsTab(person_));
			tabs_.Add(new PersonHandsTab(person_));

			foreach (var t in tabs_)
				tabsWidget_.AddTab(t.Title, t);

			Layout = new VUI.BorderLayout();
			Add(tabsWidget_, VUI.BorderLayout.Center);
		}

		public override string Title
		{
			get { return "Body"; }
		}

		public override void Update(float s)
		{
			for (int i = 0; i < tabs_.Count; ++i)
			{
				if (tabs_[i].IsVisibleOnScreen())
					tabs_[i].Update(s);
			}
		}

		public override void OnPluginState(bool b)
		{
			base.OnPluginState(b);
			foreach (var t in tabs_)
				t.OnPluginState(b);
		}
	}


	class PersonBodyPartsTab : Tab
	{
		struct PartWidgets
		{
			public BodyPart part;
			public VUI.Label name, triggering, grab, position, direction;
		}

		private readonly Person person_;
		private readonly List<PartWidgets> widgets_ = new List<PartWidgets>();

		public PersonBodyPartsTab(Person ps)
		{
			person_ = ps;

			var gl = new VUI.GridLayout(5);
			gl.UniformHeight = false;
			var p = new VUI.Panel(gl);

			p.Add(new VUI.Label("Name", UnityEngine.FontStyle.Bold));
			p.Add(new VUI.Label("Trigger", UnityEngine.FontStyle.Bold));
			p.Add(new VUI.Label("Grab", UnityEngine.FontStyle.Bold));
			p.Add(new VUI.Label("Position", UnityEngine.FontStyle.Bold));
			p.Add(new VUI.Label("Bearing", UnityEngine.FontStyle.Bold));

			for (int i = 0; i < person_.Body.Parts.Length; ++i)
			{
				var bp = person_.Body.Parts[i];

				var w = new PartWidgets();
				w.part = bp;

				w.name = new VUI.Label(bp.Name);
				w.triggering = new VUI.Label();
				w.grab = new VUI.Label();
				w.position = new VUI.Label();
				w.direction = new VUI.Label();

				int fontSize = 20;
				w.name.FontSize = fontSize;
				w.triggering.FontSize = fontSize;
				w.grab.FontSize = fontSize;
				w.position.FontSize = fontSize;
				w.direction.FontSize = fontSize;

				p.Add(w.name);
				p.Add(w.triggering);
				p.Add(w.grab);
				p.Add(w.position);
				p.Add(w.direction);

				widgets_.Add(w);
			}

			Layout = new VUI.BorderLayout();
			Add(p, VUI.BorderLayout.Top);
		}

		public override string Title
		{
			get { return "Body parts"; }
		}

		public override void Update(float s)
		{
			for (int i = 0; i < widgets_.Count; ++i)
			{
				var w = widgets_[i];

				if (w.part.Exists)
				{
					if (w.part.Sys.CanTrigger)
					{
						w.triggering.Text = w.part.Trigger.ToString("0.##");

						w.triggering.TextColor = (
							w.part.Trigger > 0 ?
							W.VamU.ToUnity(Color.Green) :
							VUI.Style.Theme.TextColor);
					}
					else
					{
						w.triggering.Text = "";
					}

					if (w.part.Sys.CanGrab)
					{
						w.grab.Text = w.part.Grabbed.ToString();

						w.grab.TextColor = (
							w.part.Grabbed ?
							W.VamU.ToUnity(Color.Green) :
							VUI.Style.Theme.TextColor);
					}
					else
					{
						w.grab.Text = "";
					}


					w.position.Text = w.part.Position.ToString();
					w.direction.Text = w.part.Rotation.Bearing.ToString("0.0");
				}
			}
		}
	}


	class PersonHandsTab : Tab
	{
		struct BoneWidgets
		{
			public Bone bone;
			public VUI.Label name, position, direction;
		}

		private readonly Person person_;
		private readonly List<BoneWidgets> widgets_ = new List<BoneWidgets>();

		public PersonHandsTab(Person ps)
		{
			person_ = ps;

			var gl = new VUI.GridLayout(3);
			gl.UniformHeight = false;
			var p = new VUI.Panel(gl);

			p.Add(new VUI.Label("Name", UnityEngine.FontStyle.Bold));
			p.Add(new VUI.Label("Position", UnityEngine.FontStyle.Bold));
			p.Add(new VUI.Label("Rotation", UnityEngine.FontStyle.Bold));

			AddHand(p, person_.Body.LeftHand);
			AddHand(p, person_.Body.RightHand);

			Layout = new VUI.BorderLayout();
			Add(p, VUI.BorderLayout.Top);
		}

		private void AddHand(VUI.Panel p, Hand h)
		{
			for (int i = 0; i < h.Fingers.Length; ++i)
			{
				var f = h.Fingers[i];

				for (int j = 0; j < f.Bones.Length; ++j)
				{
					var b = f.Bones[j];

					var w = new BoneWidgets();
					w.bone = b;

					w.name = new VUI.Label($"{h.Name}/{f.Name}/{b.Name}");
					w.position = new VUI.Label();
					w.direction = new VUI.Label();

					int fontSize = 20;
					w.name.FontSize = fontSize;
					w.position.FontSize = fontSize;
					w.direction.FontSize = fontSize;

					p.Add(w.name);
					p.Add(w.position);
					p.Add(w.direction);

					widgets_.Add(w);
				}
			}
		}

		public override string Title
		{
			get { return "Fingers"; }
		}

		public override void Update(float s)
		{
			for (int i = 0; i < widgets_.Count; ++i)
			{
				var w = widgets_[i];

				if (w.bone.Exists)
				{
					w.position.Text = w.bone.Position.ToString();
					w.direction.Text = w.bone.Rotation.ToString();
				}
			}
		}
	}
}
