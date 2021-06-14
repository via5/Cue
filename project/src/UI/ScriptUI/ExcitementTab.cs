using System.Collections.Generic;

namespace Cue
{
	class PersonExcitementTab : Tab
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


		public PersonExcitementTab(Person person)
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
			get { return "Excitement"; }
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
}
