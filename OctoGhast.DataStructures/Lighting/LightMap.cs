using OctoGhast.Spatial;

namespace OctoGhast.DataStructures.Lighting
{
    public class LightMap<T>
    {
        private readonly T[] _map;
        private readonly int _width;
        private readonly int _height;

        public LightMap(int height, int width) {
            _map = new T[width*height];
            _height = height;
            _width = width;
        }

        public T this[int x, int y] {
            get {
                var n = (x + (_width*y));
                return n < _map.Length ? _map[n] : default(T);
            }
            set {
                var n = (x + (_width*y));
                if (n < _map.Length) {
                    _map[x + (_width*y)] = value;
                }
            }
        }

        public T this[Vec position] {
            get { return this[position.X, position.Y]; }
            set { this[position.X, position.Y] = value; }
        }
    }
}