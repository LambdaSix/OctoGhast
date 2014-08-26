using System;

namespace OctoGhast.Spatial
{
    [Serializable]
    public struct Size
    {
        private readonly int _width;
        private readonly int _height;

        public Rect Bounds { get { return new Rect(this); }}

        public Size(int width, int height) {
            _width = width;
            _height = height;
        }

        public Size(Size size) {
            _width = size.Width;
            _height = size._height;
        }

        public int Width {
            get { return _width; }
        }

        public int Height {
            get { return _height; }
        }

        public bool IsEmpty {
            get { return (Width == 0 && Height == 0); }
        }

        public int Area { get { return Width*Height; } }

        public override bool Equals(object obj) {
            if (obj == null)
                return false;

            if (obj is Size)
                return Equals((Size) obj);

            return false;
        }

        /// <summary>
        /// Change the Width of this Size by the value
        /// </summary>
        /// <param name="width">Amount to change Width by</param>
        /// <returns></returns>
        public Size OffsetWidth(int width) {
            return new Size(Width + width, Height);
        }

        /// <summary>
        /// Change the Height of this Size by the value
        /// </summary>
        /// <param name="height">Amount to change Height by</param>
        /// <returns></returns>
        public Size OffsetHeight(int height) {
            return new Size(Width, Height + height);
        }

        /// <summary>
        /// Change the Height and Width of this Size by the values
        /// </summary>
        /// <param name="width">Amount to change Width by</param>
        /// <param name="height">Amount to change Height by</param>
        /// <returns></returns>
        public Size Offset(int width, int height) {
            return new Size(Width + width, Height + height);
        }

        public bool Equals(Size other) {
            return (Width == other.Width && Height == other.Height);
        }

        public override int GetHashCode() {
            return Width.GetHashCode() + Height.GetHashCode();
        }

        public override string ToString() {
            return String.Format("{0},{1}", Width, Height);
        }

        public static bool operator ==(Size left, Size right) {
            return left.Equals(right);
        }

        public static bool operator !=(Size left, Size right) {
            return !(left == right);
        }
    }
}