using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace OctoGhast.Cataclysm.Loaders.Recipe {
    public class RecipeQualityGroup : IEnumerable<RecipeQuality> {
        public IEnumerable<RecipeQuality> Qualities;

        public RecipeQualityGroup(IEnumerable<JToken> qualities) {
            Qualities = LoadQualities(qualities).ToList();
        }

        public RecipeQualityGroup(JObject jObj) {
            Qualities = new[] {new RecipeQuality(jObj)};
        }

        private static IEnumerable<RecipeQuality> LoadQualities(IEnumerable<JToken> qualities) {
            return qualities.Where(s => s.Type == JTokenType.Object)
                            .Select(quality => new RecipeQuality(quality as JObject));
        }

        public IEnumerator<RecipeQuality> GetEnumerator() {
            return Qualities.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return ((IEnumerable) Qualities).GetEnumerator();
        }
    }

    [DebuggerDisplay("{QualityName}::{Level}")]
    public class RecipeQuality {
        public string QualityName { get; set; }
        public int Level { get; set; }

        public RecipeQuality(JObject jObj) {
            QualityName = jObj["id"].Value<string>();
            Level = jObj["level"].Value<int>();
        }

        public RecipeQuality(string qualityName, int level) {
            QualityName = qualityName;
            Level = level;
        }
    }
}