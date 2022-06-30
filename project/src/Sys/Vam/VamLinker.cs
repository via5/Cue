using System.Collections.Generic;
using UnityEngine;

namespace Cue.Sys.Vam
{
	public abstract class VamBodyPartRegion : IBodyPartRegion
	{
		private VamBodyPart bp_;

		protected VamBodyPartRegion(VamBodyPart bp)
		{
			bp_ = bp;
		}

		public IBodyPart BodyPart
		{
			get { return bp_; }
		}

		public abstract string FullName { get; }
		public abstract Vector3 Position { get; }
		public abstract Quaternion Rotation { get; }
	}

	public class VamColliderRegion : VamBodyPartRegion
	{
		private Collider c_;

		public VamColliderRegion(VamBodyPart bp, Collider c)
			: base(bp)
		{
			c_ = c;
		}

		public Collider Collider
		{
			get { return c_; }
		}

		public override string FullName
		{
			get { return U.FullName(c_); }
		}

		public override Vector3 Position
		{
			get { return U.FromUnity(c_.transform.position); }
		}

		public override Quaternion Rotation
		{
			get { return U.FromUnity(c_.transform.rotation); }
		}

		public override string ToString()
		{
			return $"{c_.transform.parent.name}.{c_.transform.name}";
		}
	}


	class Linker
	{
		class Link
		{
			private VamBodyPart from_;
			private VamBodyPartRegion to_;
			private Vector3 lastPos_;
			private Quaternion lastRot_;

			private GameObject parent_, self_;

			public Link(VamBodyPart from, VamBodyPartRegion to)
			{
				from_ = from;
				to_ = to;
				lastPos_ = to_.Position;
				lastRot_ = to_.Rotation;

				parent_ = new GameObject();
				self_ = new GameObject();

				parent_.transform.SetParent(Cue.Instance.VamSys.RootTransform, false);

				self_.transform.SetParent(parent_.transform, false);
				parent_.transform.position = U.ToUnity(to.Position);
				parent_.transform.rotation = U.ToUnity(to.Rotation);

				self_.transform.position = U.ToUnity(from.ControlPosition);
				self_.transform.rotation = U.ToUnity(from.ControlRotation);
			}

			public VamBodyPart From
			{
				get { return from_; }
			}

			public VamBodyPartRegion To
			{
				get { return to_; }
			}

			public void Unlink()
			{
				UnityEngine.Object.Destroy(parent_);
			}

			public void LateUpdate(float s)
			{
				Vector3 p = to_.Position;
				Quaternion q = to_.Rotation;

				var dPos = p - lastPos_;
				var dRot = U.FromUnity(U.ToUnity(q) * UnityEngine.Quaternion.Inverse(U.ToUnity(lastRot_)));

				parent_.transform.position = U.ToUnity(to_.Position);
				parent_.transform.rotation = U.ToUnity(to_.Rotation);

				from_.ControlPosition = U.FromUnity(self_.transform.position);
				from_.ControlRotation = U.FromUnity(self_.transform.rotation);

				lastPos_ = p;
				lastRot_ = q;
			}
		}

		private List<Link> linksList_ = new List<Link>();
		private Link[] linksArray_ = new Link[0];

		public void Add(VamBodyPart from, VamBodyPartRegion to)
		{
			linksList_.Add(new Link(from, to));
			linksArray_ = linksList_.ToArray();
		}

		public void Remove(VamBodyPart from)
		{
			for (int i = 0; i < linksArray_.Length; ++i)
			{
				if (linksArray_[i].From == from)
				{
					linksList_[i].Unlink();
					linksList_.RemoveAt(i);
					linksArray_ = linksList_.ToArray();
				}
			}
		}

		public bool IsLinked(VamBodyPart from)
		{
			return (GetLink(from) != null);
		}

		public bool IsLinkedTo(VamBodyPart from, VamBodyPart to)
		{
			return (GetLink(from)?.BodyPart == to);
		}

		public VamBodyPartRegion GetLink(VamBodyPart from)
		{
			for (int i = 0; i < linksArray_.Length; ++i)
			{
				if (linksArray_[i].From == from)
					return linksArray_[i].To;
			}

			return null;
		}

		public void LateUpdate(float s)
		{
			for (int i = 0; i < linksArray_.Length; ++i)
				linksArray_[i].LateUpdate(s);
		}
	}
}
