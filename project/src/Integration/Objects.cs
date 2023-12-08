namespace Cue
{
	class VamSmoke : ISmoke
	{
		private IObject smoke_ = null;
		private Sys.Vam.FloatParameter smokeOpacityParam_ = null;
		private Vector3 pos_ = Vector3.Zero;
		private Quaternion rot_ = Quaternion.Identity;
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
				oc.Create(null, id, (o, e) => { SetSmoke(o); });
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


	class VamMaleFluid : IMaleFluid
	{
		private IObject fluid_ = null;
		private Sys.Vam.FloatParameter spray_ = null;
		private bool visible_ = false;
		private Vector3 pos_ = Vector3.Zero;
		private Quaternion rot_ = Quaternion.Identity;
		private float smokeOpacity_ = 0;
		private bool destroy_ = false;
		private Logger log_;

		private VamMaleFluid()
		{
			log_ = new Logger(Logger.Integration, "maleFluid");
			log_.ForceEnabled = true;
		}

		private VamMaleFluid(IObject o)
			: this()
		{
			SetObject(o);
		}

		private VamMaleFluid(string id)
			: this()
		{
			Log.Info("creating male fluid");

			var oc = Resources.Objects.Get("maleFluid");
			if (oc == null)
				Log.Warning("no maleFluid object creator");
			else
				oc.Create(null, id, (o, e) => { SetObject(o); });
		}

		public Logger Log
		{
			get { return log_; }
		}

		public static VamMaleFluid Create(string id, bool existsOnly)
		{
			var a = Cue.Instance.Sys.GetAtom(id);

			if (a != null)
				return new VamMaleFluid(new BasicObject(-1, a));
			else if (existsOnly)
				return null;

			return new VamMaleFluid(id);
		}

		public void Fire(float time)
		{
			Log.Info($"fluid: {fluid_}");
			var a = (fluid_ as BasicObject);
			Log.Info($"a: {a}");

			var aa = (a.Atom as Sys.Vam.VamAtom)?.Atom;
			Log.Info($"aa: {aa}");

			var st = aa.GetStorableByID("plugin#0_FluidEditorMalePlugin.FluidEditorMale");
			if (st == null)
				Log.Info(">>>> null st");
			else
				Log.Info(">>>>> " + st.GetParam("Spray X times, random duration")?.ToString());

			if (spray_ == null)
			{
				spray_ = new Sys.Vam.FloatParameter(
					fluid_,
					"FluidEditorMalePlugin.FluidEditorMale",
					"Spray X times, random duration");
			}

			Log.Info("firing");
			Visible = true;
			spray_.Value = time;

			//
			//if (stopStream_ == null)
			//{
			//	stopStream_ = new Sys.Vam.ActionParameter(
			//		fluid_,
			//		"FluidEditorMalePlugin.FluidEditorMale",
			//		"Stop stream");
			//}
			//
			//
			//startStream_?.Fire();
		}

		public void Destroy()
		{
			fluid_?.Destroy();
			destroy_ = true;
		}

		public bool Visible
		{
			get
			{
				return visible_;
			}

			set
			{
				if (visible_ != value)
				{
					visible_ = value;

					if (fluid_ != null)
						fluid_.Visible = value;
				}
			}
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

				if (fluid_ != null)
					fluid_.Position = value;
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

				if (fluid_ != null)
					fluid_.Rotation = value;
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

		private void SetObject(IObject o)
		{
			if (o == null)
			{
				Log.Warning("failed to create maleFluid");
				return;
			}

			if (destroy_)
			{
				o.Destroy();
				return;
			}

			fluid_ = o;
			fluid_.Visible = visible_;
			fluid_.Atom.Hidden = true;
		}

		private void UpdateOpacity()
		{
			/*if (fluid_ == null)
				return;

			if (smokeOpacityParam_ == null)
			{
				smokeOpacityParam_ = new Sys.Vam.FloatParameter(
					smoke_,
					"SmokeCubeEditorPlugin.SmokeCubeEditor",
					"Smoke opacity");
			}

			smokeOpacityParam_.Value = smokeOpacity_;
			smoke_.Visible = (smokeOpacity_ > 0);*/
		}
	}
}
