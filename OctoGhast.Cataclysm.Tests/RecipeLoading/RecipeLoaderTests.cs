using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OctoGhast.Cataclysm.RecipeLoader;

namespace OctoGhast.Cataclysm.Tests.RecipeLoading {
    [TestFixture]
    public class RecipeLoaderTests {
        [OneTimeSetUp]
        public void Setup() {
            // Setup the custom converters.
            var temp = new RecipeFactory();
        }

        [Test]
        public void LoadSimpleRecipe() {
            var jsonStr = "{" +
                          " 'type': 'recipe'," +
                          " 'result': 'copper_knife'," +
                          " 'category': 'CC_WEAPON'," +
                          " 'subcategory': 'CSC_WEAPON_PIERCING'," +
                          " 'skill_used': 'fabrication'," +
                          " 'difficulty': 8," +
                          " 'time': 5000," +
                          " 'reversible': true," +
                          " 'autolearn': true," +
                          " 'qualities': [ { 'id': 'HAMMER', 'level': 1 } ]," +
                          " 'tools': [ [ [ 'surface_heart', 10, 'LIST' ] ] ]," +
                          " 'components': [" +
                          "     [ [ 'rag', 1 ], [ 'felt_patch', 1 ], [ 'duct_tape', 50 ], [ 'cordage_short', 2, 'LIST' ], [ 'filament', 50, 'LIST' ] ]," +
                          "     [ [ 'scrap_copper', 2 ], [ 'copper', 200 ] ]" +
                          " ]," +
                          "}";
            var jObj = JObject.Parse(jsonStr);
            var recipe = new RecipeTypeLoader().Load(jObj);

            Assert.That(recipe, Is.Not.Null);
        }
    }
}