using OctoGhast.Cataclysm.Loaders.Terrain;
using OctoGhast.Framework;

namespace OctoGhast.Cataclysm.Loaders.ButcherEntry
{
    /// <summary>
    /// Used in the ButcherResult data type.
    ///
    /// Extends the HarvestEntry to handle creatures and various stages of butchery.
    /// </summary>
    /// <example>
    /// {
    ///  "id": "mammal_large_fur",
    ///  "//": "drops large stomach",
    ///  "type": "harvest",
    ///  "entries": [
    ///     { "drop": "meat", "type": "flesh", "mass_ratio": 0.32 },
    ///     { "drop": "meat_scrap", "type": "flesh", "mass_ratio": 0.01 },
    ///     { "drop": "lung", "type": "flesh", "mass_ratio": 0.0035 },
    ///     { "drop": "liver", "type": "offal", "mass_ratio": 0.01 },
    ///     { "drop": "brain", "type": "flesh", "mass_ratio": 0.005 },
    ///     { "drop": "sweetbread", "type": "flesh", "mass_ratio": 0.002 },
    ///     { "drop": "kidney", "type": "offal", "mass_ratio": 0.002 },
    ///     { "drop": "stomach_large", "base_num": [ 1, 1 ], "max": 1, "type": "offal" },
    ///     { "drop": "bone", "type": "bone", "mass_ratio": 0.15 },
    ///     { "drop": "sinew", "type": "bone", "mass_ratio": 0.00035 },
    ///     { "drop": "fur", "type": "skin", "mass_ratio": 0.02 },
    ///     { "drop": "fat", "type": "flesh", "mass_ratio": 0.07 }
    /// ]}
    /// </example>
    public class ButcheryData : HarvestEntry {

        /// <summary>
        /// The type of butcher harvest this is.
        /// </summary>
        [LoaderInfo("type")]
        public ButcherType Type { get; set; }

        /// <summary>
        /// Multiplier of how much of the monster's weight comprises the associated item.
        /// To conserve mass, keep between 0 and 1 combined with all drops.
        /// </summary>
        [LoaderInfo("mass_ratio")]
        public float MassRatio { get; set; }
    }
}