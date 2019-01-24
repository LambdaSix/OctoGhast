using System.Collections.Generic;
using OctoGhast.Framework;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class SlotToolMod {
        [LoaderInfo("acceptable_ammo")]
        public IEnumerable<string> AcceptableAmmo { get; set; }

        [LoaderInfo("ammo_modifier")]
        public string AmmoModifier { get; set; }

        [LoaderInfo("magazine_adaptor")]
        public Dictionary<string, IEnumerable<string>> MagazineAdapter { get; set; }

        [LoaderInfo("capacity_multiplier")]
        public float CapacityMultiplier { get; set; } = 1.0f;
    }
}