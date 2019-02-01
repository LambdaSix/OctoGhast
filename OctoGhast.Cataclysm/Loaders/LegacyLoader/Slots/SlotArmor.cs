using System.Collections.Generic;
using OctoGhast.Framework;
using OctoGhast.Units;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class SlotArmor {
        [LoaderInfo("coverage")]
        public int Coverage { get; set; }

        [LoaderInfo("covers")]
        public IEnumerable<string> Covers { get; set; }

        [LoaderInfo("sided")]
        public bool Sided { get; set; }

        [LoaderInfo("encumbrance")]
        public int Encumbrance { get; set; }

        [LoaderInfo("material_thickness")]
        public int Thickness { get; set; }

        [LoaderInfo("environmental_protection")]
        public int EnvResist { get; set; }

        [LoaderInfo("environmental_protection_with_filter")]
        public int EnvResistWithFilter { get; set; }

        [LoaderInfo("warmth")]
        public int Warmth { get; set; }

        [LoaderInfo("storage", defaultValue: "0.0L")]
        public Volume Storage { get; set; }

        [LoaderInfo("power_armor")]
        public bool PowerArmour { get; set; }
    }
}