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
	}
}
