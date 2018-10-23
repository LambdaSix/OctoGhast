﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OctoGhast.Cataclysm.LegacyLoader;
using OctoGhast.Entity;
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

        [LoaderInfo("material", false, null)]
        public StringID<Material> Material { get; set; }

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

        [LoaderInfo("vitamins")]
        public Dictionary<string,int> Vitamins { get; set; }
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
            // Cataclysm has some weird JSON storage semantics, so check we can load all the more complicated ones.
            var itemString = @"{
                // Standard dictionary, this is fine. (string:string)
                ""properties"": {
                    ""keyA"": ""value""
                },

                // Also a standard dictionary (string:int)
                ""qualities"": {
                    ""CUTTING"": ""1""
                },

                // Standard enumerable/list of string, so this is ok.
                ""flags"": [""FLAMMABLE"", ""IGNITABLE""],

                // Not sure what this is meant to be, but we can decompose it into a Dictionary<string,IEnumerable<string>>
                ""magazines"": [ ""300"", [ ""lw223mag"" ], ""50"", [ ""bigMag50"", ""alternativeLittleMag"" ] ],

                // Basically Dictionary<string,int> but stored as an array of arrays.
                // There's an alternative version of this with three elements ([""toolset"",10,""LIST""]
                // Where the 'LIST' element tells the recipe processor 'I gave you a requirement, not a direct item'
                // And that would be a Dictionary<string,object[]> really, but we can deal with the fine details
                // in recipe loading directly I guess.
                ""tools"": [ [ [""soldering_iron"", 10], [ ""toolset"", 10] ] ],
                ""vitamins"": [ [ ""calcium"", 1 ], [ ""iron"", 20 ], [ ""vitB"", 10 ] ],
            }";

            var itype = new BasicItemType();
            var jObj = JObject.Parse(itemString);

            itype.Properties = jObj.ReadProperty(() => itype.Properties);
            itype.Qualities = jObj.ReadProperty(() => itype.Qualities);
            itype.Flags = jObj.ReadProperty(() => itype.Flags);
            itype.Magazines = jObj.ReadProperty(() => itype.Magazines);
            itype.Tools = jObj.ReadProperty(() => itype.Tools);
            itype.Vitamins = jObj.ReadProperty(() => itype.Vitamins);

            Assert.That(itype.Properties, Is.Not.Null);
            Assert.That(itype.Properties, Is.Not.Empty);

            Assert.That(itype.Qualities, Is.Not.Null);
            Assert.That(itype.Qualities, Is.Not.Empty);

            Assert.That(itype.Flags, Is.Not.Null);
            Assert.That(itype.Flags, Is.Not.Empty);

            Assert.That(itype.Magazines, Is.Not.Null);
            Assert.That(itype.Magazines, Is.Not.Empty);

            Assert.That(itype.Tools, Is.Not.Null);
            Assert.That(itype.Tools, Is.Not.Empty);

            Assert.That(itype.Properties.ContainsKey("keyA"));
            Assert.That(itype.Properties["keyA"], Is.EqualTo("value"));

            Assert.That(itype.Qualities.ContainsKey("CUTTING"));
            Assert.That(itype.Qualities["CUTTING"], Is.EqualTo(1));

            Assert.That(itype.Flags, Contains.Item("FLAMMABLE").And.Contain("IGNITABLE"));

            Assert.That(itype.Magazines["300"], Contains.Item("lw223mag"));
            Assert.That(itype.Magazines["50"], Contains.Item("bigMag50").And.Contains("alternativeLittleMag"));

            Assert.That(itype.Tools["soldering_iron"], Is.EqualTo(10));
            Assert.That(itype.Tools["toolset"], Is.EqualTo(10));
        }

        [Test]
        public void LoadComplexItem() {
            var itemString = @"{
                ""volume_real"": ""20L"",
                ""mass_real"": ""20KG"",
                ""material"": ""core_water""
            }";

            var itype = new BasicItemType();
            var jObj = JObject.Parse(itemString);

            itype.RealVolume = jObj.ReadProperty(() => itype.RealVolume);
            itype.RealMass = jObj.ReadProperty(() => itype.RealMass);
            itype.Material = jObj.ReadProperty(() => itype.Material);

            Assert.That(itype.RealVolume, Is.Not.Null);
            Assert.That(itype.RealMass, Is.Not.Null);
            Assert.That(itype.Material, Is.Not.Null);

            Assert.That(itype.RealMass, Is.EqualTo((Mass) "20KG"));
            Assert.That(itype.RealVolume, Is.EqualTo((Volume) "20L"));

            Assert.That(itype.Material, Is.EqualTo(new StringID<Material>("core_water")));
            Assert.That(itype.Material == "core_water");
            Assert.That(itype.Material == (StringID<Material>) "core_water");
            Assert.That(itype.Material == CoreMaterials.Water.Id);

            Assert.That(itype.Material.Type() == typeof(Material));
        }

        [Test]
        public void LoadInheritedItem() {
            var itemString = @"{
                ""relative"": { ""mass_real"": ""10KG"" },
                ""proportional"": { ""volume_real"": ""1.5"" },
                // add 'recycled' and remove 'flammable'
                ""extend"": { ""flags"": [""RECYCLED""] },
                ""delete"": { ""flags"": [""FLAMMABLE""] }
            }";

            Mass inheritedMass = "20KG";
            Volume inheritedVolume = "20L";
            IEnumerable<string> inheritedFlags = new[] {"FLAMMABLE"};

            var itype = new BasicItemType();
            var jObj = JObject.Parse(itemString);

            itype.RealMass = inheritedMass;
            itype.RealVolume = inheritedVolume;
            itype.Flags = inheritedFlags;

            itype.RealMass = jObj.ReadProperty(() => itype.RealMass);
            itype.RealVolume = jObj.ReadProperty(() => itype.RealVolume);
            itype.Flags = jObj.ReadProperty(() => itype.Flags);

            Assert.That(itype.RealMass == (Mass) "30KG");
            Assert.That(itype.RealVolume == (Volume) "30L");
            Assert.That(itype.Flags, Contains.Item("RECYCLED"));
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