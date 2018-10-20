using OctoGhast.Units;

namespace OctoGhast.Cataclysm.LegacyLoader {
    /// <summary>
    /// Data about a shrapnel/fragmentation device's emitted fragments.
    /// </summary>
    public class ShrapnelData {
        /// <summary>
        /// Total mass of the casing in grams.
        /// </summary>
        public Mass CasingMass { get; set; } = "0g";

        /// <summary>
        /// Mass of each fragment in grams. Large fragments hit harder, small fragments more often.
        /// </summary>
        public Mass FragmentMass { get; set; } = "5g";

        /// <summary>
        /// Chance to recover a piece of the shrapnel.
        /// </summary>
        public float RecoveryChance { get; set; } = 0.0f;

        /// <summary>
        /// If dropping a recoverable piece of fragmentation, what type of item is it.
        /// </summary>
        public StringID<ItemType> ItemDropType { get; set; } = StringID<ItemType>.NullId;
    }
}