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
			AddSubTab(new PersonBodyVoiceTab(person_));
			AddSubTab(new PersonBodyPartsTab(person_));
			AddSubTab(new PersonBodyFingersTab(person_));
			AddSubTab(new PersonBodyLocksTab(person_));
			AddSubTab(new PersonBodyZonesTab(person_));
		}
	}


	class PersonBodyStateTab : Tab
	{
		private Person person_;

		private VUI.Label hasPenis_ = new VUI.Label();

		private VUI.Label sweat_ = new VUI.Label();
		private VUI.Label flush_ = new VUI.Label();
		private VUI.Label hairLoose_ = new VUI.Label();
		private VUI.Label breathing_ = new VUI.Label();
		private VUI.CheckBox forceNotBreathing_ = new VUI.CheckBox("Force");
		private VUI.Label damping_ = new VUI.Label();

		private VUI.ListView<string> list_ = new VUI.ListView<string>();
		private DebugLines debug_ = null;


		public PersonBodyStateTab(Person person)
			: base("State", false)
		{
			person_ = person;

			list_.Font = VUI.Style.Theme.MonospaceFont;
			list_.FontSize = 22;

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

			p.Add(new VUI.Label("Breathing"));
			p.Add(breathing_);
			p.Add(forceNotBreathing_);
			p.Add(new VUI.Spacer(0));

			AddForceable(p, person_.Body.DampedAir, "Air");

			p.Add(new VUI.Label("Damping"));
			p.Add(damping_);
			p.Add(new VUI.Spacer(0));
			p.Add(new VUI.Spacer(0));

			p.Add(new VUI.Button("Zap", OnZap));
			p.Add(new VUI.Spacer(0));
			p.Add(new VUI.Spacer(0));
			p.Add(new VUI.Spacer(0));


			forceNotBreathing_.Changed += (b) =>
			{
				if (b)
					person_.Body.BreathingBool.SetForced(false);
				else
					person_.Body.BreathingBool.UnsetForced();
			};

			Layout = new VUI.BorderLayout(20);
			Add(p, VUI.BorderLayout.Top);
			Add(list_, VUI.BorderLayout.Center);
		}

		private void OnZap()
		{
			person_.Body.Zapped(null, SS.Genitals);
		}

		protected override void DoUpdate(float s)
		{
			if (person_.Body.HasPenis)
			{
				if (person_.Body.Get(BP.Penis).IsPhysical)
					hasPenis_.Text = "yes";
				else
					hasPenis_.Text = "yes, not physical";
			}
			else
			{
				hasPenis_.Text = "no";
			}

			sweat_.Text = $"{person_.Atom.Body.Sweat:0.00}";
			flush_.Text = $"{person_.Atom.Body.Flush:0.00}";
			hairLoose_.Text = $"{person_.Atom.Hair.Loose:0.00}";
			breathing_.Text = $"{(person_.Body.Breathing ? "yes" : "no")}";
			damping_.Text = BodyDamping.ToString(person_.Body.Damping);

			if (debug_ == null)
				debug_ = new DebugLines();

			debug_.Clear();
			person_.Atom.Body.Debug(debug_);
			list_.SetItems(debug_.MakeArray());
		}
	}


	class PersonBodyVoiceTab : Tab
	{
		private Person person_;
		private VUI.ListView<string> list_ = new VUI.ListView<string>();

		public PersonBodyVoiceTab(Person person)
			: base("Voice", false)
		{
			person_ = person;

			Layout = new VUI.BorderLayout();
			Add(list_, VUI.BorderLayout.Center);

			list_.Font = VUI.Style.Theme.MonospaceFont;
			list_.FontSize = 22;
		}

		protected override void DoUpdate(float s)
		{
			list_.SetItems(person_.Voice.Debug());
		}
	}


	class PersonBodyPartsTab : Tab
	{
		struct PartWidgets
		{
			public BodyPart part;
			public VUI.CheckBox name;
			public VUI.Label triggering, grabbed, lk, link;
		}

		private readonly Person person_;
		private readonly List<PartWidgets> widgets_ = new List<PartWidgets>();

		public PersonBodyPartsTab(Person ps)
			: base ("Parts", false)
		{
			person_ = ps;

			var gl = new VUI.GridLayout(5);
			gl.UniformHeight = true; // exclude header
			gl.UniformWidth = true;

			var p = new VUI.Panel(gl);

			p.Add(CreateLabel("Name", UnityEngine.FontStyle.Bold));
			p.Add(CreateLabel("Collision", UnityEngine.FontStyle.Bold));
			p.Add(CreateLabel("Grabbed", UnityEngine.FontStyle.Bold));
			p.Add(CreateLabel("Lock", UnityEngine.FontStyle.Bold));
			p.Add(CreateLabel("Link", UnityEngine.FontStyle.Bold));

			for (int i = 0; i < person_.Body.Parts.Length; ++i)
			{
				var bp = person_.Body.Parts[i];

				var w = new PartWidgets();
				w.part = bp;

				w.name = CreateCheckBox(bp.Name);
				w.name.Changed += (b) => OnChecked(b, bp);
				p.Add(w.name);

				w.triggering = CreateLabel();
				p.Add(w.triggering);

				w.grabbed = CreateLabel();
				p.Add(w.grabbed);

				w.lk = CreateLabel();
				w.lk.TextColor = Sys.Vam.U.ToUnity(Color.Green);
				p.Add(w.lk);

				w.link = CreateLabel();
				w.link.WrapMode = VUI.Label.Clip;
				p.Add(w.link);

				widgets_.Add(w);
			}

			Layout = new VUI.BorderLayout();
			Add(p, VUI.BorderLayout.Top);
		}

		private VUI.Label CreateLabel(string s="")
		{
			var lb = new VUI.Label(s);
			lb.FontSize = 18;
			return lb;
		}

		private VUI.Label CreateLabel(string s, UnityEngine.FontStyle fs)
		{
			var lb = new VUI.Label(s, fs);
			lb.FontSize = 18;
			return lb;
		}

		private VUI.CheckBox CreateCheckBox(string s)
		{
			var lb = new VUI.CheckBox(s);
			lb.FontSize = 18;
			return lb;
		}

		private void OnChecked(bool b, BodyPart bp)
		{
			bp.Render = b;
		}

		protected override void DoUpdate(float s)
		{
			for (int i = 0; i < widgets_.Count; ++i)
			{
				var w = widgets_[i];

				var ts = w.part.GetTriggers();
				if (ts != null)
				{
					string ss = "";
					string one = "";
					int more = 0;

					bool sawUnknown = false;

					for (int j = 0; j < ts.Length; ++j)
					{
						if (ts[j].Type == Sys.TriggerInfo.NoneType)
						{
							if (sawUnknown)
								continue;
							else
								sawUnknown = true;
						}

						if (ss != "")
							ss += ",";

						ss += ts[j].ToString();

						if (one == "")
							one = ts[j].ToString();
						else
							++more;
					}

					if (more > 0)
						one += $", +{more}";

					bool triggered = (ts != null && ts.Length > 0);

					w.triggering.Text = one;
					w.triggering.Tooltip.Text = ss;

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
					var gs = w.part.GetGrabs();

					if (gs == null || gs.Length == 0)
					{
						w.grabbed.Text = "";
					}
					else
					{
						string ss = "";
						for (int j = 0; j < gs.Length; ++j)
						{
							if (ss != "") ss += ",";
							ss += gs[j].ToString();
						}

						w.grabbed.Text = ss;
						w.grabbed.TextColor = Sys.Vam.U.ToUnity(Color.Green);
					}
				}
				else
				{
					w.grabbed.Text = "";
				}

				w.lk.Text = w.part.Locker.DebugLockString();
				w.link.Text = (w.part.Link?.Name ?? "");
			}
		}
	}


	class PersonBodyFingersTab : Tab
	{
		struct BoneWidgets
		{
			public Bone bone;
			public VUI.Label name, source, position, direction;
		}

		private readonly Person person_;
		private readonly List<BoneWidgets> widgets_ = new List<BoneWidgets>();

		public PersonBodyFingersTab(Person ps)
			: base("Fingers", false)
		{
			person_ = ps;

			var gl = new VUI.GridLayout(4);
			gl.UniformHeight = false;
			var p = new VUI.Panel(gl);

			p.Add(new VUI.Label("Name", UnityEngine.FontStyle.Bold));
			p.Add(new VUI.Label("Source", UnityEngine.FontStyle.Bold));
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
					w.source = new VUI.Label(b.ToString());
					w.position = new VUI.Label();
					w.direction = new VUI.Label();

					int fontSize = 20;
					w.name.FontSize = fontSize;
					w.source.FontSize = fontSize;
					w.position.FontSize = fontSize;
					w.direction.FontSize = fontSize;

					p.Add(w.name);
					p.Add(w.source);
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


	class PersonBodyZonesTab : Tab
	{
		private Person person_;
		private VUI.ListView<string> list_ = new VUI.ListView<string>();
		private List<string> strings_ = new List<string>();

		public PersonBodyZonesTab(Person person)
			: base("Zones", false)
		{
			person_ = person;

			Layout = new VUI.BorderLayout(10);
			list_.Font = VUI.Style.Theme.MonospaceFont;
			list_.FontSize = 22;
			Add(list_, VUI.BorderLayout.Center);
		}

		protected override void DoUpdate(float s)
		{
			strings_.Clear();

			foreach (var z in person_.Body.Zones.All)
			{
				strings_.Add($"{z} {z.MainBodyPart} active={z.ActiveSources}");

				foreach (var src in z.Sources)
				{
					if (src.StrictlyActiveCount > 0)
					{
						foreach (var bp in BodyPartType.Values)
							AddPart(src, src.GetPart(bp));

						AddPart(src, src.GetToyPart());
						AddPart(src, src.GetExternalPart());
					}
				}
			}


			list_.SetItems(strings_);
		}

		private void AddPart(ErogenousZoneSource src, ErogenousZoneSource.Part part)
		{
			if (part.active)
				strings_.Add($"    {src}.{part.bodyPart} mag={part.magnitude:0.00} elapsed={part.elapsed:0.00}");
		}
	}
}
