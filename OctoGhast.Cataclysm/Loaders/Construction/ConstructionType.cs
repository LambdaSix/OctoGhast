using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using OctoGhast.Cataclysm.Loaders.Construction.TypeLoaders;
using OctoGhast.Cataclysm.Loaders.Recipe;
using OctoGhast.Cataclysm.Loaders.Recipe.TypeLoaders;
using OctoGhast.Framework;

namespace OctoGhast.Cataclysm.Loaders.Construction {
    public class SkillRequirement {
        public string Name { get; set; }
        public int LevelRequirement { get; set; }

        public SkillRequirement(JArray array) {
            Name = array[0].Value<string>();
            LevelRequirement = array[1].Value<int>();
        }
    }

    public static class ConstructionChecks {
        // TODO: Params should probably be (Mobile m, WorldSpace p) ?
        public static Dictionary<string, Func<dynamic, bool>> PreChecks = new Dictionary<string, Func<dynamic, bool>>()
        {
            ["check_empty"] = _ => throw new NotImplementedException(),
            ["check_deconstruct"] = _ => throw new NotImplementedException(),
            ["check_support"] = _ => throw new NotImplementedException(),
            ["check_down_ok"] = _ => throw new NotImplementedException(),
            ["check_up_ok"] = _ => throw new NotImplementedException(),
            ["check_no_trap"] = _ => throw new NotImplementedException()
        };

        public static Dictionary<string, Func<dynamic, bool>> PostChecks = new Dictionary<string, Func<dynamic, bool>>()
        {
            ["done_vehicle"] = _ => throw new NotImplementedException()
        };

        public static void AddPreCheck(string key, Func<dynamic, bool> condition) {
            PreChecks.Add(key, condition);
        }

        public static void AddPostCheck(string key, Func<dynamic, bool> condition) {
            PostChecks.Add(key, condition);
        }
    }

    

    public class ConstructionType : TemplateType {
        public override string NamespaceName { get; } = "construction";

        [LoaderInfo("description")]
        public string Description { get; set; }

        [LoaderInfo("category")]
        public string Category { get; set; }

        [LoaderInfo("required_skills", TypeLoader = typeof(SkillRequirementTypeLoader))]
        public IEnumerable<SkillRequirement> RequiredSkills { get; set; }

        [LoaderInfo("time")]
        public TimeDuration RequiredTime { get; set; }

        [LoaderInfo("components", TypeLoader =  typeof(RecipeComponentGroupTypeLoader))]
        public IEnumerable<RecipeComponentGroup> Components { get; set; }

        [LoaderInfo("qualities", TypeLoader = typeof(RecipeQualityTypeLoader))]
        public IEnumerable<RecipeQualityGroup> Qualities { get; set; }

        // TODO: Byproducts

        [LoaderInfo("pre_note")]
        public string PreNote { get; set; }

        [LoaderInfo("pre_flags")]
        public IEnumerable<string> PreFlags { get; set; }

        [LoaderInfo("pre_terrain")]
        public string PreTerrain { get; set; }

        [LoaderInfo("post_terrain")]
        public string PostTerrain { get; set; }

        [LoaderInfo("pre_special")]
        public string PreSpecialAction { get; set; }

        [LoaderInfo("post_special")]
        public string PostSpecialAction { get; set; }

        public override string GetIdentifier() {
            return $"{Id ?? (Category + Description.Replace(' ', '_'))}";
        }
    }
}