using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

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

		public override void Init()
		{
			base.Init();

			try
			{
				if (W.MockSys.Instance != null)
				{
					sys_ = W.MockSys.Instance;
					ui_ = new UI.MockUI();
				}
				else
				{
					sys_ = new W.VamSys(this);
					ui_ = new UI.VamUI();
				}

				sys_.OnReady(DoInit);
			}
			catch (Exception e)
			{
				LogError(e.Message);
			}
		}

		private void DoInit()
		{
			U.Safe(() =>
			{
				var re = new Regex(@"cue!([a-zA-Z]+)#?.*");

				foreach (var a in sys_.GetAtoms())
				{
					var m = re.Match(a.ID);

					if (m != null && m.Success)
					{
						string type = m.Groups[1].Value;
						LogError("found " + a.ID + " " + type);

						BasicObject o = new BasicObject(a);
						var s = new Slot();

						if (type == "sit")
							o.SitSlot = s;
						if (type == "stand")
							o.StandSlot = s;

						objects_.Add(o);
					}
				}

				person_ = new Person(sys_.GetAtom("Person"));

				ui_.Init();
				sys_.Nav.Update();
			});
		}

		public void Update()
		{
			U.Safe(() =>
			{
				if (!sys_.Paused)
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

		public void OnEnable()
		{
			U.Safe(() =>
			{
				sys_.OnPluginState(true);
			});
		}

		public void OnDisable()
		{
			U.Safe(() =>
			{
				sys_.OnPluginState(false);
			});
		}

		static public void LogVerbose(string s)
		{
			//Instance.sys_.Log.Verbose(s);
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
