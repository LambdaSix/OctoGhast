using System.Linq;
using NUnit.Framework;

namespace OctoGhast.Cataclysm.Tests.Architecture
{
    [TestFixture]
    public class LegacyActorInheritanceTests
    {
        private static readonly string[] RetainedLegacyActorTypes =
        {
            "OctoGhast.Framework.Mobile.Player",
            "OctoGhast.Framework.Mobile.BaseCreature",
            "OctoGhast.Framework.Mobile.BaseNpc"
        };

        [Test]
        public void ProductionAssembliesDoNotAddLegacyActorSubclasses()
        {
            var productionAssemblies = new[]
            {
                typeof(OctoGhast.World).Assembly,
                typeof(OctoGhast.Cataclysm.Items.BaseItem).Assembly
            };

            var legacyDescendants = productionAssemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsClass && IsLegacyActorDescendant(type))
                .Select(type => type.FullName)
                .Distinct()
                .OrderBy(name => name)
                .ToArray();

            Assert.That(legacyDescendants, Is.EquivalentTo(RetainedLegacyActorTypes),
                "The retained legacy classes are a migration boundary, not an extension point. " +
                "New actor capabilities belong in ECS composition and systems.");
        }

        private static bool IsLegacyActorDescendant(System.Type type)
        {
            for (var ancestor = type.BaseType; ancestor != null; ancestor = ancestor.BaseType)
            {
                if (ancestor.Namespace == "OctoGhast.Framework.Mobile")
                    return true;
            }

            return false;
        }
    }
}
