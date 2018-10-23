using System.Collections.Generic;
using OctoGhast.Entity;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class SlotTool {
        public StringID<AmmoType> AmmoId { get; set; } = StringID<AmmoType>.NullId;
        public StringID<ItemType> RevertsTo { get; set; } = StringID<ItemType>.NullId;
        public string RevertMessage { get; set; }
        public string SubType { get; set; }
        public long MaxCharges { get; set; } = default;
        public long DefaultCharges { get; set; } = default;
        public IEnumerable<long> RandomCharges { get; set; }
        public int ChargesPerUse { get; set; } = default;
        public int TurnsPerCharge { get; set; } = default;
    }
}