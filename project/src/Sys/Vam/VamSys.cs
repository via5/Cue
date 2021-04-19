using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cue.W
{
	class VamSys : ISys
	{
		private static VamSys instance_ = null;
		private readonly MVRScript script_ = null;
		private readonly VamTime time_ = new VamTime();
		private readonly VamLog log_ = new VamLog();
		private readonly VamNav nav_ = new VamNav();
		private string pluginPath_ = "";

		public VamSys(MVRScript s)
		{
			instance_ = this;
			script_ = s;
		}

		static public VamSys Instance
		{
			get { return instance_; }
		}

		public ITime Time
		{
			get { return time_; }
		}

		public ILog Log
		{
			get { return log_; }
		}

		public INav Nav
		{
			get { return nav_; }
		}

		public IAtom GetAtom(string id)
		{
			var a = SuperController.singleton.GetAtomByUid(id);
			if (a == null)
				return null;

			return new VamAtom(a);
		}

		public List<IAtom> GetAtoms(bool alsoOff = false)
		{
			var list = new List<IAtom>();

			foreach (var a in SuperController.singleton.GetAtoms())
			{
				if (a.on || alsoOff)
					list.Add(new VamAtom(a));
			}

			return list;
		}

		public IAtom ContainingAtom
		{
			get { return new VamAtom(script_.containingAtom); }
		}

		public bool Paused
		{
			get { return SuperController.singleton.freezeAnimation; }
		}

		public void OnPluginState(bool b)
		{
			nav_.OnPluginState(b);
		}

		public void OnReady(Action f)
		{
			SuperController.singleton.StartCoroutine(DeferredInit(f));
		}

		public void ReloadPlugin()
		{
			Transform uit = null;

#if (VAM_GT_1_20)
			uit = CueMain.Instance.UITransform;
#endif

			foreach (var pui in uit.parent.GetComponentsInChildren<MVRPluginUI>())
			{
				if (pui.urlText.text.Contains("Cue.cslist"))
				{
					Cue.LogError("reloading");
					pui.reloadButton.onClick.Invoke();
				}
			}
		}

		public string ReadFileIntoString(string path)
		{
			try
			{
				return SuperController.singleton.ReadFileIntoString(path);
			}
			catch (Exception e)
			{
				Cue.LogError("failed to read '" + path + "', " + e.Message);
				return "";
			}
		}

		public string GetResourcePath(string path)
		{
#if (VAM_GT_1_20)
			// based on MacGruber, which was based on VAMDeluxe, which was
			// in turn based on Alazi

			if (pluginPath_ == "")
			{
				var self = CueMain.Instance;
				string id = self.name.Substring(0, self.name.IndexOf('_'));
				string filename = self.manager.GetJSON()["plugins"][id].Value;

				pluginPath_ = filename.Substring(
					0, filename.LastIndexOfAny(new char[] { '/', '\\' }));

				pluginPath_ = pluginPath_.Replace('/', '\\');
				if (pluginPath_.EndsWith("\\"))
					pluginPath_ = pluginPath_.Substring(0, pluginPath_.Length - 1);
			}

			path = path.Replace('/', '\\');

			if (path.StartsWith("\\"))
				return pluginPath_ + "\\res" + path;
			else
				return pluginPath_ + "\\res\\" + path;
#else
			return pluginPath_;
#endif
		}

		private IEnumerator DeferredInit(Action f)
		{
			yield return new WaitForEndOfFrame();
			f?.Invoke();
		}

		public JSONStorableFloat GetFloatParameter(
			IObject o, string storable, string param)
		{
			var a = ((W.VamAtom)o.Atom).Atom;

			foreach (var id in a.GetStorableIDs())
			{
				if (id.Contains(storable))
				{
					var st = a.GetStorableByID(id);
					if (st == null)
					{
						Cue.LogError("can't find storable " + id);
						continue;
					}

					var p = st.GetFloatJSONParam(param);
					if (p == null)
					{
						Cue.LogError("no '" + param + "' param");
						continue;
					}

					return p;
				}
			}

			return null;
		}

		public JSONStorableBool GetBoolParameter(
			IObject o, string storable, string param)
		{
			var a = ((W.VamAtom)o.Atom).Atom;

			foreach (var id in a.GetStorableIDs())
			{
				if (id.Contains(storable))
				{
					var st = a.GetStorableByID(id);
					if (st == null)
					{
						Cue.LogError("can't find storable " + id);
						continue;
					}

					var p = st.GetBoolJSONParam(param);
					if (p == null)
					{
						Cue.LogError("no '" + param + "' param");
						continue;
					}

					return p;
				}
			}

			return null;
		}

		public JSONStorableString GetStringParameter(
			IObject o, string storable, string param)
		{
			var a = ((W.VamAtom)o.Atom).Atom;

			foreach (var id in a.GetStorableIDs())
			{
				if (id.Contains(storable))
				{
					var st = a.GetStorableByID(id);
					if (st == null)
					{
						Cue.LogError("can't find storable " + id);
						continue;
					}

					var p = st.GetStringJSONParam(param);
					if (p == null)
					{
						Cue.LogError("no '" + param + "' param");
						continue;
					}

					return p;
				}
			}

			return null;
		}

		public JSONStorableStringChooser GetStringChooserParameter(
			IObject o, string storable, string param)
		{
			var a = ((W.VamAtom)o.Atom).Atom;

			foreach (var id in a.GetStorableIDs())
			{
				if (id.Contains(storable))
				{
					var st = a.GetStorableByID(id);
					if (st == null)
					{
						Cue.LogError("can't find storable " + id);
						continue;
					}

					var p = st.GetStringChooserJSONParam(param);
					if (p == null)
					{
						Cue.LogError("no '" + param + "' param");
						continue;
					}

					return p;
				}
			}

			return null;
		}

		public JSONStorableAction GetActionParameter(
			IObject o, string storable, string param)
		{
			return GetActionParameter(((W.VamAtom)o.Atom).Atom, storable, param);
		}

		public JSONStorableAction GetActionParameter(
			Atom a, string storable, string param)
		{
			foreach (var id in a.GetStorableIDs())
			{
				if (id.Contains(storable))
				{
					var st = a.GetStorableByID(id);
					if (st == null)
					{
						Cue.LogError("can't find storable " + id);
						continue;
					}

					var p = st.GetAction(param);
					if (p == null)
					{
						Cue.LogError("no '" + param + "' action param");
						continue;
					}

					return p;
				}
			}

			return null;
		}

		public Rigidbody FindRigidbody(IObject o, string name)
		{
			return FindRigidbody(((W.VamAtom)o.Atom).Atom, name);
		}

		public Rigidbody FindRigidbody(Atom a, string name)
		{
			foreach (var rb in a.rigidbodies)
			{
				if (rb.name == name)
					return rb.GetComponent<Rigidbody>();
			}

			return null;
		}
	}


	class VamTime : ITime
	{
		public float deltaTime
		{
			get { return Time.deltaTime; }
		}
	}

	class VamLog : ILog
	{
		public void Verbose(string s)
		{
			SuperController.LogError(s);
		}

		public void Info(string s)
		{
			SuperController.LogError(s);
		}

		public void Error(string s)
		{
			SuperController.LogError(s);
		}
	}
}
