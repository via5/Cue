using SimpleJSON;
using System.Collections.Generic;

namespace Cue.Proc
{
	public abstract class BasicProcAnimation : BuiltinAnimation
	{
		private RootTargetGroup root_;
		private ISync oldSync_ = null;
		private bool mainSync_ = false;
		private bool applyWhenOff_ = false;

		public BasicProcAnimation(string name, bool applyWhenOff=false)
			: base(name)
		{
			root_ = new RootTargetGroup();
			root_.ParentAnimation = this;
		}

		protected void CopyFrom(BasicProcAnimation o)
		{
			base.CopyFrom(o);
			root_ = (RootTargetGroup)o.root_.Clone();
			root_.ParentAnimation = this;
		}

		public override bool Done
		{
			get { return root_.Done; }
		}

		public RootTargetGroup RootGroup
		{
			get { return root_; }
		}

		public void SetAsMainSync(Person other = null)
		{
			if (other != null)
			{
				if (other.Animator.MainSync == null)
				{
					other.Log.Verbose($"no main sync");
				}
				else
				{
					Log.Verbose($"{this}.{root_.Sync}: syncing with {other.Animator.MainSync.Target}.{other.Animator.MainSync}");
					oldSync_ = root_.Sync;
					root_.Sync = new SyncOther(other.Animator.MainSync);
				}
			}

			if (Person.Animator.MainSync != null)
			{
				Log.Error($"{this}.{root_.Sync}: already has a main sync");
			}
			else
			{
				Log.Verbose($"{this}.{root_.Sync} is main sync");
				Person.Animator.SetMainSync(root_.Sync);
				mainSync_ = true;
			}
		}

		public void MainSyncStopping(ISync s)
		{
			if (Person == null)
			{
				Log.ErrorST("person is null");
				return;
			}

			Log.Verbose($"main sync {s} stopping");

			if (root_.Sync == s)
			{
				// this is the reason for the call, ignore it
				Log.Verbose($"ignoring");
				return;
			}
			else if (oldSync_ == null)
			{
				Log.Verbose($"but oldSync is null");
			}
			else
			{
				var so = root_.Sync as SyncOther;
				if (so == null)
				{
					Log.Verbose("but root sync is not a SyncOther");
				}
				else
				{
					if (so.Other != s)
					{
						Log.Verbose("but root sync isn't synced to this");
					}
					else
					{
						Log.Verbose($"setting old sync {oldSync_}");
						root_.Sync = oldSync_;
						Person.Animator.SetMainSync(root_.Sync);
						oldSync_ = null;
					}
				}
			}
		}

		public void AddTarget(ITarget t)
		{
			root_.AddTarget(t);
		}

		public List<ITarget> Targets
		{
			get { return root_.Targets; }
		}

		public bool ApplyWhenOff
		{
			get { return applyWhenOff_; }
		}

		public ITarget FindTarget(string name)
		{
			return root_.FindTarget(name);
		}

		public override bool Start(Person p, AnimationContext cx)
		{
			base.Start(p, cx);
			root_.Start(p, cx);
			return true;
		}

		public override void RequestStop(int stopFlags = Animation.NoStopFlags)
		{
			if (oldSync_ != null)
			{
				root_.Sync = oldSync_;
				oldSync_ = null;
			}

			root_.RequestStop();
		}

		public override void Stopped()
		{
			base.Stopped();

			if (mainSync_)
			{
				mainSync_ = false;
				Person.Animator.SetMainSync(null);
			}
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

		public override void Debug(DebugLines debug)
		{
			var ds = ToDetailedString().Split('\n');

			if (ds.Length > 0)
			{
				debug.Add(ds[0]);
				for (int i = 1; i < ds.Length; ++i)
					debug.Add(I(1) + ds[i]);
			}

			debug.Add($"    applyWhenOff={applyWhenOff_}");

			DebugTarget(debug, root_, 1);
		}

		private void DebugTarget(DebugLines debug, ITarget t, int indent)
		{
			var lines = t.ToDetailedString().Split('\n');
			if (lines.Length > 0)
				debug.Add(I(indent) + lines[0]);

			{
				var syncLines = t.Sync.ToDetailedString().Split('\n');
				if (syncLines.Length > 0)
					debug.Add(I(indent + 1) + "sync: " + syncLines[0]);

				for (int i = 1; i < syncLines.Length; ++i)
					debug.Add(I(indent + 2) + syncLines[i]);
			}

			for (int i = 1; i < lines.Length; ++i)
				debug.Add(I(indent + 1) + lines[i]);

			if (t is ITargetGroup)
			{
				foreach (var c in (t as ITargetGroup).Targets)
					DebugTarget(debug, c, indent + 1);
			}
		}
	}


	class ProcAnimation : BasicProcAnimation
	{
		public ProcAnimation(string name, bool hasMovement, bool applyWhenOff)
			: base(name, applyWhenOff)
		{
			HasMovement = hasMovement;
		}

		public static ProcAnimation Create(JSONClass o)
		{
			var file = Cue.Instance.Sys.GetResourcePath(o["file"]);
			var doc = JSON.Parse(Cue.Instance.Sys.ReadFileIntoString(file));

			if (doc == null)
			{
				Logger.Global.Error($"failed to parse proc animation file '{file}'");
				return null;
			}

			var docRoot = doc.AsObject;
			string name = docRoot["name"];
			bool hasMovement = docRoot["hasMovement"].AsBool;
			bool applyWhenOff = J.OptBool(docRoot, "applyWhenOff", false);

			try
			{
				var a = new ProcAnimation(name, hasMovement, applyWhenOff);

				foreach (JSONClass n in docRoot["targets"].AsArray)
					a.AddTarget(CreateTarget(n["type"], n));

				return a;
			}
			catch (LoadFailed e)
			{
				Logger.Global.Error($"{file}: {e.Message}");
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
					Logger.Global.Info($"bad target type '{type}'");
					return null;
				}
			}
		}

		public override BuiltinAnimation Clone()
		{
			var a = new ProcAnimation(Name, HasMovement, ApplyWhenOff);
			a.CopyFrom(this);
			return a;
		}

	}
}
