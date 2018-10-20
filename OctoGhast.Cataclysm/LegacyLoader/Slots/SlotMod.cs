using System.Collections.Generic;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class SlotMod {
        public IEnumerable<string> AcceptableAmmo { get; set; }
        public string AmmoModifier { get; set; }
        public Dictionary<string, IEnumerable<string>> MagazineAdapter { get; set; }
        public float CapacityMultiplier { get; set; } = 1.0f;
    }
}