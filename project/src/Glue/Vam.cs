using System;
using UnityEngine;

namespace Cue.W
{
	class U
	{
		public static UnityEngine.Vector3 Convert(Vector3 v)
		{
			return new UnityEngine.Vector3(v.X, v.Y, v.Z);
		}

		public static Vector3 Convert(UnityEngine.Vector3 v)
		{
			return new Vector3(v.x, v.y, v.z);
		}
	}

	class VamSys : ISys
	{
		private static VamSys instance_ = null;
		private readonly MVRScript script_ = null;
		private readonly VamTime time_ = new VamTime();
		private readonly VamLog log_ = new VamLog();

		public VamSys(MVRScript s)
		{
			instance_ = this;
			script_ = s;
		}

		static public VamSys Instance
		{
			get { return instance_; }
		}

		public ITime Time
		{
			get { return time_; }
		}

		public ILog Log
		{
			get { return log_; }
		}

		public IAtom GetAtom(string id)
		{
			var a = SuperController.singleton.GetAtomByUid(id);
			if (a == null)
				return null;

			return new VamAtom(a);
		}

		public IAtom ContainingAtom
		{
			get { return new VamAtom(script_.containingAtom); }
		}

		public bool Paused
		{
			get { return SuperController.singleton.freezeAnimation; }
		}
	}

	class VamTime : ITime
	{
		public float deltaTime
		{
			get { return Time.deltaTime; }
		}
	}

	class VamLog : ILog
	{
		public void Verbose(string s)
		{
			SuperController.LogError(s);
		}

		public void Info(string s)
		{
			SuperController.LogError(s);
		}

		public void Error(string s)
		{
			SuperController.LogError(s);
		}
	}

	class VamAtom : IAtom
	{
		private readonly Atom atom_;

		public VamAtom(Atom atom)
		{
			atom_ = atom;
		}

		public bool IsPerson
		{
			get { return atom_.type == "Person"; }
		}

		public Vector3 Position
		{
			get { return U.Convert(atom_.mainController.transform.position); }
			set { atom_.mainController.MoveControl(U.Convert(value)); }
		}

		public Vector3 Direction
		{
			get
			{
				var v =
					atom_.mainController.transform.rotation *
					UnityEngine.Vector3.forward;

				return U.Convert(v);
			}

			set
			{
				var r = Quaternion.LookRotation(U.Convert(value));
				atom_.mainController.RotateTo(r);
			}
		}

		public Atom Atom
		{
			get { return atom_; }
		}
	}
}
