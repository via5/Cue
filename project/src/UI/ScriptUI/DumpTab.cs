using System.Collections.Generic;

namespace Cue
{
	class PersonDumpTab : Tab
	{
		private const int None = 0;
		private const int Gaze = 1;
		private const int Morphs = 2;
		private const int Expression = 3;

		private Person person_;
		private VUI.ListView<string> list_ = new VUI.ListView<string>();
		private int what_ = None;

		public PersonDumpTab(Person person)
			: base("Dump", false)
		{
			person_ = person;

			Layout = new VUI.BorderLayout(10);

			var p = new VUI.Panel(new VUI.HorizontalFlow(10));
			p.Add(new VUI.Button("None", () => what_ = None));
			p.Add(new VUI.Button("Gaze", () => what_ = Gaze));
			p.Add(new VUI.Button("Morphs", () => what_ = Morphs));
			p.Add(new VUI.Button("Expressions", () => what_ = Expression));

			Add(p, VUI.BorderLayout.Top);
			Add(list_, VUI.BorderLayout.Center);

			list_.Font = VUI.Style.Theme.MonospaceFont;
			list_.FontSize = 22;
		}

		protected override void DoUpdate(float s)
		{
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

				case Morphs:
				{
					DumpMorphs();
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
			var items = new List<string>();

			items.Add(person_.Gaze.LastString);
			items.Add("picker: " + person_.Gaze.Picker.LastString);
			items.Add("");

			for (int i = 0; i < targets.Length; ++i)
			{
				var t = targets[i];

				if (t.WasSet)
				{
					string s = $"#{i}  {t} w={t.Weight:0.00}: {t.Why}";

					if (t.Failure != "")
						s += $" (failed: {t.Failure})";

					items.Add(s);
				}
			}

			foreach (var a in person_.Gaze.Targets.GetAllAvoidForDebug())
				items.Add($"Avoid: {a}");

			list_.SetItems(items);
		}

		private void DumpMorphs()
		{
			var items = new List<string>();
			var mm = Sys.Vam.VamMorphManager.Instance;

			foreach (var m in mm.GetAll())
				items.Add(m.ToString());

			list_.SetItems(items);
		}

		private void DumpExpression()
		{
			list_.SetItems(person_.Expression.Debug());
		}
	}
}
