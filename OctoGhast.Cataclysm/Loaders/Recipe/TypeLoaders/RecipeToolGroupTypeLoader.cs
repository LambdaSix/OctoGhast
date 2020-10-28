using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using OctoGhast.Cataclysm.Loaders.Item;
using OctoGhast.Framework;

namespace OctoGhast.Cataclysm.Loaders.Recipe.TypeLoaders {
    public class RecipeToolGroupTypeLoader : TypeLoader, ITypeLoader<IEnumerable<RecipeToolGroup>> {
        public IEnumerable<RecipeToolGroup> Load(JObject jObj, IEnumerable<RecipeToolGroup> existing = default) {
            if (jObj.TryGetValue("tools", out var tools)) {
                foreach (var tool in tools) {
                    if (tool.Type == JTokenType.Object) {
                        yield return new RecipeToolGroup(tool as JArray);
                    } else if (tool.Type == JTokenType.Array) {
                        if (tool.Children().Count() == 1) {
                            yield return new RecipeToolGroup(tool.First() as JArray);
                        }
                        else {
                            yield return new RecipeToolGroup(tool.Children());
                        }
                    }
                }
            }
        }

        public object Load(JObject data, object existing, LoaderInfoAttribute info = null) {
            return Load(data, existing as IEnumerable<RecipeToolGroup>);
        }
    }
}