using System.Collections.Generic;
using OctoGhast.Entity;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class SlotBrewable {
        public IEnumerable<StringID<ItemType>> Results { get; set; }
        private TimeDuration Time { get; set; } = new TimeDuration(0);
    }
}