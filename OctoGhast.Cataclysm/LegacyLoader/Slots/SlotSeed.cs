using System.Collections.Generic;
using OctoGhast.Entity;
using OctoGhast.Framework;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class SlotSeed {
        [LoaderInfo("grow")]
        public TimeDuration GrowthTime { get; set; } = new TimeDuration();

        [LoaderInfo("fruit_div")]
        public int FruitDivisor { get; set; } = 1;

        [LoaderInfo("plant_name")]
        public string PlantName { get; set; }

        [LoaderInfo("fruit")]
        public StringID<ItemType> Fruit { get; set; }

        [LoaderInfo("seeds")]
        public bool SpawnSeeds { get; set; } = true;

        [LoaderInfo("byproducts")]
        public IEnumerable<string> ByProducts { get; set; }
    }
}