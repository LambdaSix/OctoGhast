﻿using System.Collections.Generic;
using System.Linq;
using OctoGhast.Framework;

namespace OctoGhast.Cataclysm.RecipeLoader {
    public class RecipeType : TemplateType {
        [LoaderInfo("result")]
        public string Result { get; set; }

        [LoaderInfo("id_suffic")]
        public string IdSuffix { get; set; }

        [LoaderInfo("override")]
        public bool Override { get; set; }

        [LoaderInfo("category")]
        public string Category { get; set; }

        [LoaderInfo("subcategory")]
        public string SubCategory { get; set; }

        [LoaderInfo("skill_used")]
        public IEnumerable<string> SkillsUsed { get; set; }

        [LoaderInfo("skills_required")]
        public Dictionary<string, int> SkillsRequired { get; set; }

        [LoaderInfo("reversible")]
        public bool Reversible { get; set; }

        /// <summary>
        /// Automatically learned upon gaining required skills
        /// </summary>
        [LoaderInfo("autolearn")]
        public bool AutoLearn { get; set; }

        /// <summary>
        /// Automatically learned upon gaining listed skills
        /// </summary>
        [LoaderInfo("autolearn")]
        public Dictionary<string,int> AutoLearnSkills { get; set; }

        [LoaderInfo("difficulty")]
        public int Difficulty { get; set; }

        [LoaderInfo("time")]
        public TimeDuration Time { get; set; }

        [LoaderInfo("book_learn")]
        public Dictionary<string, int> BookLearn { get; set; }

        [LoaderInfo("qualities", TypeLoader = typeof(RecipeQualityTypeLoader))]
        public IEnumerable<RecipeQualityGroup> Qualities { get; set; }

        [LoaderInfo("tools", TypeLoader = typeof(RecipeToolGroupTypeLoader))]
        public IEnumerable<RecipeToolGroup> Tools { get; set; }

        [LoaderInfo("components", TypeLoader = typeof(RecipeComponentGroupTypeLoader))]
        public IEnumerable<RecipeComponentGroup> Components { get; set; }

        /// <inheritdoc />
        public override string GetIdentifier() {
            return $"{Id ?? (Result + IdSuffix)}";
        }
    }
}