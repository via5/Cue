using System.Collections.Generic;

namespace Cue
{
	class PersonDumpTab : Tab
	{
		private Person person_;
		private VUI.ListView<string> list_ = new VUI.ListView<string>();

		public PersonDumpTab(Person person)
			: base("Dump")
		{
			person_ = person;

			Layout = new VUI.BorderLayout(10);

			var p = new VUI.Panel(new VUI.HorizontalFlow(10));
			p.Add(new VUI.Button("Expression", DumpExpression));
			p.Add(new VUI.Button("Animation", DumpAnimation));
			p.Add(new VUI.Button("Gaze", DumpGaze));
			p.Add(new VUI.Button("Morphs", DumpMorphs));

			Add(p, VUI.BorderLayout.Top);
			Add(list_, VUI.BorderLayout.Center);
		}

		public override void Update(float s)
		{
		}

		private string I(int i)
		{
			return new string(' ', i * 4);
		}

		private void DumpExpression()
		{
			var pex = person_.Expression as Proc.Expression;
			if (pex == null)
			{
				list_.Clear();
				list_.AddItem("not procedural");
				return;
			}

			var items = new List<string>();

			foreach (var e in pex.All)
			{
				items.Add(I(1) + e.ToString());

				foreach (var g in e.Groups)
				{
					items.Add(I(2) + g.ToString());

					foreach (var m in g.Morphs)
					{
						items.Add(I(3) + m.ToString());

						{
							var syncLines = m.Sync.ToDetailedString().Split('\n');
							if (syncLines.Length > 0)
								items.Add(I(4) + "sync: " + syncLines[0]);

							for (int i = 1; i < syncLines.Length; ++i)
								items.Add(I(5) + syncLines[i]);
						}

						foreach (var line in m.ToDetailedString().Split('\n'))
							items.Add(I(4) + line);
					}
				}
			}

			list_.SetItems(items);
		}

		private void DumpAnimation()
		{
			var player = person_.Animator.CurrentPlayer as Proc.Player;
			var p = player?.Current;

			if (p == null)
			{
				list_.Clear();
				list_.AddItem("not procedural");
				return;
			}

			var items = new List<string>();

			items.Add(p.ToDetailedString());

			foreach (var s in p.Targets)
				DumpTarget(items, s, 1);

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
					items.Add($"#{i}  {t} w={t.Weight:0.00}: {t.Why}");
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
