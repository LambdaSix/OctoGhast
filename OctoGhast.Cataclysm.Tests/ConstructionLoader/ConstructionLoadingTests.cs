using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OctoGhast.Cataclysm.Loaders.Construction;
using OctoGhast.Cataclysm.Loaders.Construction.TypeLoaders;

namespace OctoGhast.Cataclysm.Tests.ConstructionLoader {
    [TestFixture]
    public class ConstructionLoadingTests {

        [Test]
        public void LoadRequiredSkills() {
            var json = @"{ 'required_skills' : [ [ 'fabrication', 10 ], [ 'mechanics', 10 ] ] }";
            var jsonStr = json.Replace('\'', '\"');

            var skills = new SkillRequirementTypeLoader().Load(JObject.Parse(jsonStr));

            Assert.That(skills.Count(), Is.EqualTo(2));

            var testSkill = skills.First();
            Assert.That(testSkill.Name, Is.EqualTo("fabrication"));
            Assert.That(testSkill.LevelRequirement, Is.EqualTo(10));

            var testSkill2 = skills.Skip(1).Take(1).Single();
            Assert.That(testSkill2.Name, Is.EqualTo("mechanics"));
            Assert.That(testSkill2.LevelRequirement, Is.EqualTo(10));
        }
    }
}