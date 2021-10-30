using SimpleJSON;

namespace Cue
{
	static class J
	{
		public static string ReqString(JSONClass o, string key)
		{
			if (!o.HasKey(key))
				throw new LoadFailed($"key '{key}' is missing");

			return o[key].Value;
		}

		public static bool OptString(JSONClass o, string key, ref string s)
		{
			if (!o.HasKey(key))
				return false;

			s = o[key].Value;
			return true;
		}

		public static float ReqFloat(JSONClass o, string key)
		{
			if (!o.HasKey(key))
				throw new LoadFailed($"key '{key}' is missing");

			var v = o[key].Value;

			float f;
			if (!float.TryParse(v, out f))
				throw new LoadFailed($"bad float '{v}' for key '{key}'");

			return f;
		}

		public static bool OptFloat(JSONClass o, string key, ref float f)
		{
			if (!o.HasKey(key))
				return false;

			var v = o[key].Value;

			if (!float.TryParse(v, out f))
				throw new LoadFailed($"bad float '{v}' for key '{key}'");

			return true;
		}

		public static float OptFloat(JSONClass o, string key, float def)
		{
			if (!o.HasKey(key))
				return def;

			var v = o[key].Value;

			float f;
			if (!float.TryParse(v, out f))
				throw new LoadFailed($"bad float '{v}' for key '{key}'");

			return f;
		}

		public static bool ReqBool(JSONClass o, string key)
		{
			if (!o.HasKey(key))
				throw new LoadFailed($"key '{key}' is missing");

			return o[key].AsBool;
		}

		public static bool OptBool(JSONClass o, string key, ref bool b)
		{
			if (!o.HasKey(key))
				return false;

			b = o[key].AsBool;
			return true;
		}
	}
}
