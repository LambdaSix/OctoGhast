using System;

namespace OctoGhast.Spatial
{
    [Serializable]
    public struct Size
    {
        private readonly int _width;
        private readonly int _height;

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

        public override bool Equals(object obj) {
            if (obj == null)
                return false;

            if (obj is Size)
                return Equals((Size) obj);

            return false;
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