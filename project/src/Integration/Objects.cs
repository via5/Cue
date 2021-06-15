namespace Cue
{
	class VamSmoke : ISmoke
	{
		private IObject smoke_ = null;
		private W.VamFloatParameter smokeOpacityParam_ = null;
		private Vector3 pos_ = Vector3.Zero;
		private Quaternion rot_ = Quaternion.Zero;
		private float smokeOpacity_ = 0;

		public VamSmoke(string id)
		{
			var a = Cue.Instance.Sys.GetAtom(id);

			if (a != null)
			{
				Cue.LogInfo("smoke already exists, destroying");
				a.Destroy();
			}

			Cue.LogInfo("creating smoke");

			var oc = Resources.Objects.Get("cigaretteSmoke");
			if (oc == null)
				Cue.LogWarning("no cigarette smoke object creator");
			else
				oc.Create(id, (o) => { SetSmoke(o); });
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
				Cue.LogWarning("failed to create cigarette smoke");
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
				smokeOpacityParam_ = new W.VamFloatParameter(
					smoke_,
					"SmokeCubeEditorPlugin.SmokeCubeEditor",
					"Smoke opacity");
			}

			smokeOpacityParam_.Value = smokeOpacity_;
			smoke_.Visible = (smokeOpacity_ > 0);
		}
	}
}
