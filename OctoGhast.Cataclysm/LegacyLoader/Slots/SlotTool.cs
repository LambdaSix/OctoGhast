using System.Collections.Generic;
using OctoGhast.Entity;
using OctoGhast.Framework;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class SlotTool {
        [LoaderInfo("ammo")]
        public StringID<AmmoType> AmmoId { get; set; }

        [LoaderInfo("revert_to")]
        public StringID<ItemType> RevertsTo { get; set; }

        [LoaderInfo("revert_msg")]
        public string RevertMessage { get; set; }

        [LoaderInfo("sub")]
        public string SubType { get; set; }

        [LoaderInfo("max_charges")]
        public long MaxCharges { get; set; }

        [LoaderInfo("initial_charges")]
        public long DefaultCharges { get; set; }

        [LoaderInfo("rand_charges")]
        public IEnumerable<long> RandomCharges { get; set; }

        [LoaderInfo("charges_per_use")]
        public int ChargesPerUse { get; set; }

        [LoaderInfo("turns_per_charge")]
        public int TurnsPerCharge { get; set; }
    }
}