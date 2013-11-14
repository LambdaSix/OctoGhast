using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            ChunkMap.Fill(new Tile {Glyph = '.', IsVisible = true, IsWalkable = true});
        }

        public void BuildLighting() {
            throw new System.NotImplementedException();
        }

        public void BuildPathfindingGraph() {
            throw new System.NotImplementedException();
        }
    }

    public class GameMap : IMap<Tile>, IChunkedMap<Chunk>
    {
        public Array2D<Tile> MapArray { get; set; }

        public Vec Size {
            get { return MapArray.Size; }
        }

        public Array2D<Chunk> Chunks { get; set; }

        public GameMap(int width, int height) {
            MapArray = new Array2D<Tile>(width, height);
            Chunks = new Array2D<Chunk>(9, 9);
            Chunks.Fill(new Chunk());
        }

        public Array2D<Tile> GetFrustumView(ICamera frustum) {
            var dst = new Array2D<Tile>(frustum.Width, frustum.Height);

            for (int y = 0; y < frustum.Height; y++) {
                for (int x = 0; x < frustum.Width; x++) {
                    var mapX = (frustum.Position.X + x);
                    var mapY = (frustum.Position.Y + y);

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
    }
}
