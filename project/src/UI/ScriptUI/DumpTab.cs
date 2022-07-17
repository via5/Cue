using System.Collections.Generic;

namespace Cue
{
	class PersonDumpTab : Tab
	{
		private const int None = 0;
		private const int Gaze = 1;
		private const int VamMorphs = 2;
		private const int MorphGroups = 3;
		private const int Expression = 4;

		private Person person_;
		private VUI.ListView<string> list_ = new VUI.ListView<string>();
		private int what_ = None;
		private DebugLines debug_ = null;

		public PersonDumpTab(Person person)
			: base("Dump", false)
		{
			person_ = person;

			Layout = new VUI.BorderLayout(10);

			var p = new VUI.Panel(new VUI.HorizontalFlow(10));
			p.Add(new VUI.Button("None", () => what_ = None));
			p.Add(new VUI.Button("Gaze", () => what_ = Gaze));
			p.Add(new VUI.Button("VAM Morphs", () => what_ = VamMorphs));
			p.Add(new VUI.Button("Morph groups", () => what_ = MorphGroups));
			p.Add(new VUI.Button("Expressions", () => what_ = Expression));

			Add(p, VUI.BorderLayout.Top);
			Add(list_, VUI.BorderLayout.Center);

			list_.Font = VUI.Style.Theme.MonospaceFont;
			list_.FontSize = 22;
		}

		protected override void DoUpdate(float s)
		{
			if (debug_ == null)
				debug_ = new DebugLines();

			debug_.Clear();

			switch (what_)
			{
				case None:
				{
					if (list_.Count > 0)
						list_.Clear();

					break;
				}

				case Gaze:
				{
					DumpGaze();
					break;
				}

				case VamMorphs:
				{
					DumpVamMorphs();
					break;
				}

				case MorphGroups:
				{
					DumpMorphGroups();
					break;
				}

				case Expression:
				{
					DumpExpression();
					break;
				}
			}
		}

		private void DumpGaze()
		{
			var targets = person_.Gaze.Targets.All;

			debug_.Add(person_.Gaze.LastString);
			debug_.Add("picker: " + person_.Gaze.Picker.LastString);
			debug_.Add("");

			for (int i = 0; i < targets.Length; ++i)
			{
				var t = targets[i];

				if (t.WasSet)
				{
					string s = $"#{i}  {t} w={t.Weight:0.00}: {t.Why}";

					if (t.Failure != "")
						s += $" (failed: {t.Failure})";

					debug_.Add(s);
				}
			}

			foreach (var a in person_.Gaze.Targets.GetAllAvoidForDebug())
				debug_.Add($"Avoid: {a}");

			list_.SetItems(debug_.MakeArray());
		}

		private void DumpVamMorphs()
		{
			var mm = Sys.Vam.VamMorphManager.Instance;

			foreach (var m in mm.GetAll())
				debug_.Add(m.ToString());

			list_.SetItems(debug_.MakeArray());
		}

		private void DumpMorphGroups()
		{
			foreach (var e in person_.Expression.GetAllExpressions())
				e.Expression.MorphGroup.Debug(debug_);

			list_.SetItems(debug_.MakeArray());
		}

		private void DumpExpression()
		{
			person_.Expression.Debug(debug_);
			list_.SetItems(debug_.MakeArray());
		}
	}
}
