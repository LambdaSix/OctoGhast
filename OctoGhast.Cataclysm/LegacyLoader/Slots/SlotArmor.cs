using System.Collections.Generic;
using OctoGhast.Framework;
using OctoGhast.Units;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class SlotArmor {
        [LoaderInfo("coverage")]
        public IEnumerable<string> Covers { get; set; }

        [LoaderInfo("sided")]
        public bool Sided { get; set; } = false;

        [LoaderInfo("encumbrance")]
        public int Encumbrance { get; set; } = default;

        [LoaderInfo("material_thickness")]
        public int Thickness { get; set; } = default;

        [LoaderInfo("environmental_protection")]
        public int EnvResist { get; set; } = default;

        [LoaderInfo("environmental_protection_with_filter")]
        public int EnvResistWithFilter { get; set; } = default;

        [LoaderInfo("warmth")]
        public int Warmth { get; set; } = default;

        [LoaderInfo("storage")]
        public Volume Storage { get; set; } = "0.0L";

        [LoaderInfo("power_armor")]
        public bool PowerArmour { get; set; } = default;
    }
}