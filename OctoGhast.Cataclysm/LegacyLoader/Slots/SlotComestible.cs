using System.Collections.Generic;
using OctoGhast.Entity;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class SlotComestible {
        public string ComestibleType { get; set; } = default;
        public StringID<ItemType> Tool { get; set; } = StringID<ItemType>.NullId;
        public long DefaultCharges { get; set; } = 1;
        public int Quench { get; set; } = default;

        // TODO: Convert to Calories and mix in Carb/Fat/Protein info
        public int Nutrition { get; set; } = default;

        public TimeDuration SpoilsIn { get; set; } = new TimeDuration(0);
        public int AddictionFactor { get; set; } = 0;
        public AddictionType AddictionType { get; set; } = AddictionType.None;
        public int Fun { get; set; } = 0;
        public int StimulantFactor { get; set; } = 0;
        public int HealthyFactor { get; set; } = 0;
        public int ParasiteFactor { get; set; } = 0;
        public Dictionary<VitaminInfo, int> Vitamins { get; set; }

        /// <summary>
        /// 1 Nutrient ~= 8.7kCal (1Nutr/5Min = 288Nutr/day at 2500kcal/day)
        /// </summary>
        private static float kCalPerNutrients = 2500.0f / (12 * 24);

        public float GetCalories() => Nutrition * kCalPerNutrients;

        public StringID<MonsterGroup> RotSpawn { get; set; } = StringID<MonsterGroup>.NullId;
        public int RotSpawnChance { get; set; } = 10;
    }
}