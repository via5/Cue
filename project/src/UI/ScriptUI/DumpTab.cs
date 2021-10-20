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
			p.Add(new VUI.Button("Animation", DumpAnimation));
			p.Add(new VUI.Button("Gaze", DumpGaze));
			p.Add(new VUI.Button("Morphs", DumpMorphs));

			Add(p, VUI.BorderLayout.Top);
			Add(list_, VUI.BorderLayout.Center);
		}

		private string I(int i)
		{
			return new string(' ', i * 4);
		}

		private void DumpAnimation()
		{
			var items = new List<string>();

			foreach (var pl in person_.Animator.Players)
			{
				foreach (var a in pl.GetPlaying())
				{
					var p = a as Proc.BasicProcAnimation;
					if (p == null)
						continue;

					var ds = p.ToDetailedString().Split('\n');
					if (ds.Length > 0)
					{
						items.Add(ds[0]);
						for (int i = 1; i < ds.Length; ++i)
							items.Add(I(1) + ds[i]);
					}

					foreach (var s in p.Targets)
						DumpTarget(items, s, 1);
				}
			}

			if (items.Count == 0)
				list_.AddItem("not procedural");

			list_.SetItems(items);
		}

		private void DumpTarget(List<string> items, Proc.ITarget t, int indent)
		{
			var lines = t.ToDetailedString().Split('\n');
			if (lines.Length > 0)
				items.Add(I(indent) + lines[0]);

			{
				var syncLines = t.Sync.ToDetailedString().Split('\n');
				if (syncLines.Length > 0)
					items.Add(I(indent + 1) + "sync: " + syncLines[0]);

				for (int i = 1; i < syncLines.Length; ++i)
					items.Add(I(indent + 2) + syncLines[i]);
			}

			for (int i = 1; i < lines.Length; ++i)
				items.Add(I(indent + 1) + lines[i]);

			if (t is Proc.ITargetGroup)
			{
				foreach (var c in (t as Proc.ITargetGroup).Targets)
					DumpTarget(items, c, indent + 1);
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
	}
}
