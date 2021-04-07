using System;
using System.Collections;
using System.Collections.Generic;
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
				SuperController.singleton.StartCoroutine(DeferredInit());
			}
			catch (Exception e)
			{
				SuperController.LogError(e.Message);
			}
		}

		private IEnumerator DeferredInit()
		{
			yield return new WaitForEndOfFrame();

			if (W.MockSys.Instance != null)
			{
				sys_ = W.MockSys.Instance;
			}
			else
			{
				sys_ = new W.VamSys(this);
				ui_ = new UI.VamUI();
			}

			var o = new BasicObject(sys_.GetAtom("Bed1"));
			o.SitSlot = new Slot(new Vector3(0, 0, -1.3f), 180);
			objects_.Add(o);

			o = new BasicObject(sys_.GetAtom("Chair1"));
			o.SitSlot = new Slot(new Vector3(0, 0, 0.3f), 0);
			objects_.Add(o);

			o = new BasicObject(sys_.GetAtom("Empty"));
			o.SitSlot = new Slot(new Vector3(0, 0, 0.3f), 0);
			objects_.Add(o);

			o = new BasicObject(sys_.GetAtom("Table1"));
			objects_.Add(o);

			person_ = new Person(sys_.GetAtom("Person"));

			ui_.Init();

			sys_.Nav.AddBox(-1.5f, 0, 1.5f, 7);
			sys_.Nav.AddBox(-3, 2.5f, 6, 1);
			sys_.Nav.AddBox(0, 0, 1.5f, 0.8f);
			sys_.Nav.AddBox(3, 2.2f, 10, 2.0f);
			sys_.Nav.AddBox(3.7f, 6f, 4.5f, 6);
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
