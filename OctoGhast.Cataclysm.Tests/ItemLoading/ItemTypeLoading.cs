using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OctoGhast.Cataclysm.LegacyLoader;
using OctoGhast.Framework;
using OctoGhast.Units;

namespace OctoGhast.Cataclysm.Tests.ItemLoading {
    public class BasicItemType {
        [LoaderInfo("type", true)]
        public string Type { get; set; }

        [LoaderInfo("id", true)]
        public string Id { get; set; }

        [LoaderInfo("mass", true, 0)]
        public int Mass { get; set; }

        [LoaderInfo("volume", true, 0.0)]
        public double Volume { get; set; }

        [LoaderInfo("volume_real", false, "0.0L")]
        public Volume RealVolume { get; set; }

        [LoaderInfo("mass_real", false, "0.0KG")]
        public Mass RealMass { get; set; }

        [LoaderInfo("properties", false, null)]
        public Dictionary<string,string> Properties { get; set; }

        [LoaderInfo("qualities", false, null)]
        public Dictionary<string,int> Qualities { get; set; }

        [LoaderInfo("flags", false, null)]
        public IEnumerable<string> Flags { get; set; }

        [LoaderInfo("magazines", false, null)]
        public Dictionary<string,IEnumerable<string>> Magazines { get; set; }

        [LoaderInfo("tools", false, null)]
        public Dictionary<string,int> Tools { get; set; }
    }

    [TestFixture]
    public class ItemTypeLoading {
        [Test]
        public void LoadSimpleItem() {
            var itemString = @"{
                ""type"": ""GENERIC"",
                ""id"": ""concrete"",
                ""mass"": ""5"",
                ""volume"": ""-11""
            }";

            var itype = new BasicItemType();

            var jObj = JObject.Parse(itemString);
            itype.Id = jObj.ReadProperty(() => itype.Id);
            itype.Type = jObj.ReadProperty(() => itype.Type);
            itype.Mass = jObj.ReadProperty(() => itype.Mass);
            itype.Volume = jObj.ReadProperty(() => itype.Volume);

            Assert.That(itype.Id, Is.Not.Null);
            Assert.That(itype.Type, Is.Not.Null);
            Assert.That(itype.Mass, Is.Not.Null);
            Assert.That(itype.Volume, Is.Not.Null);

            Assert.AreEqual(itype.Type, "GENERIC");
            Assert.AreEqual(itype.Id, "concrete");
            Assert.AreEqual(itype.Mass, 5);
            Assert.AreEqual(itype.Volume, -11.0);
        }

        [Test]
        public void LoadGenericContainers()
        {
            var itemString = @"{
                ""properties"": {
                    ""keyA"": ""value""
                },
                ""qualities"": {
                    ""CUTTING"": ""1""
                },
                ""flags"": [""FLAMMABLE"", ""IGNITABLE""],
                ""magazines"": [ ""300"", [ ""lw223mag"" ], ""50"", [ ""bigMag50"", ""alternativeLittleMag"" ] ],
                ""tools"": [
                [ [""soldering_iron"", 10], [ ""toolset"", 10] ]
                ],
            }";

            var itype = new BasicItemType();
            var jObj = JObject.Parse(itemString);

            itype.Properties = jObj.ReadProperty(() => itype.Properties);
            itype.Qualities = jObj.ReadProperty(() => itype.Qualities);
            itype.Flags = jObj.ReadProperty(() => itype.Flags);
            itype.Magazines = jObj.ReadProperty(() => itype.Magazines);
            itype.Tools = jObj.ReadProperty(() => itype.Tools);

            Assert.That(itype.Properties, Is.Not.Null);
            Assert.That(itype.Properties, Is.Not.Empty);

            Assert.That(itype.Qualities, Is.Not.Null);
            Assert.That(itype.Qualities, Is.Not.Empty);

            Assert.That(itype.Flags, Is.Not.Null);
            Assert.That(itype.Flags, Is.Not.Empty);

            Assert.That(itype.Magazines, Is.Not.Null);
            Assert.That(itype.Magazines, Is.Not.Empty);

            Assert.That(itype.Properties.ContainsKey("keyA"));
            Assert.That(itype.Properties["keyA"], Is.EqualTo("value"));

            Assert.That(itype.Qualities.ContainsKey("CUTTING"));
            Assert.That(itype.Qualities["CUTTING"], Is.EqualTo(1));

            Assert.That(itype.Flags, Contains.Item("FLAMMABLE").And.Contain("IGNITABLE"));

            Assert.That(itype.Magazines["300"], Contains.Item("lw223mag"));
            Assert.That(itype.Magazines["50"], Contains.Item("bigMag50").And.Contains("alternativeLittleMag"));
        }

        [Test]
        public void LoadComplexItem() {
            var itemString = @"{
                ""volume_real"": ""20L"",
                ""mass_real"": ""20KG""
            }";

            var itype = new BasicItemType();
            var jObj = JObject.Parse(itemString);

            itype.RealVolume = jObj.ReadProperty(() => itype.RealVolume);
            itype.RealMass = jObj.ReadProperty(() => itype.RealMass);

            Assert.That(itype.RealVolume, Is.Not.Null);
            Assert.That(itype.RealMass, Is.Not.Null);

            Assert.That(itype.RealMass, Is.EqualTo((Mass) "20KG"));
            Assert.That(itype.RealVolume, Is.EqualTo((Volume) "20L"));
        }

        [Test]
        public void LoadLegacyUnitlessItem()
        {
            var itemString = @"{
                ""volume_real"": ""80"",
                ""mass_real"": ""20000""
            }";

            var itype = new BasicItemType();
            var jObj = JObject.Parse(itemString);

            itype.RealVolume = jObj.ReadProperty(() => itype.RealVolume);
            itype.RealMass = jObj.ReadProperty(() => itype.RealMass);

            Assert.That(itype.RealVolume, Is.Not.Null);
            Assert.That(itype.RealMass, Is.Not.Null);

            Assert.That(itype.RealMass, Is.EqualTo((Mass)"20KG"));
            Assert.That(itype.RealVolume, Is.EqualTo((Volume)"20L"));
        }

        [Test]
        public void LoaderMismatchedTypesThrow() {
            var itemString = @"{
                ""mass"": ""SomeValue""
            }";

            var basicItemType = new BasicItemType();

            var jObj = JObject.Parse(itemString);
            Assert.Throws<FormatException>(() => basicItemType.Mass = jObj.ReadProperty(() => basicItemType.Mass));
        }
    }
}