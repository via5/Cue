using System;

namespace Cue
{
	struct Vector3
	{
		public float X, Y, Z;

		public static Vector3 Zero
		{
			get
			{
				var v = new Vector3();
				v.X = 0;
				v.Y = 0;
				v.Z = 0;
				return v;
			}
		}

		public Vector3(float x, float y, float z)
		{
			X = x;
			Y = y;
			Z = z;
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

		public static Vector3 operator *(Vector3 a, Vector3 b)
		{
			return new Vector3(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
		}

		public static float Distance(Vector3 a, Vector3 b)
		{
			// todo
			return W.VamU.Distance(a, b);
		}

		public static float Angle(Vector3 a, Vector3 b)
		{
			// todo
			return W.VamU.Angle(a, b);
		}

		public static float Bearing(Vector3 dir)
		{
			return Angle(Zero, dir);
		}

		public static Vector3 Rotate(float x, float y, float z)
		{
			// todo
			return W.VamU.Rotate(x, y, z);
		}

		public static Vector3 Rotate(Vector3 v, float bearing)
		{
			// todo
			return W.VamU.Rotate(v, bearing);
		}

		public static float NormalizeAngle(float degrees)
		{
			degrees = degrees % 360;
			if (degrees < 0)
				degrees += 360;

			return degrees;
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


	struct Color
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
	}
}
