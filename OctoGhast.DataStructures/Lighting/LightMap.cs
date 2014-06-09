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
                return _map[n];
            }
            set {
                var n = (x + (_width*y));
                _map[n] = value;
            }
        }
    }
}