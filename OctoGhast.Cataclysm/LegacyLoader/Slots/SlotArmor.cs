using System.Collections.Generic;
using OctoGhast.Units;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class SlotArmor {
        public IEnumerable<string> Covers { get; set; }
        public bool Sided { get; set; } = false;
        public int Encumber { get; set; } = default;

        public int Thickness { get; set; } = default;
        public int EnvResist { get; set; } = default;
        public int EnvResistWithFilter { get; set; } = default;
        public int Warmth { get; set; } = default;

        // TODO: Units.Volume usage
        public Volume Storage { get; set; } = "0.0L";

        public bool PowerArmour { get; set; } = default;
    }
}