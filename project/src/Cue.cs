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

			U.Safe(() =>
			{
				DoInit();
			});
		}

		private void DoInit()
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


			var fp = @"[+-]?(?:[0-9]*[.])?[0-9]+";

			var re = new Regex(@"cue!([a-zA-Z]+)!(" + fp + ")!(" + fp + ")!(" + fp + ")!(" + fp + ")");

			foreach (var a in SuperController.singleton.GetAtoms())
			{
				if (!a.on)
					continue;

				var m = re.Match(a.uid);

				if (m != null && m.Success)
				{
					LogError("found " + a.uid);

					string type = m.Groups[1].Value;
					float poX, poY, poZ, bo;

					if (!float.TryParse(m.Groups[2].Value, out poX))
						LogError("bad poX '" + m.Groups[2].Value + "'");

					if (!float.TryParse(m.Groups[3].Value, out poY))
						LogError("bad poY '" + m.Groups[3].Value + "'");

					if (!float.TryParse(m.Groups[4].Value, out poZ))
						LogError("bad poZ '" + m.Groups[4].Value + "'");

					if (!float.TryParse(m.Groups[5].Value, out bo))
						LogError("bad bo '" + m.Groups[5].Value + "'");

					LogError($"type='{type}' pox={poX} poy={poY} poz={poZ} bo={bo}");

					BasicObject o = new BasicObject(new W.VamAtom(a));
					var s = new Slot(new Vector3(poX, poY, poZ), bo);

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
