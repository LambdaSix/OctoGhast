using System.Collections.Generic;
using OctoGhast.Entity;
using OctoGhast.Framework;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class SlotBrewable {
        [LoaderInfo("results")]
        public IEnumerable<StringID<ItemType>> Results { get; set; }

        [LoaderInfo("time")]
        public TimeDuration Time { get; set; }
    }
}