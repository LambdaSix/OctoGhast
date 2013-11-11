using System;
using System.Runtime.Remoting.Channels;

namespace OctoGhast.Spatial
{
    /// <summary>
    /// Generic fixed-size two-dimensional array class.
    /// </summary>
    /// <typeparam name="T">The array element type.</typeparam>
    [Serializable]
    public class Array2D<T>
    {
        private readonly int _width;
        private readonly T[] _values;

        /// <summary>
        /// Initializes a new instance of Array2D with the given dimensions.
        /// </summary>
        /// <param name="width">Width of the array.</param>
        /// <param name="height">Height of the array.</param>
        public Array2D(int width, int height) {
            if (width < 0) throw new ArgumentOutOfRangeException("width", "Width must be greater than zero.");
            if (height < 0) throw new ArgumentOutOfRangeException("height", "Height must be greater than zero.");

            _width = width;
            _values = new T[width*height];
        }

        /// <summary>
        /// Initializes a new instance of Array2D with the given size.
        /// </summary>
        /// <param name="size">Size of the array.</param>
        public Array2D(Vec size)
            : this(size.X, size.Y) {}

        /// <summary>
        /// Gets the size of the array.
        /// </summary>
        public Vec Size {
            get { return new Vec(Width, Height); }
        }

        /// <summary>
        /// Gets the bounds of the array. The top-level coordinate will be the origin.
        /// </summary>
        public Rect Bounds {
            get { return new Rect(Size); }
        }

        /// <summary>
        /// Gets the width of the array.
        /// </summary>
        public int Width {
            get { return _width; }
        }

        /// <summary>
        /// Gets the height of the array.
        /// </summary>
        public int Height {
            get { return _values.Length/_width; }
        }

        /// <summary>
        /// Gets and sets the array element at the given position.
        /// </summary>
        /// <param name="pos">The position of the element. Must be within bounds.</param>
        /// <exception cref="IndexOutOfBoundsException">if the position is out of bounds.</exception>
        public T this[Vec pos] {
            get { return this[pos.X, pos.Y]; }
            set { this[pos.X, pos.Y] = value; }
        }

        /// <summary>
        /// Gets and sets the array element at the given position.
        /// </summary>
        /// <param name="x">The X-coordinate of the element.</param>
        /// <param name="y">The Y-coordinate of the element.</param>
        /// <exception cref="IndexOutOfBoundsException">if the position is out of bounds.</exception>
        public T this[int x, int y] {
            get { return _values[GetIndex(x, y)]; }
            set { _values[GetIndex(x, y)] = value; }
        }

        /// <summary>
        /// Fills all of the elements in the array with the given value.
        /// </summary>
        /// <param name="value">The value to fill the array with.</param>
        public void Fill(T value) {
            foreach (Vec pos in new Rect(Size)) {
                this[pos] = value;
            }
        }

        /// <summary>
        /// Fills all of the elements in the array with values returned by the given callback.
        /// </summary>
        /// <param name="callback">The function to call for each element in the array.</param>
        public void Fill(Func<Vec, T> callback) {
            foreach (Vec pos in new Rect(Size)) {
                this[pos] = callback(pos);
            }
        }

        /// <summary>
        /// Return the sliced sub-array by the view frustum
        /// </summary>
        /// <param name="frustum">Viewing frustum to slice by</param>
        /// <returns>New Array2D containing the sub-set</returns>
        public Array2D<T> SliceView(Rect frustum) {
            var dst = new Array2D<T>(frustum.Width, frustum.Height);

            Array.Copy(_values, GetIndex(frustum.X, frustum.Y), dst._values, 0, dst._values.Length);
            return dst;
        }

        private bool CheckBounds(Vec pos) {
            if (pos.X < 0) return false;
            if (pos.X >= Width) return false;
            if (pos.Y < 0) return false;
            if (pos.Y >= Height) return false;

            return true;
        }

        private int GetIndex(int x, int y) {
            return (y*_width) + x;
        }
    }
}