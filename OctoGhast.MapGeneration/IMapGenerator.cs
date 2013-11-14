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
}