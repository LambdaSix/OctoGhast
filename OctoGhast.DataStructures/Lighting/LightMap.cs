using System;
using System.Linq;
using OctoGhast.Spatial;

namespace OctoGhast.DataStructures.Lighting
{
    public class TileLightInfo
    {
        public bool IsLit { get; set; }
        public IColor LightColor { get; set; }
    }

    public class LightMap<T>
    {
        private readonly T[] _map;
        private readonly int _width;
        private readonly int _height;
        private Func<T> _allocator; 

        public LightMap(int height, int width) {
            _map = new T[width*height];

            if (typeof (T).IsClass) {
                _allocator = Activator.CreateInstance<T>;

                foreach (var element in _map.Select((s,i) => new {i})) {
                    _map[element.i] = _allocator();
                }
            }

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