using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using libtcod;
using OctoGhast.DataStructures.Renderer;
using OctoGhast.Spatial;

namespace OctoGhast.DataStructures.Map
{
    public interface IChunk<T>
    {
        Array2D<T> ChunkMap { get; set; }

        // Lighting
        void BuildLighting();

        // Pathfinding
        void BuildPathfindingGraph();
    }

    public interface IChunkedMap<T>
    {
        Array2D<T> Chunks { get; set; }

        Array2D<T> GetCombinedMap();
    }

    public interface IMap<T>
    {
        Array2D<T> GetFrustumView(ICamera frustum);
    }

    public class Chunk : IChunk<Tile>
    {
        public Array2D<Tile> ChunkMap { get; set; }

        public Chunk() {
            ChunkMap = new Array2D<Tile>(64, 64);
            ChunkMap.Fill(new Tile {Glyph = '.', IsWalkable = true});
        }

        public void BuildLighting() {
            throw new System.NotImplementedException();
        }

        public void BuildPathfindingGraph() {
            throw new System.NotImplementedException();
        }
    }

    public class GameMap : IMap<Tile>, IChunkedMap<Chunk>, IDisposable
    {
        public Array2D<Tile> MapArray { get; set; }
        private TCODMap _tcodMap;

        public Vec Size {
            get { return MapArray.Size; }
        }

        public Array2D<Chunk> Chunks { get; set; }

        public GameMap(int width, int height) {
            MapArray = new Array2D<Tile>(width, height);
            //Chunks = new Array2D<Chunk>(9, 9);
            //Chunks.Fill(new Chunk());

            _tcodMap = new TCODMap(width, height);
        }

        /// <summary>
        /// Recalculate all walkable/transparency attributes for the cells in the map.
        /// </summary>
        public void InvalidateMap() {
            for (int y = 0; y < MapArray.Height; y++) {
                for (int x = 0; x < MapArray.Width; x++) {
                    if (MapArray[x, y].Glyph == '.') {
                        _tcodMap.setProperties(x, y, true, true);
                    }
                    else {
                        _tcodMap.setProperties(x, y, false, false);
                    }
                }
            }
        }

        /// <summary>
        /// Determines if the given location transparent for the purposes of lighting.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool IsTransparent(int x, int y) {
            return _tcodMap.isTransparent(x,y);
        }

        /// <summary>
        /// Determines if the given location is visible to the player.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool IsVisible(int x, int y)
        {
            if (_tcodMap.isInFov(x, y)) {
                MapArray[x, y].IsExplored = true;
                return true;
            }

            return false;
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
        public void CalculateFov(Vec playerVec, int lightRadius)
        {
            _tcodMap.computeFov(playerVec.X, playerVec.Y, lightRadius, true, TCODFOVTypes.DiamondFov);
        }

        /// <summary>
        /// Get the subset of the loaded map currently viewable by the given camera.
        /// </summary>
        /// <param name="frustum"></param>
        /// <returns></returns>
        public Array2D<Tile> GetFrustumView(ICamera frustum) {
            var dst = new Array2D<Tile>(frustum.Width, frustum.Height);

            for (int y = 0; y < frustum.Height; y++) {
                for (int x = 0; x < frustum.Width; x++) {
                    var mapX = (frustum.CameraPosition.X + x);
                    var mapY = (frustum.CameraPosition.Y + y);

                    dst[x, y] = MapArray[mapX, mapY];
                }
            }

            return dst;
        }

        /// <summary>
        /// Get the tile at the full world co-ordinates
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Tile GetTileAt(int x, int y) {
            var chunkX = Math.Min(0, x/64);
            var chunkY = Math.Min(0, y/64);

            var intraX = x - 64*chunkX;
            var intraY = y - 64*chunkY;

            return Chunks[chunkX, chunkY].ChunkMap[intraX, intraY];
        }

        public Array2D<Chunk> GetCombinedMap() {
            throw new NotImplementedException();
        }

        public void Dispose() {
            _tcodMap.Dispose();
        }
    }
}
