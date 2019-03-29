using OctoGhast.Entity;
using OctoGhast.Framework;
using OctoGhast.Units;

namespace OctoGhast.Cataclysm.Loaders.Item.DataContainers {
    /// <summary>
    /// Data about a shrapnel/fragmentation device's emitted fragments.
    /// </summary>
    public class ShrapnelData {
        /// <summary>
        /// Total mass of the casing in grams.
        /// </summary>
        [LoaderInfo("casing_mass", true, "0g")]
        public Mass CasingMass { get; set; } = "0g";

        /// <summary>
        /// Mass of each fragment in grams. Large fragments hit harder, small fragments more often.
        /// </summary>
        [LoaderInfo("freagment_mass", false, "5g")]
        public Mass FragmentMass { get; set; } = "5g";

        /// <summary>
        /// Chance to recover a piece of the shrapnel.
        /// </summary>
        [LoaderInfo("recovery", false, 0f)]
        public float RecoveryChance { get; set; }

        /// <summary>
        /// If dropping a recoverable piece of fragmentation, what type of item is it.
        /// </summary>
        [LoaderInfo("drop")]
        public StringID<ItemType> ItemDropType { get; set; }
    }
}