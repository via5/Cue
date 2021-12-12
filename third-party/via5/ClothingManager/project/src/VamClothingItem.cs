using UnityEngine;
using System.Text.RegularExpressions;

namespace ClothingManager
{
	class VamClothingItem
	{
		private const string Tag = "!via5.clothingmanager";

		private readonly VamCharacter c_;
		private readonly DAZClothingItem ci_;
		private Item item_ = null;
		private DAZSkinWrapSwitcher wrap_ = null;
		private bool enabled_ = true;

		public VamClothingItem(VamCharacter c, DAZClothingItem ci)
		{
			c_ = c;
			ci_ = ci;
		}

		public VamCharacter Character
		{
			get { return c_; }
		}

		public static string GetUid(DAZClothingItem item)
		{
			if (item.internalUid != "")
				return item.internalUid;
			else
				return item.name;
		}

		public string Uid
		{
			get { return GetUid(ci_); }
		}

		public string DisplayName
		{
			get { return ci_.displayName; }
		}

		public bool Active
		{
			get { return ci_.active; }
		}

		public Item Item
		{
			get { return item_; }
		}

		public DAZClothingItem Daz
		{
			get { return ci_; }
		}

		public int IsShoes()
		{
			if (ci_?.tagsArray != null)
			{
				for (int t = 0; t < ci_.tagsArray.Length; ++t)
				{
					if (ci_.tagsArray[t] == "heels" || ci_.tagsArray[t] == "shoes")
						return 100;
				}
			}

			var re = new Regex(@"\b(boots|shoes)\b", RegexOptions.IgnoreCase);
			if (re.IsMatch(Uid))
				return 50;

			return 0;
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
					Log.Verbose(value ? "enabled" : "disabled");
					ci_.characterSelector.SetActiveClothingItem(ci_, value);
				}
			}
		}

		public int GetGenitalsState()
		{
			if (ci_.disableAnatomy)
				return Enabled ? States.Hidden : States.Visible;

			if (item_ == null)
				return States.Bad;

			if (item_.hidesGenitalsBool)
			{
				return Enabled ? States.Hidden : States.Visible;
			}
			else if (item_.showsGenitalsBool)
			{
				return Enabled ? States.Visible : States.Hidden;
			}
			else if (item_.showsGenitalsState != "")
			{
				if (Enabled && GetWrap() == item_.showsGenitalsState)
					return States.Visible;
				else
					return States.Hidden;
			}

			return States.Bad;
		}

		public void SetGenitalsState(int s)
		{
			switch (s)
			{
				case States.Visible:
				{
					if (ci_.disableAnatomy)
					{
						Enabled = false;
					}
					else
					{
						if (item_ == null)
							return;

						if (item_.hidesGenitalsBool)
						{
							Enabled = false;
						}
						else if (item_.showsGenitalsBool)
						{
							Enabled = true;
						}
						else if (item_.showsGenitalsState != "")
						{
							Enabled = true;
							SetWrap(item_.showsGenitalsState);
						}
					}

					break;
				}

				case States.Hidden:
				{
					if (ci_.disableAnatomy)
					{
						Enabled = true;
					}
					else
					{
						if (item_ == null)
							return;

						if (item_.showsGenitalsBool)
						{
							Enabled = false;
						}
						else if (item_.hidesGenitalsBool)
						{
							Enabled = true;
						}
						else if (item_.hidesGenitalsState != "")
						{
							Enabled = true;
							SetWrap(item_.hidesGenitalsState);
						}
					}

					break;
				}
			}
		}

		public int GetBreastsState()
		{
			if (item_ == null)
				return States.Bad;

			if (item_.hidesBreastsBool)
			{
				return Enabled ? States.Hidden : States.Visible;
			}
			else if (item_.showsBreastsBool)
			{
				return Enabled ? States.Visible : States.Hidden;
			}
			else if (item_.showsBreastsState != "")
			{
				if (Enabled && GetWrap() == item_.showsBreastsState)
					return States.Visible;
				else
					return States.Hidden;
			}

			return States.Bad;
		}

		public void SetBreastsState(int s)
		{
			switch (s)
			{
				case States.Visible:
				{
					if (item_ == null)
						return;

					if (item_.hidesBreastsBool)
					{
						Enabled = false;
					}
					else if (item_.showsBreastsBool)
					{
						Enabled = true;
					}
					else if (item_.showsBreastsState != "")
					{
						Enabled = true;
						SetWrap(item_.showsBreastsState);
					}

					break;
				}

				case States.Hidden:
				{
					if (item_ == null)
						return;

					if (item_.showsBreastsBool)
					{
						Enabled = false;
					}
					else if (item_.hidesBreastsBool)
					{
						Enabled = true;
					}
					else if (item_.hidesBreastsState != "")
					{
						Enabled = true;
						SetWrap(item_.hidesBreastsState);
					}

					break;
				}
			}
		}

		private void SetWrap(string s)
		{
			EnsureWrap();

			if (wrap_ != null && s != wrap_.currentWrapName)
			{
				Log.Info($"state {wrap_.currentWrapName}->{s}");
				wrap_.currentWrapName = s;
			}
		}

		private string GetWrap()
		{
			EnsureWrap();

			if (wrap_ != null)
				return wrap_.currentWrapName;

			return "";
		}

		private void EnsureWrap()
		{
			if (wrap_ == null)
			{
				wrap_ = ci_.GetComponentInChildren<DAZSkinWrapSwitcher>();
				if (wrap_ == null)
				{
					Log.Error(
						$"clothing {Uid} has no wrap switcher " +
						$"{ci_.isDynamicRuntimeLoaded}");
				}
			}
		}

		public void Create()
		{
			item_ = ClothingManager.Instance.FindItem(Uid, null);
			if (item_ == null)
				return;

			DestroyLeft();
			if (item_.left.enabled)
				CreateLeft(item_.left);

			DestroyRight();
			if (item_.right.enabled)
				CreateRight(item_.right);
		}

		public void Destroy()
		{
			if (ci_.colliderLeft != null && ci_.colliderLeft.name.Contains(Tag))
				DestroyLeft();

			if (ci_.colliderRight != null && ci_.colliderRight.name.Contains(Tag))
				DestroyRight();
		}

		public bool HasCollider(int side)
		{
			if (side == Sides.Left || side == Sides.Both)
			{
				if (ci_.colliderLeft != null)
					return true;
			}

			if (side == Sides.Right || side == Sides.Both)
			{
				if (ci_.colliderRight != null)
					return true;
			}

			return false;
		}

		public Item.Collider GetCollider(int side)
		{
			var c = new Item.Collider();

			if (side == Sides.Left || side == Sides.Both)
			{
				c.rotation = ci_.colliderLeftRotation;
				c.size = ci_.colliderDimensions;
				c.center = ci_.colliderLeftCenter;
			}
			else
			{
				c.rotation = ci_.colliderRightRotation;
				c.size = ci_.colliderDimensions;
				c.center = ci_.colliderRightCenter;
			}

			return c;
		}

		public void SetCollider(Item.Collider c, int side)
		{
			if (side == Sides.Left || side == Sides.Both)
			{
				if (c.enabled && ci_.colliderLeft == null)
					CreateLeft(c);
				else if (!c.enabled && ci_.colliderLeft != null)
					DestroyLeft();

				ci_.colliderLeftRotation = c.rotation;
				ci_.colliderDimensions = c.size;
				ci_.colliderLeftCenter = c.center;

				ci_.colliderLeft.transform.localEulerAngles = c.rotation;
				ci_.colliderLeft.size = c.size;
				ci_.colliderLeft.center = c.center;

				item_.left = c;
			}

			if (side == Sides.Right || side == Sides.Both)
			{
				if (c.enabled && ci_.colliderRight == null)
					CreateRight(c);
				else if (!c.enabled && ci_.colliderRight != null)
					DestroyRight();

				ci_.colliderRightRotation = c.rotation;
				ci_.colliderDimensions = c.size;
				ci_.colliderRightCenter = c.center;

				ci_.colliderRight.transform.localEulerAngles = c.rotation;
				ci_.colliderRight.size = c.size;
				ci_.colliderRight.center = c.center;

				item_.right = c;
			}
		}

		private void DestroyLeft()
		{
			if (ci_.colliderLeft != null)
			{
				Log.Verbose($"{Uid}: destroying left collider");

				Object.Destroy(ci_.colliderLeft);
				ci_.colliderLeft = null;
				ci_.colliderLeftRotation = Vector3.zero;
				ci_.colliderLeftCenter = Vector3.zero;
				ci_.colliderDimensions = Vector3.zero;
				ci_.colliderTypeLeft = DAZClothingItem.ColliderType.None;
			}
		}

		private void DestroyRight()
		{
			if (ci_.colliderRight != null)
			{
				Log.Verbose($"{Uid}: destroying right collider");

				Object.Destroy(ci_.colliderRight);
				ci_.colliderRight = null;
				ci_.colliderRightRotation = Vector3.zero;
				ci_.colliderRightCenter = Vector3.zero;
				ci_.colliderDimensions = Vector3.zero;
				ci_.colliderTypeRight = DAZClothingItem.ColliderType.None;
			}
		}

		private void CreateLeft(Item.Collider c)
		{
			Log.Verbose($"{Uid}: creating left collider");

			ci_.colliderTypeLeft = DAZClothingItem.ColliderType.Shoe;
			ci_.colliderLeftRotation = c.rotation;
			ci_.colliderDimensions = c.size;
			ci_.colliderLeftCenter = c.center;

			var go = new GameObject();

			ci_.colliderLeft = go.AddComponent<BoxCollider>();
			ci_.colliderLeft.name = "lShoeCollider" + Tag;
			ci_.colliderLeft.transform.SetParent(c_.FindRigidbody("lFoot").transform, false);
			ci_.colliderLeft.transform.localEulerAngles = ci_.colliderLeftRotation;
			ci_.colliderLeft.size = ci_.colliderDimensions;
			ci_.colliderLeft.center = ci_.colliderLeftCenter;
		}

		private void CreateRight(Item.Collider c)
		{
			Log.Verbose($"{Uid}: creating right collider");

			ci_.colliderTypeRight = DAZClothingItem.ColliderType.Shoe;
			ci_.colliderRightRotation = c.rotation;
			ci_.colliderDimensions = c.size;
			ci_.colliderRightCenter = c.center;

			var go = new GameObject();
			ci_.colliderRight = go.AddComponent<BoxCollider>();
			ci_.colliderRight.name = "rShoeCollider" + Tag;
			ci_.colliderRight.transform.SetParent(c_.FindRigidbody("rFoot").transform, false);
			ci_.colliderRight.transform.localEulerAngles = ci_.colliderRightRotation;
			ci_.colliderRight.size = ci_.colliderDimensions;
			ci_.colliderRight.center = ci_.colliderRightCenter;
		}
	}
}
