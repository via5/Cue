﻿using SimpleJSON;
using System.Collections.Generic;

namespace Cue.Proc
{
	abstract class BasicProcAnimation : BuiltinAnimation
	{
		private RootTargetGroup root_;

		public BasicProcAnimation(string name)
			: base(name)
		{
			root_ = new RootTargetGroup();
		}

		protected void CopyFrom(BasicProcAnimation o)
		{
			base.CopyFrom(o);
			root_ = (RootTargetGroup)o.root_.Clone();
		}

		public override bool Done
		{
			get { return root_.Done; }
		}

		public RootTargetGroup RootGroup
		{
			get { return root_; }
		}

		public void AddTarget(ITarget t)
		{
			root_.AddTarget(t);
		}

		public List<ITarget> Targets
		{
			get { return root_.Targets; }
		}

		public ITarget FindTarget(string name)
		{
			return root_.FindTarget(name);
		}

		public override bool Start(Person p, AnimationContext cx)
		{
			person_ = p;
			SetEnergySource(p);
			root_.Start(p, cx);
			return true;
		}

		public override void RequestStop()
		{
			root_.RequestStop();
		}

		protected void SetEnergySource(Person p)
		{
			root_.SetEnergySource(p);
		}

		public override void Reset()
		{
			root_.Reset();
		}

		public override void FixedUpdate(float s)
		{
			root_.FixedUpdate(s);
		}

		public override string[] GetAllForcesDebug()
		{
			var list = new List<string>();
			root_.GetAllForcesDebug(list);
			return list.ToArray();
		}

		private string I(int i)
		{
			return new string(' ', i * 4);
		}

		public override string[] Debug()
		{
			var items = new List<string>();

			var ds = ToDetailedString().Split('\n');
			if (ds.Length > 0)
			{
				items.Add(ds[0]);
				for (int i = 1; i < ds.Length; ++i)
					items.Add(I(1) + ds[i]);
			}

			foreach (var s in Targets)
				DebugTarget(items, s, 1);

			return items.ToArray();
		}

		private void DebugTarget(List<string> items, ITarget t, int indent)
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

			if (t is ITargetGroup)
			{
				foreach (var c in (t as ITargetGroup).Targets)
					DebugTarget(items, c, indent + 1);
			}
		}
	}


	class ProcAnimation : BasicProcAnimation
	{
		public ProcAnimation(string name, bool hasMovement)
			: base(name)
		{
			HasMovement = hasMovement;
		}

		public static ProcAnimation Create(JSONClass o)
		{
			var file = Cue.Instance.Sys.GetResourcePath(o["file"]);
			var doc = JSON.Parse(Cue.Instance.Sys.ReadFileIntoString(file));

			if (doc == null)
			{
				Cue.LogError($"failed to parse proc animation file '{file}'");
				return null;
			}

			var docRoot = doc.AsObject;
			string name = docRoot["name"];
			bool hasMovement = docRoot["hasMovement"].AsBool;

			try
			{
				var a = new ProcAnimation(name, hasMovement);

				foreach (JSONClass n in docRoot["targets"].AsArray)
					a.AddTarget(CreateTarget(n["type"], n));

				return a;
			}
			catch (LoadFailed e)
			{
				Cue.LogError($"{file}: {e.Message}");
				return null;
			}
		}

		public static ITarget CreateTarget(string type, JSONClass o)
		{
			switch (type)
			{
				case "seq": return SequentialTargetGroup.Create(o);
				case "con": return ConcurrentTargetGroup.Create(o);
				case "rforce": return Force.Create(Force.RelativeForce, o);
				case "rtorque": return Force.Create(Force.RelativeTorque, o);
				case "morph": return MorphTarget.Create(o);

				default:
				{
					Cue.LogInfo($"bad target type '{type}'");
					return null;
				}
			}
		}

		public override BuiltinAnimation Clone()
		{
			var a = new ProcAnimation(Name, HasMovement);
			a.CopyFrom(this);
			return a;
		}

	}
}