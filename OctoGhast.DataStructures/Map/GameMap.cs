using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using InfiniMap;
using libtcod;
using OctoGhast.DataStructures.Lighting;
using OctoGhast.DataStructures.Renderer;
using OctoGhast.Spatial;

namespace OctoGhast.DataStructures.Map
{
    public interface IGameMap
    {
        /// <summary>
        /// Determine if the player has previously seen this tile.
        /// </summary>
        /// <param name="position">World position</param>
        /// <returns>True if the tile has previously been seen.</returns>
        bool IsExplored(Vec position);

        /// <summary>
        /// Determine if the tile at <paramref name="position"/> can be moved over
        /// </summary>
        /// <param name="position">World position</param>
        /// <returns>True if the tile can be moved over</returns>
        bool IsWalkable(Vec position);

        /// <summary>
        /// Determine if the tile at <paramref name="position"/> cannot be seen through
        /// </summary>
        /// <param name="position">World position</param>
        /// <returns>True if the tile blocks sight</returns>
        bool IsOpaque(Vec position);

        LightMap<bool> CalculateFov(Vec viewCenter, int lightRadius, Func<int, int, Vec> translateFunc);

        Tile this[Vec pos] { get; set; }
        Tile this[int x, int y] { get; set; }
    }

    public class GameMap : IGameMap
    {
        private Map2D<Tile> _map { get; set; }
        private int _screenHeight;
        private int _screenWidth;

        public GameMap(int screenHeight, int screenWidth) {
            _map = new Map2D<Tile>(16, 16);
            _screenHeight = screenHeight;
            _screenWidth = screenWidth;

            _map.RegisterReader(tuple => Enumerable.Repeat(new Tile {Glyph = '.', IsWalkable = true}, 16*16));
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

        public Tile this[int x, int y] {
            get { return _map[x, y]; }
            set { _map[x, y] = value; }
        }

        public Tile this[Vec pos] {
            get { return _map[pos.X, pos.Y]; }
            set { _map[pos.X, pos.Y] = value; }
        }

        public LightMap<bool> CalculateFov(Vec viewCenter, int lightRadius, Func<int, int, Vec> translateFunc) {
            var lightMap = new LightMap<bool>(_screenHeight, _screenWidth);

            ShadowCaster.ComputeFieldOfViewWithShadowCasting(viewCenter.X, viewCenter.Y, lightRadius,
                (x, y) => IsOpaque(new Vec(x, y)),
                (x, y) => {
                    var screenPos = translateFunc(x, y);
                    lightMap[screenPos] = true;
                });
            return lightMap;
        }
    }
}