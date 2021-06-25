using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cue.W
{
	class VamClothing : IClothing
	{
		class Item
		{
			private VamAtom atom_;
			private Logger log_;
			private DAZClothingItem ci_;
			private DAZSkinWrapSwitcher wrap_ = null;
			private bool enabled_ = true;

			public Item(VamAtom a, DAZClothingItem ci)
			{
				atom_ = a;
				ci_ = ci;
				log_ = new Logger(Logger.Clothing, a, $"VamClothingItem {Name}");
			}

			public void Init()
			{
				if (ci_.driveXAngleTarget != 0 || ci_.drive2XAngleTarget != 0)
				{
					log_.Info($"resetting for drive angles");
					ci_.enabled = false;
					ci_.enabled = true;
				}

				DestroyOwnedColliders();

				// don't use tags
				var item = Resources.Clothing.FindItem(atom_.Sex, Name, null);

				if (item != null)
				{
					if (item.left.enabled)
					{
						DestroyLeft();
						CreateLeft(item.left);
					}

					if (item.right.enabled)
					{
						DestroyRight();
						CreateRight(item.right);
					}
				}
			}

			private void DestroyOwnedColliders()
			{
				// cleanup
				if (ci_.colliderLeft != null && ci_.colliderLeft.name.Contains("!cue"))
					DestroyLeft();

				if (ci_.colliderRight != null && ci_.colliderRight.name.Contains("!cue"))
					DestroyRight();
			}

			private void DestroyLeft()
			{
				if (ci_.colliderLeft != null)
				{
					UnityEngine.Object.Destroy(ci_.colliderLeft);
					ci_.colliderLeft = null;
					ci_.colliderLeftRotation = UnityEngine.Vector3.zero;
					ci_.colliderLeftCenter = UnityEngine.Vector3.zero;
					ci_.colliderDimensions = UnityEngine.Vector3.zero;
					ci_.colliderTypeLeft = DAZClothingItem.ColliderType.None;
				}
			}

			private void DestroyRight()
			{
				if (ci_.colliderRight != null)
				{
					UnityEngine.Object.Destroy(ci_.colliderRight);
					ci_.colliderRight = null;
					ci_.colliderRightRotation = UnityEngine.Vector3.zero;
					ci_.colliderRightCenter = UnityEngine.Vector3.zero;
					ci_.colliderDimensions = UnityEngine.Vector3.zero;
					ci_.colliderTypeRight = DAZClothingItem.ColliderType.None;
				}
			}

			private void CreateLeft(ClothingResources.Item.Collider c)
			{
				log_.Info("creating left collider");

				ci_.colliderTypeLeft = DAZClothingItem.ColliderType.Shoe;
				ci_.colliderLeftRotation = new UnityEngine.Vector3(-45, 0, 0);
				ci_.colliderDimensions = new UnityEngine.Vector3(0.035f, 0.013f, 0.20f);
				ci_.colliderLeftCenter = new UnityEngine.Vector3(0, -0.137f, 0.035f);

				var go = new GameObject();

				ci_.colliderLeft = go.AddComponent<UnityEngine.BoxCollider>();
				ci_.colliderLeft.name = "lShoeCollider!cue";
				ci_.colliderLeft.transform.SetParent(
					Cue.Instance.VamSys.FindRigidbody(atom_.Atom, "lFoot").transform, false);
				ci_.colliderLeft.transform.localEulerAngles = ci_.colliderLeftRotation;
				ci_.colliderLeft.size = ci_.colliderDimensions;
				ci_.colliderLeft.center = ci_.colliderLeftCenter;
			}

			private void CreateRight(ClothingResources.Item.Collider c)
			{
				log_.Info("creating right collider");

				ci_.colliderTypeRight = DAZClothingItem.ColliderType.Shoe;
				ci_.colliderRightRotation = new UnityEngine.Vector3(-45, 0, 0);
				ci_.colliderDimensions = new UnityEngine.Vector3(0.035f, 0.013f, 0.20f);
				ci_.colliderRightCenter = new UnityEngine.Vector3(0, -0.137f, 0.035f);

				var go = new GameObject();
				ci_.colliderRight = go.AddComponent<UnityEngine.BoxCollider>();
				ci_.colliderRight.name = "rShoeCollider!cue";
				ci_.colliderRight.transform.SetParent(
					Cue.Instance.VamSys.FindRigidbody(atom_.Atom, "rFoot").transform, false);
				ci_.colliderRight.transform.localEulerAngles = ci_.colliderRightRotation;
				ci_.colliderRight.size = ci_.colliderDimensions;
				ci_.colliderRight.center = ci_.colliderRightCenter;
			}

			public DAZClothingItem Daz
			{
				get { return ci_; }
			}

			public string Name
			{
				get { return ItemName(ci_); }
			}

			public bool Enabled
			{
				get
				{
					return enabled_;
				}

				set
				{
					if (value != ci_.isActiveAndEnabled)
					{
						enabled_ = value;
						log_.Info(value ? "enabled" : "disabled");
						ci_.characterSelector.SetActiveClothingItem(ci_, value);
					}
				}
			}

			public string State
			{
				set
				{
					if (wrap_ == null)
					{
						wrap_ = ci_.GetComponentInChildren<DAZSkinWrapSwitcher>();
						if (wrap_ == null)
						{
							log_.Error(
								$"clothing {Name} has no wrap switcher " +
								$"{ci_.isDynamicRuntimeLoaded}");

							return;
						}
					}

					if (value != wrap_.currentWrapName)
					{
						log_.Info($"state {wrap_.currentWrapName}->{value}");
						wrap_.currentWrapName = value;
					}
				}
			}

			public void SetToShowGenitals()
			{
				if (ci_.disableAnatomy)
				{
					Enabled = false;
				}
				else
				{
					var item = Resources.Clothing.FindItem(
						atom_.Sex, Name, ci_.tagsArray);

					if (item == null)
						return;

					if (item.hidesGenitalsBool)
					{
						Enabled = false;
					}
					else if (item.showsGenitalsBool)
					{
						Enabled = true;
					}
					else if (item.showsGenitalsState != "")
					{
						Enabled = true;
						State = item.showsGenitalsState;
					}
				}
			}

			public void SetToHideGenitals()
			{
				if (ci_.disableAnatomy)
				{
					Enabled = true;
				}
				else
				{
					var item = Resources.Clothing.FindItem(
						atom_.Sex, Name, ci_.tagsArray);

					if (item == null)
						return;

					if (item.showsGenitalsBool)
					{
						Enabled = false;
					}
					else if (item.hidesGenitalsBool)
					{
						Enabled = true;
					}
					else if (item.hidesGenitalsState != "")
					{
						Enabled = true;
						State = item.hidesGenitalsState;
					}
				}
			}

			public void SetToShowBreasts()
			{
				var item = Resources.Clothing.FindItem(
					atom_.Sex, Name, ci_.tagsArray);

				if (item == null)
					return;

				if (item.hidesBreastsBool)
				{
					Enabled = false;
				}
				else if (item.showsBreastsBool)
				{
					Enabled = true;
				}
				else if (item.showsBreastsState != "")
				{
					Enabled = true;
					State = item.showsBreastsState;
				}
			}

			public void SetToHideBreasts()
			{
				var item = Resources.Clothing.FindItem(
					atom_.Sex, Name, ci_.tagsArray);

				if (item == null)
					return;

				if (item.showsBreastsBool)
				{
					Enabled = false;
				}
				else if (item.hidesBreastsBool)
				{
					Enabled = true;
				}
				else if (item.hidesBreastsState != "")
				{
					Enabled = true;
					State = item.hidesBreastsState;
				}
			}

			public override string ToString()
			{
				return Name;
			}
		}

		private VamAtom atom_;
		private Logger log_;
		private DAZCharacterSelector char_;
		private List<Item> items_ = new List<Item>();
		private bool genitalsVisible_ = false;
		private bool breastsVisible_ = false;
		private Item heels_ = null;

		public VamClothing(VamAtom a)
		{
			atom_ = a;
			log_ = new Logger(Logger.Clothing, atom_, "VamClothing");

			if (atom_ == null)
				return;

			try
			{
				char_ = atom_.Atom.GetComponentInChildren<DAZCharacterSelector>();

				if (char_ == null)
				{
					log_.Error($"no DAZCharacterSelector");
					return;
				}

				foreach (var c in char_.clothingItems)
				{
					if (c.isActiveAndEnabled)
					{
						log_.Info($"found {ItemName(c)}");
						items_.Add(new Item(atom_, c));
					}
				}

				GenitalsVisible = false;
				BreastsVisible = false;
			}
			catch (Exception e)
			{
				log_.Error("VamClothing: ctor failed, " + e.ToString());
			}
		}

		public static string ItemName(DAZClothingItem i)
		{
			if (i.internalUid != "")
				return i.internalUid;
			else
				return i.name;
		}

		public void Init()
		{
			foreach (var i in items_)
				i.Init();
		}

		public float HeelsAngle
		{
			get
			{
				if (heels_ == null)
					heels_ = FindHeels();

				if (heels_ == null)
					return 0;
				else
					return heels_.Daz.driveXAngleTarget;
			}
		}

		public float HeelsHeight
		{
			get
			{
				if (heels_ == null)
					heels_ = FindHeels();

				if (heels_?.Daz?.colliderLeft == null)
					return 0;

				return heels_.Daz.colliderLeft.bounds.size.y / 2;
			}
		}

		public bool GenitalsVisible
		{
			get
			{
				return genitalsVisible_;
			}

			set
			{
				genitalsVisible_ = value;

				if (value)
				{
					log_.Info("showing genitals");

					foreach (var i in items_)
						i.SetToShowGenitals();
				}
				else
				{
					log_.Info("hiding genitals");

					foreach (var i in items_)
						i.SetToHideGenitals();
				}
			}
		}

		public bool BreastsVisible
		{
			get
			{
				return breastsVisible_;
			}

			set
			{
				breastsVisible_ = value;

				if (value)
				{
					log_.Info("showing breasts");

					foreach (var i in items_)
						i.SetToShowBreasts();
				}
				else
				{
					log_.Info("hiding breasts");

					foreach (var i in items_)
						i.SetToHideBreasts();
				}
			}
		}

		public void OnPluginState(bool b)
		{
			if (!b)
			{
				if (ClothingChanged())
				{
					log_.Info("clothing changed, not resetting");
					return;
				}

				foreach (var i in items_)
					i.Enabled = true;
			}
		}

		public void Dump()
		{
			foreach (var c in char_.clothingItems)
			{
				if (c.isActiveAndEnabled)
					Cue.LogInfo($"{ItemName(c)}");
			}
		}

		private Item FindHeels()
		{
			for (int i = 0; i < items_.Count; ++i)
			{
				var c = items_[i].Daz;
				if (c.tagsArray != null)
				{
					for (int t = 0; t < c.tagsArray.Length; ++t)
					{
						if (c.tagsArray[t] == "heels")
							return items_[i];
					}
				}
			}

			return null;
		}

		private bool ClothingChanged()
		{
			foreach (var i in items_)
			{
				if (i.Daz.isActiveAndEnabled != i.Enabled)
				{
					// enabled state was changed manually
					return true;
				}
			}

			foreach (var c in char_.clothingItems)
			{
				if (c.isActiveAndEnabled)
				{
					bool found = false;

					foreach (var i in items_)
					{
						if (c == i.Daz)
						{
							found = true;
							break;
						}
					}

					if (!found)
					{
						// item was not enabled on startup
						return true;
					}
				}
			}

			return false;
		}

		public override string ToString()
		{
			return
				$"VamClothing: " +
				$"genitals={genitalsVisible_} breasts={breastsVisible_}";
		}
	}
}
