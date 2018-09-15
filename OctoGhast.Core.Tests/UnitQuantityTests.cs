using System;
using NUnit.Framework;
using NUnit.Framework.Internal;
using OctoGhast.Units;

namespace OctoGhast.Core.Tests {
    [TestFixture]
    public class UnitQuantityTests {
        [Test]
        public void MassComparable() {
            Mass grams = "100grams";
            Mass grams2 = "100grams";

            Assert.That(grams == grams2);
            Assert.That(grams == (Mass) "100g");

            grams2 += "400grams";

            Assert.That(grams != grams2);
            Assert.That(grams != (Mass) "500g");
        }

        [Test]
        public void VolumeComparable()
        {
            Volume volume = "100ml";
            Volume volume2 = "100ml";

            Assert.That(volume == volume2);
            Assert.That(volume == (Volume)"100ml");
            Assert.That(volume, Is.EqualTo(volume2));
            Assert.That(volume, Is.EqualTo((Volume) "100ml"));

            volume2 += "400ml";

            Assert.That(volume != volume2);
            Assert.That(volume != (Volume)"500ml");
            Assert.That(volume, Is.Not.EqualTo(volume2));
            Assert.That(volume, Is.Not.EqualTo((Volume) "500ml"));

            Assert.That(volume < volume2);
            Assert.That(volume2 > volume);
        }

        [Test]
        public void VolumeStringify() {
            Volume volume = "100ml";
            Assert.That(volume.ToString(), Is.EqualTo("100ml"));

            volume = "1000ml";
            Assert.That(volume.ToString(), Is.EqualTo("1L"));

            volume = "1000L";
            Assert.That(volume.ToString(), Is.EqualTo("1KL"));
        }

        [Test]
        public void VolumeHasWeight() {
            var water = new Material()
            {
                Name = "water",
                Density = 1.0f
            };

            Volume litre = "1L";
            litre.Material = water; // 1g/cm³
            Mass weightOfWater = litre.Mass();

            Assert.That(litre.Liters == 1);
            Assert.That(Math.Abs(weightOfWater.Kilograms - 1) < 0.1);

            var lead = new Material()
            {
                Name = "lead",
                Density = 11.34f
            };

            Volume litreOfLead = "1L";
            litreOfLead.Material = lead;
            Mass weightOfSolidLead = litreOfLead.Mass();

            Assert.That(litreOfLead.Liters == 1);
            Assert.That(weightOfSolidLead, Is.EqualTo((Mass)"11340g"));
        }

        [Test]
        public void MassHasVolume() {
            var ice = new Material()
            {
                Density = 0.9340f
            };
            Mass solidWater = "1KG";
            solidWater.Material = ice;
            Volume volumeOfIce = solidWater.Volume();

            Assert.That(solidWater.Kilograms == 1);
            Assert.That(volumeOfIce, Is.EqualTo((Volume) "1.07L"));
        }

        [Test]
        public void MassFromStrings() {
            Mass grams = "100g";
            Mass grams2 = "100grams";
            Mass grams3 = new Mass("100g");
            Mass grams4 = new Mass("100grams");

            Mass kilograms = "100kg";
            Mass kilograms2 = "100kilograms";
            var kilograms3 = new Mass("100kg");
            var kilograms4 = new Mass("100kilograms");

            Mass tons = "100t";
            Mass tons2 = "100ton";
            Mass tons3 = new Mass("100t");
            Mass tons4 = new Mass("100ton");

            Assert.That(grams, Is.Not.Null);
            Assert.That(grams2, Is.Not.Null);
            Assert.That(grams3, Is.Not.Null);
            Assert.That(grams4, Is.Not.Null);

            Assert.That(kilograms, Is.Not.Null);
            Assert.That(kilograms2, Is.Not.Null);
            Assert.That(kilograms3, Is.Not.Null);
            Assert.That(kilograms4, Is.Not.Null);

            Assert.That(tons, Is.Not.Null);
            Assert.That(tons2, Is.Not.Null);
            Assert.That(tons3, Is.Not.Null);
            Assert.That(tons4, Is.Not.Null);
        }

        [Test]
        public void VolumeFromStrings()
        {
            Volume ml = "100ml";
            Volume ml2 = "100milliliters";

            var ml3 = new Volume("100ml");
            var ml4 = new Volume("100milliliters");

            Volume liters = "100l";
            Volume liters2 = "100liters";

            var liters3 = new Volume("100l");
            var liters4 = new Volume("100liters");

            Volume kiloliters = "100kl";
            Volume kiloliters2 = "100kiloliters";

            var kiloliters3 = new Volume("100kl");
            Volume kiloliters4 = new Volume("100kiloliters");

            Assert.That(ml, Is.Not.Null);
            Assert.That(ml2, Is.Not.Null);
            Assert.That(ml3, Is.Not.Null);
            Assert.That(ml4, Is.Not.Null);

            Assert.That(liters, Is.Not.Null);
            Assert.That(liters2, Is.Not.Null);
            Assert.That(liters3, Is.Not.Null);
            Assert.That(liters4, Is.Not.Null);

            Assert.That(kiloliters, Is.Not.Null);
            Assert.That(kiloliters2, Is.Not.Null);
            Assert.That(kiloliters3, Is.Not.Null);
            Assert.That(kiloliters4, Is.Not.Null);
        }
    }
}
