using UnityEngine;

namespace Cue.Proc
{
	using Quaternion = UnityEngine.Quaternion;

	class Controller : BasicTarget
	{
		private readonly string cname_;
		private readonly UnityEngine.Vector3 pos_;
		private readonly UnityEngine.Vector3 rot_;
		private FreeControllerV3 fc_ = null;
		private UnityEngine.Vector3 startPos_;
		private UnityEngine.Vector3 endPos_;
		private Quaternion startRot_;
		private Quaternion endRot_;
		private bool done_ = false;

		public Controller(string cname, Vector3 pos, Vector3 rot, ISync sync)
			: base("", sync)
		{
			cname_ = cname;
			pos_ = Sys.Vam.U.ToUnity(pos);
			rot_ = Sys.Vam.U.ToUnity(rot);
		}

		public override ITarget Clone()
		{
			return new Controller(
				cname_,
				Sys.Vam.U.FromUnity(pos_),
				Sys.Vam.U.FromUnity(rot_),
				Sync.Clone());
		}

		public override bool Done
		{
			get { return done_; }
		}

		public override void Start(Person p)
		{
			fc_ = Cue.Instance.VamSys.FindController(p.VamAtom.Atom, cname_);
			if (fc_ == null)
			{
				Cue.LogError($"ProceduralStep: controller {cname_} not found in {p}");
				return;
			}

			done_ = false;
			startPos_ = fc_.transform.localPosition;
			endPos_ = pos_ + new UnityEngine.Vector3(0, p.Clothing.HeelsHeight, 0);
			startRot_ = fc_.transform.localRotation;

			if (cname_ == "lFootControl" || cname_ == "rFootControl")
			{
				endRot_ = Quaternion.Euler(
					rot_ + new UnityEngine.Vector3(p.Clothing.HeelsAngle, 0, 0));
			}
			else
			{
				endRot_ = Quaternion.Euler(rot_);
			}

			Reset();
		}

		public override void Reset()
		{
			base.Reset();
		}

		public static UnityEngine.Vector3 Bezier2(
			UnityEngine.Vector3 s,
			UnityEngine.Vector3 p,
			UnityEngine.Vector3 e,
			float t)
		{
			float rt = 1 - t;
			return rt * rt * s + 2 * rt * t * p + t * t * e;
		}

		public override void FixedUpdate(float s)
		{
			Sync.FixedUpdate(s);

			if (fc_ == null)
			{
				done_ = true;
				return;
			}


			float t = Sync.Magnitude;
			var mid = startPos_ + (endPos_ - startPos_) / 2 + new UnityEngine.Vector3(0, 0.3f, 0);

			//if (name_ == "lFootControl" || name_ == "rFootControl")
			//{
			//	fc_.transform.localPosition =
			//		Bezier2(startPos_, mid, endPos_, t);
			//}
			//else
			{
				fc_.transform.localPosition =
					UnityEngine.Vector3.Lerp(startPos_, endPos_, t);
			}

			fc_.transform.localRotation =
				Quaternion.Lerp(startRot_, endRot_, t);

			if (t >= 1)
				done_ = true;
		}

		public override string ToString()
		{
			return $"controller {cname_} pos={pos_} rot={rot_}";
		}

		public override string ToDetailedString()
		{
			return ToString();
		}
	}
}
