using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OctoGhast.Cataclysm.Loaders.Recipe;

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
                          " 'tools': [ [ [ 'surface_heat', 10, 'LIST' ] ] ]," +
                          " 'components': [" +
                          "     [ [ 'rag', 1, 'LIST' ] ]" +
                          " ]," +
                          "}";
            var jObj = JObject.Parse(jsonStr);
            var recipe = new RecipeTypeLoader().Load(jObj);

            Assert.That(recipe, Is.Not.Null);

            Assert.That(recipe.Type, Is.EqualTo("recipe"));
            Assert.That(recipe.Result, Is.EqualTo("copper_knife"));
            Assert.That(recipe.Category, Is.EqualTo("CC_WEAPON"));
            Assert.That(recipe.SubCategory, Is.EqualTo("CSC_WEAPON_PIERCING"));
            Assert.That(recipe.SkillsUsed.Single(), Is.EqualTo("fabrication"));
            Assert.That(recipe.Difficulty, Is.EqualTo(8));
            Assert.That(recipe.Time, Is.EqualTo(TimeDuration.FromMinutes(5)));
            Assert.That(recipe.Reversible, Is.EqualTo(true));
            Assert.That(recipe.AutoLearn, Is.EqualTo(true));

            var quality = recipe.Qualities.Single();
            Assert.That(quality.First().QualityName == "HAMMER");
            Assert.That(quality.First().Level == 1);

            var tool = recipe.Tools.First();
            Assert.That(tool.First().ToolName == "surface_heat");
            Assert.That(tool.First().Charges == 10);
            Assert.That(tool.First().UseList == true);

            var component = recipe.Components.Single().Single();
            Assert.That(component.ComponentItemType == "rag");
            Assert.That(component.Quantity == 1);

        }

        [Test]
        public void LoadComplexQualitiesRecipe() {
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
                          " 'qualities': [ " + // Qualities is "one of N" form
                          "     [ { 'id': 'HAMMER', 'level': 1 } ], " +
                          "     [ { 'id': 'SCREW', 'level': 2 }, { 'id': 'SCREW_FINE', 'level': 2 } ]" +
                          " ]," +
                          " 'tools': [ " + // Tools is "one of N" form.
                          "     [ [ 'surface_heat', 10, 'LIST' ], [ 'direct_heat', 10, 'LIST' ] ]," +
                          "     [ [ 'charcoal_kiln', 10 ], [ 'forge', 50 ] ] " +
                          " ]," +
                          " 'components': [" + // Components is "one of N" form
                          "     [ [ 'rag', 1 ], [ 'cloth_scraps', 10, 'LIST', 'NO_RECOVER' ] ]," +
                          "     [ [ 'short_stick', 10 ], [ 'long_stick', 1 ] ]" +
                          " ]," +
                          "}";
            var jObj = JObject.Parse(jsonStr);
            var recipe = new RecipeTypeLoader().Load(jObj);

            var quality = recipe.Qualities.First();
            Assert.That(quality.First().QualityName == "HAMMER");
            Assert.That(quality.First().Level == 1);

            var qualityGroup = recipe.Qualities.ElementAt(1).Qualities;
            Assert.That(qualityGroup.ElementAt(0).QualityName == "SCREW");
            Assert.That(qualityGroup.ElementAt(0).Level == 2);
            Assert.That(qualityGroup.ElementAt(1).QualityName == "SCREW_FINE");
            Assert.That(qualityGroup.ElementAt(1).Level == 2);

            var toolGroup = recipe.Tools.First().Tools;
            Assert.That(toolGroup.ElementAt(0).ToolName == "surface_heat");
            Assert.That(toolGroup.ElementAt(0).Charges == 10);
            Assert.That(toolGroup.ElementAt(0).UseList == true);
            Assert.That(toolGroup.ElementAt(1).ToolName == "direct_heat");
            Assert.That(toolGroup.ElementAt(1).Charges == 10);
            Assert.That(toolGroup.ElementAt(1).UseList == true);

            var componentGroup = recipe.Components.First().Components;
            Assert.That(componentGroup.ElementAt(0).ComponentItemType == "rag");
            Assert.That(componentGroup.ElementAt(0).Quantity == 1);
            Assert.That(componentGroup.ElementAt(0).UseMaterialList == false);
            Assert.That(componentGroup.ElementAt(1).ComponentItemType == "cloth_scraps");
            Assert.That(componentGroup.ElementAt(1).Quantity == 10);
            Assert.That(componentGroup.ElementAt(1).UseMaterialList == true);
        }
    }

    [TestFixture]
    public class ConstructionLoaderTests {

    }

    [TestFixture]
    public class DisassemblyLoaderTests
    {

    }
}