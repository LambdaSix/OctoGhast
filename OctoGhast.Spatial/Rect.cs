using System;
using System.Collections.Generic;

namespace OctoGhast.Spatial
{
    /// <summary>
    /// A 2D integer rectangle class. Similar to Rectangle, but not dependent on System.Drawing
    /// and much more feature-rich.
    /// </summary>
    [Serializable]
    public struct Rect : IEquatable<Rect>, IEnumerable<Vec>
    {
        /// <summary>
        /// Gets the empty rectangle.
        /// </summary>
        public readonly static Rect Empty;

        /// <summary>
        /// Creates a new rectangle a single row in height, as wide as the given size.
        /// </summary>
        /// <param name="size">The width of the rectangle.</param>
        /// <returns>The new rectangle.</returns>
        public static Rect Row(int size)
        {
            return new Rect(0, 0, size, 1);
        }

        /// <summary>
        /// Creates a new rectangle a single row in height, as wide as the given size,
        /// starting at the given top-left corner.
        /// </summary>
        /// <param name="x">The left edge of the rectangle.</param>
        /// <param name="y">The top of the rectangle.</param>
        /// <param name="size">The width of the rectangle.</param>
        /// <returns>The new rectangle.</returns>
        public static Rect Row(int x, int y, int size)
        {
            return new Rect(x, y, size, 1);
        }

        /// <summary>
        /// Creates a new rectangle a single row in height, as wide as the given size,
        /// starting at the given top-left corner.
        /// </summary>
        /// <param name="pos">The top-left corner of the rectangle.</param>
        /// <returns>The new rectangle.</returns>
        public static Rect Row(Vec pos, int size)
        {
            return new Rect(pos.X, pos.Y, size, 1);
        }

        /// <summary>
        /// Creates a new rectangle a single column in width, as tall as the given size.
        /// </summary>
        /// <param name="size">The height of the rectangle.</param>
        /// <returns>The new rectangle.</returns>
        public static Rect Column(int size)
        {
            return new Rect(0, 0, 1, size);
        }

        /// <summary>
        /// Creates a new rectangle a single column in width, as tall as the given size,
        /// starting at the given top-left corner.
        /// </summary>
        /// <param name="x">The left edge of the rectangle.</param>
        /// <param name="y">The top of the rectangle.</param>
        /// <param name="size">The height of the rectangle.</param>
        /// <returns>The new rectangle.</returns>
        public static Rect Column(int x, int y, int size)
        {
            return new Rect(x, y, 1, size);
        }

        /// <summary>
        /// Creates a new rectangle a single column in width, as tall as the given size,
        /// starting at the given top-left corner.
        /// </summary>
        /// <param name="pos">The top-left corner of the rectangle.</param>
        /// <param name="size">The height of the rectangle.</param>
        /// <returns>The new rectangle.</returns>
        public static Rect Column(Vec pos, int size)
        {
            return new Rect(pos.X, pos.Y, 1, size);
        }

        /// <summary>
        /// Creates a new rectangle that is the intersection of the two given rectangles.
        /// </summary>
        /// <example><code>
        /// .----------.
        /// | a        |
        /// | .--------+----.
        /// | | result |  b |
        /// | |        |    |
        /// '-+--------'    |
        ///   |             |
        ///   '-------------'
        /// </code></example>
        /// <param name="a">The first rectangle.</param>
        /// <param name="b">The rectangle to intersect it with.</param>
        /// <returns></returns>
        public static Rect Intersect(Rect a, Rect b)
        {
            int left = Math.Max(a.Left, b.Left);
            int right = Math.Min(a.Right, b.Right);
            int top = Math.Max(a.Top, b.Top);
            int bottom = Math.Min(a.Bottom, b.Bottom);

            int width = Math.Max(0, right - left);
            int height = Math.Max(0, bottom - top);

            return new Rect(left, top, width, height);
        }

        /// <summary>
        /// Move the Rect so its center point is the larger rects center point.
        /// </summary>
        /// <param name="toCenter">The <seealso cref="Rect"/> to center</param>
        /// <param name="main">The <seealso cref="Rect"/> to center <paramref name="toCenter"/> in</param>
        /// <returns></returns>
        public static Rect CenterIn(Rect toCenter, Rect main) {
            var pos = main.Position;
            var sizeWidth = (main.Size.Width - toCenter.Size.Width);
            var sizeHeight = (main.Size.Height - toCenter.Size.Height);

            var finalSize = new Size(sizeWidth, sizeHeight);
            var halfSize = new Size(sizeHeight/2, sizeHeight/2);

            var finalPos = pos + new Vec(halfSize.Width, halfSize.Width);

            return new Rect(pos, toCenter.Size);
        }

        #region Operators

        public static bool operator ==(Rect r1, Rect r2)
        {
            return r1.Equals(r2);
        }

        public static bool operator !=(Rect r1, Rect r2)
        {
            return !r1.Equals(r2);
        }

        public static Rect operator +(Rect r1, Vec v2)
        {
            return new Rect(r1.Position + v2, r1.Size);
        }

        public static Rect operator +(Vec v1, Rect r2)
        {
            return new Rect(r2.Position + v1, r2.Size);
        }

        public static Rect operator -(Rect r1, Vec v2)
        {
            return new Rect(r1.Position - v2, r1.Size);
        }

        #endregion

        public Vec Position { get { return mPos; } }
        public Size Size { get { return mSize; } }

        public int X { get { return mPos.X; } }
        public int Y { get { return mPos.Y; } }
        public int Width { get { return mSize.Width; } }
        public int Height { get { return mSize.Height; } }

        public int Left { get { return X; } }
        public int Top { get { return Y; } }
        public int Right { get { return X + Width; } }
        public int Bottom { get { return Y + Height; } }

        public Vec TopLeft { get { return new Vec(Left, Top); } }
        public Vec TopRight { get { return new Vec(Right, Top); } }
        public Vec BottomLeft { get { return new Vec(Left, Bottom); } }
        public Vec BottomRight { get { return new Vec(Right, Bottom); } }

        public Vec Center { get { return new Vec((Left + Right) / 2, (Top + Bottom) / 2); } }

        public int Area { get { return mSize.Area; } }

        public Rect(Vec pos, Size size)
        {
            mPos = pos;
            mSize = size;
        }

        public Rect(Size size) : this(Vec.Zero, size) {
        }

        public Rect(int x, int y, int width, int height)
            : this(new Vec(x, y), new Size(width, height))
        {
        }

        public Rect(Vec pos, int width, int height)
            : this(pos, new Size(width, height))
        {
        }

        public Rect(int width, int height)
            : this(new Size(width, height))
        {
        }

        public Rect(int x, int y, Size size)
            : this(new Vec(x, y), size)
        {
        }

        public override string ToString()
        {
            return String.Format("({0})-({1})", mPos, mSize);
        }

        public override bool Equals(object obj)
        {
            if (obj is Rect) return Equals((Rect)obj);

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return mPos.GetHashCode() + mSize.GetHashCode();
        }

        /// <summary>
        /// Offset this Rect by adding <paramref name="pos"/> to
        /// the current position.
        /// 
        /// Doesn't alter the size.
        /// </summary>
        /// <param name="pos">Vector to add to current position</param>
        /// <returns></returns>
        public Rect Offset(Vec pos) {
            return new Rect(mPos + pos, Size);
        }

        public Rect Offset(int x, int y) {
            return Offset(new Vec(x, y));
        }

        /// <summary>
        /// Offset this Rect by adding <paramref name="pos"/> and
        /// setting the size to <paramref name="size"/>
        /// </summary>
        /// <param name="pos">Vector to add to current position</param>
        /// <param name="size">New size of rectangle</param>
        /// <returns></returns>
        public Rect Offset(Vec pos, Size size) {
            return new Rect(mPos + pos, size);
        }

        public Rect Offset(int x, int y, int width, int height) {
            return Offset(new Vec(x, y), new Size(width, height));
        }

        /// <summary>
        /// Inflate this Rect's size by the delta values.
        /// Does not affect the origin of the Rect.
        /// </summary>
        /// <param name="widthDelta">Amount to add to the width</param>
        /// <param name="heightDelta">Amount to add to the height</param>
        /// <returns></returns>
        public Rect Inflate(int widthDelta, int heightDelta) {

            return new Rect(mPos, new Size(Size.Width + widthDelta, Size.Height + heightDelta));
        }

        /// <summary>
        /// Inflate this Rect's size by the delta values.
        /// </summary>
        /// <param name="delta">Amount to inflate width/height by</param>
        /// <returns></returns>
        public Rect Inflate(int delta) {
            return new Rect(mPos, new Size(Size.Width + delta, Size.Height + delta));
        }

        public bool Contains(Vec pos)
        {
            if (pos.X < mPos.X) return false;
            if (pos.X >= mPos.X + mSize.Width) return false;
            if (pos.Y < mPos.Y) return false;
            if (pos.Y >= mPos.Y + mSize.Height) return false;

            return true;
        }

        public bool Contains(Rect rect)
        {
            // all sides must be within
            if (rect.Left < Left) return false;
            if (rect.Right > Right) return false;
            if (rect.Top < Top) return false;
            if (rect.Bottom > Bottom) return false;

            return true;
        }

        public bool Overlaps(Rect rect)
        {
            // fail if they do not overlap on any axis
            if (Left > rect.Right) return false;
            if (Right < rect.Left) return false;
            if (Top > rect.Bottom) return false;
            if (Bottom < rect.Top) return false;

            // then they must overlap
            return true;
        }

        public Rect Intersect(Rect rect)
        {
            return Intersect(this, rect);
        }

        public Rect CenterIn(Rect rect)
        {
            return CenterIn(this, rect);
        }

        public IEnumerable<Vec> Trace()
        {
            if ((Width > 1) && (Height > 1))
            {
                // trace all four sides
                foreach (Vec top in Row(TopLeft, Width - 1)) yield return top;
                foreach (Vec right in Column(TopRight.OffsetX(-1), Height - 1)) yield return right;
                foreach (Vec bottom in Row(Width - 1)) yield return BottomRight.Offset(-1, -1) - bottom;
                foreach (Vec left in Column(Height - 1)) yield return BottomLeft.OffsetY(-1) - left;
            }
            else if ((Width > 1) && (Height == 1))
            {
                // a single row
                foreach (Vec pos in Row(TopLeft, Width)) yield return pos;
            }
            else if ((Height >= 1) && (Width == 1))
            {
                // a single column, or one unit
                foreach (Vec pos in Column(TopLeft, Height)) yield return pos;
            }

            // otherwise, the rect doesn't have a positive size, so there's nothing to trace
        }

        public static Rect FromCenter(Vec center, Size size) {
            var x = center.X;
            var y = center.Y;

            var target = new Rect(
                x: x - (size.Width/2),
                y: y - (size.Height/2),
                height: size.Height,
                width: size.Width);

            return new Rect(target.TopLeft, target.Width, target.Height);
        }

        #region IEquatable<Rect> Members

        public bool Equals(Rect other)
        {
            return mPos.Equals(other.mPos) && mSize.Equals(other.mSize);
        }

        #endregion

        #region IEnumerable<Vec> Members

        public IEnumerator<Vec> GetEnumerator()
        {
            if (mSize.Width < 0) throw new ArgumentOutOfRangeException("Cannot enumerate a Rectangle with a negative width.");
            if (mSize.Height < 0) throw new ArgumentOutOfRangeException("Cannot enumerate a Rectangle with a negative height.");

            for (int y = mPos.Y; y < mPos.Y + mSize.Height; y++)
            {
                for (int x = mPos.X; x < mPos.X + mSize.Width; x++)
                {
                    yield return new Vec(x, y);
                }
            }
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        private Vec mPos;
        private Size mSize;
    }
}