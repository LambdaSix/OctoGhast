using System.Collections.Generic;
using OctoGhast.Entity;
using OctoGhast.Framework;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class SlotComestible {

        [LoaderInfo("comestible_type")]
        public string ComestibleType { get; set; }

        [LoaderInfo("tool")]
        public StringID<ItemType> Tool { get; set; }

        [LoaderInfo("charges", false, 1L)]
        public long DefaultCharges { get; set; }

        [LoaderInfo("quench")]
        public int Quench { get; set; }

        // TODO: Convert to Calories and mix in Carb/Fat/Protein info
        [LoaderInfo("nutrition")]
        public int Nutrition { get; set; }

        [LoaderInfo("calories")]
        public int Calories { get; set; }

        [LoaderInfo("spoils_in")]
        public TimeDuration SpoilsIn { get; set; }

        [LoaderInfo("addiction_potential")]
        public int AddictionFactor { get; set; }

        [LoaderInfo("addiction_type")]
        public StringID<AddictionType> AddictionType { get; set; }

        [LoaderInfo("fun")]
        public int Fun { get; set; }

        [LoaderInfo("stim")]
        public int StimulantFactor { get; set; }

        [LoaderInfo("healthy")]
        public int HealthyFactor { get; set; }

        [LoaderInfo("parasites")]
        public int ParasiteFactor { get; set; }

        [LoaderInfo("vitamins", false, null)]
        public Dictionary<string, float> Vitamins { get; set; }

        [LoaderInfo("rot_spawn", false, null)]
        public StringID<MonsterGroup> RotSpawn { get; set; }

        [LoaderInfo("rot_spawn_chance", false, 10)]
        public int RotSpawnChance { get; set; }

        /// <summary>
        /// 1 Nutrient ~= 8.7kCal (1Nutr/5Min = 288Nutr/day at 2500kcal/day)
        /// </summary>
        private static float kCalPerNutrients = 2500.0f / (12 * 24);

        public float GetCalories() => Nutrition * kCalPerNutrients;
    }
}