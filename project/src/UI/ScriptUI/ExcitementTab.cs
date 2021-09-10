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
			private VUI.IgnoreFlag ignore_ = new VUI.IgnoreFlag();

			public ForceableFloatWidgets(ForceableFloat f, string caption)
			{
				f_ = f;
				caption_ = new VUI.Label(caption);
				value_ = new VUI.Label();
				isForced_ = new VUI.CheckBox("Force");
				forced_ = new VUI.FloatTextSlider();

				forced_.MaximumSize = new VUI.Size(200, DontCare);

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
				if (!isForced_.Checked)
				{
					ignore_.Do(() =>
					{
						forced_.Value = f_.Value;
					});
				}
			}

			private void OnForceChecked(bool b)
			{
				if (ignore_) return;

				if (b)
					f_.SetForced(forced_.Value);
				else
					f_.UnsetForced();
			}

			private void OnForceChanged(float f)
			{
				if (ignore_) return;

				if (isForced_.Checked)
					f_.SetForced(forced_.Value);
				else
					f_.Value = forced_.Value;
			}
		}

		private readonly Person person_;

		private VUI.Label state_ = new VUI.Label();
		private VUI.Label rate_ = new VUI.Label();

		private VUI.Label sweat_ = new VUI.Label();
		private VUI.Label flush_ = new VUI.Label();
		private VUI.Label hairLoose_ = new VUI.Label();

		private VUI.Label exMax_ = new VUI.Label();
		private VUI.Label[] reasons_ = new VUI.Label[Excitement.ReasonCount];
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
			gl.HorizontalStretch = new List<bool>() { false, true, false, false };
			var p = new VUI.Panel(gl);



			p.Add(new VUI.Label("State"));
			p.Add(state_);
			p.Add(new VUI.Spacer(0));
			p.Add(new VUI.Spacer(0));

			AddForceable(p, person_.Mood.FlatExcitementValue, "Excitement");
			AddForceable(p, person_.Mood.TirednessValue, "Tiredness");

			p.Add(new VUI.Label("Rate"));
			p.Add(rate_);
			p.Add(new VUI.Spacer(0));
			p.Add(new VUI.Spacer(0));

			p.Add(new VUI.Button("Orgasm", () => { person_.Mood.ForceOrgasm(); }));
			p.Add(new VUI.Spacer(0));
			p.Add(new VUI.Spacer(0));
			p.Add(new VUI.Spacer(0));



			p.Add(new VUI.Spacer(30));
			p.Add(new VUI.Spacer(30));
			p.Add(new VUI.Spacer(30));
			p.Add(new VUI.Spacer(30));



			p.Add(new VUI.Label("Body", UnityEngine.FontStyle.Bold));
			p.Add(new VUI.Spacer(0));
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



			p.Add(new VUI.Spacer(30));
			p.Add(new VUI.Spacer(30));
			p.Add(new VUI.Spacer(30));
			p.Add(new VUI.Spacer(30));

			p.Add(new VUI.Label("Excitement", UnityEngine.FontStyle.Bold));
			p.Add(new VUI.Spacer(0));
			p.Add(new VUI.Spacer(0));
			p.Add(new VUI.Spacer(0));

			p.Add(new VUI.Label("Max"));
			p.Add(exMax_);
			p.Add(new VUI.Spacer(0));
			p.Add(new VUI.Spacer(0));

			for (int i = 0; i < Excitement.ReasonCount; ++i)
			{
				reasons_[i] = new VUI.Label();
				p.Add(new VUI.Label(person_.Excitement.GetReason(i).Name));
				p.Add(reasons_[i]);
				p.Add(new VUI.Spacer(0));
				p.Add(new VUI.Spacer(0));
			}

			AddForceable(p, person_.Excitement.ForceablePhysicalRate, "Physical rate");
			AddForceable(p, person_.Excitement.ForceableEmotionalRate, "Emotional rate");

			p.Add(new VUI.Label("Total rate"));
			p.Add(total_);
			p.Add(new VUI.Spacer(0));
			p.Add(new VUI.Spacer(0));


			Add(p);
		}

		public override void Update(float s)
		{
			state_.Text = person_.Mood.StateString;
			rate_.Text = person_.Mood.RateString;

			sweat_.Text = $"{person_.Atom.Body.Sweat:0.000000}";
			flush_.Text = $"{person_.Atom.Body.Flush:0.000000}";
			hairLoose_.Text = $"{person_.Atom.Hair.Loose:0.000000}";

			var e = person_.Excitement;

			exMax_.Text = $"{e.Max:0.000000}";

			for (int i = 0; i < reasons_.Length; ++i)
				reasons_[i].Text = e.GetReason(i).ToString();

			total_.Text = $"{e.TotalRate:0.000000}";

			for (int i = 0; i < forceables_.Count; ++i)
				forceables_[i].Update(s);
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
