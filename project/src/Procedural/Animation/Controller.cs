using UnityEngine;

namespace Cue.Proc
{
	using Quaternion = UnityEngine.Quaternion;

	class Controller : ITarget
	{
		private readonly string name_;
		private readonly UnityEngine.Vector3 pos_;
		private readonly UnityEngine.Vector3 rot_;
		private FreeControllerV3 fc_ = null;
		private UnityEngine.Vector3 startPos_;
		private UnityEngine.Vector3 endPos_;
		private Quaternion startRot_;
		private Quaternion endRot_;
		private float elapsed_ = 0;
		private bool done_ = false;
		private IEasing easing_ = new SinusoidalEasing();

		public Controller(string name, Vector3 pos, Vector3 rot)
		{
			name_ = name;
			pos_ = Sys.Vam.U.ToUnity(pos);
			rot_ = Sys.Vam.U.ToUnity(rot);
		}

		public ITarget Clone()
		{
			return new Controller(
				name_,
				Sys.Vam.U.FromUnity(pos_),
				Sys.Vam.U.FromUnity(rot_));
		}

		public bool Done
		{
			get { return done_; }
		}

		public void Start(Person p)
		{
			fc_ = Cue.Instance.VamSys.FindController(p.VamAtom.Atom, name_);
			if (fc_ == null)
			{
				Cue.LogError($"ProceduralStep: controller {name_} not found in {p}");
				return;
			}

			done_ = false;
			elapsed_ = 0;
			startPos_ = fc_.transform.localPosition;
			endPos_ = pos_ + new UnityEngine.Vector3(0, p.Clothing.HeelsHeight, 0);
			startRot_ = fc_.transform.localRotation;

			if (name_ == "lFootControl" || name_ == "rFootControl")
			{
				endRot_ = Quaternion.Euler(
					rot_ + new UnityEngine.Vector3(p.Clothing.HeelsAngle, 0, 0));
			}
			else
			{
				endRot_ = Quaternion.Euler(rot_);
			}
		}

		public void Reset()
		{
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

		public void FixedUpdate(float s)
		{
			if (fc_ == null)
			{
				done_ = true;
				return;
			}

			elapsed_ += s;

			float t = easing_.Magnitude(U.Clamp(elapsed_, 0, 1));

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
			return $"controller {name_} pos={pos_} rot={rot_}";
		}

		public string ToDetailedString()
		{
			return ToString();
		}
	}
}
