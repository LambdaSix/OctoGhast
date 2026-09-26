using System;
using System.Collections.Generic;
using Capsicum;
using NUnit.Framework;
using OctoGhast.Components;
using OctoGhast.Framework.Data;
using OctoGhast.Framework.Ecs;

namespace OctoGhast.Core.Tests {
    [TestFixture]
    public class DefinitionCatalogTests {
        private sealed class TestDefinition : IContentDefinition {
            public DefinitionId Id { get; }
            public string Name { get; }

            public TestDefinition(DefinitionId id, string name) {
                Id = id;
                Name = name;
            }
        }

        [Test]
        public void DefinitionIdHasStableTypedOrdinalRepresentation() {
            var original = new DefinitionId("monster", "zombie_dog");
            var parsed = DefinitionId.Parse("monster::zombie_dog");

            Assert.That(parsed, Is.EqualTo(original));
            Assert.That(parsed.ToString(), Is.EqualTo("monster::zombie_dog"));
            Assert.That(new DefinitionId("item", "zombie_dog"), Is.Not.EqualTo(original));
            Assert.That(DefinitionId.Parse("Monster::zombie_dog"), Is.Not.EqualTo(original));
        }

        [Test]
        public void DefinitionIdRejectsAmbiguousAndEmptyValues() {
            Assert.Throws<FormatException>(() => DefinitionId.Parse("monster"));
            Assert.Throws<FormatException>(() => DefinitionId.Parse("monster::"));
            Assert.Throws<ArgumentException>(() => new DefinitionId("monster::item", "id"));
            Assert.Throws<ArgumentException>(() => new DefinitionId("monster", " "));
        }

        [Test]
        public void CatalogFreezesRegistryMembershipAndCarriesGenerationIdentity() {
            var id = new DefinitionId("monster", "zombie");
            var builder = new DefinitionRegistryBuilder<TestDefinition>();
            builder.Add(new TestDefinition(id, "Zombie"));
            var catalog = builder.Freeze("content-sha256:abc123");

            Assert.That(catalog.ContentGenerationId, Is.EqualTo("content-sha256:abc123"));
            Assert.That(catalog.GetRequired(id).Name, Is.EqualTo("Zombie"));
            Assert.Throws<InvalidOperationException>(() => builder.Add(new TestDefinition(new DefinitionId("monster", "dog"), "Dog")));
            Assert.Throws<KeyNotFoundException>(() => catalog.GetRequired(new DefinitionId("monster", "dog")));
        }

        [Test]
        public void DuplicateDefinitionsRequireExplicitProfileOverride() {
            var id = new DefinitionId("item", "rock");
            var builder = new DefinitionRegistryBuilder<TestDefinition>();
            builder.Add(new TestDefinition(id, "Rock"));

            Assert.Throws<ArgumentException>(() => builder.Add(new TestDefinition(id, "Improved Rock")));
            builder.Replace(new TestDefinition(id, "Improved Rock"));

            Assert.That(builder.Freeze("generation-2").GetRequired(id).Name, Is.EqualTo("Improved Rock"));
        }

        [Test]
        public void EntityComponentsHoldIdentityReferencesNotCapsicumStorageIdentityOrCopies() {
            var entityId = EntityId.Parse("00000000000000000000000000000001");
            var definitionId = DefinitionId.Parse("monster::zombie");
            var identity = new EntityIdentityComponent(entityId);
            var reference = new DefinitionReferenceComponent(definitionId);
            var entity = new Entity().AddComponent(identity).AddComponent(reference);

            Assert.That(identity.Id, Is.EqualTo(entityId));
            Assert.That(reference.DefinitionId, Is.EqualTo(definitionId));
            Assert.That(entity.GetComponent<EntityIdentityComponent>().Id, Is.EqualTo(entityId));
            Assert.That(entity.GetComponent<DefinitionReferenceComponent>().DefinitionId, Is.EqualTo(definitionId));
            Assert.Throws<ArgumentException>(() => new EntityIdentityComponent(default(EntityId)));
            Assert.Throws<ArgumentException>(() => new DefinitionReferenceComponent(default(DefinitionId)));
        }
    }
}
