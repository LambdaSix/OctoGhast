using System.Collections.Generic;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class SlotSeed {
        public TimeDuration GrowthTime { get; set; } = new TimeDuration();
        public int FruitDivisor { get; set; } = 1;
        public string PlantName { get; set; }
        public StringID<ItemType> Fruit { get; set; }
        public bool SpawnSeeds { get; set; } = true;
        public IEnumerable<string> ByProducts { get; set; }
    }
}