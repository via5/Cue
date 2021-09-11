using System;
using UnityEngine;

namespace Cue.Sys.Vam
{
	class VamLog
	{
		private UnityEngine.UI.Button clear_ = null;
		private UnityEngine.UI.Text logText_ = null;
		private UIStyleText logStyle_ = null;
		private int oldLogFontSize_ = -1;
		private Font oldLogFont_ = null;

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
			var t = DateTime.Now.ToString("hh:mm:ss.fff");
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
				{
					// use the message log panel font instead if the error
					// one, because the old font might not always be restored
					// if cue has a hard failure somewhere
					var vp = VUI.Utilities.FindChildRecursive(
						SuperController.singleton.msgLogPanel, "Viewport");

					var textObject = VUI.Utilities.FindChildRecursive(vp, "Text");

					var t = textObject.GetComponent<UnityEngine.UI.Text>();

					oldLogFontSize_ = t.fontSize;
					oldLogFont_ = t.font;
				}

				var f = VUI.Style.Theme.MonospaceFont;

				logText_.resizeTextForBestFit = false;
				logText_.font = f;
				logText_.fontSize = f.fontSize;

				logStyle_.fontSize = f.fontSize;
				logStyle_.UpdateStyle();
			}
			else
			{
				logText_.resizeTextForBestFit = true;
				logText_.font = oldLogFont_;
				logText_.fontSize = oldLogFontSize_;

				logStyle_.fontSize = oldLogFontSize_;
				logStyle_.UpdateStyle();
			}
		}
	}
}
