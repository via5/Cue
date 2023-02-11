using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace ClothingManager
{
	class ClothingManager
	{
		private const string RootName = "via5.clothingmanager.root";

		private static ClothingManager instance_;
		private bool registered_ = false;
		private GameObject root_ = null;

		private readonly MVRScript s_;
		private VamCharacter char_ = null;
		private float elapsed_ = 0;
		private UI ui_ = new UI();
		private Editor editor_ = new Editor();
		private Loader ld_ = new Loader();

		private Dictionary<string, Item> ids_ =
			new Dictionary<string, Item>();

		private Dictionary<string, Item> tags_ =
			new Dictionary<string, Item>();

		public ClothingManager(MVRScript s)
		{
			instance_ = this;
			s_ = s;

			DestroyOldRoot();
			CreateRoot();

			//editor_.Enabled = true;
			ui_.Init(s);

			LoadGlobal();
			Register();

			s.RegisterBool(new JSONStorableBool("test", false));
		}

		public static ClothingManager Instance
		{
			get { return instance_; }
		}

		public Loader Loader
		{
			get { return ld_ ; }
		}

		public Editor Editor
		{
			get { return editor_; }
		}

		public VamCharacter Character
		{
			get { return char_; }
		}

		public Transform Root
		{
			get { return root_.transform; }
		}

		public MVRScript Script
		{
			get { return s_; }
		}

		private void DestroyOldRoot()
		{
			var r = U.FindChildRecursive(SuperController.singleton.transform.root, RootName);

			if (r != null)
			{
				Log.Verbose("destroying old root");
				r.name = RootName + $".deleted";
				Object.Destroy(r);
			}
		}

		private void CreateRoot()
		{
			Log.Verbose("creating root");
			root_ = new GameObject(RootName);
			root_.transform.SetParent(SuperController.singleton.transform.root, false);
		}

		public void Reload()
		{
			LoadGlobal();
			Check(true);
		}

		private void LoadGlobal()
		{
			ids_.Clear();
			tags_.Clear();

			foreach (var i in ld_.LoadGlobal())
				Add(i);
		}

		public void Update()
		{
			elapsed_ += Time.deltaTime;
			if (elapsed_ > 1)
			{
				elapsed_ = 0;
				Check();
			}

			editor_.Update();
			ui_.Update();
		}

		public void Add(Item item)
		{
			if (item.id != "")
				ids_.Add(item.id, item);
			else
				tags_.Add(item.tag, item);
		}

		public void Select(VamClothingItem item, int side)
		{
			editor_.Select(item, side);
			ui_.Select(item, side);
		}

		public Item FindItem(string id, string[] tags)
		{
			{
				var item = FindCachedItem(id, tags);
				if (item != null)
					return item;

				Log.Verbose($"item {id} not in cache");
			}

			var items = ld_.TryLoad(id);

			if (items.Count > 0)
			{
				Log.Verbose($"found meta file");

				foreach (var i in items)
					Add(i);

				var item = FindCachedItem(id, tags);
				if (item != null)
					return item;

				Log.Verbose($"but still not in cache");
			}

			Log.Verbose($"no meta for {id}");

			return null;
		}

		private Item FindCachedItem(string id, string[] tags)
		{
			Item item;

			if (ids_.TryGetValue(id, out item))
				return item;

			if (tags != null)
			{
				for (int i = 0; i < tags.Length; ++i)
				{
					if (tags_.TryGetValue(tags[i], out item))
						return item;
				}
			}

			return null;
		}

		private void Register()
		{
			Log.Verbose("reg");
			if (registered_)
				return;

			var cs = s_.containingAtom.GetComponentInChildren<DAZCharacterSelector>();
			if (cs != null)
			{
				char_ = new VamCharacter(cs);
				char_.ClothingAdded += OnClothingAdded;
				char_.ClothingRemoved += OnClothingRemoved;
				char_.Check();

				ui_.MakeStale();
			}

			registered_ = true;
		}

		private void Unregister()
		{
			if (!registered_)
				return;

			registered_ = false;
		}

		private void OnClothingAdded(VamClothingItem item)
		{
			Log.Verbose($"added {item.Uid}");
			ui_.MakeStale();
		}

		private void OnClothingRemoved(VamClothingItem item)
		{
			Log.Verbose($"removed {item.Uid}");
			ui_.MakeStale();
		}


		private void Check(bool force = false)
		{
			var sw = new Stopwatch();
			sw.Start();

			char_.Check(force);

			sw.Stop();
			float ms = (float)((((double)sw.ElapsedTicks) / Stopwatch.Frequency) * 1000);
			//Log.Verbose($"checked {ms:0.000}ms");
		}

		public void OnPluginEnable()
		{
			Register();

			if (root_ != null)
				root_.SetActive(true);
		}

		public void OnPluginDisable()
		{
			Unregister();

			if (root_ != null)
				root_.SetActive(false);
		}

		public static void Main()
		{
		}
	}
}
