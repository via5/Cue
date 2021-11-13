namespace Cue
{
	class VamSmoke : ISmoke
	{
		private IObject smoke_ = null;
		private Sys.Vam.FloatParameter smokeOpacityParam_ = null;
		private Vector3 pos_ = Vector3.Zero;
		private Quaternion rot_ = Quaternion.Zero;
		private float smokeOpacity_ = 0;
		private bool destroy_ = false;
		private Logger log_;

		private VamSmoke()
		{
			log_ = new Logger(Logger.Integration, "smoke");
		}

		private VamSmoke(IObject o)
			: this()
		{
			SetSmoke(o);
		}

		private VamSmoke(string id)
			: this()
		{
			Log.Info("creating smoke");

			var oc = Resources.Objects.Get("cigaretteSmoke");
			if (oc == null)
				Log.Warning("no cigarette smoke object creator");
			else
				oc.Create(null, id, (o) => { SetSmoke(o); });
		}

		public Logger Log
		{
			get { return log_; }
		}

		public static VamSmoke Create(string id, bool existsOnly)
		{
			var a = Cue.Instance.Sys.GetAtom(id);

			if (a != null)
				return new VamSmoke(new BasicObject(-1, a));
			else if (existsOnly)
				return null;

			return new VamSmoke(id);
		}

		public void Destroy()
		{
			smoke_?.Destroy();
			destroy_ = true;
		}

		public Vector3 Position
		{
			get
			{
				return pos_;
			}

			set
			{
				pos_ = value;

				if (smoke_ != null)
					smoke_.Position = value;
			}
		}

		public Quaternion Rotation
		{
			get
			{
				return rot_;
			}

			set
			{
				rot_ = value;

				if (smoke_ != null)
					smoke_.Rotation = value;
			}
		}

		public float Opacity
		{
			get
			{
				return smokeOpacity_;
			}

			set
			{
				smokeOpacity_ = value;
				UpdateOpacity();
			}
		}

		private void SetSmoke(IObject o)
		{
			if (o == null)
			{
				Log.Warning("failed to create cigarette smoke");
				return;
			}

			if (destroy_)
			{
				o.Destroy();
				return;
			}

			smoke_ = o;
			smoke_.Visible = false;
			smoke_.Atom.Collisions = false;
			smoke_.Atom.Physics = false;
			smoke_.Atom.Hidden = true;
		}

		private void UpdateOpacity()
		{
			if (smoke_ == null)
				return;

			if (smokeOpacityParam_ == null)
			{
				smokeOpacityParam_ = new Sys.Vam.FloatParameter(
					smoke_,
					"SmokeCubeEditorPlugin.SmokeCubeEditor",
					"Smoke opacity");
			}

			smokeOpacityParam_.Value = smokeOpacity_;
			smoke_.Visible = (smokeOpacity_ > 0);
		}
	}
}
