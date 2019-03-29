using OctoGhast.Cataclysm.Loaders.Item.Types;
using OctoGhast.Units;

namespace OctoGhast.Cataclysm.Loaders.Item.DataContainers {
    /// <summary>
    /// Defines data about an object that can explode, based on it's Material & Mass.
    /// Covers Fragmentation & Concussive blasts.
    /// </summary>
    public class ExplosiveData {
        /// <summary>
        /// Mass of the explosive material
        /// </summary>
        public Mass Mass { get; set; }

        /// <summary>
        /// The specific explosive material being used
        /// </summary>
        public ExplosiveMaterial ExplosiveMaterial { get; set; }

        /// <summary>
        /// This explosive object is fitted with an impact fuze and detonates when hitting something.
        /// If <see cref="FuzeDelay"/> is also set, this changes to be a delayed impact fuze.
        /// We assume this is a 100% reliable fuze for simplicity.
        /// </summary>
        public bool ImpactFuze { get; set; }

        /// <summary>
        /// The delay from initiating the fuze to the point of detonation.
        /// If both <see cref="ImpactFuze"/> and <see cref="FuzeDelay"/> are both set, this is delay from impact before detonation.
        /// </summary>
        public TimeDuration FuzeDelay { get; set; } = TimeDuration.FromTurns(1); // 6 seconds is a standard M67 grenade's fuze

        /// <summary>
        /// Information about the fragmenation/shrapnel this explosive may emit.
        /// Without this field, the explosive will act as a pure concussive detonation.
        /// </summary>
        public ShrapnelData Shrapnel { get; set; }
    }
}