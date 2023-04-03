//
// System.Drawing.Point.cs
//
// Author:
//   Mike Kestner (mkestner@speakeasy.net)
//
// Copyright (C) 2001 Mike Kestner
// Copyright (C) 2004 Novell, Inc.  http://www.novell.com 
//

using System;
using System.Globalization;

namespace Terminal.Gui
{
	/// <summary>
	/// Represents an ordered pair of integer x- and y-coordinates that defines a point in a two-dimensional plane.
	/// </summary>
	public struct Point
	{
		/// <summary>
		/// Gets or sets the x-coordinate of this Point.
		/// </summary>
		[System.Text.Json.Serialization.JsonInclude]
		public int X;

		/// <summary>
		/// Gets or sets the y-coordinate of this Point.
		/// </summary>
		[System.Text.Json.Serialization.JsonInclude]
		public int Y;

		// -----------------------
		// Public Shared Members
		// -----------------------

		/// <summary>
		///	Empty Shared Field
		/// </summary>
		///
		/// <remarks>
		///	An uninitialized Point Structure.
		/// </remarks>
		
		public static readonly Point Empty;

		/// <summary>
		///	Addition Operator
		/// </summary>
		///
		/// <remarks>
		///	Translates a Point using the Width and Height
		///	properties of the given <typeref>Size</typeref>.
		/// </remarks>

		public static Point operator + (Point pt, Size sz)
		{
			return new Point (pt.X + sz.Width, pt.Y + sz.Height);
		}
		
		/// <summary>
		///	Equality Operator
		/// </summary>
		///
		/// <remarks>
		///	Compares two Point objects. The return value is
		///	based on the equivalence of the X and Y properties 
		///	of the two points.
		/// </remarks>

		public static bool operator == (Point left, Point right)
		{
			return ((left.X == right.X) && (left.Y == right.Y));
		}
		
		/// <summary>
		///	Inequality Operator
		/// </summary>
		///
		/// <remarks>
		///	Compares two Point objects. The return value is
		///	based on the equivalence of the X and Y properties 
		///	of the two points.
		/// </remarks>

		public static bool operator != (Point left, Point right)
		{
			return ((left.X != right.X) || (left.Y != right.Y));
		}
		
		/// <summary>
		///	Subtraction Operator
		/// </summary>
		///
		/// <remarks>
		///	Translates a Point using the negation of the Width 
		///	and Height properties of the given Size.
		/// </remarks>

		public static Point operator - (Point pt, Size sz)
		{
			return new Point (pt.X - sz.Width, pt.Y - sz.Height);
		}
		
		/// <summary>
		///	Point to Size Conversion
		/// </summary>
		///
		/// <remarks>
		///	Returns a Size based on the Coordinates of a given 
		///	Point. Requires explicit cast.
		/// </remarks>

		public static explicit operator Size (Point p)
		{
			if (p.X < 0 || p.Y < 0)
				throw new ArgumentException ("Either Width and Height must be greater or equal to 0.");

			return new Size (p.X, p.Y);
		}

		// -----------------------
		// Public Constructors
		// -----------------------
		/// <summary>
		///	Point Constructor
		/// </summary>
		///
		/// <remarks>
		///	Creates a Point from a Size value.
		/// </remarks>
		
		public Point (Size sz)
		{
			X = sz.Width;
			Y = sz.Height;
		}

		/// <summary>
		///	Point Constructor
		/// </summary>
		///
		/// <remarks>
		///	Creates a Point from a specified x,y coordinate pair.
		/// </remarks>
		
		public Point (int x, int y)
		{
			this.X = x;
			this.Y = y;
		}

		// -----------------------
		// Public Instance Members
		// -----------------------

		/// <summary>
		///	IsEmpty Property
		/// </summary>
		///
		/// <remarks>
		///	Indicates if both X and Y are zero.
		/// </remarks>		
		[System.Text.Json.Serialization.JsonIgnore]
		public bool IsEmpty {
			get {
				return ((X == 0) && (Y == 0));
			}
		}

		/// <summary>
		///	Equals Method
		/// </summary>
		///
		/// <remarks>
		///	Checks equivalence of this Point and another object.
		/// </remarks>
		
		public override bool Equals (object obj)
		{
			if (!(obj is Point))
				return false;

			return (this == (Point) obj);
		}

		/// <summary>
		///	GetHashCode Method
		/// </summary>
		///
		/// <remarks>
		///	Calculates a hashing value.
		/// </remarks>
		
		public override int GetHashCode ()
		{
			return X^Y;
		}

		/// <summary>
		///	Offset Method
		/// </summary>
		///
		/// <remarks>
		///	Moves the Point a specified distance.
		/// </remarks>

		public void Offset (int dx, int dy)
		{
			X += dx;
			Y += dy;
		}
		
		/// <summary>
		///	ToString Method
		/// </summary>
		///
		/// <remarks>
		///	Formats the Point as a string in coordinate notation.
		/// </remarks>
		
		public override string ToString ()
		{
			return string.Format ("{{X={0},Y={1}}}", X.ToString (CultureInfo.InvariantCulture), 
				Y.ToString (CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Adds the specified Size to the specified Point.
		/// </summary>
		/// <returns>The Point that is the result of the addition operation.</returns>
		/// <param name="pt">The Point to add.</param>
		/// <param name="sz">The Size to add.</param>
		public static Point Add (Point pt, Size sz)
		{
			return new Point (pt.X + sz.Width, pt.Y + sz.Height);
		}

		/// <summary>
		/// Translates this Point by the specified Point.
		/// </summary>
		/// <returns>The offset.</returns>
		/// <param name="p">The Point used offset this Point.</param>
		public void Offset (Point p)
		{
			Offset (p.X, p.Y);
		}

		/// <summary>
		/// Returns the result of subtracting specified Size from the specified Point.
		/// </summary>
		/// <returns>The Point that is the result of the subtraction operation.</returns>
		/// <param name="pt">The Point to be subtracted from.</param>
		/// <param name="sz">The Size to subtract from the Point.</param>
		public static Point Subtract (Point pt, Size sz)
		{
			return new Point (pt.X - sz.Width, pt.Y - sz.Height);
		}

	}
}
