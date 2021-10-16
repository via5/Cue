using SimpleJSON;
using System;
using System.Collections;
using UnityEngine;

namespace Cue.Sys.Vam
{
	abstract class VamBasicObjectCreator : IObjectCreator
	{
		private string name_;
		protected ObjectParameters ps_;

		protected VamBasicObjectCreator(string name, ObjectParameters ps)
		{
			name_ = name;
			ps_ = ps;
		}

		public string Name
		{
			get { return name_; }
		}

		public abstract void Create(IAtom user, string id, Action<IObject> callback);
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

		public override void Create(IAtom user, string id, Action<IObject> callback)
		{
			if (creating_)
				return;

			creating_ = true;

			sc_.StartCoroutine(CreateObjectRoutine(
				id, (o) =>
				{
					creating_ = false;
					callback(o);
				}));
		}

		public override void Destroy(IAtom user, string id)
		{
			// todo
			throw new NotImplementedException();
		}

		private IEnumerator CreateObjectRoutine(string id, Action<IObject> f)
		{
			var atom = sc_.GetAtomByUid(id);

			if (atom != null)
			{
				Cue.LogInfo($"atom {id} already exists, taking it");
			}
			else
			{
				Cue.LogInfo($"creating atom {id}");

				yield return sc_.AddAtomByType(type_, id);

				atom = sc_.GetAtomByUid(id);
				if (atom == null)
				{
					Cue.LogError($"failed to create atom '{id}'");
					f(null);
					yield break;
				}

				Cue.LogInfo($"atom {id} created");
			}

			if (!string.IsNullOrEmpty(preset_))
			{
				var path = Cue.Instance.Sys.GetResourcePath(preset_);
				Cue.LogInfo($"atom {id} loading preset {path}");
				atom.LoadPreset(path);
			}

			var a = new VamAtom(atom);

			yield return Setup(a, f);


			try
			{
				f(new BasicObject(-1, a, ps_));
			}
			catch (Exception e)
			{
				Cue.LogError($"exception while creating atom {id}");
				Cue.LogError(e.ToString());
			}
		}

		protected virtual IEnumerator Setup(VamAtom a, Action<IObject> f)
		{
			if (scale_ >= 0)
				a.Scale = scale_;

			if (hasPosOffset_)
			{
				var o = a.Atom.transform.Find("reParentObject/object/rescaleObject");
				if (o != null)
					o.localPosition = U.ToUnity(posOffset_);
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

		protected override IEnumerator Setup(VamAtom a, Action<IObject> f)
		{
			yield return base.Setup(a, f);

			var atom = a.Atom;

			Cue.LogInfo($"getting components");

			var cua = atom.GetComponentInChildren<CustomUnityAssetLoader>();
			if (cua == null)
			{
				Cue.LogError($"object '{atom.uid}' has no CustomUnityAssetLoader component");
				f(null);
				yield break;
			}

			var asset = atom.GetStorableByID("asset");
			if (asset == null)
			{
				Cue.LogError($"object '{atom.uid}' has no asset storable");
				f(null);
				yield break;
			}

			var url = asset.GetUrlJSONParam("assetUrl");
			if (asset == null)
			{
				Cue.LogError($"object '{atom.uid}' asset has no assetUrl param");
				f(null);
				yield break;
			}

			var name = asset.GetStringChooserJSONParam("assetName");
			if (asset == null)
			{
				Cue.LogError($"object '{atom.uid}' asset has no assetName param");
				f(null);
				yield break;
			}


			if (!string.IsNullOrEmpty(assetUrl_) && !string.IsNullOrEmpty(assetName_))
			{
				cua.RegisterAssetLoadedCallback(() =>
				{
					Cue.LogInfo($"object {atom.uid} done, name is {name.val}");
				});

				Cue.LogInfo($"object {atom.uid} loading url");
				url.val = assetUrl_;

				for (; ; )
				{
					yield return new WaitForSeconds(0.25f);
					if (name.choices.Count > 0)
					{
						Cue.LogInfo($"object {atom.uid} url loaded, setting name");
						name.val = assetName_;
						yield break;
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

		public override void Create(IAtom user, string unusedId, Action<IObject> callback)
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
				a.Log.Error("no DAZCharacterSelector");
				return;
			}

			var s = cs.GetClothingItem(id_);
			if (s == null)
			{
				a.Log.Error($"no strapon clothing item '{id_}'");
				return;
			}

			cs.SetActiveClothingItem(s, b);
		}
	}
}
