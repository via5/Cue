﻿using SimpleJSON;
using System;

namespace Cue
{
	public struct Vector3
	{
		public float X, Y, Z;

		public static Vector3 Zero
		{
			get { return new Vector3(0, 0, 0); }
		}

		public static Vector3 MaxValue
		{
			get
			{
				return new Vector3(
					float.MaxValue, float.MaxValue, float.MaxValue);
			}
		}

		public static Vector3 Abs(Vector3 v)
		{
			return new Vector3(Math.Abs(v.X), Math.Abs(v.Y), Math.Abs(v.Z));
		}

		public Vector3(float x, float y, float z)
		{
			X = x;
			Y = y;
			Z = z;
		}

		public static Vector3 FromJSON(JSONClass o, string key, bool mandatory = false)
		{
			if (!o.HasKey(key))
			{
				if (mandatory)
					throw new LoadFailed($"vector3 '{key}' is missing");
				else
					return Zero;
			}

			var a = o[key].AsArray;
			if (a == null)
				throw new LoadFailed($"vector3 '{key}' node is not an array");

			if (a.Count != 3)
				throw new LoadFailed($"vector3 '{key}' array must have 3 elements");

			float x;
			if (!float.TryParse(a[0], out x))
				throw new LoadFailed($"vector3 '{key}' x is not a number");

			float y;
			if (!float.TryParse(a[1], out y))
				throw new LoadFailed($"vector3 '{key}' is not a number");

			float z;
			if (!float.TryParse(a[2], out z))
				throw new LoadFailed($"vector3 '{key}' is not a number");

			return new Vector3(x, y, z);
		}

		public Vector3 Normalized
		{
			get
			{
				var len = Length;
				if (len == 0)
					return Zero;
				else
					return new Vector3(X / len, Y / len, Z / len);
			}
		}

		public float Length
		{
			get
			{
				return (float)Math.Sqrt(X * X + Y * Y + Z * Z);
			}
		}

		public override bool Equals(object o)
		{
			if (!(o is Vector3))
				return false;

			var r = (Vector3)o;
			return (this == r);
		}

		public override int GetHashCode()
		{
			return HashHelper.GetHashCode(X, Y, Z);
		}

		public override string ToString()
		{
			return X.ToString("0.00") + "," + Y.ToString("0.00") + "," + Z.ToString("0.00");
		}

		public static Vector3 operator +(Vector3 a, Vector3 b)
		{
			return new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
		}

		public static Vector3 operator -(Vector3 a, Vector3 b)
		{
			return new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
		}

		public static Vector3 operator *(Vector3 v, float f)
		{
			return new Vector3(v.X * f, v.Y * f, v.Z * f);
		}

		public static Vector3 operator *(float f, Vector3 v)
		{
			return new Vector3(v.X * f, v.Y * f, v.Z * f);
		}

		public static Vector3 operator *(Vector3 a, Vector3 b)
		{
			return new Vector3(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
		}

		public static Vector3 operator /(Vector3 v, float f)
		{
			return new Vector3(v.X / f, v.Y / f, v.Z / f);
		}

		public static Vector3 operator -(Vector3 v)
		{
			return new Vector3(-v.X, -v.Y, -v.Z);
		}

		public static bool operator ==(Vector3 a, Vector3 b)
		{
			return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
		}

		public static bool operator !=(Vector3 a, Vector3 b)
		{
			return !(a == b);
		}

		public static float Distance(Vector3 a, Vector3 b)
		{
			Vector3 d = new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
			return (float)Math.Sqrt(d.X * d.X + d.Y * d.Y + d.Z * d.Z);
		}

		public static float Angle(Vector3 a, Vector3 b)
		{
			// todo
			return Sys.Vam.U.Angle(a, b);
		}

		public static float Bearing(Vector3 dir)
		{
			return Angle(Zero, dir.Normalized);
		}

		public static float AngleBetweenBearings(float bearing1, float bearing2)
		{
			return ((((bearing2 - bearing1) % 360) + 540) % 360) - 180;
		}

		public static Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDistance)
		{
			// todo
			return Sys.Vam.U.MoveTowards(current, target, maxDistance);
		}

		public static Vector3 Lerp(Vector3 a, Vector3 b, float p)
		{
			p = U.Clamp(p, 0, 1);

			return new Vector3(
				a.X + (b.X - a.X) * p,
				a.Y + (b.Y - a.Y) * p,
				a.Z + (b.Z - a.Z) * p);
		}
	}

	public struct Quaternion
	{
		private UnityEngine.Quaternion q_;

		private Quaternion(UnityEngine.Quaternion q)
		{
			q_ = q;
		}

		public UnityEngine.Quaternion Internal
		{
			get { return q_; }
		}

		public static Quaternion Identity
		{
			get { return new Quaternion(UnityEngine.Quaternion.identity); }
		}

		public static Quaternion FromInternal(UnityEngine.Quaternion q)
		{
			return new Quaternion(q);
		}

		public static Quaternion FromEuler(float x, float y, float z)
		{
			return new Quaternion(UnityEngine.Quaternion.Euler(x, y, z));
		}

		public static Quaternion FromBearing(float b)
		{
			return new Quaternion(UnityEngine.Quaternion.Euler(0, b, 0));
		}

		public static Quaternion FromJSON(JSONClass o, string key, bool mandatory = false)
		{
			if (!o.HasKey(key))
			{
				if (mandatory)
					throw new LoadFailed($"quaternion '{key}' is missing");
				else
					return Identity;
			}

			var a = o[key].AsArray;
			if (a == null)
				throw new LoadFailed($"quaternion '{key}' node is not an array");

			if (a.Count != 3)
				throw new LoadFailed($"quaternion '{key}' array must have 3 elements");

			float x;
			if (!float.TryParse(a[0], out x))
				throw new LoadFailed($"quaternion '{key}' x is not a number");

			float y;
			if (!float.TryParse(a[1], out y))
				throw new LoadFailed($"quaternion '{key}' is not a number");

			float z;
			if (!float.TryParse(a[2], out z))
				throw new LoadFailed($"quaternion '{key}' is not a number");

			return FromEuler(x, y, z);
		}

		public float Bearing
		{
			get
			{
				var d = Sys.Vam.U.FromUnity(q_ * UnityEngine.Vector3.forward);
				return Vector3.Angle(Vector3.Zero, d);
			}
		}

		public Vector3 Euler
		{
			get { return Sys.Vam.U.FromUnity(q_.eulerAngles); }
		}

		public Vector3 Rotate(Vector3 v)
		{
			return Sys.Vam.U.FromUnity(q_ * Sys.Vam.U.ToUnity(v));
		}

		public Vector3 RotateInv(Vector3 v)
		{
			return Sys.Vam.U.FromUnity(UnityEngine.Quaternion.Inverse(q_) * Sys.Vam.U.ToUnity(v));
		}

		public static Quaternion Lerp(Quaternion a, Quaternion b, float f)
		{
			return Sys.Vam.U.FromUnity(UnityEngine.Quaternion.Lerp(
				Sys.Vam.U.ToUnity(a), Sys.Vam.U.ToUnity(b), f));
		}

		public static Quaternion Slerp(Quaternion a, Quaternion b, float f)
		{
			return Sys.Vam.U.FromUnity(UnityEngine.Quaternion.Slerp(
				Sys.Vam.U.ToUnity(a), Sys.Vam.U.ToUnity(b), f));
		}

		public static float NormalizeAngle(float degrees)
		{
			degrees = degrees % 360;
			if (degrees < 0)
				degrees += 360;

			return degrees;
		}

		public override string ToString()
		{
			return Euler.ToString();
		}

		public override bool Equals(object o)
		{
			if (!(o is Quaternion))
				return false;

			var r = (Quaternion)o;
			return (this == r);
		}

		public override int GetHashCode()
		{
			return q_.GetHashCode();
		}

		public static Quaternion operator *(Quaternion a, Quaternion b)
		{
			return Sys.Vam.U.FromUnity(
				Sys.Vam.U.ToUnity(a) * Sys.Vam.U.ToUnity(b));
		}

		public static bool operator ==(Quaternion a, Quaternion b)
		{
			return (a.q_ == b.q_);
		}

		public static bool operator !=(Quaternion a, Quaternion b)
		{
			return !(a == b);
		}
	}

	struct Point
	{
		public float X, Y;

		public static Point Zero()
		{
			var p = new Point();
			p.X = 0;
			p.Y = 0;
			return p;
		}

		public Point(Point p)
		{
			X = p.X;
			Y = p.Y;
		}

		public Point(float x, float y)
		{
			X = x;
			Y = y;
		}

		public static Point operator -(Point p)
		{
			return new Point(-p.X, -p.Y);
		}
	}

	struct Size
	{
		public float Width, Height;

		public static Size Zero
		{
			get
			{
				var p = new Size();
				p.Width = 0;
				p.Height = 0;
				return p;
			}
		}

		public Size(float w, float h)
		{
			Width = w;
			Height = h;
		}

		public static Size Min(Size a, Size b)
		{
			return new Size(
				Math.Min(a.Width, b.Width),
				Math.Min(a.Height, b.Height));
		}

		public static Size Max(Size a, Size b)
		{
			return new Size(
				Math.Max(a.Width, b.Width),
				Math.Max(a.Height, b.Height));
		}

		public static Size operator +(Size a, Size b)
		{
			return new Size(a.Width + b.Width, a.Height + b.Height);
		}

		public override string ToString()
		{
			return Width.ToString("0.00") + "*" + Height.ToString("0.00");
		}
	}

	struct Rectangle
	{
		public float Left, Top, Right, Bottom;

		public static Rectangle Zero
		{
			get
			{
				var r = new Rectangle();
				r.Left = 0;
				r.Top = 0;
				r.Right = 0;
				r.Bottom = 0;
				return r;
			}
		}

		public Rectangle(Rectangle r)
			: this(r.Left, r.Top, r.Size)
		{
		}

		public Rectangle(Point p, Size s)
			: this(p.X, p.Y, s)
		{
		}

		public Rectangle(float x, float y, Size s)
		{
			Left = x;
			Top = y;
			Right = Left + s.Width;
			Bottom = Top + s.Height;
		}

		static public Rectangle FromSize(float x, float y, float w, float h)
		{
			return new Rectangle(x, y, new Size(w, h));
		}

		static public Rectangle FromPoints(float x1, float y1, float x2, float y2)
		{
			return new Rectangle(x1, y1, new Size(x2 - x1, y2 - y1));
		}

		public float Width
		{
			get { return Right - Left; }
			set { Right = Left + value; }
		}

		public float Height
		{
			get { return Bottom - Top; }
			set { Bottom = Top + value; }
		}

		public Point TopLeft
		{
			get { return new Point(Left, Top); }
		}

		public Point TopRight
		{
			get { return new Point(Right, Top); }
		}

		public Point BottomLeft
		{
			get { return new Point(Left, Bottom); }
		}

		public Point BottomRight
		{
			get { return new Point(Right, Bottom); }
		}

		public Point Center
		{
			get { return new Point(Left + Width / 2, Top + Height / 2); }
		}

		public Size Size
		{
			get { return new Size(Width, Height); }
		}

		public Rectangle TranslateCopy(Point p)
		{
			var r = new Rectangle(this);
			r.Translate(p.X, p.Y);
			return r;
		}

		public Rectangle TranslateCopy(float dx, float dy)
		{
			var r = new Rectangle(this);
			r.Translate(dx, dy);
			return r;
		}

		public void Translate(Point p)
		{
			Translate(p.X, p.Y);
		}

		public void Translate(float dx, float dy)
		{
			Left += dx;
			Right += dx;

			Top += dy;
			Bottom += dy;
		}

		public void MoveTo(float x, float y)
		{
			Translate(x - Left, y - Top);
		}

		public void Deflate(Insets i)
		{
			Left += i.Left;
			Top += i.Top;
			Right -= i.Right;
			Bottom -= i.Bottom;
		}

		public void Deflate(float f)
		{
			Left += f;
			Top += f;
			Right -= f;
			Bottom -= f;
		}

		public Rectangle DeflateCopy(Insets i)
		{
			var r = new Rectangle(this);
			r.Deflate(i);
			return r;
		}

		public Rectangle DeflateCopy(float f)
		{
			var r = new Rectangle(this);
			r.Deflate(f);
			return r;
		}

		public override string ToString()
		{
			return
				"(" + Left.ToString("0.00") + "," + Top.ToString("0.00") + ")-" +
				"(" + Right.ToString("0.00") + "," + Bottom.ToString("0.00") + ")";
		}
	}

	struct Insets
	{
		public float Left, Top, Right, Bottom;

		public static Insets Zero
		{
			get
			{
				var r = new Insets();
				r.Left = 0;
				r.Top = 0;
				r.Right = 0;
				r.Bottom = 0;
				return r;
			}
		}

		public Insets(float all)
			: this(all, all, all, all)
		{
		}

		public Insets(float left, float top, float right, float bottom)
		{
			Left = left;
			Top = top;
			Right = right;
			Bottom = bottom;
		}

		public Size Size
		{
			get { return new Size(Left + Right, Top + Bottom); }
		}

		public static Insets operator +(Insets a, Insets b)
		{
			return new Insets(
				a.Left + b.Left,
				a.Top + b.Top,
				a.Right + b.Right,
				a.Bottom + b.Bottom);
		}

		public override string ToString()
		{
			return
				Left.ToString("0.00") + "," + Top.ToString("0.00") + "," +
				Right.ToString("0.00") + "," + Bottom.ToString("0.00");
		}
	}


	public struct Color
	{
		public float r, g, b, a;

		public Color(float r, float g, float b, float a)
		{
			this.r = r;
			this.g = g;
			this.b = b;
			this.a = a;
		}

		public static Color Zero
		{
			get { return new Color(0, 0, 0, 0); }
		}

		public static Color White
		{
			get { return new Color(1, 1, 1, 1); }
		}

		public static Color Black
		{
			get { return new Color(0, 0, 0, 1); }
		}

		public static Color Red
		{
			get { return new Color(1, 0, 0, 1); }
		}

		public static Color Green
		{
			get { return new Color(0, 1, 0, 1); }
		}

		public static Color Blue
		{
			get { return new Color(0, 0, 1, 1); }
		}

		public static Color Yellow
		{
			get { return new Color(1, 1, 0, 1); }
		}

		public static Color Lerp(Color a, Color b, float f)
		{
			f = U.Clamp(f, 0, 1);

			return new Color(
				a.r + (b.r - a.r) * f,
				a.g + (b.g - a.g) * f,
				a.b + (b.b - a.b) * f,
				a.a + (b.a - a.a) * f);
		}

		public static float Distance(Color c1, Color c2)
		{
			return
				Math.Abs(c2.r - c1.r) +
				Math.Abs(c2.g - c1.g) +
				Math.Abs(c2.b - c1.b) +
				Math.Abs(c2.a - c1.a);
		}

		public static bool operator ==(Color a, Color b)
		{
			return
				(a.r == b.r) &&
				(a.g == b.g) &&
				(a.b == b.b) &&
				(a.a == b.a);
		}

		public static bool operator !=(Color a, Color b)
		{
			return !(a == b);
		}

		public override bool Equals(object o)
		{
			if (!(o is Color))
				return false;

			var c = (Color)o;
			return (this == c);
		}

		public override int GetHashCode()
		{
			return HashHelper.GetHashCode(r, g, b, a);
		}

		public override string ToString()
		{
			return $"rgba({r:0.00},{g:0.00},{b:0.00},{a:0.00})";
		}
	}


	public struct Box
	{
		public Vector3 center, size;

		public Box(Vector3 center, Vector3 size)
		{
			this.center = center;
			this.size = size;
		}

		public static Box Zero
		{
			get { return new Box(Vector3.Zero, Vector3.Zero); }
		}

		public override string ToString()
		{
			return $"{center} {size}";
		}
	}


	public struct Plane
	{
		// todo
		public UnityEngine.Plane p_;

		public Plane(Vector3 a, Vector3 b, Vector3 c)
		{
			p_ = new UnityEngine.Plane(
				Sys.Vam.U.ToUnity(a),
				Sys.Vam.U.ToUnity(b),
				Sys.Vam.U.ToUnity(c));
		}

		public Plane(Vector3 point, Vector3 dir)
		{
			p_ = new UnityEngine.Plane(
				Sys.Vam.U.ToUnity(dir), Sys.Vam.U.ToUnity(point));
		}

		public Vector3 Point
		{
			get
			{
				return Sys.Vam.U.FromUnity(
					p_.ClosestPointOnPlane(UnityEngine.Vector3.zero));
			}
		}

		public Quaternion Rotation
		{
			get
			{
				return Sys.Vam.U.FromUnity(
					UnityEngine.Quaternion.LookRotation(p_.normal));
			}
		}

		public bool PointInFront(Vector3 v)
		{
			return p_.GetSide(Sys.Vam.U.ToUnity(v));
		}

		public override string ToString()
		{
			return $"{p_}";
		}
	}


	public struct Frustum
	{
		public Vector3 nearTL;
		public Vector3 nearTR;
		public Vector3 nearBL;
		public Vector3 nearBR;

		public Vector3 farTL;
		public Vector3 farTR;
		public Vector3 farBL;
		public Vector3 farBR;

		public Plane[] planes;

		// x=width, y=height z=distance
		//
		public Frustum(Vector3 near, Vector3 far)
			: this(near.X, near.Y, near.Z, far.X, far.Y, far.Z)
		{
		}

		public Frustum(
			float nearWidth, float nearHeight, float nearDistance,
			float farWidth, float farHeight, float farDistance)
		{
			var nearOffset = new Vector3(
				-nearWidth / 2, nearHeight / 2, nearDistance);

			var farOffset = new Vector3(
				-farWidth / 2, farHeight / 2, farDistance);

			nearTL = nearOffset;
			nearTR = nearOffset + new Vector3(nearWidth, 0, 0);
			nearBL = nearOffset + new Vector3(0, -nearHeight, 0);
			nearBR = nearOffset + new Vector3(nearWidth, -nearHeight, 0);

			farTL = farOffset;
			farTR = farOffset + new Vector3(farWidth, 0, 0);
			farBL = farOffset + new Vector3(0, -farHeight, 0);
			farBR = farOffset + new Vector3(farWidth, -farHeight, 0);

			planes = new Plane[]
			{
				new Plane(farTL,  nearTL, nearBL),  // left
				new Plane(nearTR, farTR,  farBR),   // right
				new Plane(nearBL, nearBR, farBR),   // down
				new Plane(nearTL, farTL,  farTR),   // up
				new Plane(nearTL, nearTR, nearBR),  // near
				new Plane(farTR,  farTL,  farBL)    // far
			};
		}

		public void UpdatePlanes()
		{
			planes = new Plane[]
			{
				new Plane(farTL,  nearTL, nearBL),  // left
				new Plane(nearTR, farTR,  farBR),   // right
				new Plane(nearBL, nearBR, farBR),   // down
				new Plane(nearTL, farTL,  farTR),   // up
				new Plane(nearTL, nearTR, nearBR),  // near
				new Plane(farTR,  farTL,  farBL)    // far
			};
		}

		public static Frustum Zero
		{
			get { return new Frustum(Vector3.Zero, Vector3.Zero); }
		}

		public bool Empty
		{
			get
			{
				return
					NearSize() == Vector3.Zero &&
					FarSize() == Vector3.Zero;
			}
		}

		public Frustum[] Split(int xcount, int ycount)
		{
			var fs = new Frustum[xcount * ycount];

			for (int x = 0; x < xcount; ++x)
			{
				for (int y = 0; y < ycount; ++y)
				{
					var nearWidth = NearSize().X / xcount;
					var nearHeight = NearSize().Y / ycount;
					var nearOffset = new Vector3(
						-NearSize().X / 2 + x * nearWidth + nearWidth/2,
						NearSize().Y / 2 - y * nearHeight - nearHeight/2,
						0);

					var farWidth = FarSize().X / xcount;
					var farHeight = FarSize().Y / ycount;
					var farOffset = new Vector3(
						-FarSize().X / 2 + x * farWidth + farWidth/2,
						FarSize().Y / 2 - y * farHeight - farHeight/2,
						0);

					var f = new Frustum(
						nearWidth, nearHeight, nearTL.Z,
						farWidth, farHeight, farTL.Z);

					f.nearTL += nearOffset;
					f.nearTR += nearOffset;
					f.nearBL += nearOffset;
					f.nearBR += nearOffset;

					f.farTL += farOffset;
					f.farTR += farOffset;
					f.farBL += farOffset;
					f.farBR += farOffset;

					f.UpdatePlanes();

					fs[y * xcount + x] = f;
				}
			}

			return fs;
		}

		public Vector3 NearCenter()
		{
			return new Vector3(
				nearTL.X + Math.Abs(nearBR.X - nearTL.X) / 2,
				nearTL.Y - Math.Abs(nearBR.Y - nearTL.Y) / 2,
				nearTL.Z + Math.Abs(nearBR.Z - nearTL.Z) / 2);
		}

		public Vector3 NearSize()
		{
			return new Vector3(
				Math.Abs(nearBR.X - nearTL.X),
				Math.Abs(nearBR.Y - nearTL.Y),
				Math.Abs(nearBR.Z - nearTL.Z));
		}

		public Vector3 FarCenter()
		{
			return new Vector3(
				farTL.X + Math.Abs(farBR.X - farTL.X) / 2,
				farTL.Y - Math.Abs(farBR.Y - farTL.Y) / 2,
				farTL.Z + Math.Abs(farBR.Z - farTL.Z) / 2);
		}

		public Vector3 FarSize()
		{
			return new Vector3(
				Math.Abs(farBR.X - farTL.X),
				Math.Abs(farBR.Y - farTL.Y),
				Math.Abs(farBR.Z - farTL.Z));
		}

		public Vector3 RandomPoint()
		{
			var nearWidth = nearTR.X - nearTL.X;
			var nearHeight = nearBL.Y - nearTL.Y;
			var farWidth = farTR.X - farTL.X;
			var farHeight = farBL.Y - farTL.Y;

			var nearPoint = new Vector3(
				nearTL.X + U.RandomFloat(0, nearWidth),
				nearTL.Y - U.RandomFloat(0, nearHeight),
				nearTL.Z);

			var farPoint = new Vector3(
				farTL.X + U.RandomFloat(0, farWidth),
				farTL.Y + U.RandomFloat(0, farHeight),
				farTL.Z);

			var d = Vector3.Distance(nearPoint, farPoint);
			var rd = U.RandomFloat(0, d);

			return nearPoint + (farPoint - nearPoint).Normalized * rd;
		}

		public bool TestPlanesAABB(Box box)
		{
			// todo
			return Sys.Vam.U.TestPlanesAABB(planes, box);
		}
	}
}
