// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Copied from: https://github.com/dotnet/corefx/tree/master/src/System.Drawing.Primitives/src/System/Drawing

using System;
using System.ComponentModel;

namespace Terminal.Gui {
	/// <summary>
	/// Represents an ordered pair of x and y coordinates that define a point in a two-dimensional plane.
	/// </summary>
	public struct PointF : IEquatable<PointF> {
		/// <summary>
		/// Creates a new instance of the <see cref='Terminal.Gui.PointF'/> class with member data left uninitialized.
		/// </summary>
		public static readonly PointF Empty;
		private float x; // Do not rename (binary serialization)
		private float y; // Do not rename (binary serialization)

		/// <summary>
		/// Initializes a new instance of the <see cref='Terminal.Gui.PointF'/> class with the specified coordinates.
		/// </summary>
		public PointF (float x, float y)
		{
			this.x = x;
			this.y = y;
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref='Terminal.Gui.PointF'/> is empty.
		/// </summary>
		[Browsable (false)]
		public bool IsEmpty => x == 0f && y == 0f;

		/// <summary>
		/// Gets the x-coordinate of this <see cref='Terminal.Gui.PointF'/>.
		/// </summary>
		public float X {
			get => x;
			set => x = value;
		}

		/// <summary>
		/// Gets the y-coordinate of this <see cref='Terminal.Gui.PointF'/>.
		/// </summary>
		public float Y {
			get => y;
			set => y = value;
		}

		/// <summary>
		/// Translates a <see cref='Terminal.Gui.PointF'/> by a given <see cref='Terminal.Gui.Size'/> .
		/// </summary>
		public static PointF operator + (PointF pt, Size sz) => Add (pt, sz);

		/// <summary>
		/// Translates a <see cref='Terminal.Gui.PointF'/> by the negative of a given <see cref='Terminal.Gui.Size'/> .
		/// </summary>
		public static PointF operator - (PointF pt, Size sz) => Subtract (pt, sz);

		/// <summary>
		/// Translates a <see cref='Terminal.Gui.PointF'/> by a given <see cref='Terminal.Gui.SizeF'/> .
		/// </summary>
		public static PointF operator + (PointF pt, SizeF sz) => Add (pt, sz);

		/// <summary>
		/// Translates a <see cref='Terminal.Gui.PointF'/> by the negative of a given <see cref='Terminal.Gui.SizeF'/> .
		/// </summary>
		public static PointF operator - (PointF pt, SizeF sz) => Subtract (pt, sz);

		/// <summary>
		/// Compares two <see cref='Terminal.Gui.PointF'/> objects. The result specifies whether the values of the
		/// <see cref='Terminal.Gui.PointF.X'/> and <see cref='Terminal.Gui.PointF.Y'/> properties of the two
		/// <see cref='Terminal.Gui.PointF'/> objects are equal.
		/// </summary>
		public static bool operator == (PointF left, PointF right) => left.X == right.X && left.Y == right.Y;

		/// <summary>
		/// Compares two <see cref='Terminal.Gui.PointF'/> objects. The result specifies whether the values of the
		/// <see cref='Terminal.Gui.PointF.X'/> or <see cref='Terminal.Gui.PointF.Y'/> properties of the two
		/// <see cref='Terminal.Gui.PointF'/> objects are unequal.
		/// </summary>
		public static bool operator != (PointF left, PointF right) => !(left == right);

		/// <summary>
		/// Translates a <see cref='Terminal.Gui.PointF'/> by a given <see cref='Terminal.Gui.Size'/> .
		/// </summary>
		public static PointF Add (PointF pt, Size sz) => new PointF (pt.X + sz.Width, pt.Y + sz.Height);

		/// <summary>
		/// Translates a <see cref='Terminal.Gui.PointF'/> by the negative of a given <see cref='Terminal.Gui.Size'/> .
		/// </summary>
		public static PointF Subtract (PointF pt, Size sz) => new PointF (pt.X - sz.Width, pt.Y - sz.Height);

		/// <summary>
		/// Translates a <see cref='Terminal.Gui.PointF'/> by a given <see cref='Terminal.Gui.SizeF'/> .
		/// </summary>
		public static PointF Add (PointF pt, SizeF sz) => new PointF (pt.X + sz.Width, pt.Y + sz.Height);

		/// <summary>
		/// Translates a <see cref='Terminal.Gui.PointF'/> by the negative of a given <see cref='Terminal.Gui.SizeF'/> .
		/// </summary>
		public static PointF Subtract (PointF pt, SizeF sz) => new PointF (pt.X - sz.Width, pt.Y - sz.Height);

		/// <summary>
		/// Compares two <see cref='Terminal.Gui.PointF'/> objects. The result specifies whether the values of the
		/// <see cref='Terminal.Gui.PointF.X'/> and <see cref='Terminal.Gui.PointF.Y'/> properties of the two
		/// <see cref='Terminal.Gui.PointF'/> objects are equal.
		/// </summary>
		public override bool Equals (object obj) => obj is PointF && Equals ((PointF)obj);

		/// <summary>
		/// Compares two <see cref='Terminal.Gui.PointF'/> objects. The result specifies whether the values of the
		/// <see cref='Terminal.Gui.PointF.X'/> and <see cref='Terminal.Gui.PointF.Y'/> properties of the two
		/// <see cref='Terminal.Gui.PointF'/> objects are equal.
		/// </summary>
		public bool Equals (PointF other) => this == other;

		/// <summary>
		/// Generates a hashcode from the X and Y components
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode ()
		{
			return X.GetHashCode() ^ Y.GetHashCode ();
		}

		/// <summary>
		/// Returns a string including the X and Y values
		/// </summary>
		/// <returns></returns>
		public override string ToString () => "{X=" + x.ToString () + ", Y=" + y.ToString () + "}";
	}
}
