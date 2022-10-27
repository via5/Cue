﻿using System;
using System.Collections.Generic;

namespace Cue
{
	class PersonAITab : Tab
	{
		private Person person_;

		public PersonAITab(Person p)
			: base("AI", true)
		{
			person_ = p;

			AddSubTab(new PersonAIStateTab(person_));
			AddSubTab(new PersonAIMoodTab(person_));
			AddSubTab(new PersonAIExcitementTab(person_));
			AddSubTab(new PersonAIPersonalityTab(person_));
			AddSubTab(new PersonAIGazeTab(person_));
			AddSubTab(new PersonAIEventsTab(person_));
		}
	}


	class PersonAIStateTab : Tab
	{
		private Person person_;
		private PersonAI ai_;

		private VUI.Label enabled_ = new VUI.Label();
		private VUI.Label traits_ = new VUI.Label();
		private VUI.Label interacting_ = new VUI.Label();
		private VUI.Label close_ = new VUI.Label();
		private VUI.Label groped_ = new VUI.Label();
		private VUI.Label penetrated_ = new VUI.Label();
		private VUI.Label penetrating_ = new VUI.Label();
		private VUI.Label zapped_ = new VUI.Label();

		public PersonAIStateTab(Person p)
			: base("State", false)
		{
			person_ = p;
			ai_ = (PersonAI)person_.AI;

			var gl = new VUI.GridLayout(2);
			gl.HorizontalSpacing = 20;
			gl.HorizontalStretch = new List<bool>() { false, true };

			var state = new VUI.Panel(gl);
			state.Add(new VUI.Label("Enabled"));
			state.Add(enabled_);

			state.Add(new VUI.Label("Traits"));
			state.Add(traits_);

			state.Add(new VUI.Spacer(20));
			state.Add(new VUI.Spacer(20));

			state.Add(new VUI.Label("Interacting with"));
			state.Add(interacting_);

			state.Add(new VUI.Label("Close"));
			state.Add(close_);

			state.Add(new VUI.Label("Groped by"));
			state.Add(groped_);

			state.Add(new VUI.Label("Penetrated by"));
			state.Add(penetrated_);

			state.Add(new VUI.Label("Penetrating"));
			state.Add(penetrating_);

			state.Add(new VUI.Label("Zapped"));
			state.Add(zapped_);

			Layout = new VUI.BorderLayout();
			Add(state, VUI.BorderLayout.Top);
		}

		protected override void DoUpdate(float s)
		{
			string es = "";

			if (ai_.EventsEnabled)
			{
				if (es != "")
					es += "|";

				es += "events";
			}

			if (es == "")
				es = "nothing";

			enabled_.Text = es;
			traits_.Text = string.Join(", ", person_.Traits);

			string interacting = null, close = null, groped = null;
			string penetrated = null, penetrating = null;

			foreach (var p in Cue.Instance.ActivePersons)
			{
				if (person_.Status.InteractingWith(p))
				{
					if (interacting == null)
						interacting = "";
					else
						interacting += ", ";

					interacting += p.ID;
				}

				if (person_.Status.InsidePersonalSpace(p))
				{
					if (close == null)
						close = "";
					else
						close += ", ";

					close += p.ID;
				}

				if (person_.Status.GropedBy(p))
				{
					if (groped == null)
						groped = "";
					else
						groped += ", ";

					groped += p.ID;
				}

				if (person_.Status.PenetratedBy(p))
				{
					if (penetrated == null)
						penetrated = "";
					else
						penetrated += ", ";

					penetrated += p.ID;
				}

				if (person_.Status.Penetrating(p))
				{
					if (penetrating == null)
						penetrating = "";
					else
						penetrating += ", ";

					penetrating += p.ID;
				}
			}

			interacting_.Text = interacting ?? "nobody";
			close_.Text = close ?? "nobody";
			groped_.Text = groped ?? "nobody";
			penetrated_.Text = penetrated ?? "nobody";
			penetrating_.Text = penetrating ?? "nobody";
			zapped_.Text = person_.Body.Zap.DebugLine(person_);
		}
	}


	class PersonAIMoodTab : Tab
	{
		private readonly Person person_;

		private VUI.Label state_ = new VUI.Label();
		private VUI.Label gazeEnergy_ = new VUI.Label();
		private VUI.Label gazeTiredness_ = new VUI.Label();
		private VUI.Label movementEnergy_ = new VUI.Label();

		public PersonAIMoodTab(Person person)
			: base("Mood", false)
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

			p.Add(new VUI.Label("Gaze energy"));
			p.Add(gazeEnergy_);
			p.Add(new VUI.Spacer(0));
			p.Add(new VUI.Spacer(0));

			p.Add(new VUI.Label("Gaze tiredness"));
			p.Add(gazeTiredness_);
			p.Add(new VUI.Spacer(0));
			p.Add(new VUI.Spacer(0));

			p.Add(new VUI.Label("Movement energy"));
			p.Add(movementEnergy_);
			p.Add(new VUI.Spacer(0));
			p.Add(new VUI.Spacer(0));

			p.Add(new VUI.Spacer(30));
			p.Add(new VUI.Spacer(30));
			p.Add(new VUI.Spacer(30));
			p.Add(new VUI.Spacer(30));


			p.Add(new VUI.Label("Moods", UnityEngine.FontStyle.Bold));
			p.Add(new VUI.Spacer(0));
			p.Add(new VUI.Spacer(0));
			p.Add(new VUI.Spacer(0));

			foreach (MoodType i in MoodType.Values)
				AddForceable(p, person_.Mood.GetValue(i), $"    {MoodType.ToString(i)}");

			p.Add(new VUI.Spacer(30));
			p.Add(new VUI.Spacer(30));
			p.Add(new VUI.Spacer(30));
			p.Add(new VUI.Spacer(30));

			p.Add(new VUI.Button("Orgasm", () => { person_.Mood.ForceOrgasm(); }));
			p.Add(new VUI.Spacer(0));
			p.Add(new VUI.Spacer(0));
			p.Add(new VUI.Spacer(0));


			Add(p);
		}

		protected override void DoUpdate(float s)
		{
			state_.Text = person_.Mood.StateString;
			gazeEnergy_.Text = $"{person_.Mood.GazeEnergy:0.00}";
			gazeTiredness_.Text = $"{person_.Mood.GazeTiredness:0.00}";
			movementEnergy_.Text = $"{person_.Mood.MovementEnergy:0.00}";
		}
	}


	class PersonAIExcitementTab : Tab
	{
		private Person person_;
		private VUI.ListView<string> list_ = new VUI.ListView<string>();

		public PersonAIExcitementTab(Person p)
			: base("Excitement", false)
		{
			person_ = p;

			Layout = new VUI.BorderLayout();
			Add(list_, VUI.BorderLayout.Center);

			list_.Font = VUI.Style.Theme.MonospaceFont;
			list_.FontSize = 22;
		}

		protected override void DoUpdate(float s)
		{
			list_.SetItems(person_.Excitement.Debug());
		}
	}


	class PersonAIPersonalityTab : Tab
	{
		private Person person_;

		private VUI.ListView<string> list_ = new VUI.ListView<string>();

		public PersonAIPersonalityTab(Person p)
			: base("Personality", false)
		{
			person_ = p;

			Layout = new VUI.BorderLayout();
			Add(list_, VUI.BorderLayout.Center);

			list_.Font = VUI.Style.Theme.MonospaceFont;
			list_.FontSize = 22;
		}

		protected override void DoUpdate(float s)
		{
			var ps = person_.Personality;
			var exps = new List<string[]>();

			exps.Add(new string[] { "name", ps.Name });

			foreach (ZoneType z in ZoneType.Values)
			{
				var ss = ps.Sensitivities.Get(z);

				exps.Add(new string[]
				{
					ZoneType.ToString(ss.Type),
					$"pRate={ss.PhysicalRate:0.00000} " +
					$"pMax={ss.PhysicalMaximum:0.00} " +
					$"npRate={ss.NonPhysicalRate:0.00000} " +
					$"npMax={ss.NonPhysicalMaximum:0.00}"
				});

				foreach (var m in ss.Modifiers)
					exps.Add(new string[] { $"    {m}", "" });
			}

			//var v = ps.Voice;
			//exps.Add(new string[] { "orgasm ds", v.OrgasmDataset.Name });
			//
			//foreach (var ds in v.Datasets)
			//{
			//	exps.Add(new string[] {
			//		ds.dataset.Name,
			//		$"[{ds.intensityMin}, {ds.intensityMax}]" });
			//}

			list_.SetItems(MakeTable(ps, exps.ToArray()));
		}

		private static List<string> MakeTable(EnumValueManager v, string[][] more)
		{
			int longest = 0;

			foreach (var n in v.Values.GetAllNames())
				longest = Math.Max(longest, n.Length);


			var items = new List<string>();

			foreach (var i in v.Values.GetDurationIndexes())
			{
				items.Add(
					v.Values.GetDurationName(i).PadRight(longest) +
					"   " +
					v.GetDuration(i).ToString());
			}

			foreach (var i in v.Values.GetBoolIndexes())
			{
				items.Add(
					v.Values.GetBoolName(i).PadRight(longest) +
					"   " +
					v.GetBool(i).ToString());
			}

			foreach (var i in v.Values.GetFloatIndexes())
			{
				items.Add(
					v.Values.GetFloatName(i).PadRight(longest) +
					"   " +
					v.Get(i).ToString());
			}

			foreach (var i in v.Values.GetStringIndexes())
			{
				items.Add(
					v.Values.GetStringName(i).PadRight(longest) +
					"   " +
					v.GetString(i));
			}

			for (int i = 0; i < more.Length; ++i)
			{
				items.Add(
					more[i][0].PadRight(longest) +
					"   " +
					more[i][1]);
			}

			return items;
		}
	}

	class PersonAIGazeTab : Tab
	{
		private Person person_;
		private PersonAI ai_;

		private VUI.Label eyesBlink_ = new VUI.Label();
		private VUI.Label eyesPos_ = new VUI.Label();
		private VUI.Label gazerType_ = new VUI.Label();
		private VUI.Label gazerEnabled_ = new VUI.Label();
		private VUI.Label gazerDuration_ = new VUI.Label();
		private VUI.Label gazerVariance_ = new VUI.Label();
		private VUI.Label debug_ = new VUI.Label();
		private VUI.Label targetType_ = new VUI.Label();
		private VUI.Label targetTemporary_ = new VUI.Label();
		private VUI.Label targetEmergency_ = new VUI.Label();
		private VUI.Label targetReluctant_ = new VUI.Label();
		private VUI.Label avoid_ = new VUI.Label();
		private VUI.Label next_ = new VUI.Label();

		public PersonAIGazeTab(Person person)
			: base("Gaze", false)
		{
			person_ = person;
			ai_ = (PersonAI)person_.AI;

			Layout = new VUI.VerticalFlow();


			var gl = new VUI.GridLayout(2);
			gl.HorizontalSpacing = 20;
			gl.HorizontalStretch = new List<bool>() { false, true };

			var p = new VUI.Panel(gl);

			p.Add(new VUI.Label("Eyes",	UnityEngine.FontStyle.Bold));
			p.Add(new VUI.Spacer());

			p.Add(new VUI.Label("Blink"));
			p.Add(eyesBlink_);

			p.Add(new VUI.Label("Look at"));
			p.Add(eyesPos_);


			p.Add(new VUI.Spacer(20));
			p.Add(new VUI.Spacer(20));


			p.Add(new VUI.Label("Gazer", UnityEngine.FontStyle.Bold));
			p.Add(new VUI.Spacer());

			p.Add(new VUI.Label("Type"));
			p.Add(gazerType_);

			p.Add(new VUI.Label("Enabled"));
			p.Add(gazerEnabled_);

			p.Add(new VUI.Label("Duration"));
			p.Add(gazerDuration_);

			p.Add(new VUI.Label("Variance"));
			p.Add(gazerVariance_);

			p.Add(new VUI.Label("Debug"));
			p.Add(debug_);


			p.Add(new VUI.Spacer(20));
			p.Add(new VUI.Spacer(20));


			p.Add(new VUI.Label("Target", UnityEngine.FontStyle.Bold));
			p.Add(new VUI.Spacer());

			p.Add(new VUI.Label("Type"));
			p.Add(targetType_);

			p.Add(new VUI.Label("Temporary"));
			p.Add(targetTemporary_);

			p.Add(new VUI.Label("Emergency"));
			p.Add(targetEmergency_);

			p.Add(new VUI.Label("Reluctant"));
			p.Add(targetReluctant_);

			p.Add(new VUI.Label("Avoid"));
			p.Add(avoid_);

			p.Add(new VUI.Label("Next in"));
			p.Add(next_);

			p.Add(new VUI.Spacer(20));
			p.Add(new VUI.Spacer(20));

			Add(p);


			p = new VUI.Panel(new VUI.VerticalFlow(5));

			p.Add(new VUI.CheckBox(
				"Render frustums",
				(b) => person_.Gaze.Render.Frustums = b,
				person_.Gaze.Render.Frustums));

			p.Add(new VUI.CheckBox(
				"Render front plane",
				(b) => person_.Gaze.Render.FrontPlane = b,
				person_.Gaze.Render.FrontPlane));

			p.Add(new VUI.ComboBox<string>(ForceLooks.Names, OnForceLook));
			p.Add(new VUI.CheckBox("Manage blink", OnAutoBlink, true));

			Add(p);
		}

		protected override void DoUpdate(float s)
		{
			var g = person_.Gaze;

			eyesBlink_.Text = $"{(g.Eyes.Blink ? "yes" : "no")}";
			eyesPos_.Text = $"{g.Eyes.TargetPosition}";

			gazerType_.Text = $"{g.Gazer.Name}";
			gazerEnabled_.Text = $"{(g.Gazer.Enabled ? "yes" : "no")}";
			gazerDuration_.Text = $"{g.Gazer.Duration:0.00}s";
			gazerVariance_.Text = $"{g.Gazer.Variance:0.00}";
			debug_.Text = g.DebugString();

			if (g.Picker.HasTarget)
			{
				targetType_.Text = $"{g.Picker.CurrentTarget}";
				targetEmergency_.Text = $"{(g.IsEmergency ? "yes" : "no")}";
				targetReluctant_.Text = $"{(g.Picker.CurrentTarget.Reluctant ? "yes" : "no")}";

				if (g.Picker.IsTargetTemporary)
				{
					targetTemporary_.Text =
						$"yes " +
						$"{g.Picker.TemporaryTargetElapsed:0.00}/" +
						$"{g.Picker.TemporaryTargetTime:0.00}";
				}
				else
				{
					targetTemporary_.Text = "no";
				}
			}
			else
			{
				targetType_.Text = "none";
				targetEmergency_.Text = "no";
				targetReluctant_.Text = "no";
				targetTemporary_.Text = "no";
			}

			string avoidString = g.Picker.AvoidString;

			if (g.Targets.TemporaryAvoidTarget != null)
			{
				if (avoidString != "")
					avoidString += ", ";

				avoidString +=
					$"temp {g.Targets.TemporaryAvoidTarget} " +
					$"{g.Targets.TemporaryAvoidElapsed:0.00}/" +
					$"{g.Targets.TemporaryAvoidTime:0.00}";
			}

			avoid_.Text = avoidString;

			next_.Text =
				$"{g.Picker.NextInterval.Remaining:0.0} " +
				$"({g.Picker.NextInterval.Minimum:0.0}-{g.Picker.NextInterval.Maximum:0.0})";

		}

		private void OnForceLook(int s)
		{
			person_.Gaze.ForceLook = s;
		}

		private void OnAutoBlink(bool b)
		{
			person_.Gaze.AutoBlink = b;
		}
	}



	class PersonAIEventsTab : Tab
	{
		private Person person_;

		private VUI.ComboBox<IEvent> events_;
		private VUI.ListView<string> list_ = new VUI.ListView<string>();
		private VUI.Panel buttons_ = new VUI.Panel();
		private DebugLines debug_ = new DebugLines();

		public PersonAIEventsTab(Person person)
			: base("Events", false)
		{
			person_ = person;

			var es = new List<IEvent>(person_.AI.Events);
			U.NatSort(es);

			Layout = new VUI.BorderLayout(10);

			events_ = new VUI.ComboBox<IEvent>(es, OnSelection);
			list_.Font = VUI.Style.Theme.MonospaceFont;
			list_.FontSize = 22;

			buttons_.Layout = new VUI.HorizontalFlow();

			var content = new VUI.Panel(new VUI.BorderLayout());
			content.Add(buttons_, VUI.BorderLayout.Top);
			content.Add(list_, VUI.BorderLayout.Center);

			Add(events_, VUI.BorderLayout.Top);
			Add(content, VUI.BorderLayout.Center);

			OnSelection(null);
		}

		protected override void DoUpdate(float s)
		{
			if (person_.ID == "Person")
				list_.Name = "eventsList";

			debug_.Clear();

			if (events_.Selected != null)
				events_.Selected.Debug(debug_);

			list_.SetItems(debug_.MakeArray());
		}

		private void OnSelection(IEvent e)
		{
			buttons_.RemoveAllChildren();

			if (e != null)
			{
				var bs = e.DebugButtons();

				if (bs != null)
				{
					foreach (var b in bs.buttons)
						buttons_.Add(new VUI.Button(b.text, () => b.f()));
				}
			}
		}
	}
}
