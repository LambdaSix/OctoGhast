using OctoGhast.Entity;
using OctoGhast.Framework;
using OctoGhast.Units;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class SlotContainer {
        [LoaderInfo("contains", false, "0.0L")]
        public Volume Contains { get; set; }

        [LoaderInfo("seals", false, false)]
        public bool Seals { get; set; }

        [LoaderInfo("watertight", false, false)]
        public bool Watertight { get; set; }

        [LoaderInfo("preserves", false, false)]
        public bool Preserves { get; set; }

        [LoaderInfo("unseals_into", false, null)]
        public StringID<ItemType> UnsealsInto { get; set; } = StringID<ItemType>.NullId;
    }
}