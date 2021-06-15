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
		private VUI.Label[] reasons_ = new VUI.Label[Excitement.ReasonCount];
		private VUI.Label mouth_ = new VUI.Label();
		private VUI.Label breasts_ = new VUI.Label();
		private VUI.Label genitals_ = new VUI.Label();
		private VUI.Label penetration_ = new VUI.Label();
		private VUI.Label otherSex_ = new VUI.Label();
		private VUI.Label physical_ = new VUI.Label();
		private VUI.Label emotional_ = new VUI.Label();
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


			for (int i = 0; i < Excitement.ReasonCount; ++i)
			{
				reasons_[i] = new VUI.Label();
				p.Add(new VUI.Label(person_.Excitement.GetReason(i).Name));
				p.Add(reasons_[i]);
			}

			p.Add(new VUI.Spacer(30));
			p.Add(new VUI.Spacer(30));

			p.Add(new VUI.Label("Physical rate"));
			p.Add(physical_);

			p.Add(new VUI.Label("Emotional rate"));
			p.Add(emotional_);

			p.Add(new VUI.Label("Total rate"));
			p.Add(total_);

			p.Add(new VUI.Spacer(30));
			p.Add(new VUI.Spacer(30));

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

			for (int i = 0; i < reasons_.Length; ++i)
				reasons_[i].Text = e.GetReason(i).ToString();

			physical_.Text = $"{e.PhysicalRate:0.000000}";
			emotional_.Text = $"{e.EmotionalRate:0.000000}";
			total_.Text = $"{e.TotalRate:0.000000}";

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
