using System.Linq;
using NUnit.Framework;
using OctoGhast.Entity;
using OctoGhast.Units;

namespace OctoGhast.Core.Tests {
    public class Foo { }

    public class Bar { }

    [TestFixture]
    public class StringIdTests {
        [Test]
        public void SameTypeAndValueEqual() {
            var a = new StringID<Foo>("A");
            var b = new StringID<Foo>("A");

            Assert.That(a == b);
            Assert.That(a, Is.EqualTo(b));
            Assert.AreEqual(a, b);
        }

        [Test]
        public void DifferentTypesNotComparable() {
            var a = new StringID<Foo>("A");
            var b = new StringID<Bar>("A");

            // Assert.That(a != b); // Automatically invalid between <Foo> and <Bar> by the compiler, thanks csc
            Assert.That(a, Is.Not.EqualTo(b));
            Assert.AreNotEqual(a, b);
        }

        [Test]
        public void SameTypeDifferentValueNotEqual() {
            var a = new StringID<Foo>("A");
            var b = new StringID<Foo>("B");

            Assert.That(a != b);
            Assert.That(a, Is.Not.EqualTo(b));
            Assert.AreNotEqual(a, b);
        }

        [Test]
        public void Sortable() {
            var ids = new StringID<Foo>[]
            {
                "A", "B", "D", "E", "G", "F", "m"
            };

            ids.ToList().Sort();
            Assert.That(ids, Is.EquivalentTo(new StringID<Foo>[] {"m", "A", "B", "D", "E", "F", "G"}));
        }

        [Test]
        public void QualifiedName() {
            var id = CoreMaterials.Water;
            var qualifiedName = id.Id;

            Assert.That(qualifiedName, Is.EqualTo("Material::core_water"));

        }

        [Test]
        public void Castable() {
            var id = new StringID<Foo>("A");
            var idStr = "A";

            StringID<Foo> idFromStr = idStr;
            var idFromStr2 = (StringID<Foo>) idStr;

            string strFromId = id;
            var strFromId2 = (string) id;

            Assert.That(idFromStr, Is.Not.Null);
            Assert.That(idFromStr, Is.EqualTo((StringID<Foo>)"A"));

            Assert.That(idFromStr2, Is.Not.Null);
            Assert.That(idFromStr2, Is.EqualTo((StringID<Foo>)"A"));

            Assert.That(strFromId, Is.Not.Null);
            Assert.That(strFromId, Is.EqualTo("A"));

            Assert.That(strFromId2, Is.Not.Null);
            Assert.That(strFromId2, Is.EqualTo("A"));
        }
    }
}