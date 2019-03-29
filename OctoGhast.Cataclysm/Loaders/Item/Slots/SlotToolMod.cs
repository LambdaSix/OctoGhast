using System.Collections.Generic;
using OctoGhast.Framework;

namespace OctoGhast.Cataclysm.Loaders.Item.Slots {
    public class SlotToolMod {
        [LoaderInfo("acceptable_ammo")]
        public IEnumerable<string> AcceptableAmmo { get; set; }

        [LoaderInfo("ammo_modifier")]
        public string AmmoModifier { get; set; }

        [LoaderInfo("magazine_adaptor")]
        public Dictionary<string, IEnumerable<string>> MagazineAdapter { get; set; }

        [LoaderInfo("capacity_multiplier", defaultValue: 1.0f)]
        public float CapacityMultiplier { get; set; }
    }
}