using SimpleJSON;
using System.Collections.Generic;
using UnityEngine;

namespace ClothingManager
{
	class UI
	{
		private JSONStorableBool edit_ = null;
		private JSONStorableStringChooser items_ = null;
		private JSONStorableStringChooser sides_ = null;
		private JSONStorableBool left_ = null;
		private JSONStorableBool right_ = null;

		private JSONStorableFloat rotX_ = null;
		private JSONStorableFloat rotY_ = null;
		private JSONStorableFloat rotZ_ = null;

		private JSONStorableFloat scaleX_ = null;
		private JSONStorableFloat scaleY_ = null;
		private JSONStorableFloat scaleZ_ = null;

		private JSONStorableFloat posX_ = null;
		private JSONStorableFloat posY_ = null;
		private JSONStorableFloat posZ_ = null;

		private VamClothingItem ci_ = null;
		private bool stale_ = true;


		public void Init(MVRScript s)
		{
			edit_ = new JSONStorableBool(
				"Edit mode", ClothingManager.Instance.Editor.Enabled,
				OnEditMode);

			items_ = new JSONStorableStringChooser(
				"Item", new List<string>(), "", "Item", OnItemChanged);

			sides_ = new JSONStorableStringChooser(
				"Side",
				new List<string> { $"{Sides.Both}", $"{Sides.Left}", $"{Sides.Right}" },
				new List<string> { "Both", "Left", "Right" },
				$"{Sides.Both}", "Side", OnSideChanged);

			left_ = new JSONStorableBool("Left collider", false, OnLeftCollider);
			right_ = new JSONStorableBool("Right collider", false, OnRightCollider);

			rotX_ = new JSONStorableFloat("Rotation X", 0, OnChanged, 0, 360);
			rotY_ = new JSONStorableFloat("Rotation Y", 0, OnChanged, 0, 360);
			rotZ_ = new JSONStorableFloat("Rotation Z", 0, OnChanged, 0, 360);

			scaleX_ = new JSONStorableFloat("Scale X", 0, OnChanged, 0, 1);
			scaleY_ = new JSONStorableFloat("Scale Y", 0, OnChanged, 0, 1);
			scaleZ_ = new JSONStorableFloat("Scale Z", 0, OnChanged, 0, 1);

			posX_ = new JSONStorableFloat("Position X", 0, OnChanged, -1, 1);
			posY_ = new JSONStorableFloat("Position Y", 0, OnChanged, -1, 1);
			posZ_ = new JSONStorableFloat("Position Z", 0, OnChanged, -1, 1);

			s.CreateButton("Reload meta files").button.onClick.AddListener(OnReload);
			s.CreateToggle(edit_);
			s.CreatePopup(items_);
			s.CreatePopup(sides_);
			s.CreateToggle(left_);
			s.CreateToggle(right_);
			s.CreateButton("Save meta file...").button.onClick.AddListener(OnSave);

			CreateSlider(s, rotX_, true);
			CreateSlider(s, rotY_, true);
			CreateSlider(s, rotZ_, true);

			CreateSlider(s, scaleX_, true);
			CreateSlider(s, scaleY_, true);
			CreateSlider(s, scaleZ_, true);

			CreateSlider(s, posX_, true);
			CreateSlider(s, posY_, true);
			CreateSlider(s, posZ_, true);
		}

		private void OnSave()
		{
			if (ci_ == null)
			{
				Log.Error("no selection");
				return;
			}

			string[] cs = ClothingManager.Instance.Loader.MakePath(ci_.Uid);
			if (cs == null || cs.Length != 2)
			{
				Log.Error("no path");
				return;
			}

			string path = cs[0], file = cs[1];

			var a = new JSONArray();
			a.Add(ci_.Item.ToJSON());

			var doc = new JSONClass();
			doc["clothing"] = a;

			var outPath = path + "\\" + file;
			Log.Info($"saving {outPath}");

			SuperController.singleton.SaveJSON(doc, outPath);
		}

		public void MakeStale()
		{
			stale_ = true;
		}

		private void CreateSlider(MVRScript s, JSONStorableFloat st, bool rightSide = false)
		{
			var sl = s.CreateSlider(st, rightSide);

			if (st.max < 5)
				sl.valueFormat = "F4";
		}

		public void Select(VamClothingItem ci, int side)
		{
			ci_ = ci;

			if (ci_ == null)
			{
				items_.valNoCallback = "";
				items_.displayChoices = new List<string>();
				items_.choices = new List<string>();
			}
			else
			{
				items_.valNoCallback = ci.Uid;
				sides_.valNoCallback = $"{side}";
				left_.valNoCallback = ci.HasCollider(Sides.Left);
				right_.valNoCallback = ci.HasCollider(Sides.Right);

				var c = ci.GetCollider(side);

				rotX_.valNoCallback = c.rotation.x;
				rotY_.valNoCallback = c.rotation.y;
				rotZ_.valNoCallback = c.rotation.z;

				scaleX_.valNoCallback = c.size.x;
				scaleY_.valNoCallback = c.size.y;
				scaleZ_.valNoCallback = c.size.z;

				posX_.valNoCallback = c.center.x;
				posY_.valNoCallback = c.center.y;
				posZ_.valNoCallback = c.center.z;
			}
		}

		public void Update()
		{
			if (stale_)
			{
				stale_ = false;
				UpdateItems();
			}
		}

		private void UpdateItems()
		{
			var tags = new List<string>();
			var display = new List<string>();
			var old = items_.val;

			tags.Add("");
			display.Add("");

			var c = ClothingManager.Instance.Character;

			string bestShoes = "";
			int bestShoesConfidence = 0;

			foreach (var ci in c.Items)
			{
				tags.Add(ci.Uid);
				display.Add(ci.DisplayName);

				int s = ci.IsShoes();
				if (s > 0 && s > bestShoesConfidence)
					bestShoes = ci.Uid;
			}

			items_.choices = tags;
			items_.displayChoices = display;

			if (old == "")
			{
				if (bestShoes != "")
					items_.val = bestShoes;
				else
					items_.valNoCallback = "";
			}
			else
			{
				if (tags.Contains(old))
					items_.valNoCallback = old;
				else
					items_.valNoCallback = "";
			}
		}

		private void OnReload()
		{
			ClothingManager.Instance.Reload();
			UpdateItems();
			SelectionChanged();
		}

		private void OnEditMode(bool b)
		{
			ClothingManager.Instance.Editor.Enabled = b;
		}

		private void OnItemChanged(string s)
		{
			SelectionChanged();
		}

		private void OnSideChanged(string s)
		{
			SelectionChanged();
		}

		private void SelectionChanged()
		{
			VamClothingItem item = null;

			var c = ClothingManager.Instance.Character;

			foreach (var i in c.Items)
			{
				if (i.Uid == items_.val)
				{
					item = i;
					break;
				}
			}

			ClothingManager.Instance.Select(item, int.Parse(sides_.val));
		}

		private void OnChanged(float f)
		{
			if (ci_ != null)
			{
				var c = new Item.Collider();

				c.enabled = true;
				c.rotation = new Vector3(rotX_.val, rotY_.val, rotZ_.val);
				c.size = new Vector3(scaleX_.val, scaleY_.val, scaleZ_.val);
				c.center = new Vector3(posX_.val, posY_.val, posZ_.val);

				ci_.SetCollider(c, int.Parse(sides_.val));
			}
		}

		private void OnLeftCollider(bool b)
		{
			SetColliderEnabled(Sides.Left, b);
		}

		private void OnRightCollider(bool b)
		{
			SetColliderEnabled(Sides.Right, b);
		}

		private void SetColliderEnabled(int side, bool b)
		{
			if (ci_ != null)
			{
				var item = ci_.Item;

				if (b)
				{
					Log.Verbose($"{ci_.Uid}: enabling {Sides.ToString(side)} collider");

					if (item == null)
					{
						Log.Verbose($"{ci_.Uid}: item doesn't exist yet");

						item = new Item(ci_.Uid, null);
						ClothingManager.Instance.Add(item);
						ci_.Create();
					}

					var c = ci_.GetCollider(side);

					c.enabled = true;
					c.rotation = new Vector3(rotX_.val, rotY_.val, rotZ_.val);
					c.size = new Vector3(scaleX_.val, scaleY_.val, scaleZ_.val);
					c.center = new Vector3(posX_.val, posY_.val, posZ_.val);

					ci_.SetCollider(c, side);
				}
				else
				{
					Log.Verbose($"{ci_.Uid}: disabling {Sides.ToString(side)} collider");

					if (item != null)
					{
						var c = ci_.GetCollider(side);
						c.enabled = false;
						ci_.SetCollider(c, side);
					}
				}
			}
		}
	}
}
