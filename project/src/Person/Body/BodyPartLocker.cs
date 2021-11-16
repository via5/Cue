using System;
using System.Collections.Generic;
using System.Text;

namespace Cue
{
	class BodyPartLocker
	{
		private readonly BodyPart bp_;
		private List<BodyPartLock> locks_ = new List<BodyPartLock>();

		public BodyPartLocker(BodyPart p)
		{
			bp_ = p;
		}

		public BodyPartLock LockInternal(
			int lockType, string why, bool strong, ulong key)
		{
			for (int i = 0; i < locks_.Count; ++i)
			{
				if (locks_[i].Prevents(lockType, BodyPartLock.NoKey))
					return null;
			}

			for (int i = 0; i < locks_.Count; ++i)
			{
				if (locks_[i].IsWeakFor(lockType))
					locks_[i].SetExpired();
			}

			var lk = new BodyPartLock(bp_, lockType, strong, why, key);
			locks_.Add(lk);
			return lk;
		}

		public BodyPartLock Lock(int lockType, string why, bool strong = true)
		{
			return LockInternal(lockType, why, strong, BodyPartLock.NextKey());
		}

		public void UnlockInternal(BodyPartLock lk)
		{
			for (int i = 0; i < locks_.Count; ++i)
			{
				if (locks_[i] == lk)
				{
					locks_.RemoveAt(i);
					return;
				}
			}

			bp_.Log.Error($"can't unlock {lk}, not in list");
		}

		public bool LockedFor(int lockType, ulong key = BodyPartLock.NoKey)
		{
			for (int i = 0; i < locks_.Count; ++i)
			{
				if (locks_[i].Prevents(lockType, key))
					return true;
			}

			return false;
		}

		public string DebugLockString()
		{
			int lockType = 0;

			for (int i = 0; i < locks_.Count; ++i)
			{
				if (!locks_[i].Expired)
					lockType |= locks_[i].Type;
			}

			if (lockType == 0 && locks_.Count > 0)
				return "?";

			return BodyPartLock.TypeToString(lockType);
		}

		public void DebugAllLocks(List<string> list)
		{
			for (int i = 0; i < locks_.Count; ++i)
				list.Add(locks_[i].ToDetailedString());
		}
	}


	class BodyPartLock
	{
		public const ulong NoKey = 0;

		public const int NoLock = 0x00;
		public const int Move = 0x01;
		public const int Morph = 0x02;
		public const int Anim = Move | Morph;

		private static ulong nextKey_ = 1;

		private BodyPart bp_;
		private int type_;
		private bool strong_;
		private bool expired_ = false;
		private string why_;
		private ulong key_;

		public BodyPartLock(BodyPart bp, int type, bool strong, string why, ulong key)
		{
			bp_ = bp;
			type_ = type;
			strong_ = strong;
			why_ = why;
			key_ = key;
		}

		public static ulong NextKey()
		{
			return nextKey_++;
		}

		public static BodyPartLock[] LockMany(
			Person p, int[] bodyParts, int lockType, string why, bool strong = true)
		{
			List<BodyPartLock> list = null;
			bool failed = false;

			try
			{
				ulong key = BodyPartLock.NextKey();

				for (int i = 0; i < bodyParts.Length; ++i)
				{
					var lk = p.Body.Get(bodyParts[i]).Locker.LockInternal(
						lockType, why, strong, key);

					if (lk == null)
					{
						failed = true;
						break;
					}

					if (list == null)
						list = new List<BodyPartLock>();

					list.Add(lk);
				}
			}
			catch (Exception e)
			{
				p.Body.Log.Error(
					$"exception while locking body parts, " +
					$"unlocking all; was locking:");

				for (int i = 0; i < bodyParts.Length; ++i)
					p.Body.Log.Error($"  - {BP.ToString(bodyParts[i])}");

				p.Body.Log.Error($"lockType={lockType}, why={why}, strong={strong}");
				p.Body.Log.Error($"exception:");
				p.Body.Log.Error(e.ToString());

				failed = true;
			}


			if (failed)
			{
				if (list != null)
				{
					for (int i = 0; i < list.Count; ++i)
						list[i].Unlock();
				}

				return null;
			}


			return list?.ToArray();
		}

		public int Type
		{
			get { return type_; }
		}

		public ulong Key
		{
			get { return key_; }
		}

		public bool Prevents(int type, ulong key)
		{
			if (key_ != key)
			{
				if (Bits.IsAnySet(type_, type) && strong_)
					return true;
			}

			return false;
		}

		public bool IsWeakFor(int type)
		{
			if (Bits.IsAnySet(type_, type) && !strong_)
				return true;
			else
				return false;
		}

		public bool Expired
		{
			get { return expired_; }
		}

		public void SetExpired()
		{
			expired_ = true;
		}

		public void Unlock()
		{
			bp_.Locker.UnlockInternal(this);
		}

		public string Why
		{
			get { return Why; }
		}

		public static string TypeToString(int type)
		{
			if (type == 0)
				return "";

			var list = new List<string>();

			if (Bits.IsSet(type, Move))
				list.Add("move");

			if (Bits.IsSet(type, Morph))
				list.Add("morph");

			if (list.Count == 0)
				return $"?{type}";

			return string.Join("|", list.ToArray());
		}

		public string ToDetailedString()
		{
			string s = "";

			s += $"{bp_.Name}: {TypeToString(type_)}, ";
			s += $"{(strong_ ? "strong" : "weak")}";

			if (expired_)
				s += ", expired";

			s += $" ({ why_})";

			if (key_ != NoKey)
				s += $" k={key_}";
			else
				s += $" k=X";

			return s;
		}

		public override string ToString()
		{
			return $"{bp_}/{TypeToString(type_)}";
		}
	}
}
