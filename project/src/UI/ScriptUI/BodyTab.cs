using System.Collections.Generic;

namespace Cue
{
	class PersonBodyTab : Tab
	{
		private readonly Person person_;

		public PersonBodyTab(Person person)
			: base("Body", true)
		{
			person_ = person;

			AddSubTab(new PersonBodyStateTab(person_));
			AddSubTab(new PersonBodyPartsTab(person_));
			AddSubTab(new PersonBodyFingersTab(person_));
			AddSubTab(new PersonBodyLocksTab(person_));
		}
	}


	class PersonBodyStateTab : Tab
	{
		private Person person_;

		private VUI.Label hasPenis_ = new VUI.Label();

		private VUI.Label sweat_ = new VUI.Label();
		private VUI.Label flush_ = new VUI.Label();
		private VUI.Label hairLoose_ = new VUI.Label();


		public PersonBodyStateTab(Person person)
			: base("State", false)
		{
			person_ = person;

			var gl = new VUI.GridLayout(4);
			gl.HorizontalSpacing = 20;
			gl.HorizontalStretch = new List<bool>() { false, true, false, false };
			var p = new VUI.Panel(gl);


			p.Add(new VUI.Label("Has penis"));
			p.Add(hasPenis_);
			p.Add(new VUI.Spacer(0));
			p.Add(new VUI.Spacer(0));

			AddForceable(p, person_.Body.DampedTemperature, "Temperature");

			p.Add(new VUI.Label("Sweat"));
			p.Add(sweat_);
			p.Add(new VUI.Spacer(0));
			p.Add(new VUI.Spacer(0));

			p.Add(new VUI.Label("Flush"));
			p.Add(flush_);
			p.Add(new VUI.Spacer(0));
			p.Add(new VUI.Spacer(0));

			p.Add(new VUI.Label("Hair loose"));
			p.Add(hairLoose_);
			p.Add(new VUI.Spacer(0));
			p.Add(new VUI.Spacer(0));



			Layout = new VUI.BorderLayout();
			Add(p, VUI.BorderLayout.Top);
		}

		protected override void DoUpdate(float s)
		{
			hasPenis_.Text = person_.Body.HasPenis.ToString();

			sweat_.Text = $"{person_.Atom.Body.Sweat:0.000000}";
			flush_.Text = $"{person_.Atom.Body.Flush:0.000000}";
			hairLoose_.Text = $"{person_.Atom.Hair.Loose:0.000000}";
		}
	}


	class PersonBodyPartsTab : Tab
	{
		struct PartWidgets
		{
			public BodyPart part;
			public VUI.Label name, triggering, grab, lk, source;
			public VUI.Label position, direction;
		}

		private readonly Person person_;
		private readonly List<PartWidgets> widgets_ = new List<PartWidgets>();

		private static bool Positions = false;

		public PersonBodyPartsTab(Person ps)
			: base ("Parts", false)
		{
			person_ = ps;

			var gl = new VUI.GridLayout(Positions ? 7 : 5);
			gl.UniformHeight = false;
			var p = new VUI.Panel(gl);

			p.Add(new VUI.Label("Name", UnityEngine.FontStyle.Bold));
			p.Add(new VUI.Label("Trigger", UnityEngine.FontStyle.Bold));
			p.Add(new VUI.Label("Grab", UnityEngine.FontStyle.Bold));
			p.Add(new VUI.Label("Lock", UnityEngine.FontStyle.Bold));
			p.Add(new VUI.Label("Source", UnityEngine.FontStyle.Bold));

			if (Positions)
			{
				p.Add(new VUI.Label("Position", UnityEngine.FontStyle.Bold));
				p.Add(new VUI.Label("Bearing", UnityEngine.FontStyle.Bold));
			}

			int fontSize = 20;

			for (int i = 0; i < person_.Body.Parts.Length; ++i)
			{
				var bp = person_.Body.Parts[i];

				var w = new PartWidgets();
				w.part = bp;

				w.name = new VUI.Label(bp.Name);
				w.name.FontSize = fontSize;
				p.Add(w.name);

				w.triggering = new VUI.Label();
				w.triggering.FontSize = fontSize;
				p.Add(w.triggering);

				w.grab = new VUI.Label();
				w.grab.FontSize = fontSize;
				p.Add(w.grab);

				w.lk = new VUI.Label();
				w.lk.FontSize = fontSize;
				w.lk.TextColor = Sys.Vam.U.ToUnity(Color.Green);
				p.Add(w.lk);

				w.source = new VUI.Label();
				w.source.FontSize = fontSize;
				p.Add(w.source);

				if (Positions)
				{
					w.position = new VUI.Label();
					w.position.FontSize = fontSize;
					p.Add(w.position);

					w.direction = new VUI.Label();
					w.direction.FontSize = fontSize;
					p.Add(w.direction);
				}

				widgets_.Add(w);
			}

			Layout = new VUI.BorderLayout();
			Add(p, VUI.BorderLayout.Top);
		}

		protected override void DoUpdate(float s)
		{
			for (int i = 0; i < widgets_.Count; ++i)
			{
				var w = widgets_[i];

				if (w.part.CanTrigger)
				{
					var ss = "";

					var ts = w.part.GetTriggers();
					if (ts != null)
					{
						bool sawUnknown = false;

						for (int j = 0; j < ts.Length; ++j)
						{
							if (!ts[j].IsPerson())
							{
								if (sawUnknown)
									continue;
								else
									sawUnknown = true;
							}

							if (ss != "")
								ss += ",";

							ss += ts[j].ToString();
						}
					}

					bool triggered = (ts != null && ts.Length > 0);

					w.triggering.Text = ss;

					w.triggering.TextColor = (
						triggered ?
						Sys.Vam.U.ToUnity(Color.Green) :
						VUI.Style.Theme.TextColor);
				}
				else
				{
					w.triggering.Text = "";
				}

				if (w.part.CanGrab)
				{
					w.grab.Text = (w.part.Grabbed ? "grabbed" : "");

					w.grab.TextColor = (
						w.part.Grabbed ?
						Sys.Vam.U.ToUnity(Color.Green) :
						VUI.Style.Theme.TextColor);
				}
				else
				{
					w.grab.Text = "";
				}

				w.lk.Text = w.part.DebugLockString();
				w.source.Text = w.part.Source;

				if (Positions)
				{
					w.position.Text = w.part.Position.ToString();
					w.direction.Text = w.part.Rotation.Bearing.ToString("0.0");
				}
			}
		}
	}


	class PersonBodyFingersTab : Tab
	{
		struct BoneWidgets
		{
			public Bone bone;
			public VUI.Label name, position, direction;
		}

		private readonly Person person_;
		private readonly List<BoneWidgets> widgets_ = new List<BoneWidgets>();

		public PersonBodyFingersTab(Person ps)
			: base("Fingers", false)
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

		protected override void DoUpdate(float s)
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


	class PersonBodyLocksTab : Tab
	{
		private Person person_;
		private VUI.ListView<string> list_ = new VUI.ListView<string>();
		private List<string> strings_ = new List<string>();

		public PersonBodyLocksTab(Person person)
			: base("Locks", false)
		{
			person_ = person;

			Layout = new VUI.BorderLayout(10);
			list_.Font = VUI.Style.Theme.MonospaceFont;
			list_.FontSize = 22;
			Add(list_, VUI.BorderLayout.Center);
		}

		protected override void DoUpdate(float s)
		{
			person_.Body.DebugAllLocks(strings_);
			list_.SetItems(strings_);
		}
	}
}
