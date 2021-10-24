using SimpleJSON;
using System.Collections.Generic;

namespace Cue.Proc
{
	abstract class BasicProcAnimation : IAnimation
	{
		private readonly string name_;
		private bool hasMovement_;
		private RootTargetGroup root_;
		protected Person person_ = null;

		public BasicProcAnimation(string name, bool hasMovement = true)
		{
			name_ = name;
			hasMovement_ = hasMovement;
			root_ = new RootTargetGroup();
		}

		public abstract BasicProcAnimation Clone();

		protected virtual void CopyFrom(BasicProcAnimation o)
		{
			root_ = (RootTargetGroup)o.root_.Clone();
		}

		public string Name
		{
			get { return name_; }
		}

		public virtual bool Done
		{
			get { return root_.Done; }
		}

		// todo
		public float InitFrame { get { return -1; } }
		public float FirstFrame { get { return -1; } }
		public float LastFrame { get { return -1; } }

		public bool HasMovement
		{
			get { return hasMovement_; }
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

		public virtual bool Start(Person p, AnimationContext cx)
		{
			person_ = p;
			SetEnergySource(p);
			root_.Start(p, cx);
			return true;
		}

		protected void SetEnergySource(Person p)
		{
			root_.SetEnergySource(p);
		}

		public virtual void Reset()
		{
			root_.Reset();
		}

		public virtual void FixedUpdate(float s)
		{
			root_.FixedUpdate(s);
		}

		public virtual string[] GetAllForcesDebug()
		{
			var list = new List<string>();
			root_.GetAllForcesDebug(list);
			return list.ToArray();
		}

		private string I(int i)
		{
			return new string(' ', i * 4);
		}

		public virtual string[] Debug()
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

		public override string ToString()
		{
			return name_;
		}

		public virtual string ToDetailedString()
		{
			return ToString();
		}
	}


	class ProcAnimation : BasicProcAnimation
	{
		public ProcAnimation(string name, bool hasMovement=true)
			: base(name, hasMovement)
		{
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

		public override BasicProcAnimation Clone()
		{
			var a = new ProcAnimation(Name, HasMovement);
			a.CopyFrom(this);
			return a;
		}

	}
}
