using System;
using System.Text;
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
				i + $"anchorPos: {rt.anchoredPosition}\n";
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

				if (c is Image)
				{
					s += $" {(c as Image).color}";
				}

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


	// from https://www.gavpugh.com/2010/04/05/xnac-a-garbage-free-stringbuilder-format-method/
	//
	public static partial class StringBuilderExtensions
	{
		// These digits are here in a static array to support hex with simple, easily-understandable code.
		// Since A-Z don't sit next to 0-9 in the ascii table.
		private static readonly char[] ms_digits = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

		private static readonly uint ms_default_decimal_places = 5; //< Matches standard .NET formatting dp's
		private static readonly char ms_default_pad_char = '0';

		//! Convert a given unsigned integer value to a string and concatenate onto the stringbuilder. Any base value allowed.
		public static StringBuilder Concat(this StringBuilder string_builder, uint uint_val, uint pad_amount, char pad_char, uint base_val)
		{
			Debug.Assert(pad_amount >= 0);
			Debug.Assert(base_val > 0 && base_val <= 16);

			// Calculate length of integer when written out
			uint length = 0;
			uint length_calc = uint_val;

			do
			{
				length_calc /= base_val;
				length++;
			}
			while (length_calc > 0);

			// Pad out space for writing.
			string_builder.Append(pad_char, (int)Math.Max(pad_amount, length));

			int strpos = string_builder.Length;

			// We're writing backwards, one character at a time.
			while (length > 0)
			{
				strpos--;

				// Lookup from static char array, to cover hex values too
				string_builder[strpos] = ms_digits[uint_val % base_val];

				uint_val /= base_val;
				length--;
			}

			return string_builder;
		}

		//! Convert a given unsigned integer value to a string and concatenate onto the stringbuilder. Assume no padding and base ten.
		public static StringBuilder Concat(this StringBuilder string_builder, uint uint_val)
		{
			string_builder.Concat(uint_val, 0, ms_default_pad_char, 10);
			return string_builder;
		}

		//! Convert a given unsigned integer value to a string and concatenate onto the stringbuilder. Assume base ten.
		public static StringBuilder Concat(this StringBuilder string_builder, uint uint_val, uint pad_amount)
		{
			string_builder.Concat(uint_val, pad_amount, ms_default_pad_char, 10);
			return string_builder;
		}

		//! Convert a given unsigned integer value to a string and concatenate onto the stringbuilder. Assume base ten.
		public static StringBuilder Concat(this StringBuilder string_builder, uint uint_val, uint pad_amount, char pad_char)
		{
			string_builder.Concat(uint_val, pad_amount, pad_char, 10);
			return string_builder;
		}

		//! Convert a given signed integer value to a string and concatenate onto the stringbuilder. Any base value allowed.
		public static StringBuilder Concat(this StringBuilder string_builder, int int_val, uint pad_amount, char pad_char, uint base_val)
		{
			Debug.Assert(pad_amount >= 0);
			Debug.Assert(base_val > 0 && base_val <= 16);

			// Deal with negative numbers
			if (int_val < 0)
			{
				string_builder.Append('-');
				uint uint_val = uint.MaxValue - ((uint)int_val) + 1; //< This is to deal with Int32.MinValue
				string_builder.Concat(uint_val, pad_amount, pad_char, base_val);
			}
			else
			{
				string_builder.Concat((uint)int_val, pad_amount, pad_char, base_val);
			}

			return string_builder;
		}

		//! Convert a given signed integer value to a string and concatenate onto the stringbuilder. Assume no padding and base ten.
		public static StringBuilder Concat(this StringBuilder string_builder, int int_val)
		{
			string_builder.Concat(int_val, 0, ms_default_pad_char, 10);
			return string_builder;
		}

		//! Convert a given signed integer value to a string and concatenate onto the stringbuilder. Assume base ten.
		public static StringBuilder Concat(this StringBuilder string_builder, int int_val, uint pad_amount)
		{
			string_builder.Concat(int_val, pad_amount, ms_default_pad_char, 10);
			return string_builder;
		}

		//! Convert a given signed integer value to a string and concatenate onto the stringbuilder. Assume base ten.
		public static StringBuilder Concat(this StringBuilder string_builder, int int_val, uint pad_amount, char pad_char)
		{
			string_builder.Concat(int_val, pad_amount, pad_char, 10);
			return string_builder;
		}

		//! Convert a given float value to a string and concatenate onto the stringbuilder
		public static StringBuilder Concat(this StringBuilder string_builder, float float_val, uint decimal_places, uint pad_amount, char pad_char)
		{
			Debug.Assert(pad_amount >= 0);

			if (decimal_places == 0)
			{
				// No decimal places, just round up and print it as an int

				// Agh, Math.Floor() just works on doubles/decimals. Don't want to cast! Let's do this the old-fashioned way.
				int int_val;
				if (float_val >= 0.0f)
				{
					// Round up
					int_val = (int)(float_val + 0.5f);
				}
				else
				{
					// Round down for negative numbers
					int_val = (int)(float_val - 0.5f);
				}

				string_builder.Concat(int_val, pad_amount, pad_char, 10);
			}
			else
			{
				int int_part = (int)float_val;

				// First part is easy, just cast to an integer
				string_builder.Concat(int_part, pad_amount, pad_char, 10);

				// Decimal point
				string_builder.Append('.');

				// Work out remainder we need to print after the d.p.
				float remainder = Math.Abs(float_val - int_part);

				// Multiply up to become an int that we can print
				do
				{
					remainder *= 10;
					decimal_places--;
				}
				while (decimal_places > 0);

				// Round up. It's guaranteed to be a positive number, so no extra work required here.
				remainder += 0.5f;

				// All done, print that as an int!
				string_builder.Concat((uint)remainder, 0, '0', 10);
			}
			return string_builder;
		}

		//! Convert a given float value to a string and concatenate onto the stringbuilder. Assumes five decimal places, and no padding.
		public static StringBuilder Concat(this StringBuilder string_builder, float float_val)
		{
			string_builder.Concat(float_val, ms_default_decimal_places, 0, ms_default_pad_char);
			return string_builder;
		}

		//! Convert a given float value to a string and concatenate onto the stringbuilder. Assumes no padding.
		public static StringBuilder Concat(this StringBuilder string_builder, float float_val, uint decimal_places)
		{
			string_builder.Concat(float_val, decimal_places, 0, ms_default_pad_char);
			return string_builder;
		}

		//! Convert a given float value to a string and concatenate onto the stringbuilder.
		public static StringBuilder Concat(this StringBuilder string_builder, float float_val, uint decimal_places, uint pad_amount)
		{
			string_builder.Concat(float_val, decimal_places, pad_amount, ms_default_pad_char);
			return string_builder;
		}

		//! Concatenate a formatted string with arguments
		public static StringBuilder ConcatFormat<A>(this StringBuilder string_builder, String format_string, A arg1)
			where A : IConvertible
		{
			return string_builder.ConcatFormat<A, int, int, int>(format_string, arg1, 0, 0, 0);
		}

		//! Concatenate a formatted string with arguments
		public static StringBuilder ConcatFormat<A, B>(this StringBuilder string_builder, String format_string, A arg1, B arg2)
			where A : IConvertible
			where B : IConvertible
		{
			return string_builder.ConcatFormat<A, B, int, int>(format_string, arg1, arg2, 0, 0);
		}

		//! Concatenate a formatted string with arguments
		public static StringBuilder ConcatFormat<A, B, C>(this StringBuilder string_builder, String format_string, A arg1, B arg2, C arg3)
			where A : IConvertible
			where B : IConvertible
			where C : IConvertible
		{
			return string_builder.ConcatFormat<A, B, C, int>(format_string, arg1, arg2, arg3, 0);
		}

		//! Concatenate a formatted string with arguments
		public static StringBuilder ConcatFormat<A, B, C, D>(this StringBuilder string_builder, String format_string, A arg1, B arg2, C arg3, D arg4)
			where A : IConvertible
			where B : IConvertible
			where C : IConvertible
			where D : IConvertible
		{
			int verbatim_range_start = 0;

			for (int index = 0; index < format_string.Length; index++)
			{
				if (format_string[index] == '{')
				{
					// Formatting bit now, so make sure the last block of the string is written out verbatim.
					if (verbatim_range_start < index)
					{
						// Write out unformatted string portion
						string_builder.Append(format_string, verbatim_range_start, index - verbatim_range_start);
					}

					uint base_value = 10;
					uint padding = 0;
					uint decimal_places = 5; // Default decimal places in .NET libs

					index++;
					char format_char = format_string[index];
					if (format_char == '{')
					{
						string_builder.Append('{');
						index++;
					}
					else
					{
						index++;

						if (format_string[index] == ':')
						{
							// Extra formatting. This is a crude first pass proof-of-concept. It's not meant to cover
							// comprehensively what the .NET standard library Format() can do.
							index++;

							// Deal with padding
							while (format_string[index] == '0')
							{
								index++;
								padding++;
							}

							if (format_string[index] == 'X')
							{
								index++;

								// Print in hex
								base_value = 16;

								// Specify amount of padding ( "{0:X8}" for example pads hex to eight characters
								if ((format_string[index] >= '0') && (format_string[index] <= '9'))
								{
									padding = (uint)(format_string[index] - '0');
									index++;
								}
							}
							else if (format_string[index] == '.')
							{
								index++;

								// Specify number of decimal places
								decimal_places = 0;

								while (format_string[index] == '0')
								{
									index++;
									decimal_places++;
								}
							}
						}


						// Scan through to end bracket
						while (format_string[index] != '}')
						{
							index++;
						}

						// Have any extended settings now, so just print out the particular argument they wanted
						switch (format_char)
						{
							case '0': string_builder.ConcatFormatValue<A>(arg1, padding, base_value, decimal_places); break;
							case '1': string_builder.ConcatFormatValue<B>(arg2, padding, base_value, decimal_places); break;
							case '2': string_builder.ConcatFormatValue<C>(arg3, padding, base_value, decimal_places); break;
							case '3': string_builder.ConcatFormatValue<D>(arg4, padding, base_value, decimal_places); break;
							default: Debug.Assert(false, "Invalid parameter index"); break;
						}
					}

					// Update the verbatim range, start of a new section now
					verbatim_range_start = (index + 1);
				}
			}

			// Anything verbatim to write out?
			if (verbatim_range_start < format_string.Length)
			{
				// Write out unformatted string portion
				string_builder.Append(format_string, verbatim_range_start, format_string.Length - verbatim_range_start);
			}

			return string_builder;
		}

		//! The worker method. This does a garbage-free conversion of a generic type, and uses the garbage-free Concat() to add to the stringbuilder
		private static void ConcatFormatValue<T>(this StringBuilder string_builder, T arg, uint padding, uint base_value, uint decimal_places) where T : IConvertible
		{
			switch (arg.GetTypeCode())
			{
				case System.TypeCode.UInt32:
				{
					string_builder.Concat(arg.ToUInt32(System.Globalization.NumberFormatInfo.CurrentInfo), padding, '0', base_value);
					break;
				}

				case System.TypeCode.Int32:
				{
					string_builder.Concat(arg.ToInt32(System.Globalization.NumberFormatInfo.CurrentInfo), padding, '0', base_value);
					break;
				}

				case System.TypeCode.Single:
				{
					string_builder.Concat(arg.ToSingle(System.Globalization.NumberFormatInfo.CurrentInfo), decimal_places, padding, '0');
					break;
				}

				case System.TypeCode.String:
				{
					string_builder.Append(Convert.ToString(arg));
					break;
				}

				default:
				{
					Debug.Assert(false, "Unknown parameter type");
					break;
				}
			}
		}
	}
}
