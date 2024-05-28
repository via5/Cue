using MeshVR;
using SimpleJSON;
using System;
using System.Collections;
using UnityEngine;

namespace Cue.Sys.Vam
{
	abstract class VamBasicObjectCreator : IObjectCreator
	{
		private string name_;
		private Logger log_;
		protected ObjectParameters ps_;

		protected VamBasicObjectCreator(string name, ObjectParameters ps)
		{
			name_ = name;
			log_ = new Logger(Logger.Sys, $"objectCreator.{name}");
			ps_ = ps;
		}

		public string Name
		{
			get { return name_; }
		}

		public Logger Log
		{
			get { return log_; }
		}

		public override string ToString()
		{
			return $"VamObjectCreator[{name_}]";
		}

		public abstract void Create(IAtom user, string id, Action<IObject, bool> callback);
		public abstract void Destroy(IAtom user, string id);
	}


	class VamAtomObjectCreator : VamBasicObjectCreator
	{
		private SuperController sc_;
		private string type_;
		private float scale_;
		private string preset_;
		private Vector3 posOffset_;
		private bool hasPosOffset_;
		private bool creating_ = false;

		public VamAtomObjectCreator(string name, JSONClass opts, ObjectParameters ps)
			: this(name, opts["type"].Value, opts, ps)
		{
		}

		protected VamAtomObjectCreator(string name, string type, JSONClass opts, ObjectParameters ps)
			: base(name, ps)
		{
			sc_ = SuperController.singleton;
			type_ = type;

			if (opts.HasKey("positionOffset"))
			{
				posOffset_ = Vector3.FromJSON(opts, "positionOffset");
				hasPosOffset_ = true;
			}
			else
			{
				hasPosOffset_ = false;
			}

			if (opts.HasKey("scale"))
				scale_ = opts["scale"].AsFloat;
			else
				scale_ = -1;

			preset_ = opts["preset"].Value;
		}

		public override void Create(IAtom user, string id, Action<IObject, bool> callback)
		{
			Log.Verbose($"creating for {user}, id={id}");

			sc_.StartCoroutine(CreateObjectRoutine(
				id, (o, e) =>
				{
					creating_ = false;
					callback(o, e);
				}));
		}

		public override void Destroy(IAtom user, string id)
		{
			// todo
			throw new NotImplementedException();
		}

		private IEnumerator CreateObjectRoutine(string id, Action<IObject, bool> f)
		{
			while (creating_)
				yield return new WaitForSeconds(0.25f);

			creating_ = true;

			var atom = sc_.GetAtomByUid(id);
			bool existing = false;

			if (atom != null)
			{
				Log.Info($"atom {id} already exists, taking it");
				existing = true;
			}
			else
			{
				Log.Info($"creating atom {id}");

				yield return sc_.AddAtomByType(type_, id);

				atom = sc_.GetAtomByUid(id);
				if (atom == null)
				{
					Log.Error($"failed to create atom '{id}'");
					f(null, false);
					yield break;
				}

				Log.Info($"atom {id} created");

				if (!string.IsNullOrEmpty(preset_))
				{
					var path = Cue.Instance.Sys.GetResourcePath(preset_);
					Log.Info($"atom {id} loading preset {path}");

					var json = SuperController.singleton.LoadJSON(path);

					var pm = atom.GetComponentInChildren<PresetManager>();
					pm.LoadPresetFromJSON(json as JSONClass);
					atom.Restore(json as JSONClass);
				}
			}

			var a = new VamAtom(atom);

			yield return Setup(a, f);

			VamFixes.FixKnownAtom(atom);

			try
			{
				f(new BasicObject(-1, a, ps_), existing);
				Log.Info($"atom {id} loaded");


				if (atom.on)
				{
					var o = atom.transform.Find("reParentObject");
					o.gameObject.SetActive(true);

					o = o.Find("object");
					o.gameObject.SetActive(true);
				}
			}
			catch (Exception e)
			{
				Log.Error($"exception while creating atom {id}");
				Log.Error(e.ToString());
			}
		}

		protected virtual IEnumerator Setup(VamAtom a, Action<IObject, bool> f)
		{
			if (scale_ >= 0)
				a.Scale = scale_;

			if (hasPosOffset_)
			{
				var o = a.Atom.transform.Find("reParentObject/object/rescaleObject");

				if (o != null)
				{
					foreach (Transform t in o)
						t.localPosition = U.ToUnity(posOffset_);
				}
			}

			yield return null;
		}
	}


	class VamCuaObjectCreator : VamAtomObjectCreator
	{
		private string assetUrl_;
		private string assetName_;

		public VamCuaObjectCreator(string name, JSONClass opts, ObjectParameters ps)
			: base(name, "CustomUnityAsset", opts, ps)
		{
			assetUrl_ = opts["url"].Value;
			assetName_ = opts["name"].Value;
		}

		protected override IEnumerator Setup(VamAtom a, Action<IObject, bool> f)
		{
			yield return base.Setup(a, f);

			var atom = a.Atom;

			Log.Verbose($"getting components");

			var cua = atom.GetComponentInChildren<CustomUnityAssetLoader>();
			if (cua == null)
			{
				Log.Error($"object '{atom.uid}' has no CustomUnityAssetLoader component");
				f(null, false);
				yield break;
			}

			var asset = atom.GetStorableByID("asset");
			if (asset == null)
			{
				Log.Error($"object '{atom.uid}' has no asset storable");
				f(null, false);
				yield break;
			}

			var url = asset.GetUrlJSONParam("assetUrl");
			if (asset == null)
			{
				Log.Error($"object '{atom.uid}' asset has no assetUrl param");
				f(null, false);
				yield break;
			}

			var name = asset.GetStringChooserJSONParam("assetName");
			if (asset == null)
			{
				Log.Error($"object '{atom.uid}' asset has no assetName param");
				f(null, false);
				yield break;
			}


			if (!string.IsNullOrEmpty(assetUrl_) && !string.IsNullOrEmpty(assetName_))
			{
				Log.Info($"object {atom.uid} loading url");

				bool loadingUrl = true;

				url.val = assetUrl_;

				for (; ; )
				{
					yield return new WaitForSeconds(0.25f);

					if (loadingUrl)
					{
						if (name.choices.Count > 0)
						{
							Log.Info($"object {atom.uid} url loaded, setting name");
							name.val = assetName_;
							loadingUrl = false;
						}
					}
					else
					{
						if (cua.isAssetLoaded)
						{
							Log.Info($"object {atom.uid} asset loaded, done");
							yield break;
						}
					}
				}
			}
		}
	}


	class VamClothingObjectCreator : VamBasicObjectCreator
	{
		private SuperController sc_;
		private string id_;

		public VamClothingObjectCreator(string name, JSONClass opts, ObjectParameters ps)
			: base(name, ps)
		{
			sc_ = SuperController.singleton;
			id_ = opts["id"].Value;
		}

		public override void Create(IAtom user, string unusedId, Action<IObject, bool> callback)
		{
			SetActive(user, true);
		}

		public override void Destroy(IAtom user, string unusedId)
		{
			SetActive(user, false);
		}

		private void SetActive(IAtom user, bool b)
		{
			var a = user as VamAtom;

			var cs = a.Atom.GetComponentInChildren<DAZCharacterSelector>();
			if (cs == null)
			{
				Log.Error($"{a.ID}: no DAZCharacterSelector");
				return;
			}

			var s = cs.GetClothingItem(id_);
			if (s == null)
			{
				Log.Error($"{a.ID}: no strapon clothing item '{id_}'");
				return;
			}

			cs.SetActiveClothingItem(s, b);
		}
	}
}
