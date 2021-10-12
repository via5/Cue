﻿using SimpleJSON;
using System;
using System.Collections;
using UnityEngine;

namespace Cue.Sys.Vam
{
	class VamAtomObjectCreator : IObjectCreator
	{
		private SuperController sc_;
		private string name_;
		private string type_;
		private float scale_;
		private bool creating_ = false;

		public VamAtomObjectCreator(string name, JSONClass opts)
		{
			sc_ = SuperController.singleton;
			name_ = name;
			type_ = opts["type"].Value;
			scale_ = opts["scale"].AsFloat;
		}

		public string Name
		{
			get { return name_; }
		}

		public void Create(IAtom user, string id, Action<IObject> callback)
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

		public void Destroy(IAtom user, string id)
		{
			// todo
			throw new NotImplementedException();
		}

		private IEnumerator CreateObjectRoutine(string id, Action<IObject> f)
		{
			Cue.LogInfo($"creating atom {id}");
			yield return sc_.AddAtomByType(type_, id);

			var atom = sc_.GetAtomByUid(id);
			if (atom == null)
			{
				Cue.LogError($"failed to create atom '{id}'");
				f(null);
				yield break;
			}

			Cue.LogInfo($"atom {id} created");

			var a = new VamAtom(atom);
			a.Scale = scale_;

			try
			{
				f(new BasicObject(-1, a));
			}
			catch (Exception e)
			{
				Cue.LogError($"exception while creating atom {id}");
				Cue.LogError(e.ToString());
			}
		}
	}


	class VamCuaObjectCreator : IObjectCreator
	{
		private SuperController sc_;
		private string name_;
		private string assetUrl_;
		private string assetName_;
		private Vector3 posOffset_;
		private float scale_;
		private string preset_;
		private bool creating_ = false;

		public VamCuaObjectCreator(string name, JSONClass opts)
		{
			sc_ = SuperController.singleton;
			name_ = name;
			assetUrl_ = opts["url"].Value;
			assetName_ = opts["name"].Value;
			posOffset_ = Vector3.FromJSON(opts, "positionOffset");
			scale_ = opts["scale"].AsFloat;
			preset_ = opts["preset"].Value;
		}

		public string Name
		{
			get { return name_; }
		}

		public void Create(IAtom user, string id, Action<IObject> callback)
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

		public void Destroy(IAtom user, string id)
		{
			// todo
			throw new NotImplementedException();
		}

		private IEnumerator CreateObjectRoutine(string id, Action<IObject> f)
		{
			Cue.LogInfo($"creating cua {id}");
			yield return sc_.AddAtomByType("CustomUnityAsset", id);

			var atom = sc_.GetAtomByUid(id);
			if (atom == null)
			{
				Cue.LogError($"failed to create cua '{id}'");
				f(null);
				yield break;
			}

			Cue.LogInfo($"cua {id} created, getting components");

			var cua = atom.GetComponentInChildren<CustomUnityAssetLoader>();
			if (cua == null)
			{
				Cue.LogError($"object '{id}' has no CustomUnityAssetLoader component");
				f(null);
				yield break;
			}

			var asset = atom.GetStorableByID("asset");
			if (asset == null)
			{
				Cue.LogError($"object '{id}' has no asset storable");
				f(null);
				yield break;
			}

			var url = asset.GetUrlJSONParam("assetUrl");
			if (asset == null)
			{
				Cue.LogError($"object '{id}' asset has no assetUrl param");
				f(null);
				yield break;
			}

			var name = asset.GetStringChooserJSONParam("assetName");
			if (asset == null)
			{
				Cue.LogError($"object '{id}' asset has no assetName param");
				f(null);
				yield break;
			}


			if (!string.IsNullOrEmpty(preset_))
			{
				atom.LoadPreset(Cue.Instance.Sys.GetResourcePath(preset_));
				f(new BasicObject(-1, new VamAtom(atom)));
			}
			else
			{
				cua.RegisterAssetLoadedCallback(() =>
				{
					Cue.LogInfo($"object {id} done, name is {name.val}");

					var a = new VamAtom(atom);

					var o = atom.transform.Find("reParentObject/object/rescaleObject");
					o.localPosition = U.ToUnity(posOffset_);
					a.Scale = scale_;

					f(new BasicObject(-1, a));
				});

				Cue.LogInfo($"object {id} loading url");
				url.val = assetUrl_;

				for (; ; )
				{
					yield return new WaitForSeconds(0.25f);
					if (name.choices.Count > 0)
					{
						Cue.LogInfo($"object {id} url loaded, setting name");
						name.val = assetName_;
						yield break;
					}
				}
			}
		}
	}


	class VamClothingObjectCreator : IObjectCreator
	{
		private SuperController sc_;
		private string name_;
		private string id_;

		public VamClothingObjectCreator(string name, JSONClass opts)
		{
			sc_ = SuperController.singleton;
			name_ = name;
			id_ = opts["id"].Value;
		}

		public string Name
		{
			get { return name_; }
		}

		public void Create(IAtom user, string unusedId, Action<IObject> callback)
		{
			SetActive(user, true);
		}

		public void Destroy(IAtom user, string unusedId)
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
