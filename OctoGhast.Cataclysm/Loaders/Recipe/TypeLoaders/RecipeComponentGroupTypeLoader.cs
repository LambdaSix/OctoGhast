using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using OctoGhast.Cataclysm.Loaders.Item;
using OctoGhast.Framework;

namespace OctoGhast.Cataclysm.Loaders.Recipe.TypeLoaders {
    public class RecipeComponentGroupTypeLoader : TypeLoader, ITypeLoader<IEnumerable<RecipeComponentGroup>> {
        public IEnumerable<RecipeComponentGroup> Load(JObject jObj, IEnumerable<RecipeComponentGroup> existing = default) {
            if (jObj.TryGetValue("components", out var components)) {
                foreach (var component in components) {
                    if (component.Type == JTokenType.Object) {
                        yield return new RecipeComponentGroup(component as JArray);
                    }
                    else if (component.Type == JTokenType.Array) {
                        if (component.Children().Count() == 1) {
                            yield return new RecipeComponentGroup(component.First() as JArray);
                        }
                        else {
                            yield return new RecipeComponentGroup(component.Children());
                        }
                    }
                }
            }
        }

        public object Load(JObject data, object existing, LoaderInfoAttribute info = null) {
            return Load(data, existing as IEnumerable<RecipeComponentGroup>);
        }
    }
}