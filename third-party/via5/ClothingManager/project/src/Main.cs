using ClothingManager;

namespace via5
{
	using CM = global::ClothingManager.ClothingManager;

	class Version
	{
		public int Major = 1;
		public int Minor = 0;
	}


	class ClothingManager : MVRScript
	{
		private static ClothingManager instance_ = null;
		private CM m_ = null;

		public ClothingManager()
		{
			instance_ = this;
		}

		public static ClothingManager Instance
		{
			get { return instance_; }
		}

		public override void Init()
		{
			Log.Verbose($"init m={m_}");
		}

		public void Start()
		{
			Log.Verbose($"start m={m_}");
		}

		public void Update()
		{
			U.Safe(() =>
			{
				if (m_ == null)
					m_ = new CM(this);

				m_.Update();
			});
		}

		public void OnEnable()
		{
			U.Safe(() =>
			{
				if (m_ != null)
					m_.OnPluginEnable();
			});
		}

		public void OnDisable()
		{
			U.Safe(() =>
			{
				if (m_ != null)
					m_.OnPluginDisable();
			});
		}

		public static void DisablePlugin()
		{
			if (instance_?.enabledJSON != null)
				instance_.enabledJSON.val = false;
		}

		public string PluginPath
		{
			get
			{
				// based on MacGruber, which was based on VAMDeluxe, which was
				// in turn based on Alazi

				string id = name.Substring(0, name.IndexOf('_'));
				string filename = manager.GetJSON()["plugins"][id].Value;

				var path = filename.Substring(
					0, filename.LastIndexOfAny(new char[] { '/', '\\' }));

				path = path.Replace('/', '\\');
				if (path.EndsWith("\\"))
					path = path.Substring(0, path.Length - 1);

				return path;
			}
		}
	}
}
