using System;
using System.Text;
using System.Text.RegularExpressions;
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

		public static float Clamp(float val, float min, float max)
		{
			if (val < min)
				return min;
			else if (val > max)
				return max;
			else
				return val;
		}

		public static int Clamp(int val, int min, int max)
		{
			if (val < min)
				return min;
			else if (val > max)
				return max;
			else
				return val;
		}

		public static bool IsRegex(string s)
		{
			return (s.Length >= 2 && s[0] == '/' && s[s.Length - 1] == '/');
		}

		public static Regex CreateRegex(string s)
		{
			if (s.Length >= 2 && s[0] == '/' && s[s.Length - 1] == '/')
			{
				try
				{
					return new Regex(
						s.Substring(1, s.Length - 2), RegexOptions.IgnoreCase);
				}
				catch (Exception)
				{
					return null;
				}
			}

			return null;
		}

		public static void DebugTimeThis(string what, Action a)
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

		public static Rectangle RectTransformBounds(Root root, RectTransform rt)
		{
			var r = RectTransformToScreenSpace(rt, Camera.main);
			return root.ToLocal(Rectangle.FromRect(r));
		}

		private static Rect RectTransformToScreenSpace(RectTransform transform, Camera cam, bool cutDecimals = false)
		{
			var worldCorners = new Vector3[4];
			var screenCorners = new Vector3[4];

			transform.GetWorldCorners(worldCorners);

			for (int i = 0; i < 4; i++)
			{
				screenCorners[i] = cam.WorldToScreenPoint(worldCorners[i]);
				if (cutDecimals)
				{
					screenCorners[i].x = (int)screenCorners[i].x;
					screenCorners[i].y = (int)screenCorners[i].y;
				}
			}

			var r = new Rect(screenCorners[0].x,
							screenCorners[0].y,
							screenCorners[2].x - screenCorners[0].x,
							screenCorners[2].y - screenCorners[0].y);

			var temp = r.yMin;
			r.yMin = r.yMax;
			r.yMax = temp;

			return r;
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

		public static string ToString(RectTransform rt, int indent = 0)
		{
			var i = new string(' ', indent);

			return
				i + $"rect: {rt.rect}\n" +
				i + $"offsetMin: {rt.offsetMin}\n" +
				i + $"offsetMax: {rt.offsetMax}\n" +
				i + $"anchorMin: {rt.anchorMin}\n" +
				i + $"anchorMax: {rt.anchorMax}\n" +
				i + $"anchoredPosition: {rt.anchoredPosition}\n" +
				i + $"anchoredPosition3D: {rt.anchoredPosition3D}\n" +
				i + $"sizeDelta: {rt.sizeDelta}\n" +
				i + $"pivot: {rt.pivot}\n";
		}

		public static void DumpComponents(Transform t, int indent = 0)
		{
			DumpComponents(t.gameObject, indent);
		}

		public static void DumpComponents(GameObject o, int indent = 0)
		{
			foreach (var c in o.GetComponents(typeof(Component)))
			{
				if (c == null)
				{
					Glue.LogError(new string(' ', indent * 2) + "null?");
					continue;
				}

				string s = new string(' ', indent * 2) + c.ToString();

				if (c is UnityEngine.UI.Image)
					s += $" {(c as UnityEngine.UI.Image).color}";
				else if (c is Text)
					s += $" '{(c as Text).text}'";

				Glue.LogError(s);
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
				Glue.LogError(ToString(rt, 2));

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
					Glue.LogError(ToString(rt, indent * 2));
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

	public struct Point
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

	public struct Size
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


	public struct Rectangle
	{
		public float Left, Top, Right, Bottom;

		public static Rectangle Zero
		{
			get { return new Rectangle(0, 0, 0, 0); }
		}

		public Rectangle(Rectangle r)
			: this(r.Left, r.Top, r.Right, r.Bottom)
		{
		}

		public Rectangle(Point p, Size s)
			: this(p.X, p.Y, p.X + s.Width, p.Y + s.Height)
		{
		}

		public Rectangle(Point topLeft, Point bottomRight)
			: this(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y)
		{
		}

		public Rectangle(float x, float y, Size s)
			: this(x, y, x + s.Width, y + s.Height)
		{
		}

		private Rectangle(float left, float top, float right, float bottom)
		{
			Left = left;
			Top = top;
			Right = right;
			Bottom = bottom;
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

		public static Rectangle FromRect(UnityEngine.Rect r)
		{
			return FromPoints(r.xMin, r.yMin, r.xMax, r.yMax);
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


	public struct Insets
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


	// adapted from https://github.com/mpaland/printf/blob/master/printf.c
	//
	public static partial class StringBuilderExtensions
	{
		private static int PRINTF_FTOA_BUFFER_SIZE = 32;
		private static double PRINTF_MAX_FLOAT = 1e9;
		private static uint PRINTF_DEFAULT_FLOAT_PRECISION = 6;
		private static char[] buf = new char[PRINTF_FTOA_BUFFER_SIZE];

		// powers of 10
		private static double[] pow10 = new double[]
		{
			1, 10, 100, 1000, 10000, 100000, 1000000, 10000000, 100000000, 1000000000
		};


		// internal flag definitions
		private static uint FLAGS_ZEROPAD   = (1 <<  0);
		private static uint FLAGS_LEFT      = (1 <<  1);
		private static uint FLAGS_PLUS      = (1 <<  2);
		private static uint FLAGS_SPACE     = (1 <<  3);
		private static uint FLAGS_PRECISION = (1 << 10);

		private static double DBL_MAX = double.MaxValue;

		private static char[] nan_string = new char[] { 'n', 'a', 'n' };
		private static char[] fni_minus_string = new char[] { 'f', 'n', 'i', '-' };
		private static char[] fni_plus_string = new char[] { 'f', 'n', 'i', '+' };
		private static char[] fni_string = new char[] { 'f', 'n', 'i' };

		private static void out2(char character, char[] outbuf, uint idx, uint maxlen)
		{
			if (idx < maxlen)
			{
				outbuf[idx] = character;
			}
		}

		// output the specified string in reverse, taking care of any zero-padding

		private static uint _out_rev(char[] outbuf, uint idx, uint maxlen,
			char[] buf, uint len, uint width, uint flags)
		{
		  uint start_idx = idx;

		  // pad spaces up to given width
		  if ((flags & FLAGS_LEFT) == 0 && (flags & FLAGS_ZEROPAD) == 0) {
			for (uint i = len; i < width; i++) {
			  out2(' ', outbuf, idx++, maxlen);
			}
		  }

		  // reverse string
		  while (len > 0) {
			out2(buf[--len], outbuf, idx++, maxlen);
		  }

		  // append pad spaces up to given width
		  if ((flags & FLAGS_LEFT) != 0) {
			while (idx - start_idx < width) {
			  out2(' ', outbuf, idx++, maxlen);
			}
		  }

		  return idx;
		}



		// internal ftoa for fixed decimal floating point
		private static uint _ftoa(char[] outbuf, uint idx, uint maxlen,
			double value, uint prec, uint width, uint flags)
		{
			uint len = 0;
			double diff = 0.0;

			// test for special values
			if (double.IsNaN(value))
				return _out_rev(outbuf, idx, maxlen, nan_string, 3, width, flags);
			if (value < -DBL_MAX)
				return _out_rev(outbuf, idx, maxlen, fni_minus_string, 4, width, flags);
			if (value > DBL_MAX)
				return _out_rev(outbuf, idx, maxlen, ((flags & FLAGS_PLUS) != 0) ? fni_plus_string : fni_string, ((flags & FLAGS_PLUS) != 0) ? 4U : 3U, width, flags);

			// test for very large values
			// standard printf behavior is to print EVERY whole number digit -- which could be 100s of characters overflowing your buffers == bad
			if ((value > PRINTF_MAX_FLOAT) || (value < -PRINTF_MAX_FLOAT))
			{
				return 0U;
			}

			// test for negative
			bool negative = false;
			if (value < 0)
			{
				negative = true;
				value = 0 - value;
			}

			// set default precision, if not set explicitly
			if ((flags & FLAGS_PRECISION) == 0)
			{
				prec = PRINTF_DEFAULT_FLOAT_PRECISION;
			}
			// limit precision to 9, cause a prec >= 10 can lead to overflow errors
			while ((len < PRINTF_FTOA_BUFFER_SIZE) && (prec > 9U))
			{
				buf[len++] = '0';
				prec--;
			}

			int whole = (int)value;
			double tmp = (value - whole) * pow10[prec];
			uint frac = (uint)tmp;
			diff = tmp - frac;

			if (diff > 0.5)
			{
				++frac;
				// handle rollover, e.g. case 0.99 with prec 1 is 1.0
				if (frac >= pow10[prec])
				{
					frac = 0;
					++whole;
				}
			}
			else if (diff < 0.5)
			{
			}
			else if ((frac == 0) || ((frac & 1) != 0))
			{
				// if halfway, round up if odd OR if last digit is 0
				++frac;
			}

			if (prec == 0U)
			{
				diff = value - (double)whole;
				if ((!(diff < 0.5) || (diff > 0.5)) && ((whole & 1) != 0))
				{
					// exactly 0.5 and ODD, then round up
					// 1.5 -> 2, but 2.5 -> 2
					++whole;
				}
			}
			else
			{
				uint count = prec;
				// now do fractional part, as an unsigned number
				while (len < PRINTF_FTOA_BUFFER_SIZE)
				{
					--count;
					buf[len++] = (char)(48U + (frac % 10U));
					if ((frac /= 10U) == 0)
					{
						break;
					}
				}
				// add extra 0s
				while ((len < PRINTF_FTOA_BUFFER_SIZE) && (count-- > 0U))
				{
					buf[len++] = '0';
				}
				if (len < PRINTF_FTOA_BUFFER_SIZE)
				{
					// add decimal
					buf[len++] = '.';
				}
			}

			// do whole part, number is reversed
			while (len < PRINTF_FTOA_BUFFER_SIZE)
			{
				buf[len++] = (char)(48 + (whole % 10));
				if ((whole /= 10) == 0)
				{
					break;
				}
			}

			// pad leading zeros
			if ((flags & FLAGS_LEFT) == 0 && (flags & FLAGS_ZEROPAD) != 0)
			{
				if (width != 0 && (negative || (flags & (FLAGS_PLUS | FLAGS_SPACE)) != 0))
				{
					width--;
				}
				while ((len < width) && (len < PRINTF_FTOA_BUFFER_SIZE))
				{
					buf[len++] = '0';
				}
			}

			if (len < PRINTF_FTOA_BUFFER_SIZE)
			{
				if (negative)
				{
					buf[len++] = '-';
				}
				else if ((flags & FLAGS_PLUS) != 0)
				{
					buf[len++] = '+';  // ignore the space if the '+' exists
				}
				else if ((flags & FLAGS_SPACE) != 0)
				{
					buf[len++] = ' ';
				}
			}

			return _out_rev(outbuf, idx, maxlen, buf, len, width, flags);
		}



		private static char[] outbuf = new char[32];

		public static void AppendFloat(this StringBuilder string_builder, float value, int decimals)
		{
			uint n = _ftoa(outbuf, 0, 32, (double)value, (uint)decimals, 0, FLAGS_PRECISION);

			for (uint i = 0; i < n; ++i)
				string_builder.Append(outbuf[i]);
		}
	}
}
