using System.Collections.Generic;
using OctoGhast.Framework;

namespace OctoGhast.Cataclysm.Loaders.Terrain {
    /// <summary>
    /// Generic Harvestable entry suitable for Terrain, Animal and Crop harvests.
    /// </summary>
    public class HarvestEntry {
        
        /// <summary>
        /// ItemType that this harvest entry will produce
        /// </summary>
        [LoaderInfo("drop")]
        public string Drop { get; set; }

        /// <summary>
        /// This harvest entry will produce a quantity between these two values.
        /// 
        /// Array of 2 floating point values.
        /// </summary>
        [LoaderInfo("base_num", expectedCount: 2)]
        public IEnumerable<float> BaseNumber { get; set; }

        /// <summary>
        /// This harvest entry benefits from a skill, and the min/max
        /// drop count is increased by (Skill * Scalar).
        ///
        /// Array of 2 floating point values.
        /// </summary>
        /// <example>
        /// BaseNum of [5,10], SkillBonus of [ 0, 0.6 ], and a character skill of 5 in Survival:
        ///     Min: 5 * (0 * 5) = 5
        ///     Max: 10 * (0.6 * 5) = 30
        /// </example>
        [LoaderInfo("skill_bonus", expectedCount: 2)]
        public IEnumerable<float> SkillBonus { get; set; }

        /// <summary>
        /// Which skill should be used for SkillBonus calculation.
        /// </summary>
        [LoaderInfo("skill_used")]
        public string SkillUsed { get; set; }

        /// <summary>
        /// Absolute limit on how many items can be dropped for this entry.
        /// </summary>
        /// <example>
        /// This is applied after BaseNum and SkillBonus and acts as:
        ///     Min( (BaseNum[*]*SkillBonus[*])[rng(0,1)], Maximum)
        /// </example>
        [LoaderInfo("max")]
        public float Maximum { get; set; }
    }
}