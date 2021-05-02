using System;
using System.Collections.Generic;

namespace Cue
{
	class BodyPartTypes
	{
		public const int Head = 1;
		public const int Lips = 2;
		public const int Mouth = 3;
		public const int LeftBreast = 4;
		public const int RightBreast = 5;
		public const int Labia = 6;
		public const int Vagina = 7;
		public const int DeepVagina = 8;
		public const int DeeperVagina = 9;

		private static string[] names_ = new string[]
		{
			"", "head", "lips", "mouth", "leftbreast", "rightbreast",
			"labia", "vagina", "deepvagina", "deepervagina"
		};

		public static string ToString(int t)
		{
			if (t >= 0 && t < names_.Length)
				return names_[t];

			return $"?{t}";
		}
	}


	class BodyPart
	{
		private Person person_;
		private int type_;
		private W.IBodyPart part_;

		public BodyPart(Person p, int type, W.IBodyPart part)
		{
			person_ = p;
			type_ = type;
			part_ = part;
		}

		public Person Person
		{
			get { return person_; }
		}

		public W.IBodyPart Sys
		{
			get { return part_; }
		}

		public bool Exists
		{
			get { return (part_ != null); }
		}

		public string Name
		{
			get { return BodyPartTypes.ToString(type_); }
		}

		public int Type
		{
			get { return type_; }
		}

		public bool Triggering
		{
			get { return part_?.Triggering ?? false; }
		}

		public Vector3 Position
		{
			get { return part_?.Position ?? Vector3.Zero; }
		}

		public Vector3 Direction
		{
			get { return part_?.Direction ?? Vector3.Zero; }
		}
	}


	class Body
	{
		private Person person_;
		private readonly List<BodyPart> all_ = new List<BodyPart>();
		private BodyPart head_;
		private BodyPart lips_;
		private BodyPart mouth_;
		private BodyPart leftBreast_;
		private BodyPart rightBreast_;
		private BodyPart labia_;
		private BodyPart vagina_;
		private BodyPart deepVagina_;
		private BodyPart deeperVagina_;

		public Body(Person p)
		{
			person_ = p;

			var parts = p.Atom.GetBodyParts();

			head_ = GetPart(parts, BodyPartTypes.Head);
			lips_ = GetPart(parts, BodyPartTypes.Lips);
			mouth_ = GetPart(parts, BodyPartTypes.Mouth);
			leftBreast_ = GetPart(parts, BodyPartTypes.LeftBreast);
			rightBreast_ = GetPart(parts, BodyPartTypes.RightBreast);
			labia_ = GetPart(parts, BodyPartTypes.Labia);
			vagina_ = GetPart(parts, BodyPartTypes.Vagina);
			deepVagina_ = GetPart(parts, BodyPartTypes.DeepVagina);
			deeperVagina_ = GetPart(parts, BodyPartTypes.DeeperVagina);
		}

		public List<BodyPart> Parts { get { return all_; } }

		public BodyPart Head { get { return head_; } }
		public BodyPart Lips { get { return lips_; } }
		public BodyPart Mouth { get { return mouth_; } }
		public BodyPart LeftBreast { get { return leftBreast_; } }
		public BodyPart RightBreast { get { return rightBreast_; } }
		public BodyPart Labia { get { return labia_; } }
		public BodyPart Vagina { get { return vagina_; } }
		public BodyPart DeepVagina { get { return deepVagina_; } }
		public BodyPart DeeperVagina { get { return deeperVagina_; } }

		public override string ToString()
		{
			string s =
				(Lips.Triggering ? "M|" : "") +
				(Mouth.Triggering ? "MM|" : "") +
				(LeftBreast.Triggering ? "LB|" : "") +
				(RightBreast.Triggering ? "RB|" : "") +
				(Labia.Triggering ? "L|" : "") +
				(Vagina.Triggering ? "V|" : "") +
				(DeepVagina.Triggering ? "VV|" : "") +
				(DeeperVagina.Triggering ? "VVV|" : "");

			if (s.EndsWith("|"))
				return s.Substring(0, s.Length - 1);
			else
				return s;
		}

		private BodyPart GetPart(List<W.IBodyPart> list, int type)
		{
			BodyPart p = null;

			for (int i = 0; i < list.Count; ++i)
			{
				if (list[i].Type == type)
				{
					p = new BodyPart(person_, type, list[i]);
					break;
				}
			}

			if (p == null)
				p = new BodyPart(person_, type, null);

			all_.Add(p);

			return p;
		}
	}


	class Excitement
	{
		private Person person_;
		private float lip_ = 0;
		private float mouth_ = 0;
		private float lBreast_ = 0;
		private float rBreast_ = 0;
		private float labia_ = 0;
		private float vagina_ = 0;
		private float deep_ = 0;
		private float deeper_ = 0;

		private float decay_ = 1;

		public Excitement(Person p)
		{
			person_ = p;
		}

		public void Update(float s)
		{
			lip_ = Check(s, person_.Body.Lips, lip_);
			mouth_ = Check(s, person_.Body.Mouth, mouth_);
			lBreast_ = Check(s, person_.Body.LeftBreast, lBreast_);
			rBreast_ = Check(s, person_.Body.RightBreast, rBreast_);
			labia_ = Check(s, person_.Body.Labia, labia_);
			vagina_ = Check(s, person_.Body.Vagina, vagina_);
			deep_ = Check(s, person_.Body.DeepVagina, deep_);
			deeper_ = Check(s, person_.Body.DeeperVagina, deeper_);
		}

		private float Check(float s, BodyPart p, float v)
		{
			if (p?.Triggering ?? false)
				return 1;
			else
				return Math.Max(v - s * decay_, 0);
		}

		public float Genitals
		{
			get
			{
				return Math.Min(1,
					labia_ * 0.005f +
					vagina_ * 0.01f +
					deep_ * 0.2f +
					deeper_ * 1);
			}
		}

		public float Mouth
		{
			get
			{
				return
					lip_ * 0.1f +
					mouth_ * 0.9f;
			}
		}

		public float Breasts
		{
			get
			{
				return
					lBreast_ * 0.5f +
					rBreast_ * 0.5f;
			}
		}

		public override string ToString()
		{
			return
				$"l={lip_:0.00} m={mouth_:0.00} " +
				$"lb={lBreast_:0.00} rb={rBreast_:0.00} " +
				$"L={labia_:0.00} V={vagina_:0.00} " +
				$"VV={deep_:0.00} VVV={deeper_:0.00}";
		}
	}


	class Gaze
	{
		private Person person_;
		private IEyes eyes_;
		private IGazer gazer_;
		private bool interested_ = false;

		public Gaze(Person p)
		{
			person_ = p;
			eyes_ = Integration.CreateEyes(p);
			gazer_ = Integration.CreateGazer(p);
		}

		public IEyes Eyes { get { return eyes_; } }
		public IGazer Gazer { get { return gazer_; } }

		public bool HasInterestingTarget
		{
			get { return interested_; }
		}

		public void Update(float s)
		{
			eyes_.Update(s);
			gazer_.Update(s);
		}

		public void LookAtDefault()
		{
			if (person_ == Cue.Instance.Player)
				LookAtNothing();
			else if (Cue.Instance.Player == null)
				LookAtCamera();
			else
				LookAt(Cue.Instance.Player);

			interested_ = false;
		}

		public void LookAtCamera()
		{
			eyes_.LookAtCamera();
			interested_ = false;
		}

		public void LookAt(IObject o, bool gaze = true)
		{
			eyes_.LookAt(o);
			gazer_.Enabled = gaze;
			interested_ = true;
		}

		public void LookAt(Vector3 p, bool gaze = true)
		{
			eyes_.LookAt(p);
			gazer_.Enabled = gaze;
			interested_ = false;
		}

		public void LookAtNothing()
		{
			eyes_.LookAtNothing();
			gazer_.Enabled = false;
			interested_ = false;
		}

		public void LookInFront()
		{
			eyes_.LookInFront();
			gazer_.Enabled = false;
			interested_ = false;
		}
	}
}
