using System.Collections.Generic;
using UnityEngine;

namespace ClothingManager
{
	class VamCharacter
	{
		public delegate void Handler(VamClothingItem item);
		public event Handler ClothingAdded, ClothingRemoved;

		private readonly DAZCharacterSelector s_;
		private readonly Dictionary<string, VamClothingItem> cs_ =
			new Dictionary<string, VamClothingItem>();
		private VamClothingItem shoes_ = null;

		private int genitalsState_ = States.Bad;
		private int breastsState_ = States.Bad;

		private JSONStorableFloat shoesAngleParam_;
		private JSONStorableFloat shoesHeightParam_;
		private JSONStorableStringChooser genitalsStateParam_;
		private JSONStorableStringChooser breastsStateParam_;


		public VamCharacter(DAZCharacterSelector s)
		{
			s_ = s;

			shoesAngleParam_ = new JSONStorableFloat("Shoes angle", 0, 0, 0);
			ClothingManager.Instance.Script.RegisterFloat(shoesAngleParam_);

			shoesHeightParam_ = new JSONStorableFloat("Shoes height", 0, 0, 0);
			ClothingManager.Instance.Script.RegisterFloat(shoesHeightParam_);

			genitalsStateParam_ = new JSONStorableStringChooser(
				"Genitals state", States.All(),
				"", "Genitals state", OnGenitalsStateChanged);
			ClothingManager.Instance.Script.RegisterStringChooser(genitalsStateParam_);

			breastsStateParam_ = new JSONStorableStringChooser(
				"Breasts state", States.All(),
				"", "Breasts state", OnBreastsStateChanged);
			ClothingManager.Instance.Script.RegisterStringChooser(breastsStateParam_);
		}

		public string ID
		{
			get { return s_.containingAtom.uid; }
		}

		public VamClothingItem[] Items
		{
			get
			{
				var items = new VamClothingItem[cs_.Count];
				cs_.Values.CopyTo(items, 0);
				return items;
			}
		}

		public bool Is(Atom a)
		{
			return (s_.containingAtom == a);
		}

		public float ShoesAngle
		{
			get { return shoesAngleParam_.val; }
		}

		public float ShoesHeight
		{
			get { return shoesHeightParam_.val; }
		}

		public int GenitalsState
		{
			get { return genitalsState_; }
			set { SetGenitalsState(value); }
		}

		public int BreastsVisible
		{
			get { return breastsState_; }
			set { SetBreastsState(value); }
		}

		private void SetGenitalsState(int s)
		{
			foreach (var p in cs_)
				p.Value.SetGenitalsState(s);

			Log.Verbose($"genitals {States.ToString(genitalsState_)}->{States.ToString(s)}");

			genitalsState_ = s;
			genitalsStateParam_.valNoCallback = States.ToString(s);
		}

		private void OnGenitalsStateChanged(string ss)
		{
			var s = States.FromString(ss);

			if (s != States.Bad)
				SetGenitalsState(s);
		}

		private void SetBreastsState(int s)
		{
			foreach (var p in cs_)
				p.Value.SetBreastsState(s);

			Log.Verbose($"breasts {States.ToString(breastsState_)}->{States.ToString(s)}");

			breastsState_ = s;
			breastsStateParam_.valNoCallback = States.ToString(s);
		}

		private void OnBreastsStateChanged(string ss)
		{
			var s = States.FromString(ss);

			if (s != States.Bad)
				SetBreastsState(s);
		}

		private VamClothingItem FindShoes()
		{
			VamClothingItem bestShoes = null;
			int bestShoesConfidence = 0;

			foreach (var p in cs_)
			{
				int s = p.Value.IsShoes();
				if (s > 0 && s > bestShoesConfidence)
					bestShoes = p.Value;
			}

			return bestShoes;
		}

		public Rigidbody FindRigidbody(string name)
		{
			var a = s_?.containingAtom;
			if (a == null)
				return null;

			foreach (var rb in a.rigidbodies)
			{
				if (rb.name == name)
					return rb.GetComponent<Rigidbody>();
			}

			return null;
		}

		public void Check(bool force = false)
		{
			var cs = s_.clothingItems;
			List<VamClothingItem> added = null;
			List<VamClothingItem> removed = null;

			foreach (var p in cs_)
			{
				if (!p.Value.Active)
				{
					if (removed == null)
						removed = new List<VamClothingItem>();

					removed.Add(p.Value);
				}
			}

			if (removed != null)
			{
				for (int i = 0; i < removed.Count; i++)
					cs_.Remove(removed[i].Uid);
			}

			for (int i = 0; i < cs.Length; i++)
			{
				if (cs[i].active)
				{
					var id = VamClothingItem.GetUid(cs[i]);

					if (!cs_.ContainsKey(id))
					{
						if (added == null)
							added = new List<VamClothingItem>();

						var ci = new VamClothingItem(this, cs[i]);
						added.Add(ci);
						cs_.Add(id, ci);
					}
				}
			}

			if (removed != null)
			{
				for (int i = 0; i < removed.Count; i++)
					Remove(removed[i]);
			}

			if (added != null)
			{
				for (int i = 0; i < added.Count; i++)
					Add(added[i]);
			}

			if (force)
			{
				Log.Verbose("force create");
				foreach (var c in cs_)
					c.Value.Create();
			}

			CheckState();
		}

		private void CheckState()
		{
			int newBreastState = breastsState_;
			int newGenitalsState = genitalsState_;

			foreach (var p in cs_)
			{
				var bs = p.Value.GetBreastsState();
				if (bs != States.Bad)
					newBreastState = bs;

				var gs = p.Value.GetGenitalsState();
				if (gs != States.Bad)
					newGenitalsState = gs;
			}

			if (newBreastState != breastsState_)
				SetBreastsState(newBreastState);

			if (newGenitalsState != genitalsState_)
				SetGenitalsState(newGenitalsState);
		}

		private void Add(VamClothingItem i)
		{
			i.Create();
			CheckShoes();
			ClothingAdded?.Invoke(i);
		}

		private void Remove(VamClothingItem i)
		{
			i.Destroy();
			CheckShoes();
			ClothingRemoved?.Invoke(i);
		}

		private void CheckShoes()
		{
			shoes_ = FindShoes();

			if (shoes_?.Daz == null)
				shoesAngleParam_.valNoCallback = 0;
			else
				shoesAngleParam_.valNoCallback = shoes_.Daz.driveXAngleTarget;

			if (shoes_?.Daz?.colliderLeft == null)
				shoesHeightParam_.valNoCallback = 0;
			else
				shoesHeightParam_.valNoCallback = shoes_.Daz.colliderLeft.bounds.size.y / 2;
		}
	}
}
