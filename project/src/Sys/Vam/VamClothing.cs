using System;
using System.Collections.Generic;

namespace Cue.W
{
	class VamClothing : IClothing
	{
		class Item
		{
			private VamAtom atom_;
			private DAZClothingItem ci_;
			private DAZSkinWrapSwitcher wrap_ = null;
			private bool enabled_ = true;

			public Item(VamAtom a, DAZClothingItem ci)
			{
				atom_ = a;
				ci_ = ci;
			}

			public void Init()
			{
				if (ci_.driveXAngleTarget != 0 || ci_.drive2XAngleTarget != 0)
				{
					Cue.LogInfo($"resetting {ci_.name} for drive angles");
					ci_.enabled = false;
					ci_.enabled = true;
				}
			}

			public DAZClothingItem Daz
			{
				get { return ci_; }
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

						Cue.LogInfo(
							ToString() + ": " + (value ? "enabled" : "disabled"));

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
							Cue.LogError("clothing " + ci_.name + " has no wrap switcher");
							return;
						}
					}

					if (value != wrap_.currentWrapName)
					{
						Cue.LogInfo(
							ToString() + ": state " +
							wrap_.currentWrapName + "->" + value);

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
						atom_.Sex, ci_.name, ci_.tagsArray);

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
						atom_.Sex, ci_.name, ci_.tagsArray);

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
					atom_.Sex, ci_.name, ci_.tagsArray);

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
					atom_.Sex, ci_.name, ci_.tagsArray);

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
				return ci_.name;
			}
		}

		private VamAtom atom_;
		private DAZCharacterSelector char_;
		private List<Item> items_ = new List<Item>();
		private bool genitalsVisible_ = false;
		private bool breastsVisible_ = false;
		private Item heels_ = null;

		public VamClothing(VamAtom a)
		{
			try
			{
				atom_ = a;
				char_ = atom_.Atom.GetComponentInChildren<DAZCharacterSelector>();

				if (char_ == null)
				{
					Cue.LogError($"VamClothing: {atom_.ID} has no DAZCharacterSelector");
					return;
				}

				foreach (var c in char_.clothingItems)
				{
					if (c.isActiveAndEnabled)
					{
						Cue.LogInfo($"VamClothing: found {c.name}");
						items_.Add(new Item(atom_, c));
					}
				}

				GenitalsVisible = false;
				BreastsVisible = false;
			}
			catch (Exception e)
			{
				Cue.LogError("VamClothing: ctor failed, " + e.ToString());
			}
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
					Cue.LogInfo(atom_.ID + ": showing genitals");

					foreach (var i in items_)
						i.SetToShowGenitals();
				}
				else
				{
					Cue.LogInfo(atom_.ID + ": hiding genitals");

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
					Cue.LogInfo(atom_.ID + ": showing breasts");

					foreach (var i in items_)
						i.SetToShowBreasts();
				}
				else
				{
					Cue.LogInfo(atom_.ID + ": hiding breasts");

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
					Cue.LogInfo("clothing changed, not resetting");
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
					Cue.LogInfo(c.name);
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
			return $"Vam: genitals={genitalsVisible_} breasts={breastsVisible_}";
		}
	}
}
