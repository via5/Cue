using SimpleJSON;
using System.Collections.Generic;

namespace Cue
{
	public interface IObject
	{
		int ObjectIndex { get; }
		string ID { get; }
		bool IsPlayer { get; }
		bool Visible { get; set; }
		Sys.IAtom Atom { get; }
		Vector3 Position { get; set; }
		Quaternion Rotation { get; set; }
		Vector3 EyeInterest { get; }
		bool Possessed { get; }

		bool HasTrait(string name);
		string[] Traits { get; set; }

		void Destroy();
		void FixedUpdate(float s);
		void Update(float s);
		void OnPluginState(bool b);

		JSONNode ToJSON();
		void Load(JSONClass r);

		string GetParameter(string key);
	}


	public class BasicObject : IObject
	{
		public const float NoBearing = float.MaxValue;

		private readonly int objectIndex_;
		private readonly Sys.IAtom atom_;
		protected readonly Logger log_;
		private Sys.ObjectParameters ps_ = null;
		private string[] traits_ = new string[0];
		private IPossesser possesser_ = null;


		public BasicObject(int index, Sys.IAtom atom, Sys.ObjectParameters ps = null)
		{
			objectIndex_ = index;
			atom_ = atom;
			log_ = new Logger(Logger.Object, this, "");
			ps_ = ps;
			possesser_ = new AcidBubbles.Embody(atom);
		}

		public string GetParameter(string key)
		{
			if (ps_ == null)
				return "";
			else
				return ps_.Get(key);
		}

		public void Destroy()
		{
			atom_?.Destroy();
		}

		public int ObjectIndex
		{
			get { return objectIndex_; }
		}

		public Logger Log
		{
			get { return log_; }
		}

		public Sys.Vam.VamAtom VamAtom
		{
			get { return atom_ as Sys.Vam.VamAtom; }
		}

		public Sys.IAtom Atom
		{
			get { return atom_; }
		}

		public string ID
		{
			get { return atom_.ID; }
		}

		public bool IsPlayer
		{
			get { return (this == Cue.Instance.Player); }
		}

		public bool Visible
		{
			get { return atom_.Visible; }
			set { atom_.Visible = value; }
		}

		public Vector3 Position
		{
			get { return atom_.Position; }
			set { atom_.Position = value; }
		}

		public Quaternion Rotation
		{
			get { return atom_.Rotation; }
			set { atom_.Rotation = value; }
		}

		public virtual Vector3 EyeInterest
		{
			get { return Position; }
		}

		public bool Possessed
		{
			get
			{
				if (Atom.Possessed)
					return true;

				if (possesser_ != null && possesser_.Possessed)
					return true;

				return false;
			}
		}

		public bool HasTrait(string name)
		{
			for (int i = 0; i < traits_.Length; ++i)
			{
				if (traits_[i] == name)
					return true;
			}

			return false;
		}

		public string[] Traits
		{
			get
			{
				return traits_;
			}

			set
			{
				traits_ = value;
				Cue.Instance.Save();
			}
		}

		public virtual void Update(float s)
		{
			I.Start(I.UpdateObjectsAtoms);
			{
				Atom.Update(s);
			}
			I.End();
		}

		public void LateUpdate(float s)
		{
			Atom.LateUpdate(s);
		}

		public virtual void Load(JSONClass r)
		{
			var ts = new List<string>();
			foreach (JSONNode n in r["traits"].AsArray)
				ts.Add(n.Value);
			traits_ = ts.ToArray();
		}

		public virtual JSONNode ToJSON()
		{
			var o = new JSONClass();

			if (traits_.Length > 0)
			{
				var a = new JSONArray();

				for (int i = 0; i < traits_.Length; ++i)
					a.Add(new JSONData(traits_[i]));

				o.Add("traits", a);
			}

			return o;
		}

		public virtual void FixedUpdate(float s)
		{
		}

		public virtual void OnPluginState(bool b)
		{
			atom_.OnPluginState(b);
		}

		public override string ToString()
		{
			return atom_.ID;
		}
	}
}
