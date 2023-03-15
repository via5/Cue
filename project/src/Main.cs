using SimpleJSON;
using System;
using System.Threading;
using UnityEngine;

namespace Cue
{
#if MOCK
	public class CueToken
	{
		private int time_;

		public CueToken()
		{
			time_ = System.Environment.TickCount;
		}

		public bool Same(CueToken other)
		{
			return (time_ == other.time_);
		}

		public override string ToString()
		{
			return $"{time_}";
		}
	}


	public class CueMain
	{
		static private CueMain instance_ = null;

		private readonly CueToken token_ = new CueToken();
		private Sys.Mock.MockSys sys_ = null;
		private Cue cue_ = null;

		public CueMain()
		{
			instance_ = this;

			sys_ = new Sys.Mock.MockSys();
			cue_ = new Cue();
			//cue_.Init();
		}

		public void Run()
		{
			float deltaTime = 0;
			long last = 0;

			var r = new NormalRandom(0.2f, 0.3f, 1, 1);

			for (int i = 0; i < 10; ++i)
			{
				Console.WriteLine($"intensity: {i/10.0f}");
				NormalTest(r, 0, 1, i / 10.0f);
			}

			//TestIntensities();
			//NormalTest(0, 0.4f, 0.3f, 1);

			for (; ; )
			{
				var now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

				if (last > 0)
				{
					deltaTime = (float)((now - deltaTime) / 1000.0);

					cue_.FixedUpdate(deltaTime);
					cue_.Update(deltaTime);
					cue_.LateUpdate(deltaTime);
				}

				Thread.Sleep(1);
				sys_.Tick();
				last = now;
			}
		}

		public static string PluginCslist
		{
			get { return "Cue.cslist"; }
		}

		private void TestIntensities()
		{
			for (int i = 0; i < 10; ++i)
			{
				float intensity_ = (i + 1) / 10.0f;
				Console.WriteLine($"intensity: {intensity_}");
				TestIntensity(intensity_);
			}

			for (; ;)
			{
				Console.Write("intensity: ");
				float intensity_ = float.Parse(Console.ReadLine());
				TestIntensity(intensity_);
			}
		}

		private void TestIntensity(float i)
		{
			float breathingMax_ = 0.1f;
			float min = 0;
			float breathingCutoff = 1;

			if (i >= breathingCutoff)
				min = breathingMax_ + 0.01f;
			else
				min = 0;

			float max = i;
			float center = U.Clamp(min + (max - min) * 0.8f, 0, 1);

			NormalTest(min, max, center, 3);
		}

		private void NormalTest(float min, float max, float center, float width)
		{
			int bucketCount = 10;
			float bucketSize = 1.0f / bucketCount;
			int n = 1000;

			int[] buckets = new int[bucketCount];

			for (int i = 0; i < n; ++i)
			{
				float f = U.RandomNormal(min, max, center, width);
				bool okay = false;

				for (int b = 0; b < bucketCount; ++b)
				{
					if (f <= (bucketSize * (b + 1)))
					{
						++buckets[b];
						okay = true;
						break;
					}
				}

				if (!okay)
					Console.WriteLine($"!! {f}");
			}

			for (int i = 0; i < buckets.Length; ++i)
				Console.WriteLine($"{i}  {buckets[i]}");
		}

		private void NormalTest(NormalRandom r, float first, float last, float mag)
		{
			int bucketCount = 10;
			float bucketSize = 1.0f / bucketCount;
			int n = 1000;

			int[] buckets = new int[bucketCount];

			for (int i = 0; i < n; ++i)
			{
				float f = r.RandomFloat(first, last, mag);
				bool okay = false;

				for (int b = 0; b < bucketCount; ++b)
				{
					if (f <= (bucketSize * (b + 1)))
					{
						++buckets[b];
						okay = true;
						break;
					}
				}

				if (!okay)
					Console.WriteLine($"!! {f}");
			}

			for (int i = 0; i < buckets.Length; ++i)
				Console.WriteLine($"{i}  {buckets[i]}");
		}

		public static void Main()
		{
			new CueMain().Run();
		}

		static public CueMain Instance
		{
			get { return instance_; }
		}

		public CueToken Token
		{
			get { return token_; }
		}

		public UnityEngine.Transform UITransform
		{
			get { return null; }
		}

		public MVRScriptUI MVRScriptUI
		{
			get { return null; }
		}

		public MVRPluginManager MVRPluginManager
		{
			get { return null; }
		}

		public string PluginPath
		{
			get { return ""; }
		}

		public Sys.ISys Sys
		{
			get { return sys_; }
		}

		public bool PluginEnabled
		{
			get { return true; }
		}

		public void DisablePlugin()
		{
		}

		public static void OnException(Exception e)
		{
			Console.Write(e.ToString());
		}
	}
#else
	class CueToken
	{
		private float time_;

		public CueToken()
		{
			time_ = UnityEngine.Time.realtimeSinceStartup;
		}

		public bool Same(CueToken other)
		{
			return (time_ == other.time_);
		}

		public override string ToString()
		{
			return $"{time_}";
		}
	}


	class CueMain : MVRScript
	{
		const float MaxDeltaTime = 0.1f;
		static private CueMain instance_ = null;

		private readonly CueToken token_ = new CueToken();
		private Sys.ISys sys_ = null;
		private Cue cue_ = null;
		private bool inited_ = false;
		private float lastUpdateTime_ = 0;
		private float lastFixedUpdateTime_ = 0;

		private static float lastErrorTime_ = 0;
		private static int errorCount_ = 0;
		private const int MaxErrors = 3;

		public CueMain()
		{
			instance_ = this;
		}

		static public CueMain Instance
		{
			get { return instance_; }
		}

		public static string PluginCslist
		{
			get { return "Cue.cslist"; }
		}

		public CueToken Token
		{
			get { return token_; }
		}

		public Sys.ISys Sys
		{
			get { return sys_; }
		}

		public string PluginID
		{
			get { return name; }
		}

		public bool PluginEnabled
		{
			get { return enabledJSON.val; }
		}

		public override void Init()
		{
			base.Init();

			try
			{
				lastUpdateTime_ = Time.realtimeSinceStartup;
				lastFixedUpdateTime_ = Time.realtimeSinceStartup;

				sys_ = new Sys.Vam.VamSys(this);
				cue_ = new Cue();
				sys_.OnReady(DoInit);
			}
			catch(PluginGone e)
			{
				Logger.SafeLogError("plugin disabled");
			}
			catch (Exception e)
			{
				Logger.SafeLogError(e.ToString());
				Logger.SafeLogError("cue: Init failed, disabling");
				DisablePlugin();
			}
		}

		private void DoInit()
		{
			try
			{
				cue_.Init();
				inited_ = true;
			}
			catch(PluginGone e)
			{
				Logger.SafeLogError("plugin disabled");
			}
			catch(Exception e)
			{
				Logger.SafeLogError(e.ToString());
				Logger.SafeLogError("cue: DoInit failed, disabling");
				DisablePlugin();
			}
		}

		private float GetFixedUpdateTime()
		{
			// see GetUpdateTime()

			if (lastFixedUpdateTime_ == 0)
			{
				lastFixedUpdateTime_ = Time.realtimeSinceStartup;
				return 0;
			}

			float now = Time.realtimeSinceStartup;
			float d = Math.Min(now - lastFixedUpdateTime_, MaxDeltaTime);

			lastFixedUpdateTime_ = now;

			return d * Time.timeScale;
		}

		public void FixedUpdate()
		{
			if (!inited_)
				return;

			try
			{
				float s = GetFixedUpdateTime();
				if (s == 0)
					return;

				cue_.FixedUpdate(s);
			}
			catch(PluginGone e)
			{
				Logger.SafeLogError("plugin disabled");
			}
			catch(Exception e)
			{
				OnException(e);
			}
		}

		private float GetUpdateTime()
		{
			// Time.deltaTime is capped at a pretty low value, controlled by
			// Time.maximumDeltaTime, which cannot be changed since it also
			// controls FixedUpdate()
			//
			// maximumDeltaTime depends on the physics rate setting, but it
			// seems to be 0.033 at 72hz, and so everything slow down if the fps
			// is lower than 30
			//
			// since cue relies on accurate timing for lots of things, like
			// raising excitement over time, it has its own deltaTime, capped
			// at a much higher value

			if (lastUpdateTime_ == 0)
			{
				lastUpdateTime_ = Time.realtimeSinceStartup;
				return 0;
			}

			float now = Time.realtimeSinceStartup;
			float d = Math.Min(now - lastUpdateTime_, MaxDeltaTime);

			lastUpdateTime_ = now;

			return d * Time.timeScale;
		}

		public void Update()
		{
			if (!inited_)
				return;

			try
			{
				float s = GetUpdateTime();
				if (s == 0)
					return;

				cue_.Update(s);
			}
			catch(PluginGone e)
			{
				Logger.SafeLogError("plugin disabled");
			}
			catch(Exception e)
			{
				OnException(e);
			}
		}

		public void LateUpdate()
		{
			if (!inited_)
				return;

			try
			{
				cue_.LateUpdate(Time.deltaTime);
			}
			catch(PluginGone e)
			{
				Logger.SafeLogError("plugin disabled");
			}
			catch(Exception e)
			{
				OnException(e);
			}
		}

		public void OnEnable()
		{
			if (!inited_)
				return;

			try
			{
				if (cue_ != null)
					cue_.OnPluginState(true);
			}
			catch(PluginGone e)
			{
				Logger.SafeLogError("plugin disabled");
			}
			catch(Exception e)
			{
				OnException(e);
			}
		}

		public void OnDisable()
		{
			if (!inited_)
				return;

			try
			{
				if (cue_ != null)
					cue_.OnPluginState(false);
			}
			catch(PluginGone e)
			{
				Logger.SafeLogError("plugin disabled");
			}
			catch(Exception e)
			{
				OnException(e);
			}
		}

		public MVRScriptUI MVRScriptUI
		{
			get
			{
				return UITransform.GetComponentInChildren<MVRScriptUI>();
			}
		}

		public MVRPluginManager MVRPluginManager
		{
			get { return manager; }
		}

		public string PluginPath
		{
			get
			{
				// based on MacGruber, which was based on VAMDeluxe, which was
				// in turn based on Alazi

				string id = name.Substring(0, name.IndexOf('_'));
				string filename = manager.GetJSON()["plugins"][id].Value;

				var path = filename.Substring(
					0, filename.LastIndexOfAny(new char[] { '/', '\\' }));

				path = path.Replace('/', '\\');
				if (path.EndsWith("\\"))
					path = path.Substring(0, path.Length - 1);

				return path;
			}
		}

		public void DisablePlugin()
		{
			enabledJSON.val = false;
			throw new PluginGone();
		}

		public static void OnException(Exception e)
		{
			Logger.SafeLogError(e.ToString());

			var now = Cue.Instance.Sys.RealtimeSinceStartup;

			if (now - lastErrorTime_ < 1)
			{
				++errorCount_;
				if (errorCount_ > MaxErrors)
				{
					Logger.SafeLogError(
						$"more than {MaxErrors} errors in the last " +
						"second, disabling plugin");

					Cue.Instance.DisablePlugin();
				}
			}
			else
			{
				errorCount_ = 0;
			}

			lastErrorTime_ = now;
		}
	}
#endif
}
