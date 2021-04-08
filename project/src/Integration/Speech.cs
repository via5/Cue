using System;

namespace Cue
{
	interface ISpeaker
	{
		void Say(string s);
	}


	class VamSpeaker : ISpeaker
	{
		private Person person_ = null;
		private JSONStorableString text_ = null;

		public VamSpeaker(Person p)
		{
			person_ = p;
		}

		public void Say(string s)
		{
			GetParameters();
			if (text_ == null)
				return;

			try
			{
				text_.val = s;
			}
			catch (Exception e)
			{
				Cue.LogError(
					person_.ToString() + " can't speak, " + e.Message + " " +
					"(while trying to say '" + s + "'");
			}
		}

		private void GetParameters()
		{
			if (text_ != null)
				return;

			var vsys = ((W.VamSys)Cue.Instance.Sys);

			text_ = vsys.GetStringParameter(
				person_, "SpeechBubble", "bubbleText");
		}
	}
}
