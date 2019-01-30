using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using OctoGhast.Framework;
using OctoGhast.Framework.Data.Loading;

namespace OctoGhast.Cataclysm.RecipeLoader {
    [DataObject("RecipeFactory", "Provide hydration of recipe data from storage")]
    public class RecipeFactory : TemplateFactoryBase<RecipeType, RecipeTypeLoader> {
        public RecipeFactory() {
            JsonDataLoader.RegisterConverter(typeof(RecipeQuality),
                (token, type) => new RecipeQuality(token as JObject));
            JsonDataLoader.RegisterConverter(typeof(RecipeComponent),
                (token,type) => new RecipeComponent(token as JObject));
            JsonDataLoader.RegisterConverter(typeof(RecipeTool),
                (token, type) => new RecipeTool(token as JObject));
        }

        /// <inheritdoc />
        public override void LoadFrom(string directory) {
            base.LoadFrom(directory);
        }

        /// <inheritdoc />
        protected override IEnumerable<string> LoadableTypes { get; } = new[]
        {
            "RECIPE", "UNCRAFT"
        };
    }
}