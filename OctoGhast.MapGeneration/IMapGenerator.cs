using System;
using OctoGhast.DataStructures.Map;
using OctoGhast.Spatial;

namespace OctoGhast.MapGeneration
{
    public interface IMapGenerator<T>
    {
        /// <summary>
        /// Resulting map array
        /// </summary>
        Array2D<T> Map { get; }

        /// <summary>
        /// Seed to use in RNG calls
        /// </summary>
        uint Seed { get; set; }

        /// <summary>
        /// Generate a map for use
        /// </summary>
        /// <param name="dimensions">Dimensions to generate within</param>
        void GenerateMap(Rect dimensions);

        // TODO: Properties for supported methods? (Contiguous, layered, indoor/outdoor, portalled, chunkable, threading, etc)
    }

	public interface ITileMapGenerator : IMapGenerator<Tile>
	{
		/// <summary>
		/// Provides co-ordinates for the player in the form of the room size
		/// </summary>
		Action<Rect> PlayerPlacementFunc { get; set; }

		/// <summary>
		/// Provides for placing NPC's or recording their suggested position
		/// </summary>
		Action<Rect> MobilePlacementFunc { get; set; }
	}
}