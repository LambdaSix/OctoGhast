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
        VisibilityMap<Tile> VisibilityMap { get; }

        /// <summary>
        /// Recalculate visibility/walkability flags for the map.
        /// </summary>
        /// <param name="dirtySet">Tiles to be recalculated</param>
        void InvalidateMap(Rect dirtySet);

        /// <summary>
        /// Determines if the given location is visible to the player.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        bool IsVisible(int x, int y);

        bool IsExplored(int x, int y);
        bool IsWalkable(Vec position);

        /// <summary>
        /// Update the FOV state based on the players current location of the radius of
        /// their light.
        /// </summary>
        /// <param name="playerVec"></param>
        /// <param name="lightRadius"></param>
        void CalculateFov(Vec playerVec, int lightRadius);

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
        public VisibilityMap<Tile> VisibilityMap { get; private set; }
        private int _screenHeight;
        private int _screenWidth;

        public GameMap(int screenHeight, int screenWidth) {
            _map = new Map2D<Tile>(16, 16);
            VisibilityMap = new VisibilityMap<Tile>(screenHeight, screenWidth);
            _screenHeight = screenHeight;
            _screenWidth = screenWidth;

            _map.RegisterReader(tuple => Enumerable.Repeat(new Tile {Glyph = '.', IsWalkable = true}, 16*16));
        }

        /// <summary>
        /// Recalculate visibility/walkability flags for the map.
        /// </summary>
        /// <param name="dirtySet">Tiles to be recalculated</param>
        public void InvalidateMap(Rect dirtySet) {
            var tl = dirtySet.TopLeft;
            var br = dirtySet.BottomRight;
            VisibilityMap.RefreshMapFrom(_map.Within(tl.X, tl.Y, br.X, br.Y), _screenWidth);
        }

        /// <summary>
        /// Determines if the given location is visible to the player.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool IsVisible(int x, int y) {
            var isVisible = VisibilityMap.IsVisible(x, y);

	        if (isVisible) {
		        _map[x, y].IsExplored = true;
	        }

	        return isVisible;
        }

        public bool IsExplored(int x, int y)
        {
            return _map[x, y].IsExplored;
        }

        public bool IsWalkable(Vec position) {
            return _map[position.X, position.Y].IsWalkable;
        }

        /// <summary>
        /// Update the FOV state based on the players current location of the radius of
        /// their light.
        /// </summary>
        /// <param name="playerVec"></param>
        /// <param name="lightRadius"></param>
        public void CalculateFov(Vec playerVec, int lightRadius) {
            VisibilityMap.ComputeFov(playerVec, new LightSource {Intensity = lightRadius});
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
