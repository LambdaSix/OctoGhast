using System;
using OctoGhast.DataStructures.Lighting;
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

        LightMap<TileLightInfo> CalculateFov(Vec viewCenter, int lightRadius, Func<int, int, Vec> translateFunc);

        Tile this[Vec pos] { get; set; }
        Tile this[int x, int y] { get; set; }
    }
}