using System.Collections.Generic;
using OctoGhast.Entity;
using OctoGhast.Framework;

namespace OctoGhast.Cataclysm.Loaders.Item.Slots {
    public class SlotSeed {
        [LoaderInfo("grow")]
        public TimeDuration GrowthTime { get; set; }

        [LoaderInfo("fruit_div", defaultValue: 1)]
        public int FruitDivisor { get; set; }

        [LoaderInfo("plant_name")]
        public string PlantName { get; set; }

        [LoaderInfo("fruit")]
        public StringID<ItemType> Fruit { get; set; }

        [LoaderInfo("seeds", defaultValue: true)]
        public bool SpawnSeeds { get; set; }

        [LoaderInfo("byproducts")]
        public IEnumerable<string> ByProducts { get; set; }
    }
}