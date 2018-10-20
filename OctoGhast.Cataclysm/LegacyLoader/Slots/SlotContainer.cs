using OctoGhast.Units;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class SlotContainer {
        public Volume Contains { get; set; } = "0.0L";
        public bool Seals { get; set; } = default;
        public bool Watertight { get; set; } = default;
        public bool Preserves { get; set; } = default;
        public StringID<ItemType> UnsealsInto { get; set; } = StringID<ItemType>.NullId;
    }
}