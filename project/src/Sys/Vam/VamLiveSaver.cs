using System;
using SimpleJSON;
using System.Collections;
using UnityEngine;

namespace Cue.Sys.Vam
{
	public class LiveSaver : ILiveSaver
	{
		private const string AtomID = "cue!config";

		private Atom atom_ = null;
		private JSONStorableString p_ = null;
		private JSONClass save_ = null;

		public LiveSaver()
		{
			FindAtom();
			CheckAtom();
		}

		public void Save(JSONClass o)
		{
			if (save_ != null)
			{
				// already in process
				save_ = o;
				return;
			}

			if (p_ == null)
			{
				save_ = o;
				SuperController.singleton.StartCoroutine(CreateAtom());
			}
			else
			{
				DoSave(o);
			}
		}

		public JSONClass Load()
		{
			FindAtom();

			if (p_ != null)
			{
				var s = p_.val;
				if (s != "")
				{
					try
					{
						var o = JSON.Parse(s);
						if (o?.AsObject != null)
							return o.AsObject;
					}
					catch (Exception e)
					{
						Cue.LogError($"livesaver: failed to parse json, {e}");
					}
				}
			}

			return new JSONClass();
		}

		private void FindAtom()
		{
			if (atom_ == null)
				atom_ = GetAtom();

			if (atom_ != null)
			{
				if (p_ == null)
					p_ = GetParameter();
			}

			CheckAtom();
		}

		private Atom GetAtom()
		{
			return SuperController.singleton.GetAtomByUid(AtomID);
		}

		private JSONStorableString GetParameter()
		{
			if (atom_ == null)
				return null;

			var st = atom_.GetStorableByID("Text");
			if (st == null)
			{
				Cue.LogError($"lifesaver: atom {atom_.uid} has no Text storable");
				return null;
			}

			return st.GetStringJSONParam("text");
		}

		private IEnumerator CreateAtom()
		{
			if (atom_ == null)
			{
				atom_ = GetAtom();
				if (atom_ == null)
				{
					Cue.LogInfo($"livesaver: creating atom {AtomID}");
					yield return DoCreateAtom();
				}

				Cue.LogInfo($"livesaver: looking for atom");

				for (int tries = 0; tries < 10; ++tries)
				{
					atom_ = GetAtom();
					if (atom_ != null)
					{
						Cue.LogInfo($"livesaver: atom created");
						break;
					}

					Cue.LogInfo($"livesaver: waiting");
					yield return new WaitForSeconds(0.5f);
				}

				if (atom_ == null)
					Cue.LogError("lifesaver: failed to create live saver atom");
			}

			if (atom_ != null && p_ == null)
			{
				p_ = GetParameter();
				if (p_ == null)
				{
					Cue.LogError($"lifesaver: storable has no text parameter");
					yield break;
				}
			}

			if (p_ != null)
			{
				CheckAtom();
				DoSave(save_);
			}

			save_ = null;
		}

		private void DoSave(JSONClass o)
		{
			p_.val = o.ToString();
		}

		private IEnumerator DoCreateAtom()
		{
			return SuperController.singleton.AddAtomByType("UIText", AtomID);
		}

		private void CheckAtom()
		{
			if (atom_ != null)
			{
				atom_.SetOn(false);
				atom_.mainController.MoveControl(
					new UnityEngine.Vector3(10000, 10000, 10000));
			}
		}
	}
}
