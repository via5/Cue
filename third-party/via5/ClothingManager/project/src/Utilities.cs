using SimpleJSON;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClothingManager
{
	class LoadFailed : Exception
	{
		public LoadFailed(string what = "")
			: base(what)
		{
		}
	}


	static class Sides
	{
		public const int Both = 0;
		public const int Left = 1;
		public const int Right = 2;

		public static bool IsLeft(int s)
		{
			return (s == Both || s == Left);
		}

		public static bool IsRight(int s)
		{
			return (s == Both || s == Right);
		}

		public static string ToString(int s)
		{
			switch (s)
			{
				case Both: return "both";
				case Left: return "left";
				case Right: return "right";
				default: return $"?{s}";
			}
		}
	}


	static class States
	{
		public const int Bad = -1;
		public const int Visible = 0;
		public const int Hidden = 1;

		public static string ToString(int i)
		{
			switch (i)
			{
				case Visible: return "visible";
				case Hidden: return "hidden";
				default: return "";
			}
		}

		public static int FromString(string s)
		{
			if (s == "hidden")
				return Hidden;
			else
				return Visible;
		}

		public static List<string> All()
		{
			return new List<string> { "visible", "hidden" };
		}
	}


	static class U
	{
		private static float lastErrorTime_ = 0;
		private static int errorCount_ = 0;
		private const int MaxErrors = 3;

		public static void Safe(Action a)
		{
			try
			{
				a();
			}
			catch (Exception e)
			{
				SuperController.LogError(e.ToString());

				var now = Time.realtimeSinceStartup;

				if (now - lastErrorTime_ < 1)
				{
					++errorCount_;
					if (errorCount_ > MaxErrors)
					{
						SuperController.LogError(
							$"more than {MaxErrors} errors in the last " +
							"second, disabling plugin");

						via5.ClothingManager.DisablePlugin();
					}
				}
				else
				{
					errorCount_ = 0;
				}

				lastErrorTime_ = now;
			}
		}

		public static GameObject FindChildRecursive(Component c, string name)
		{
			return FindChildRecursive(c.gameObject, name);
		}

		public static GameObject FindChildRecursive(GameObject o, string name)
		{
			if (o == null)
				return null;

			if (o.name == name)
				return o;

			foreach (Transform c in o.transform)
			{
				var r = FindChildRecursive(c.gameObject, name);
				if (r != null)
					return r;
			}

			return null;
		}

		public static Vector3 VectorFromJSON(JSONClass o, string key, bool mandatory = false)
		{
			if (!o.HasKey(key))
			{
				if (mandatory)
					throw new LoadFailed($"vector3 '{key}' is missing");
				else
					return Vector3.zero;
			}

			var a = o[key].AsArray;
			if (a == null)
				throw new LoadFailed($"vector3 '{key}' node is not an array");

			if (a.Count != 3)
				throw new LoadFailed($"vector3 '{key}' array must have 3 elements");

			float x;
			if (!float.TryParse(a[0], out x))
				throw new LoadFailed($"vector3 '{key}' x is not a number");

			float y;
			if (!float.TryParse(a[1], out y))
				throw new LoadFailed($"vector3 '{key}' is not a number");

			float z;
			if (!float.TryParse(a[2], out z))
				throw new LoadFailed($"vector3 '{key}' is not a number");

			return new Vector3(x, y, z);
		}

		public static JSONNode ToJSON(Vector3 v)
		{
			var a = new JSONArray();

			a.Add(new JSONData(v.x));
			a.Add(new JSONData(v.y));
			a.Add(new JSONData(v.z));

			return a;
		}
	}


	static class HashHelper
	{
		public static int GetHashCode<T1, T2>(T1 arg1, T2 arg2)
		{
			unchecked
			{
				return 31 * arg1.GetHashCode() + arg2.GetHashCode();
			}
		}

		public static int GetHashCode<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3)
		{
			unchecked
			{
				int hash = arg1.GetHashCode();
				hash = 31 * hash + arg2.GetHashCode();
				return 31 * hash + arg3.GetHashCode();
			}
		}

		public static int GetHashCode<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3,
			T4 arg4)
		{
			unchecked
			{
				int hash = arg1.GetHashCode();
				hash = 31 * hash + arg2.GetHashCode();
				hash = 31 * hash + arg3.GetHashCode();
				return 31 * hash + arg4.GetHashCode();
			}
		}
	}
}
