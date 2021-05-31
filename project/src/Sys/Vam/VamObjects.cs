using SimpleJSON;
using System;
using System.Collections;
using UnityEngine;

namespace Cue.W
{
	class VamCuaObjectCreator : IObjectCreator
	{
		private string name_;
		private string assetUrl_;
		private string assetName_;
		private Vector3 posOffset_;
		private float scale_;

		public VamCuaObjectCreator(string name, JSONClass opts)
		{
			name_ = name;
			assetUrl_ = opts["url"].Value;
			assetName_ = opts["name"].Value;
			posOffset_ = Vector3.FromJSON(opts, "positionOffset");
			scale_ = opts["scale"].AsFloat;
		}

		public string Name
		{
			get { return name_; }
		}

		public void Create(string id, Action<IObject> callback)
		{
			var sc = SuperController.singleton;
			sc.StartCoroutine(CreateObjectRoutine(id, callback));
		}

		private IEnumerator CreateObjectRoutine(string id, Action<IObject> f)
		{
			var sc = SuperController.singleton;

			Cue.LogInfo($"creating cua {id}");
			yield return sc.AddAtomByType("CustomUnityAsset", id);

			var atom = sc.GetAtomByUid(id);
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

			cua.RegisterAssetLoadedCallback(() =>
			{
				Cue.LogInfo($"object {id} done, name is {name.val}");

				var a = new VamAtom(atom);

				var o = atom.transform.Find("reParentObject/object/rescaleObject");
				o.localPosition = VamU.ToUnity(posOffset_);
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
