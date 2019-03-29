using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using InfiniMap;
using NUnit.Framework;
using OctoGhast.Cataclysm.LegacyLoader;
using OctoGhast.Cataclysm.Loaders.Item.DataContainers;
using OctoGhast.Cataclysm.Loaders.Item.Types;
using OctoGhast.Map;
using OctoGhast.Units;

namespace OctoGhast.Cataclysm.Tests {
    [TestFixture]
    public class ExplosiveTests {
        /*
         * Assumptions:
         *  Reference Explosive: 1KG of TNT. (6,900 m/s velocity)
         *  Energy (kJ): 4184 kilojoules
         *  Energy (Wh): 1.163 kWh
         *  
         * Converted:
         *  Explosive Power = 1.0
         */

        [Test]
        public void ConcussiveFalloff() {
            /*
             * Assumption:
             * 1KG of Comp-B
             *
             */
        }

        [Test]
        public void FragmentationCloud() {
            var explosiveData = new ExplosiveData()
            {
                ExplosiveMaterial = new ExplosiveMaterial()
                {
                    DetonationVelocity = 6900,
                    RelativeEffectiveness = 1.0,
                    Name = "TNT",
                    Density = 1.60
                },
                Mass = "1g",
                Shrapnel = new ShrapnelData()
                {
                    CasingMass = "5g",
                    FragmentMass = "1g",
                }
            };

            var exp = new Explosion(explosiveData);
            var result = exp.CalculateFragments(new WorldSpace2D(0, 0));

            // Min/Max?
            double maxValue = result.Within((-32, -32), (32, 32)).Max(s => s?.Velocity ?? 0);
            double minValue = result.Within((-32, -32), (32, 32)).Min(s => s?.Velocity ?? 0);

            if (Double.IsNaN(minValue))
                minValue = 0;
            if (Double.IsNaN(maxValue))
                maxValue = 1.0d;

            char calcValue(double? density, double? velocity) {
                var scaledValue = (velocity - minValue) / (maxValue - minValue);
                var ch = $"{scaledValue:#}".ToCharArray().FirstOrDefault();
                return ch == '\u0000' ? '0' : ch;
            }

            var map = result.Distance(0, 0, 32);

            for (int x = -32; x <= 32; x++) {
                for (int y = -32; y <= 32; y++) {
                    Console.Write(calcValue(map[x, y]?.Density, map[x, y]?.Velocity));
                }
                Console.WriteLine();
            }
        }

        [Test]
        public void IncendiaryArea() { }

        [Test]
        public void EpicenterPower() { }

        [Test]
        public void SoundLevelFromPower() {
            var referenceValues = new List<(Mass mass, double distance, SoundLevel decibels)>()
            {
                // 1g - 100g @ 1 Meter
                (Mass.FromGrams(1), 1.0, SoundLevel.FromDecibels(177.4401)),
                (Mass.FromGrams(25), 1.0, SoundLevel.FromDecibels(193.5214)),
                (Mass.FromGrams(75), 1.0, SoundLevel.FromDecibels(200.4099)),
                (Mass.FromGrams(100), 1.0, SoundLevel.FromDecibels(202.3301)),
                
                // 1g - 100g @ 10 Meters
                (Mass.FromGrams(1), 10.0, SoundLevel.FromDecibels(154.0091)),
                (Mass.FromGrams(25), 10.0, SoundLevel.FromDecibels(164.0427)),
                (Mass.FromGrams(75), 10.0, SoundLevel.FromDecibels(167.7138)),
                (Mass.FromGrams(100), 10.0, SoundLevel.FromDecibels(168.7090)),

                // 1g - 100g @ 50 Meters
                (Mass.FromGrams(1), 50.0, SoundLevel.FromDecibels(139.7403)),
                (Mass.FromGrams(25), 50.0, SoundLevel.FromDecibels(149.1984)),
                (Mass.FromGrams(75), 50.0, SoundLevel.FromDecibels(152.4730)),
                (Mass.FromGrams(100), 50.0, SoundLevel.FromDecibels(153.3369)),

                // 1g - 100g @ 100 Meters
                (Mass.FromGrams(1), 50.0, SoundLevel.FromDecibels(139.7403)),
                (Mass.FromGrams(25), 50.0, SoundLevel.FromDecibels(149.1984)),
                (Mass.FromGrams(75), 50.0, SoundLevel.FromDecibels(152.4730)),
                (Mass.FromGrams(100), 50.0, SoundLevel.FromDecibels(153.3369)),

                // 100g - 1KG @ 1 Meter
                (Mass.FromGrams(100), 1.0, SoundLevel.FromDecibels(202.3301)),
                (Mass.FromGrams(250), 1.0, SoundLevel.FromDecibels(208.7355)),
                (Mass.FromGrams(750), 1.0, SoundLevel.FromDecibels(216.9118)),
                (Mass.FromKilograms(1), 1.0, SoundLevel.FromDecibels(219.1265)),
                
                // 100g - 1KG @ 10 Meters
                (Mass.FromGrams(100), 10.0, SoundLevel.FromDecibels(168.7090)),
                (Mass.FromGrams(250), 10.0, SoundLevel.FromDecibels(171.9992)),
                (Mass.FromGrams(750), 10.0, SoundLevel.FromDecibels(176.2554)),
                (Mass.FromKilograms(1), 10.0, SoundLevel.FromDecibels(177.4401)),

                // 1KG - 1000KG @ 1 Meter
                (Mass.FromKilograms(1), 1.0, SoundLevel.FromDecibels(219.1265)),
                (Mass.FromKilograms(10), 1.0, SoundLevel.FromDecibels(237.6242)),
                (Mass.FromKilograms(100), 1.0, SoundLevel.FromDecibels(256.9442)),
                (Mass.FromKilograms(1000), 1.0, SoundLevel.FromDecibels(276.6355)),

                // 1KG - 1000KG @ 10 Meters
                (Mass.FromKilograms(1), 10.0, SoundLevel.FromDecibels(177.4401)),
                (Mass.FromKilograms(10), 10.0, SoundLevel.FromDecibels(188.3416)),
                (Mass.FromKilograms(100), 10.0, SoundLevel.FromDecibels(202.3301)),
                (Mass.FromKilograms(1000), 10.0, SoundLevel.FromDecibels(219.1265)),
            };

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Grams,Distance,dB");

            foreach (var reference in referenceValues) {
                var pressure = Explosion.ExplosionOverpressureGroundLevel(reference.mass, reference.distance).Pascals;
                var decibels = SoundLevel.FromPascals(pressure);

                //Assert.That($"{decibels.Decibels:F4}", Is.EqualTo($"{reference.decibels.Decibels:F4}"));

                sb.AppendLine($"{reference.mass.Grams},{reference.distance:F2},{decibels.Decibels:F4}");
            }

            Console.WriteLine(sb.ToString());
        }

        [Test]
        public void AsTonnageOfTnt() {
            var referenceValues = new Dictionary<string, (float relativeEffectiveness, float density)>()
            {
                ["Ammonnium Nitrate"] = (0.42f, 0.88f),
                ["Black powder"] = (0.50f, 1.65f),
                ["ANFO"] = (0.74f, 0.92f),
                ["Comp-B"] = (1.33f, 1.72f),
                ["Octanitrocubane"] = (2.38f, 1.95f)
            };

            var expectedValues = new Dictionary<string, Mass>()
            {
                ["Ammonnium Nitrate"] = "0.42kg",
                ["Black powder"] = "0.50kg",
                ["ANFO"] = "0.74kg",
                ["Comp-B"] = "1.33kg",
                ["Octanitrocubane"] = "2.38kg"
            };

            foreach (var type in referenceValues) {
                var asTnt = Explosion.AsTnt(Mass.FromKilograms(1), type.Value.relativeEffectiveness);
                Console.WriteLine(asTnt);
                Assert.AreEqual(asTnt.Grams, expectedValues[type.Key].Grams);
            }
        }
    }
}