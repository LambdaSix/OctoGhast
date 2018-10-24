using System;
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

        [LoaderInfo("fun")]
        public int Fun { get; set; }

        [LoaderInfo("mass", true, 0)]
        public int Mass { get; set; }

        [LoaderInfo("volume", true, 0.0)]
        public double Volume { get; set; }

        [LoaderInfo("material")]
        public StringID<Material> Material { get; set; }

        [LoaderInfo("volume_real", false, "0.0L")]
        public Volume RealVolume { get; set; }

        [LoaderInfo("mass_real", false, "0.0KG")]
        public Mass RealMass { get; set; }

        [LoaderInfo("properties")]
        public Dictionary<string,string> Properties { get; set; }

        [LoaderInfo("qualities")]
        public Dictionary<string,int> Qualities { get; set; }

        [LoaderInfo("flags")]
        public IEnumerable<string> Flags { get; set; }

        [LoaderInfo("magazines")]
        public Dictionary<StringID<AmmoType>,IEnumerable<StringID<ItemType>>> Magazines { get; set; }

        [LoaderInfo("tools")]
        public Dictionary<string,int> Tools { get; set; }

        [LoaderInfo("vitamins")]
        public Dictionary<string,double> Vitamins { get; set; }
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
                ""vitamins"": [ [ ""calcium"", 0.5 ], [ ""iron"", 20 ], [ ""vitB"", 10 ] ],
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

            Assert.That(itype.Magazines["300"], Contains.Item(new StringID<ItemType>("lw223mag")));
            Assert.That(itype.Magazines["50"], Contains.Item(new StringID<ItemType>("bigMag50")));
            Assert.That(itype.Magazines["50"], Contains.Item(new StringID<ItemType>("alternativeLittleMag")));

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
                // Multiply the volume by 1.5
                // Also scale the tools requirements by 0.5 and 1.5
                ""proportional"": { 
                     ""volume_real"": ""1.5"",
                     ""tools"": [ [ [""soldering_iron"", 0.5], [""toolset"", 1.5] ] ]
                },
                // add 'RECYCLED' flag
                // Also add to the 'tools' dictionary
                ""extend"": { 
                     ""flags"": [""RECYCLED""],
                     ""tools"": [ [ [""screwdriver"", 1 ] ] ]
                },
                // Remove the 'FLAMMABLE' flag
                // Also remove the Hammer tool from the tools list
                ""delete"": { 
                     ""flags"": [""FLAMMABLE""],
                     ""tools"": [ [ [""hammer"" ] ] ]
                },
                // Add 10KG to the mass
                // Add 5 and 4 to the vitA and Iron values
                // Remove 5 from the Fun value
                // Add more tool requirements.
                ""relative"": { 
                      ""mass_real"": ""10KG"", 
                      ""vitamins"": [ [ ""vitA"", 5 ], [ ""iron"", 4 ] ], 
                      ""fun"": -5,
                      ""tools"": [ [ [""soldering_iron"", 5], [ ""toolset"", 20] ] ]  }
            }";

            Mass inheritedMass = "20KG";
            Volume inheritedVolume = "20L";
            IEnumerable<string> inheritedFlags = new[] {"FLAMMABLE"};
            var inheritedVitamins = new Dictionary<string, double>()
            {
                ["vitA"] = 0.5,
                ["iron"] = 4,
            };

            var inheritedTools = new Dictionary<string, int>()
            {
                ["soldering_iron"] = 5,
                ["toolset"] = 10,
                //["screwdriver"] = 1,
                ["hammer"] = 5,
            };

            var itype = new BasicItemType();
            var jObj = JObject.Parse(itemString);

            itype.Fun = 0;
            itype.RealMass = inheritedMass;
            itype.RealVolume = inheritedVolume;
            itype.Flags = inheritedFlags;
            itype.Vitamins = inheritedVitamins;
            itype.Tools = inheritedTools;

            itype.Fun = jObj.ReadProperty(() => itype.Fun);
            itype.RealMass = jObj.ReadProperty(() => itype.RealMass);
            itype.RealVolume = jObj.ReadProperty(() => itype.RealVolume);
            itype.Flags = jObj.ReadProperty(() => itype.Flags);
            itype.Vitamins = jObj.ReadProperty(() => itype.Vitamins);
            itype.Tools = jObj.ReadProperty(() => itype.Tools);

            Assert.That(itype.Fun, Is.EqualTo(-5));
            Assert.That(itype.RealMass == (Mass) "30KG");
            Assert.That(itype.RealVolume == (Volume) "30L");
            Assert.That(itype.Flags, Contains.Item("RECYCLED"));

            Assert.That(itype.Vitamins["vitA"], Is.EqualTo(5.5));
            Assert.That(itype.Vitamins["iron"], Is.EqualTo(8));

            // Order of operations: Delete, Extends, Relative, Proportional
            Assert.That(itype.Tools["soldering_iron"], Is.EqualTo(5));
            Assert.That(itype.Tools["toolset"], Is.EqualTo(45));
            Assert.That(itype.Tools["screwdriver"], Is.Not.Null);
            Assert.That(itype.Tools["screwdriver"], Is.EqualTo(1));
            Assert.That(itype.Tools.ContainsKey("hammer"), Is.False);
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