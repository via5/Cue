using System;
using UnityEngine;
using UnityEngine.UI;

namespace VUI
{
	class WidgetBorderGraphics : MaskableGraphic
	{
		private Insets borders_ = new Insets();
		private Color color_ = new Color(0, 0, 0, 0);

		public WidgetBorderGraphics()
		{
			raycastTarget = false;
		}

		public Insets Borders
		{
			get
			{
				return borders_;
			}

			set
			{
				borders_ = value;
				SetVerticesDirty();
			}
		}

		public Color Color
		{
			get
			{
				return color_;
			}

			set
			{
				color_ = value;
				SetVerticesDirty();
			}
		}

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			vh.Clear();

			if (!gameObject.activeSelf)
				return;

			var rt = rectTransform;

			// left
			Line(vh,
				new Point(rt.rect.xMin, -rt.rect.yMin),
				new Point(rt.rect.xMin + borders_.Left, -rt.rect.yMax),
				color_);

			// top
			Line(vh,
				new Point(rt.rect.xMin, -rt.rect.yMin),
				new Point(rt.rect.xMax, -rt.rect.yMin - borders_.Top),
				color_);

			// right
			Line(vh,
				new Point(rt.rect.xMax - borders_.Right, -rt.rect.yMin),
				new Point(rt.rect.xMax, -rt.rect.yMax),
				color_);

			// bottom
			Line(vh,
				new Point(rt.rect.xMin, -rt.rect.yMax + borders_.Bottom),
				new Point(rt.rect.xMax, -rt.rect.yMax),
				color_);
		}

		private void Line(VertexHelper vh, Point a, Point b, Color c)
		{
			Color32 c32 = c;
			var i = vh.currentVertCount;

			vh.AddVert(new Vector3(a.X, a.Y), c32, new Vector2(0f, 0f));
			vh.AddVert(new Vector3(a.X, b.Y), c32, new Vector2(0f, 1f));
			vh.AddVert(new Vector3(b.X, b.Y), c32, new Vector2(1f, 1f));
			vh.AddVert(new Vector3(b.X, a.Y), c32, new Vector2(1f, 0f));

			vh.AddTriangle(i + 0, i + 1, i + 2);
			vh.AddTriangle(i + 2, i + 3, i + 0);
		}
	}


	class Bits
	{
		public static bool IsSet(int flag, int bits)
		{
			return ((flag & bits) == bits);
		}
	}

	public class IgnoreFlag
	{
		private bool ignore_ = false;

		public static implicit operator bool(IgnoreFlag f)
		{
			return f.ignore_;
		}

		public void Do(Action a)
		{
			try
			{
				ignore_ = true;
				a();
			}
			finally
			{
				ignore_ = false;
			}
		}
	}

	public class ScopedFlag : IDisposable
	{
		private readonly Action<bool> a_;
		private readonly bool start_;

		public ScopedFlag(Action<bool> a, bool start = true)
		{
			a_ = a;
			start_ = start;

			a_(start_);
		}

		public void Dispose()
		{
			a_(!start_);
		}
	}

	public static class HashHelper
	{
		public static int GetHashCode<T1, T2>(T1 arg1, T2 arg2)
		{
			unchecked
			{
				return 31 * arg1.GetHashCode() + arg2.GetHashCode();
			}
		}

		public static int GetHashCode<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3)
		{
			unchecked
			{
				int hash = arg1.GetHashCode();
				hash = 31 * hash + arg2.GetHashCode();
				return 31 * hash + arg3.GetHashCode();
			}
		}

		public static int GetHashCode<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
		{
			unchecked
			{
				int hash = arg1.GetHashCode();
				hash = 31 * hash + arg2.GetHashCode();
				hash = 31 * hash + arg3.GetHashCode();
				return 31 * hash + arg4.GetHashCode();
			}
		}
	}

	class Utilities
	{
		public const string AddSymbol = "+";
		public const string CloneSymbol = "+*";
		public const string CloneZeroSymbol = "+*0";
		public const string CloneSyncSymbol = "+*S";
		public const string RemoveSymbol = "\x2013";  // en dash
		public const string UpArrow = "\x25b2";
		public const string DownArrow = "\x25bc";

		// no emojis
		public const string LockedSymbol = "U";//"\U0001F512";
		public const string UnlockedSymbol = "L";//"\U0001F513";

		public static void Handler(Action a)
		{
			try
			{
				a();
			}
			catch (Exception e)
			{
				Glue.LogError(e.ToString());
			}
		}

		public static T Clamp<T>(T val, T min, T max)
			where T : IComparable<T>
		{
			if (val.CompareTo(min) < 0)
				return min;
			else if (val.CompareTo(max) > 0)
				return max;
			else
				return val;
		}

		public static void TimeThis(string what, Action a)
		{
			var start = Time.realtimeSinceStartup;
			a();
			var end = Time.realtimeSinceStartup;
			Glue.LogError(what + ": " + (end - start) + "s");
		}

		public static void BringToTop(GameObject o)
		{
			BringToTop(o.transform);
		}

		public static void BringToTop(Transform t)
		{
			while (t != null)
			{
				t.SetAsLastSibling();
				t = t.transform.parent;
			}
		}

		public static int[] WordRange(string text, int caret)
		{
			if (text.Length == 0)
				return new int[2] { 0, 0 };

			int begin = caret;

			if (caret >= text.Length)
			{
				// double-clicked past the end of the text
				--begin;
			}


			{
				var startedOnWs = char.IsWhiteSpace(text, begin);

				while (begin > 0)
				{
					--begin;

					var ws = char.IsWhiteSpace(text, begin);
					if (ws != startedOnWs)
					{
						++begin;
						break;
					}
				}
			}


			int end = caret;

			if (end >= text.Length)
			{
				// double-clicked past the end of the text
			}
			else
			{
				var startedOnWs = char.IsWhiteSpace(text, end);

				while (end < text.Length)
				{
					++end;

					if (end >= text.Length)
						break;

					var ws = char.IsWhiteSpace(text, end);
					if (ws != startedOnWs)
					{
						break;
					}
				}
			}

			return new int[2] { begin, end };
		}

		public static void SetRectTransform(RectTransform rt, Rectangle r)
		{
			rt.offsetMin = new Vector2((int)r.Left, (int)r.Top);
			rt.offsetMax = new Vector2((int)r.Right, (int)r.Bottom);
			rt.anchorMin = new Vector2(0, 1);
			rt.anchorMax = new Vector2(0, 1);
			rt.anchoredPosition = new Vector2(r.Center.X, -r.Center.Y);
		}

		public static void SetRectTransform(Component c, Rectangle r)
		{
			SetRectTransform(c.GetComponent<RectTransform>(), r);
		}

		public static void SetRectTransform(GameObject o, Rectangle r)
		{
			SetRectTransform(o.GetComponent<RectTransform>(), r);
		}

		public static GameObject FindChildRecursive(Component c, string name)
		{
			return FindChildRecursive(c.gameObject, name);
		}

		public static GameObject FindChildRecursive(GameObject o, string name)
		{
			if (o == null)
				return null;

			if (o.name == name)
				return o;

			foreach (Transform c in o.transform)
			{
				var r = FindChildRecursive(c.gameObject, name);
				if (r != null)
					return r;
			}

			return null;
		}

		public static void DumpComponents(GameObject o, int indent = 0)
		{
			foreach (var c in o.GetComponents(typeof(Component)))
			{
				if (c == null)
					Glue.LogError(new string(' ', indent * 2) + "null?");
				else
					Glue.LogError(new string(' ', indent * 2) + c.ToString());
			}
		}

		public static void DumpComponentsAndUp(Component c)
		{
			DumpComponentsAndUp(c.gameObject);
		}

		public static void DumpComponentsAndUp(GameObject o)
		{
			Glue.LogError(o.name);

			var rt = o.GetComponent<RectTransform>();
			if (rt != null)
			{
				Glue.LogError("  rect: " + rt.rect.ToString());
				Glue.LogError("  offsetMin: " + rt.offsetMin.ToString());
				Glue.LogError("  offsetMax: " + rt.offsetMax.ToString());
				Glue.LogError("  anchorMin: " + rt.anchorMin.ToString());
				Glue.LogError("  anchorMax: " + rt.anchorMax.ToString());
				Glue.LogError("  anchorPos: " + rt.anchoredPosition.ToString());
			}

			DumpComponents(o);
			Glue.LogError("---");

			var parent = o?.transform?.parent?.gameObject;
			if (parent != null)
				DumpComponentsAndUp(parent);
		}

		public static void DumpComponentsAndDown(Component c, bool dumpRt = false)
		{
			DumpComponentsAndDown(c.gameObject, dumpRt);
		}

		public static void DumpComponentsAndDown(
			GameObject o, bool dumpRt = false, int indent = 0)
		{
			Glue.LogError(new string(' ', indent * 2) + o.name);

			if (dumpRt)
			{
				var rt = o.GetComponent<RectTransform>();
				if (rt != null)
				{
					Glue.LogError(new string(' ', indent * 2) + "->rect: " + rt.rect.ToString());
					Glue.LogError(new string(' ', indent * 2) + "->offsetMin: " + rt.offsetMin.ToString());
					Glue.LogError(new string(' ', indent * 2) + "->offsetMax: " + rt.offsetMax.ToString());
					Glue.LogError(new string(' ', indent * 2) + "->anchorMin: " + rt.anchorMin.ToString());
					Glue.LogError(new string(' ', indent * 2) + "->anchorMax: " + rt.anchorMax.ToString());
					Glue.LogError(new string(' ', indent * 2) + "->anchorPos: " + rt.anchoredPosition.ToString());
				}
			}

			DumpComponents(o, indent);

			foreach (Transform c in o.transform)
				DumpComponentsAndDown(c.gameObject, dumpRt, indent + 1);
		}

		public static void DumpRectsAndDown(Component c)
		{
			DumpRectsAndDown(c.gameObject);
		}

		public static void DumpRectsAndDown(GameObject o, int indent = 0)
		{
			if (o == null)
				return;

			var rt = o.GetComponent<RectTransform>();

			if (rt == null)
			{
				Glue.LogError(new string(' ', indent * 2) + o.name);
			}
			else
			{
				Glue.LogError(new string(' ', indent * 2) + o.name + " " +
					"omin=" + rt.offsetMin.ToString() + " " +
					"omax=" + rt.offsetMax.ToString() + " " +
					"amin=" + rt.anchorMin.ToString() + " " +
					"amxn=" + rt.anchorMax.ToString() + " " +
					"ap=" + rt.anchoredPosition.ToString());
			}

			foreach (Transform c in o.transform)
				DumpRectsAndDown(c.gameObject, indent + 1);
		}

		public static void DumpChildren(GameObject o, int indent = 0)
		{
			Glue.LogError(new string(' ', indent * 2) + o.name);

			foreach (Transform c in o.transform)
				DumpChildren(c.gameObject, indent + 1);
		}
	}

	struct Point
	{
		public float X, Y;

		public static Point Zero
		{
			get { return new Point(0, 0); }
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

		public override int GetHashCode()
		{
			return HashHelper.GetHashCode(X, Y);
		}

		public override bool Equals(object obj)
		{
			if (obj is Point)
				return (this == (Point)obj);
			else
				return false;
		}

		public override string ToString()
		{
			return $"{X} {Y}";
		}

		public static Point operator -(Point a, Point b)
		{
			return new Point(a.X - b.X, a.Y - b.Y);
		}

		public static bool operator ==(Point a, Point b)
		{
			return (a.X == b.X) && (a.Y == b.Y);
		}

		public static bool operator !=(Point a, Point b)
		{
			return !(a == b);
		}
	}

	struct Size
	{
		public float Width, Height;

		public static Size Zero
		{
			get { return new Size(0, 0); }
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

		public static Size operator *(Size s, float f)
		{
			return new Size(s.Width * f, s.Height * f);
		}

		public override int GetHashCode()
		{
			return HashHelper.GetHashCode(Width, Height);
		}

		public override bool Equals(object obj)
		{
			if (obj is Size)
				return (this == (Size)obj);
			else
				return false;
		}

		public static bool operator ==(Size a, Size b)
		{
			return (a.Width == b.Width) && (a.Height == b.Height);
		}

		public static bool operator !=(Size a, Size b)
		{
			return !(a == b);
		}

		public override string ToString()
		{
			return Width.ToString() + "*" + Height.ToString();
		}
	}


	struct Rectangle
	{
		public float Left, Top, Right, Bottom;

		public static Rectangle Zero
		{
			get { return FromPoints(0, 0, 0, 0); }
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

		public bool Contains(Point p)
		{
			return
				p.X >= Left && p.X <= Right &&
				p.Y >= Top && p.Y <= Bottom;
		}

		public UnityEngine.Rect ToRect()
		{
			return new Rect(Left, Top, Width, Height);
		}

		public override string ToString()
		{
			return
				"(" + Left.ToString() + "," + Top.ToString() + ")-" +
				"(" + Right.ToString() + "," + Bottom.ToString() + ")";
		}
	}


	struct Insets
	{
		public float Left, Top, Right, Bottom;

		public static Insets Zero
		{
			get { return new Insets(0); }
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

		public bool Empty
		{
			get { return Left == 0 && Top == 0 && Right == 0 && Bottom == 0; }
		}

		public static Insets operator +(Insets a, Insets b)
		{
			return new Insets(
				a.Left + b.Left,
				a.Top + b.Top,
				a.Right + b.Right,
				a.Bottom + b.Bottom);
		}

		public override int GetHashCode()
		{
			return HashHelper.GetHashCode(Left, Top, Right, Bottom);
		}

		public override bool Equals(object obj)
		{
			if (obj is Insets)
				return (this == (Insets)obj);
			else
				return false;
		}

		public static bool operator ==(Insets a, Insets b)
		{
			return (
				a.Left == b.Left &&
				a.Top == b.Top &&
				a.Right == b.Right &&
				a.Bottom == b.Bottom);
		}

		public static bool operator !=(Insets a, Insets b)
		{
			return !(a == b);
		}

		public override string ToString()
		{
			return
				Left.ToString() + "," + Top.ToString() + "," +
				Right.ToString() + "," + Bottom.ToString();
		}
	}
}
