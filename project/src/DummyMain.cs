using System.Threading;

namespace Cue
{
	class DummyCue
	{
		public static void Main()
		{
			var sys = new W.MockSys();
			var s = new Cue();
			s.Init();

			for (; ; )
			{
				s.Update();
				Thread.Sleep(1);
				sys.Tick();
			}
		}
	}
}
