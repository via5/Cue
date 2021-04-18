using System;
using UnityEngine;

namespace Cue
{
	class Bits
	{
		public static bool IsSet(int flag, int bits)
		{
			return ((flag & bits) == bits);
		}
	}


	public class Strings
	{
		public static string Get(string s, params object[] ps)
		{
			if (ps.Length > 0)
				return string.Format(s, ps);
			else
				return s;
		}
	}


	class U
	{
		static public void Safe(Action a)
		{
			try
			{
				a();
			}
			catch (Exception e)
			{
				Cue.LogError(e.ToString());
			}
		}

		public static T Clamp<T>(T val, T min, T max)
			where T : IComparable<T>
		{
			if (val.CompareTo(min) < 0)
				return min;
			else if (val.CompareTo(max) > 0)
				return max;
			else
				return val;
		}

		// [begin, end[
		//
		public static int RandomInt(int begin, int end)
		{
			return UnityEngine.Random.Range(begin, end);
		}

		public static void DumpComponents(GameObject o, int indent = 0)
		{
			foreach (var c in o.GetComponents(typeof(Component)))
				Cue.LogError(new string(' ', indent * 2) + c.ToString());
		}

		public static void DumpComponentsAndUp(Component c)
		{
			DumpComponentsAndUp(c.gameObject);
		}

		public static void DumpComponentsAndUp(GameObject o)
		{
			Cue.LogError(o.name);

			var rt = o.GetComponent<RectTransform>();
			if (rt != null)
			{
				Cue.LogError("  rect: " + rt.rect.ToString());
				Cue.LogError("  offsetMin: " + rt.offsetMin.ToString());
				Cue.LogError("  offsetMax: " + rt.offsetMax.ToString());
				Cue.LogError("  anchorMin: " + rt.anchorMin.ToString());
				Cue.LogError("  anchorMax: " + rt.anchorMax.ToString());
				Cue.LogError("  anchorPos: " + rt.anchoredPosition.ToString());
			}

			DumpComponents(o);
			Cue.LogError("---");

			var parent = o?.transform?.parent?.gameObject;
			if (parent != null)
				DumpComponentsAndUp(parent);
		}
	}
}
