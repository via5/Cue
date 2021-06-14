using System.Collections.Generic;

namespace Cue
{
	class PersonBodyTab : Tab
	{
		private readonly Person person_;

		private VUI.Label sweat_ = new VUI.Label();
		private VUI.Label flush_ = new VUI.Label();
		private VUI.Label hairLoose_ = new VUI.Label();

		private VUI.Label excitement_ = new VUI.Label();
		private VUI.Label mouth_ = new VUI.Label();
		private VUI.Label breasts_ = new VUI.Label();
		private VUI.Label genitals_ = new VUI.Label();
		private VUI.Label penetration_ = new VUI.Label();
		private VUI.Label total_ = new VUI.Label();
		private VUI.Label state_ = new VUI.Label();

		private VUI.CheckBox forceExcitement_ = new VUI.CheckBox("Force excitement");
		private VUI.FloatTextSlider forceExcitementValue_ = new VUI.FloatTextSlider();


		public PersonBodyTab(Person person)
		{
			person_ = person;

			var gl = new VUI.GridLayout(2);
			gl.HorizontalSpacing = 20;
			gl.HorizontalStretch = new List<bool>() { false, true };

			var p = new VUI.Panel(gl);

			p.Add(new VUI.Label("Sweat"));
			p.Add(sweat_);

			p.Add(new VUI.Label("Flush"));
			p.Add(flush_);

			p.Add(new VUI.Label("Hair loose"));
			p.Add(hairLoose_);

			p.Add(new VUI.Spacer(30));
			p.Add(new VUI.Spacer(30));

			p.Add(new VUI.Label("Excitement", UnityEngine.FontStyle.Bold));
			p.Add(new VUI.Spacer(0));

			p.Add(new VUI.Label("Value"));
			p.Add(excitement_);

			p.Add(new VUI.Label("Mouth"));
			p.Add(mouth_);

			p.Add(new VUI.Label("Breasts"));
			p.Add(breasts_);

			p.Add(new VUI.Label("Genitals"));
			p.Add(genitals_);

			p.Add(new VUI.Label("Penetration"));
			p.Add(penetration_);

			p.Add(new VUI.Label("Total rate"));
			p.Add(total_);

			p.Add(new VUI.Label("State"));
			p.Add(state_);

			p.Add(forceExcitement_);
			p.Add(forceExcitementValue_);


			Layout = new VUI.BorderLayout();
			Add(p, VUI.BorderLayout.Top);


			forceExcitement_.Changed += OnForceExcitementCheck;
			forceExcitementValue_.ValueChanged += OnForceExcitement;
		}

		public override string Title
		{
			get { return "Body"; }
		}

		public override void Update(float s)
		{
			sweat_.Text = $"{person_.Body.DampedSweat}";
			flush_.Text = $"{person_.Body.DampedFlush}";
			hairLoose_.Text = $"{person_.Hair.DampedLoose}";

			var e = person_.Excitement;
			excitement_.Text = e.ToString();
			mouth_.Text = $"{e.Mouth:0.000000} {e.MouthRate:0.000000}";
			breasts_.Text = $"{e.Breasts:0.000000} {e.BreastsRate:0.000000}";
			genitals_.Text = $"{e.Genitals:0.000000} {e.GenitalsRate:0.000000}";
			penetration_.Text = $"{e.Penetration:0.000000} {e.PenetrationRate:0.000000}";
			total_.Text = $"{e.Rate:0.000000}";
			state_.Text = e.StateString;
		}

		private void OnForceExcitementCheck(bool b)
		{
			if (b)
				person_.Excitement.ForceValue(forceExcitementValue_.Value);
			else
				person_.Excitement.ForceValue(-1);
		}

		private void OnForceExcitement(float f)
		{
			if (forceExcitement_.Checked)
				person_.Excitement.ForceValue(f);
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

				int fontSize = 24;
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

					int fontSize = 24;
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
