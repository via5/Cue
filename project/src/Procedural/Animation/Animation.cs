using SimpleJSON;
using System.Collections.Generic;

namespace Cue.Proc
{
	class ProcAnimation : IAnimation
	{
		private readonly string name_;
		private bool hasMovement_;
		private ConcurrentTargetGroup root_ = new ConcurrentTargetGroup("root");

		public ProcAnimation(string name, bool hasMovement=true)
		{
			name_ = name;
			hasMovement_ = hasMovement;
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
					a.root_.AddTarget(CreateTarget(n["type"], n));

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
				case "morph": return AnimatedMorph.Create(o);

				default:
				{
					Cue.LogInfo($"bad target type '{type}'");
					return null;
				}
			}
		}

		public ProcAnimation Clone()
		{
			var a = new ProcAnimation(name_, hasMovement_);
			a.root_ = (ConcurrentTargetGroup)root_.Clone();
			return a;
		}

		public bool Done
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

		public void Start(Person p)
		{
			root_.Start(p);
		}

		public void Reset()
		{
			root_.Reset();
		}

		public void FixedUpdate(float s)
		{
			root_.FixedUpdate(s);
		}

		public override string ToString()
		{
			return name_ + " " + root_.ToString();
		}

		public string ToDetailedString()
		{
			return ToString();
		}
	}
}
