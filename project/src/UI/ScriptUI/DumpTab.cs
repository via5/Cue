using System.Collections.Generic;

namespace Cue
{
	class PersonDumpTab : Tab
	{
		private Person person_;
		private VUI.ListView<string> list_ = new VUI.ListView<string>();

		public PersonDumpTab(Person person)
			: base("Dump", false)
		{
			person_ = person;

			Layout = new VUI.BorderLayout(10);

			var p = new VUI.Panel(new VUI.HorizontalFlow(10));
			p.Add(new VUI.Button("Gaze", DumpGaze));
			p.Add(new VUI.Button("Morphs", DumpMorphs));

			Add(p, VUI.BorderLayout.Top);
			Add(list_, VUI.BorderLayout.Center);
		}

		private string I(int i)
		{
			return new string(' ', i * 4);
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
	}
}
