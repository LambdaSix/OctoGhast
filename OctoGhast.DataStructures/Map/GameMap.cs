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
        bool IsExplored(int x, int y);
        bool IsWalkable(Vec position);

        /// <summary>
        /// Get the subset of the loaded map currently viewable by the given camera.
        /// </summary>
        /// <param name="frustum"></param>
        /// <returns></returns>
        IEnumerable<Tile> GetFrustumView(Rect frustum);
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

        public bool IsExplored(int x, int y)
        {
            return _map[x, y].IsExplored;
        }

        public bool IsWalkable(Vec position) {
            return _map[position.X, position.Y].IsWalkable;
        }

        /// <summary>
        /// Get the subset of the loaded map currently viewable by the given camera.
        /// </summary>
        /// <param name="frustum"></param>
        /// <returns></returns>
        public IEnumerable<Tile> GetFrustumView(Rect frustum) {
            var tl = frustum.TopLeft;
            var br = frustum.BottomRight;
            return _map.Within(tl.X, tl.Y, br.X, br.Y);
        }
    }
}
