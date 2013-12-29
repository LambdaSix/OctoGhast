using System;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using libtcod;
using OctoGhast.DataStructures.Lighting;
using OctoGhast.DataStructures.Renderer;
using OctoGhast.Spatial;

namespace OctoGhast.DataStructures.Map
{
    public interface IGameMap
    {
        LightingMap LightMap { get; }
        Vec Size { get; }
        Rect Bounds { get; }

        /// <summary>
        /// Recalculate all walkable/transparency attributes for the cells in the map.
        /// </summary>
        void InvalidateMap();

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
        Array2D<Tile> GetFrustumView(Rect frustum);

        void SetFrom(Array2D<Tile> map);
    }

    public class GameMap : IGameMap
    {
        private Array2D<Tile> MapArray { get; set; }
        public LightingMap LightMap { get; private set; }

        public Vec Size {
            get { return MapArray.Size; }
        }

        public Rect Bounds {
            get { return MapArray.Bounds; }
        }

        public GameMap(int width, int height) {
            MapArray = new Array2D<Tile>(width, height);

            LightMap = new LightingMap(width, height);
        }

        /// <summary>
        /// Recalculate all walkable/transparency attributes for the cells in the map.
        /// </summary>
        public void InvalidateMap() {
            LightMap.RefreshMapFrom(MapArray);
        }

        public void SetFrom(Array2D<Tile> map) {
            MapArray = map;
        }

        /// <summary>
        /// Determines if the given location is visible to the player.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool IsVisible(int x, int y) {
            var isVisible = LightMap.IsVisible(x, y);

	        if (isVisible) {
		        MapArray[x, y].IsExplored = true;
	        }

	        return isVisible;
        }

        public bool IsExplored(int x, int y)
        {
            return MapArray[x, y].IsExplored;
        }

        public bool IsWalkable(Vec position) {
            return MapArray[position.X, position.Y].IsWalkable;
        }

        /// <summary>
        /// Update the FOV state based on the players current location of the radius of
        /// their light.
        /// </summary>
        /// <param name="playerVec"></param>
        /// <param name="lightRadius"></param>
        public void CalculateFov(Vec playerVec, int lightRadius) {
            LightMap.ComputeFov(playerVec, new LightSource {Intensity = lightRadius});
        }

        /// <summary>
        /// Get the subset of the loaded map currently viewable by the given camera.
        /// </summary>
        /// <param name="frustum"></param>
        /// <returns></returns>
        public Array2D<Tile> GetFrustumView(Rect frustum) {
            var dst = new Array2D<Tile>(frustum.Width, frustum.Height);

            for (int y = 0; y < frustum.Height; y++) {
                for (int x = 0; x < frustum.Width; x++) {
                    var mapX = (frustum.TopLeft.X + x);
                    var mapY = (frustum.TopLeft.Y + y);

                    dst[x, y] = MapArray[mapX, mapY];
                }
            }

            return dst;
        }
    }
}
