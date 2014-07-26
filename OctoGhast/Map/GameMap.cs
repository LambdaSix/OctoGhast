using System;
using System.Linq;
using InfiniMap;
using OctoGhast.DataStructures.Lighting;
using OctoGhast.DataStructures.Map;
using OctoGhast.Spatial;

namespace OctoGhast.Map
{
    public class GameMap : IGameMap
    {
        private Map2D<ITile> _map { get; set; }
        private int _screenHeight;
        private int _screenWidth;

        public GameMap(int screenHeight, int screenWidth) {
            _map = new Map2D<ITile>(16, 16);
            _screenHeight = screenHeight;
            _screenWidth = screenWidth;

            _map.RegisterReader(tuple => Enumerable.Repeat(new Tile {Glyph = '.', IsWalkable = true, IsTransparent = true}, 16*16));
        }

        public bool IsExplored(Vec position) {
            return _map[position.X, position.Y].IsExplored;
        }

        public bool IsWalkable(Vec position) {
            return _map[position.X, position.Y].IsWalkable;
        }

        public bool IsOpaque(Vec position) {
            return !_map[position.X, position.Y].IsTransparent;
        }

        public ITile this[int x, int y] {
            get { return _map[x, y]; }
            set { _map[x, y] = value; }
        }

        public ITile this[Vec pos] {
            get { return _map[pos.X, pos.Y]; }
            set { _map[pos.X, pos.Y] = value; }
        }

        public LightMap<TileLightInfo> CalculateFov(Vec viewCenter, int lightRadius, Func<int, int, Vec> translateFunc) {
            var lightMap = new LightMap<TileLightInfo>(_screenHeight, _screenWidth);

            // TODO: Loop a list of lights, calculate the FOV for each light then mix it's colour into the tile.

            ShadowCaster.ComputeFieldOfViewWithShadowCasting(viewCenter.X, viewCenter.Y, lightRadius,
                (x, y) => IsOpaque(new Vec(x, y)),
                (x, y) => {
                    var screenPos = translateFunc(x, y);
                    lightMap[screenPos].IsLit = true;
                });
            return lightMap;
        }
    }
}