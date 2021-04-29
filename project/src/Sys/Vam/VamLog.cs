using System;
using UnityEngine;

namespace Cue.W
{
	class VamLog
	{
		private UnityEngine.UI.Button clear_ = null;
		private UnityEngine.UI.Text logText_ = null;
		private UIStyleText logStyle_ = null;
		private int logFontSize_ = -1;
		private Font logFont_ = null;

		public VamLog()
		{
		}

		public void Clear()
		{
			if (clear_ == null)
			{
				var t = VUI.Utilities.FindChildRecursive(
					SuperController.singleton.errorLogPanel, "ClearButton");

				clear_ = t?.GetComponent<UnityEngine.UI.Button>();
			}

			clear_?.onClick?.Invoke();
		}

		public void Log(string s, int level)
		{
			foreach (var line in s.Split('\n'))
				LogLine(line, level);
		}

		private void LogLine(string line, int level)
		{
			var t = DateTime.Now.ToString("hh:mm:ss");
			string p = LogLevels.ToShortString(level);

			if (level == LogLevels.Error)
				SuperController.LogError($"{t} !![{p}] {line}");
			else
				SuperController.LogError($"{t}   [{p}] {line}");
		}

		public void OnPluginState(bool b)
		{
			if (logText_ == null)
			{
				var vp = VUI.Utilities.FindChildRecursive(
					SuperController.singleton.errorLogPanel, "Viewport");

				var textObject = VUI.Utilities.FindChildRecursive(vp, "Text");

				logText_ = textObject.GetComponent<UnityEngine.UI.Text>();
				logStyle_ = textObject.GetComponent<UIStyleText>();
			}

			if (b)
			{
				logFontSize_ = logStyle_.fontSize;
				logFont_ = logText_.font;
				logStyle_.fontSize = 24;
				logText_.resizeTextForBestFit = false;
				logText_.font = Font.CreateDynamicFontFromOSFont("Consolas", 24);
				logStyle_.UpdateStyle();
			}
			else
			{
				logStyle_.fontSize = logFontSize_;
				logText_.fontSize = logFontSize_;
				logText_.font = logFont_;
				logText_.resizeTextForBestFit = true;
				logStyle_.UpdateStyle();
			}
		}
	}
}
