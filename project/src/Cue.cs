using System.Collections.Generic;

namespace Cue
{
	class Cue : MVRScript
	{
		private static Cue instance_ = null;
		private W.ISys sys_ = null;
		private Person person_ = null;
		private readonly List<IObject> objects_ = new List<IObject>();
		private UI.IUI ui_ = null;

		public Cue()
		{
			instance_ = this;
		}

		public static Cue Instance
		{
			get { return instance_; }
		}

		public W.ISys Sys
		{
			get { return sys_; }
		}

		public List<IObject> Objects
		{
			get { return objects_; }
		}

		public Person Person
		{
			get { return person_; }
		}

		public void Start()
		{
			if (W.MockSys.Instance != null)
			{
				sys_ = W.MockSys.Instance;
			}
			else
			{
				sys_ = new W.VamSys(this);
				ui_ = new UI.VamUI();
			}

			objects_.Add(new Bed(sys_.GetAtom("Bed1")));
			objects_.Add(new Chair(sys_.GetAtom("Chair1")));
			objects_.Add(new Table(sys_.GetAtom("Table1")));

			person_ = new Person(sys_.GetAtom("Person"));

			ui_.Init();
		}

		public void Update()
		{
			U.Safe(() =>
			{
				if (sys_.Paused)
					return;

				person_.Update(sys_.Time.deltaTime);
				ui_.Update();
			});
		}

		public void FixedUpdate()
		{
			U.Safe(() =>
			{
				if (sys_.Paused)
					return;

				person_.FixedUpdate(sys_.Time.deltaTime);
			});
		}

		static public void LogVerbose(string s)
		{
			Instance.sys_.Log.Verbose(s);
		}

		static public void LogInfo(string s)
		{
			Instance.sys_.Log.Info(s);
		}

		static public void LogWarning(string s)
		{
			Instance.sys_.Log.Error(s);
		}

		static public void LogError(string s)
		{
			Instance.sys_.Log.Error(s);
		}
	}
}
