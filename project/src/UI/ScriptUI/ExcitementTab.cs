using System.Collections.Generic;

namespace Cue
{
	class PersonExcitementTab : Tab
	{
		class ForceableFloatWidgets
		{
			private readonly ForceableFloat f_;
			private readonly VUI.Label caption_;
			private readonly VUI.Label value_;
			private readonly VUI.CheckBox isForced_;
			private readonly VUI.FloatTextSlider forced_;

			public ForceableFloatWidgets(ForceableFloat f, string caption)
			{
				f_ = f;
				caption_ = new VUI.Label(caption);
				value_ = new VUI.Label();
				isForced_ = new VUI.CheckBox("Force");
				forced_ = new VUI.FloatTextSlider();

				isForced_.Changed += OnForceChecked;
				forced_.ValueChanged += OnForceChanged;
			}

			public VUI.Label Caption { get { return caption_; } }
			public VUI.Label Value { get { return value_; } }
			public VUI.CheckBox IsForced { get { return isForced_; } }
			public VUI.FloatTextSlider Forced { get { return forced_; } }

			public void Update(float s)
			{
				value_.Text = $"{f_}";
			}

			private void OnForceChecked(bool b)
			{
				if (b)
					f_.SetForced(forced_.Value);
				else
					f_.UnsetForced();
			}

			private void OnForceChanged(float f)
			{
				if (isForced_.Checked)
					f_.SetForced(forced_.Value);
			}
		}

		private readonly Person person_;

		private VUI.Label state_ = new VUI.Label();

		private VUI.Label temperature_ = new VUI.Label();
		private VUI.Label sweat_ = new VUI.Label();
		private VUI.Label flush_ = new VUI.Label();
		private VUI.Label hairLoose_ = new VUI.Label();

		private VUI.Label exValue_ = new VUI.Label();
		private VUI.Label exFlat_ = new VUI.Label();
		private VUI.Label exMax_ = new VUI.Label();
		private VUI.Label[] reasons_ = new VUI.Label[Excitement.ReasonCount];
		private VUI.Label physical_ = new VUI.Label();
		private VUI.Label emotional_ = new VUI.Label();
		private VUI.Label total_ = new VUI.Label();

		private List<ForceableFloatWidgets> forceables_ =
			new List<ForceableFloatWidgets>();


		public PersonExcitementTab(Person person)
			: base("Mood")
		{
			person_ = person;

			Layout = new VUI.VerticalFlow();


			var gl = new VUI.GridLayout(4);
			gl.HorizontalSpacing = 20;
			gl.HorizontalStretch = new List<bool>() { false, true };

			var p = new VUI.Panel(gl);
			p.Add(new VUI.Label("State"));
			p.Add(state_);
			p.Add(new VUI.Spacer(0));
			p.Add(new VUI.Spacer(0));

			AddForceable(p, person_.Mood.ExcitementValue, "Excitement");
			AddForceable(p, person_.Mood.DampedTiredness, "Tiredness");

			p.Add(new VUI.Button("Orgasm", () => { person_.Mood.ForceOrgasm(); }));
			p.Add(new VUI.Spacer(0));
			p.Add(new VUI.Spacer(0));
			p.Add(new VUI.Spacer(0));

			Add(p);
			Add(new VUI.Spacer(30));



			gl = new VUI.GridLayout(2);
			gl.HorizontalSpacing = 20;
			gl.HorizontalStretch = new List<bool>() { false, true };

			p = new VUI.Panel(gl);

			p.Add(new VUI.Spacer(30));
			p.Add(new VUI.Spacer(30));

			p.Add(new VUI.Label("Body", UnityEngine.FontStyle.Bold));
			p.Add(new VUI.Spacer(0));

			p.Add(new VUI.Label("Temperature"));
			p.Add(temperature_);

			p.Add(new VUI.Label("Sweat"));
			p.Add(sweat_);

			p.Add(new VUI.Label("Flush"));
			p.Add(flush_);

			p.Add(new VUI.Label("Hair loose"));
			p.Add(hairLoose_);

			Add(p);



			gl = new VUI.GridLayout(2);
			gl.HorizontalSpacing = 20;
			gl.HorizontalStretch = new List<bool>() { false, true };

			p = new VUI.Panel(gl);

			p.Add(new VUI.Spacer(30));
			p.Add(new VUI.Spacer(30));

			p.Add(new VUI.Label("Excitement", UnityEngine.FontStyle.Bold));
			p.Add(new VUI.Spacer(0));

			p.Add(new VUI.Label("Value"));
			p.Add(exValue_);

			p.Add(new VUI.Label("Flat"));
			p.Add(exFlat_);

			p.Add(new VUI.Label("Max"));
			p.Add(exMax_);

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


			Add(p);
		}

		public override void Update(float s)
		{
			for (int i = 0; i < forceables_.Count; ++i)
				forceables_[i].Update(s);

			temperature_.Text = $"{person_.Body.DampedTemperature}";
			sweat_.Text = $"{person_.Atom.Body.Sweat:0.000000}";
			flush_.Text = $"{person_.Atom.Body.Flush:0.000000}";
			hairLoose_.Text = $"{person_.Atom.Hair.Loose:0.000000}";

			var e = person_.Excitement;

			exValue_.Text = $"{e.Value:0.000000}";
			exFlat_.Text = $"{e.FlatValue:0.000000}";
			exMax_.Text = $"{e.Max:0.000000}";

			for (int i = 0; i < reasons_.Length; ++i)
				reasons_[i].Text = e.GetReason(i).ToString();

			physical_.Text = $"{e.PhysicalRate:0.000000}";
			emotional_.Text = $"{e.EmotionalRate:0.000000}";
			total_.Text = $"{e.TotalRate:0.000000}";

			state_.Text = person_.Mood.StateString;
		}

		private void AddForceable(VUI.Panel p, ForceableFloat v, string caption)
		{
			var w = new ForceableFloatWidgets(v, caption);

			p.Add(w.Caption);
			p.Add(w.Value);
			p.Add(w.IsForced);
			p.Add(w.Forced);

			forceables_.Add(w);
		}
	}
}
