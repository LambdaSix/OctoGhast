using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    }
}
