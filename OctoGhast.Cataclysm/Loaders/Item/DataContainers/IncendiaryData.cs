using OctoGhast.Cataclysm.LegacyLoader;
using OctoGhast.Units;

namespace OctoGhast.Cataclysm.Loaders.Item.DataContainers {
    /// <summary>
    /// Defines data about an object that will spread fire or heat on triggering.
    /// </summary>
    public class IncendiaryData {
        /// <summary>
        /// Amount of incendiary material contained.
        /// </summary>
        public Volume Volume { get; set; }

        /// <summary>
        /// The material this device is filled with
        /// </summary>
        public Material IncendiaryMaterial { get; set; }

        /// <summary>
        /// Does this explode on impact with something?
        /// </summary>
        public bool ImpactFuze { get; set; }

        /// <summary>
        /// The delay from initiating the fuze to the point of detonation.
        /// If both <see cref="ImpactFuze"/> and <see cref="FuzeDelay"/> are set, this is the delay from impact before detonation.
        /// </summary>
        public TimeDuration FuzeDelay { get; set; }
    }
}