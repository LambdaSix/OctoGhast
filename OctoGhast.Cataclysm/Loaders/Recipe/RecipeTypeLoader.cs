using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using OctoGhast.Cataclysm.LegacyLoader;
using OctoGhast.Cataclysm.Loaders.Item;
using OctoGhast.Framework;

namespace OctoGhast.Cataclysm.Loaders.Recipe {
    public class RecipeTypeLoader : TypeLoader, ITypeLoader<RecipeType>
    {
        /// <inheritdoc />
        public RecipeType Load(JObject jObj, RecipeType existing = default(RecipeType)) {
            var load = new RecipeType();
            load.Id = jObj.ReadProperty(() => existing.Id);
            load.Type = jObj.ReadProperty(() => existing.Type);
            load.Result = jObj.ReadProperty(() => existing.Result);
            load.IdSuffix = jObj.ReadProperty(() => existing.IdSuffix);
            load.Category = jObj.ReadProperty(() => existing.Category);
            load.SubCategory = jObj.ReadProperty(() => existing.SubCategory);
            load.SkillsUsed = jObj.ReadProperty(() => existing.SkillsUsed);
            load.Difficulty = jObj.ReadProperty(() => existing.Difficulty);
            load.Time = LoadRecipeTime(jObj, existing);
            load.Reversible = jObj.ReadProperty(() => existing.Reversible);
            LoadAutoLearn(jObj, existing, v => load.AutoLearn = v, map => load.AutoLearnSkills = map);
            load.Qualities = jObj.ReadProperty(() => existing.Qualities).ToList();
            load.Tools = jObj.ReadProperty(() => existing.Tools).ToList();
            load.Components = jObj.ReadProperty(() => existing.Components).ToList();

            return load;
        }

        private static TimeDuration LoadRecipeTime(JObject jObj, RecipeType existing) {
            if (jObj.TryGetValue("time", out var tokenVal)) {
                if (tokenVal.Type == JTokenType.Integer) {
                    // If the value is an integer, then it's probably an old style recipe time.
                    // These are expressed at a value of 1000/Minute, so convert it to the new system.
                    // ReadProperty gives us relative/proportional for free
                    var time = jObj.ReadProperty(
                        name: "time",
                        existingValue: (existing?.Time.Turns ?? 0) * 100,
                        mapFunc: s => new TimeDuration(long.Parse(s) / 100),
                        relativeFunc: (v, acc) => v + acc,
                        // Just clamp it down so that 2.1 * 125 seconds == 262 rather than 262.5
                        proportionalFunc: (v, acc) => (long)Math.Floor(v * acc)); 

                    return time;
                }

                // Otherwise, probably a new '1 hour' style string.
                if (tokenVal.Type == JTokenType.String) {
                    return jObj.ReadProperty(() => existing.Time);
                }
            }

            return null;
        }

        private static void LoadAutoLearn(JObject jObj, RecipeType existing, Action<bool> autoLearn, Action<Dictionary<string,int>> autolearnSkills) {
            if (!jObj.TryGetValue("autolearn", out var token))
                return;

            switch (token.Type) {
                case JTokenType.Array:
                    autolearnSkills(jObj.ReadProperty(() => existing.AutoLearnSkills));
                    break;
                case JTokenType.Boolean:
                    autoLearn(token.Value<bool>());
                    break;
            }
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            return Load(data, existing as RecipeType);
        }
    }
}