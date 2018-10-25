using System;
using NUnit.Framework;
using NUnit.Framework.Internal;
using OctoGhast.Units;

namespace OctoGhast.Core.Tests {
    [TestFixture]
    public class EnergyQuantityTests {
        [Test]
        public void EnergyComparable() {
            Energy e1 = "100W";
            Energy e2 = "100W";

            Assert.That(e1 == e2);
            Assert.That(e1, Is.EqualTo(e2));
            Assert.AreEqual(e1, e2);

            var e3 = e1 + (Energy) "100W";

            Assert.That(e1 != e3);
            Assert.That(e3 != (Energy) "100W");
        }

        [Test]
        public void UnitConversions() {
            Energy e1 = "1W";

            Assert.AreEqual(e1,Energy.FromJoules(1));
            Assert.AreEqual(e1, Energy.FromKilojoules(0.001));
            Assert.AreEqual(Energy.FromMegajoules(1), Energy.FromJoules(1_000_000));

            Assert.AreEqual(Energy.FromCalories(1), Energy.FromJoules(4.1868));
            Assert.AreEqual(Energy.FromKilocalories(1), Energy.FromCalories(1000));

            Assert.AreEqual(Energy.FromWatts(1), e1);
            Assert.AreEqual(Energy.FromKilowatts(1), Energy.FromWatts(1000));

            Assert.AreEqual(Energy.FromWattHours(1), Energy.FromWatts(3600));
            Assert.AreEqual(Energy.FromKilowattHours(1), Energy.FromWattHours(1000));

            Assert.AreEqual(Energy.FromHorsepower(1), Energy.FromWatts(745));        
        }

        [Test]
        public void EnergyDensity() {
            var e1 = Energy.FromMaterial("1L", 38.6); // 1 Litre of Diesel
            var e2 = Energy.FromMaterial("60L", 38.6); // 60 Litres of Diesel

            Assert.That(e1, Is.EqualTo((Energy) "38.6MJ"));
            Assert.That(e2, Is.EqualTo((Energy) "2316MJ"));
            Assert.That(e1.KilowattHours, Is.EqualTo(((Energy)"10.722222222222221kWh").KilowattHours));
        }
    }

    [TestFixture]
    public class SoundLevelPressureQuantityTests {
        [Test]
        public void SoundLevelComparable() {
            SoundLevel db1 = "20dB";
            SoundLevel db2 = "20db";

            Assert.That(db1 == db2);
            Assert.That(db1 == (SoundLevel) "20dB");

            // Raw addition, not correct.
            db1 += "4dB";

            Assert.That(db1 != db2);
            Assert.That(db1 != (SoundLevel)"20dB");
        }

        [Test]
        public void PressureComparable() {
            Pressure p1 = "20kPa";
            Pressure p2 = "20kpa";

            Assert.That(p1 == p2);
            Assert.That(p1 == (Pressure) "20kPa");

            
            p1 += "40kPa";

            Assert.That(p1 != p2);
            Assert.That(p1 != (Pressure)"20kPa");
        }

        [Test]
        public void SoundLevelToPressure() {
            SoundLevel db1 = "100dB";
            Pressure p2 = "2kPa";

            Assert.AreEqual(db1.KiloPascals, p2.KiloPascals);
            Assert.AreEqual(db1, p2.AsDecibels());
            Assert.AreEqual(db1.Pascals, p2.Pascals);
            Assert.AreEqual(SoundLevel.FromPressure(p2).KiloPascals, p2.KiloPascals);
        }

        [Test]
        public void Constants() {
            Pressure oneAtmosphere = Pressure.Atmosphere;

            Assert.AreEqual(oneAtmosphere.KiloPascals, ((Pressure)"101.325kPa").KiloPascals);
            Assert.AreEqual(oneAtmosphere.KiloPascals, Pressure.FromAtmospheres(1).KiloPascals);
            // Approximate, 1Atm == 14.69psi
            Assert.AreEqual((int)oneAtmosphere.Psi, 14);

            Assert.AreEqual(oneAtmosphere.Atmospheres, new Pressure("101.325kPa").Atmospheres);

        }
    }

    [TestFixture]
    public class MassVolumeQuantityTests {
        [Test]
        public void MassComparable() {
            Mass grams = "100grams";
            Mass grams2 = "100grams";

            Assert.That(grams == grams2);
            Assert.That(grams == (Mass) "100g");

            grams2 += "100g";

            Assert.That(grams != grams2);
            Assert.That(grams != (Mass) "500g");

            Assert.That(grams, Is.Not.EqualTo(grams2));
            Assert.That(grams, Is.Not.EqualTo((Mass)"500g"));

            Assert.That(grams < grams2);
            Assert.That(grams2 > grams);
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

            var v3 = (volume2 + (Volume) "400ml");

            Assert.That(volume != v3);
            Assert.That(volume != (Volume)"500ml");
            Assert.That(volume, Is.Not.EqualTo(v3));
            Assert.That(volume, Is.Not.EqualTo((Volume) "500ml"));

            Assert.That(volume < v3);
            Assert.That(v3 > volume);
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
            Volume litre = "1L";
            litre.Material = CoreMaterials.Water; // 1g/cm³
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
