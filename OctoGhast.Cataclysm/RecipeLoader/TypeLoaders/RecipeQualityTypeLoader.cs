using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using OctoGhast.Cataclysm.LegacyLoader;

namespace OctoGhast.Cataclysm.RecipeLoader {
    public class RecipeQualityTypeLoader : TypeLoader, ITypeLoader<IEnumerable<RecipeQualityGroup>> {
        public IEnumerable<RecipeQualityGroup> Load(JObject jObj, IEnumerable<RecipeQualityGroup> existing = default) {
            if (jObj.TryGetValue("qualities", out var qualities)) {
                foreach (var quality in qualities) {
                    if (quality.Type == JTokenType.Object) {
                        yield return new RecipeQualityGroup(quality as JObject);
                    }
                    else if (quality.Type == JTokenType.Array) {
                        if (quality.Children().Count() == 1) {
                            yield return new RecipeQualityGroup(quality.First() as JObject);
                        }
                        else {
                            yield return new RecipeQualityGroup(quality.Children());
                        }
                    }
                }
            }
        }

        public object Load(JObject data, object existing) {
            return Load(data, existing as IEnumerable<RecipeQualityGroup>);
        }
    }
}