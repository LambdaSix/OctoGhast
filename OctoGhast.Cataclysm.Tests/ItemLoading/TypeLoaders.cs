using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OctoGhast.Cataclysm.LegacyLoader;
using OctoGhast.Entity;
using OctoGhast.Framework;
using OctoGhast.Units;

namespace OctoGhast.Cataclysm.Tests.ItemLoading {
    [TestFixture]
    public class TypeLoaders {
        [OneTimeSetUp]
        public void Setup() {
            JsonDataLoader.RegisterConverter(typeof(GunModifierData),
                (token, type) => token.Count() == 4
                    ? new GunModifierData(token[1].Value<string>(), token[2].Value<int>(), token[3].Value<IEnumerable<string>>())
                    : new GunModifierData(token[1].Value<string>(), token[2].Value<int>(), Enumerable.Empty<string>()));
            JsonDataLoader.RegisterConverter(typeof(GunType), (token, t) => new GunType(token.Value<string>()));

            JsonDataLoader.RegisterTypeLoader(typeof(GunModLocation), (token, t, v, _) => new GunModLocation(token.GetValue("location").Value<string>()));

        }

        [Test]
        public void DamageInfoTypeLoader() {
            var json = "{ 'thrown_damage': [ { 'damage_type': 'bash', 'amount': 5 }, { 'damage_type': 'stab', 'amount': 17 } ] }";
            json = json.Replace('\'', '\"');
            var jsonStr = JObject.Parse(json);

            var loader = new DamageInfoTypeLoader();
            var res = loader.Load(jsonStr, null);

            Assert.That(res.DamageForType(DamageType.Bash), Is.EqualTo(5));
            Assert.That(res.DamageForType(DamageType.Stab), Is.EqualTo(17));
        }

        [Test]
        public void ContainerTypeLoader() {
            var json = "{ 'contains': '5L', 'seals': true, 'watertight': true, 'preserves': true, 'unseals_into': 'meat_slice' } ";
            json = json.Replace('\'', '\"');
            var jsonStr = JObject.Parse(json);

            var loader = new ContainerTypeLoader();
            var res = loader.Load(jsonStr, null);

            Assert.That(res.Contains, Is.EqualTo(Volume.FromLiters(5)));
            Assert.That(res.Seals, Is.True);
            Assert.That(res.Watertight, Is.True);
            Assert.That(res.Preserves, Is.True);
            Assert.That(res.UnsealsInto, Is.EqualTo(new StringID<ItemType>("meat_slice")));
        }

        [Test]
        public void ToolTypeLoader() {
            var json = "{ " +
                       " 'ammo': 'battery', " +
                       " 'revert_to': 'hedgetrimmer_off', " +
                       " 'revert_msg': 'the trimmer switches off', " +
                       " 'sub': 'abc', " +
                       " 'max_charges': 5, " +
                       " 'initial_charges': 5, " +
                       " 'rand_charges': [ 3,4,5,19 ], " +
                       " 'charges_per_use': 1," +
                       " 'turns_per_charge': 1 " +
                       "}";
            json = json.Replace('\'', '\"');
            var jsonStr = JObject.Parse(json);

            var loader = new ToolTypeLoader();
            var res = loader.Load(jsonStr, null);

            Assert.That(res.AmmoId, Is.EqualTo(new StringID<AmmoType>("battery")));
            Assert.That(res.RevertsTo, Is.EqualTo(new StringID<ItemType>("hedgetrimmer_off")));
            Assert.That(res.RevertMessage, Is.EqualTo("the trimmer switches off"));
            Assert.That(res.SubType, Is.EqualTo("abc")); // Interesting that the library converts "null" -> null
            Assert.That(res.MaxCharges, Is.EqualTo(5));
            Assert.That(res.DefaultCharges, Is.EqualTo(5));
            Assert.That(res.RandomCharges, Is.EquivalentTo(new[] {3, 4, 5, 19}));
            Assert.That(res.ChargesPerUse, Is.EqualTo(1));
            Assert.That(res.TurnsPerCharge, Is.EqualTo(1));
        }

        [Test]
        public void ComestibleTypeLoader() {
            var json = "{ " +
                       " 'comestible_type': 'FOOD'," +
                       " 'tool': 'hammer'," +
                       " 'charges': 5," +
                       " 'quench': 5," +
                       " 'nutrition': 30," +
                       " 'calories': 300," +
                       " 'spoils_in': 10," +
                       " 'addiction_potential': 1," +
                       " 'addiction_type': 'methadone'," +
                       " 'fun': 5," +
                       " 'stim': 10," +
                       " 'healthy': -5," +
                       " 'parasites': 10," +
                       " 'vitamins': [ [ 'calcium', 0.5 ], [ 'iron', 20 ], [ 'vitB', 10 ] ]," +
                       " 'rot_spawn': 'rot_group_default'," +
                       " 'rot_spawn_chance': 5" +
                       "}";

            json = json.Replace('\'', '\"');
            var jsonStr = JObject.Parse(json);

            var loader = new ComestibleTypeLoader();
            var res = loader.Load(jsonStr, null);

            Assert.That(res.ComestibleType, Is.EqualTo("FOOD"));
            Assert.That(res.Tool, Is.EqualTo(new StringID<ItemType>("hammer")));
            Assert.That(res.DefaultCharges, Is.EqualTo(5));
            Assert.That(res.Quench, Is.EqualTo(5));
            Assert.That(res.Nutrition, Is.EqualTo(30));
            Assert.That(res.Calories, Is.EqualTo(300));
            Assert.That(res.SpoilsIn, Is.EqualTo(TimeDuration.FromTurns(10)));
            Assert.That(res.AddictionFactor, Is.EqualTo(1));
            Assert.That(res.AddictionType, Is.EqualTo(new StringID<AddictionType>("methadone")));
            Assert.That(res.Fun, Is.EqualTo(5));
            Assert.That(res.StimulantFactor, Is.EqualTo(10));
            Assert.That(res.HealthyFactor, Is.EqualTo(-5));
            Assert.That(res.ParasiteFactor, Is.EqualTo(10));

            Assert.That(res.Vitamins["calcium"], Is.EqualTo(0.5));
            Assert.That(res.Vitamins["iron"], Is.EqualTo(20));
            Assert.That(res.Vitamins["vitB"], Is.EqualTo(10));
            Assert.That(res.RotSpawn, Is.EqualTo(new StringID<MonsterGroup>("rot_group_default")));
            Assert.That(res.RotSpawnChance, Is.EqualTo(5));
        }

        [Test]
        public void BrewableTypeLoader() {
            // "brewable": { "time" : 7200, "results" : [ "wash_whiskey", "yeast" ] }
            var json = "{ 'results': [ 'wash_whiskey', 'yeast' ], 'time': 7200 }";
            json = json.Replace('\'', '\"');

            var jsonStr = JObject.Parse(json);

            var loader = new BrewableTypeLoader();
            var res = loader.Load(jsonStr, null);

            Assert.That(res.Results, Contains.Item((StringID<ItemType>) "wash_whiskey"));
            Assert.That(res.Results, Contains.Item((StringID<ItemType>) "yeast"));
            Assert.That(res.Results.Count(), Is.EqualTo(2));

            Assert.That(res.Time, Is.EqualTo(TimeDuration.FromTurns(7200)));
        }

        [Test]
        public void ArmorTypeLoader() {
            var json = "{" +
                       " 'coverage': [ 'TORSO' ], " +
                       " 'encumbrance': 15, " +
                       " 'material_thickness': 2, " +
                       " 'sided': true," + // Legacy data this is calculated from coverage
                       " 'environmental_protection': 15, " +
                       " 'environmental_protection_with_filter' : 15, " +
                       " 'warmth': 20, " +
                       " 'storage': '10L', " +
                       " 'power_armor': false" +
                       " }";
            json = json.Replace('\'', '\"');

            var jsonStr = JObject.Parse(json);

            var loader = new ArmorTypeLoader();
            var res = loader.Load(jsonStr, null);

            Assert.That(res.Covers, Contains.Item("TORSO"));
            Assert.That(res.Encumbrance, Is.EqualTo(15));
            Assert.That(res.Thickness, Is.EqualTo(2));
            Assert.That(res.Sided, Is.True);
            Assert.That(res.EnvResist, Is.EqualTo(15));
            Assert.That(res.EnvResistWithFilter, Is.EqualTo(15));
            Assert.That(res.Warmth, Is.EqualTo(20));
            Assert.That(res.Storage, Is.EqualTo((Volume) "10L"));
            Assert.That(res.PowerArmour, Is.False);
        }

        [Test]
        public void BookTypeLoader() {
            var json = "{ 'skill': 'archery', 'required_level': 3, 'max_level': 5, 'fun': 3, 'intelligence': 10, 'time': 300, 'chapters': 5 }";
            json = json.Replace('\'', '\"');

            var jsonStr = JObject.Parse(json);

            var loader = new BookTypeLoader();
            var res = loader.Load(jsonStr, null);

            Assert.That(res.Skill, Is.EqualTo((StringID<Skill>) "archery"));
            Assert.That(res.RequiredLevel, Is.EqualTo(3));
            Assert.That(res.MaxLevel, Is.EqualTo(5));
            Assert.That(res.Fun, Is.EqualTo(3));
            Assert.That(res.Intel, Is.EqualTo(10));
            Assert.That(res.Time, Is.EqualTo(TimeDuration.FromTurns(300)));
            Assert.That(res.Chapters, Is.EqualTo(5));
        }

        [Test]
        public void EngineTypeLoader() {
            var json = "{ 'displacement': '100cc', 'faults': [ 'SERPENTINE_BELT' ] } ";
            json = json.Replace('\'', '\"');

            var jsonStr = JObject.Parse(json);

            var loader = new EngineTypeLoader();
            var res = loader.Load(jsonStr, null);

            Assert.That(res.Displacement, Is.EqualTo((Volume) "100ml"));
            Assert.That(res.Faults, Contains.Item("SERPENTINE_BELT"));
        }

        [Test]
        public void WheelTypeLoader() {
            var json = "{ 'width': 150, 'diameter': 50 } ";
            json = json.Replace('\'', '\"');

            var jsonStr = JObject.Parse(json);

            var loader = new WheelTypeLoader();
            var res = loader.Load(jsonStr, null);

            Assert.That(res.Width, Is.EqualTo(150));
            Assert.That(res.Diameter, Is.EqualTo(50));
        }

        [Test]
        public void FuelTypeLoader() {
            var json = "{ 'energy': '38.6MJ' } ";
            json = json.Replace('\'', '\"');

            var jsonStr = JObject.Parse(json);

            var loader = new FuelTypeLoader();
            var res = loader.Load(jsonStr, null);

            Assert.That(res.Energy, Is.EqualTo((Energy) "38.6MJ"));
        }

        [Test]
        public void GunTypeLoader() {
            var json =
                "{ " +
                " 'ranged_damage': [ { 'damage_type': 'bash', 'amount': 5 }, { 'damage_type': 'stab', 'amount': 17 } ], " +
                " 'range': 5," +
                " 'dispersion': 5," +
                " 'pierce': 5," + // -> legacy_pierce
                //" 'ranged_damage': 5," + // -> legacy_damage
                " 'skill': 'archery'," +
                " 'ammo': 'arrow'," +
                " 'durability': 5," +
                " 'integral_magazine_volume': '500ML'," +
                " 'reload': 10," +
                " 'reload_noise': 'click'," +
                " 'reload_noise_volume': '5dB'," +
                " 'sight_dispersion': 50," +
                " 'loudness': '1dB'," +
                " 'ups_charges': 50," +
                " 'barrel_length': '45ml'," +
                " 'ammo_effects': [ 'FIRE', 'FLECHETTE' ]," +
                " 'valid_mod_locations': [ [ 'grip', 1 ] ]," +
                " 'built_in_mods': [ 'SUPPRESSOR' ]," +
                " 'default_mods': [ 'BOW_SIGHT' ]," +
                " 'modes': [ [ 'DEFAULT', 'auto', 8 ], [ 'SEMI', 'semi-auto', 1 ] ], " +
                " 'burst': 5, " +
                " 'handling': 10, " +
                " 'recoil': 10," +
                " }";
            json = json.Replace('\'', '\"');

            var jsonStr = JObject.Parse(json);

            var loader = new GunTypeLoader();
            var res = loader.Load(jsonStr, null);

            Assert.That(res.Damage.DamageForType(DamageType.Bash), Is.EqualTo(5));
            Assert.That(res.Damage.DamageForType(DamageType.Stab), Is.EqualTo(17));

            Assert.That(res.Range, Is.EqualTo(5));
            Assert.That(res.Dispersion, Is.EqualTo(5));
            Assert.That(res.LegacyPierce, Is.EqualTo(5));
            Assert.That(res.LegacyDamage, Is.EqualTo(null));
            Assert.That(res.SkillUsed, Is.EqualTo((StringID<Skill>) "archery"));
            Assert.That(res.Ammo, Is.EqualTo((StringID<AmmoType>) "arrow"));
            Assert.That(res.Durability, Is.EqualTo(5));
            Assert.That(res.IntegralMagazineSize, Is.EqualTo((Volume) "500ML"));
            Assert.That(res.ReloadTime, Is.EqualTo(TimeDuration.FromTurns(10)));
            Assert.That(res.ReloadNoise, Is.EqualTo("click"));
            Assert.That(res.ReloadNoiseVolume, Is.EqualTo((SoundLevel) "5dB"));
            Assert.That(res.SightDispersion, Is.EqualTo(50));
            Assert.That(res.Loudness, Is.EqualTo((SoundLevel) "1dB"));
            Assert.That(res.UPSCharges, Is.EqualTo(50));
            Assert.That(res.BarrelLength, Is.EqualTo((Volume) "45ml"));
            Assert.That(res.AmmoEffects, Is.EquivalentTo(new[] {"FIRE", "FLECHETTE"}));
            Assert.That(res.ValidModLocations["grip"], Is.EqualTo(1));
            Assert.That(res.IntegralModifications, Is.EquivalentTo(new StringID<ItemType>[] {"SUPPRESSOR"}));
            Assert.That(res.DefaultMods, Is.EquivalentTo(new StringID<ItemType>[] {"BOW_SIGHT"}));

            Assert.That(res.ModeModifier["DEFAULT"].Name, Is.EqualTo("auto"));
            Assert.That(res.ModeModifier["DEFAULT"].Quantity, Is.EqualTo(8));
            Assert.That(res.ModeModifier["SEMI"].Name, Is.EqualTo("semi-auto"));
            Assert.That(res.ModeModifier["SEMI"].Quantity, Is.EqualTo(1));

            Assert.That(res.Burst, Is.EqualTo(5));
            Assert.That(res.Handling, Is.EqualTo(10));
            Assert.That(res.Recoil, Is.EqualTo(10));
        }

        [Test]
        public void GunModTypeLoader() {
            var json =
                "{ " +
                " 'ranged_damage': [ { 'damage_type': 'bash', 'amount': 5 }, { 'damage_type': 'stab', 'amount': 17 } ], " +
                " 'range': 5," +
                " 'dispersion': 5," +
                " 'pierce': 5," + // -> legacy_pierce
              //" 'ranged_damage': 5," + // -> legacy_damage
                " 'skill': 'archery'," +
                " 'ammo': 'arrow'," +
                " 'durability': 5," +
                " 'integral_magazine_volume': '500ML'," +
                " 'reload': 10," +
                " 'reload_noise': 'click'," +
                " 'reload_noise_volume': '5dB'," +
                " 'sight_dispersion': 50," +
                " 'loudness': '1dB'," +
                " 'ups_charges': 50," +
                " 'barrel_length': '45ml'," +
                " 'ammo_effects': [ 'FIRE', 'FLECHETTE' ]," +
                " 'valid_mod_locations': [ [ 'grip', 1 ] ]," +
                " 'built_in_mods': [ 'SUPPRESSOR' ]," +
                " 'default_mods': [ 'BOW_SIGHT' ]," +
                " 'modes': [ [ 'DEFAULT', 'auto', 8 ], [ 'SEMI', 'semi-auto', 1 ] ], " +
                " 'burst': 5, " +
                " 'handling': 10, " +
                " 'recoil': 10," +
                // GunMod specific
                " 'location': 'grip'," +
                " 'mod_targets': [ 'rifle', 'smg', 'crossbow' ]," +
                " 'sight_dispersion': -4," +
                " 'aim_speed': -5," +
                " 'mode_modifier': [ [ 'AUTO', 'full-auto', 16 ] ], " +
                " 'handling_modifier': 10," +
                " 'loudness_modifier': -10," +
                " 'install_time': 100" +
                " }";
            json = json.Replace('\'', '\"');

            var jsonStr = JObject.Parse(json);

            var loader = new GunModTypeLoader();
            var res = loader.Load(jsonStr, null);

            Assert.That(res.Damage.DamageForType(DamageType.Bash), Is.EqualTo(5));
            Assert.That(res.Damage.DamageForType(DamageType.Stab), Is.EqualTo(17));

            Assert.That(res.Range, Is.EqualTo(5));
            Assert.That(res.Dispersion, Is.EqualTo(5));
            Assert.That(res.LegacyPierce, Is.EqualTo(5));
            Assert.That(res.LegacyDamage, Is.EqualTo(null));
            Assert.That(res.SkillUsed, Is.EqualTo((StringID<Skill>)"archery"));
            Assert.That(res.Ammo, Is.EqualTo((StringID<AmmoType>)"arrow"));
            Assert.That(res.Durability, Is.EqualTo(5));
            Assert.That(res.IntegralMagazineSize, Is.EqualTo((Volume)"500ML"));
            Assert.That(res.ReloadTime, Is.EqualTo(TimeDuration.FromTurns(10)));
            Assert.That(res.ReloadNoise, Is.EqualTo("click"));
            Assert.That(res.ReloadNoiseVolume, Is.EqualTo((SoundLevel)"5dB"));
            //Assert.That(res.SightDispersion, Is.EqualTo(50));
            //Assert.That(res.Loudness, Is.EqualTo((SoundLevel)"1dB"));
            Assert.That(res.UPSCharges, Is.EqualTo(50));
            Assert.That(res.BarrelLength, Is.EqualTo((Volume)"45ml"));
            Assert.That(res.AmmoEffects, Is.EquivalentTo(new[] { "FIRE", "FLECHETTE" }));
            Assert.That(res.ValidModLocations["grip"], Is.EqualTo(1));
            Assert.That(res.IntegralModifications, Is.EquivalentTo(new StringID<ItemType>[] { "SUPPRESSOR" }));
            Assert.That(res.DefaultMods, Is.EquivalentTo(new StringID<ItemType>[] { "BOW_SIGHT" }));

            /*
            Assert.That(res.ModeModifier["DEFAULT"].Name, Is.EqualTo("auto"));
            Assert.That(res.ModeModifier["DEFAULT"].Quantity, Is.EqualTo(8));
            Assert.That(res.ModeModifier["SEMI"].Name, Is.EqualTo("semi-auto"));
            Assert.That(res.ModeModifier["SEMI"].Quantity, Is.EqualTo(1));
            */

            Assert.That(res.Burst, Is.EqualTo(5));
            Assert.That(res.HandlingModifier, Is.EqualTo(10));
            Assert.That(res.Recoil, Is.EqualTo(10));

            Assert.That(res.Location.Id, Is.EqualTo("grip"));
            Assert.That(res.ModTargets,
                Is.EquivalentTo(new StringID<GunType>[] {"rifle", "smg", "crossbow"}));

            Assert.That(res.ModeModifier["AUTO"].Name, Is.EqualTo("full-auto"));
            Assert.That(res.ModeModifier["AUTO"].Quantity, Is.EqualTo(16));

            Assert.That(res.SightDispersion, Is.EqualTo(-4));
            Assert.That(res.AimSpeed, Is.EqualTo(-5));
            Assert.That(res.Loudness, Is.EqualTo(-10));
            Assert.That(res.InstallationTime, Is.EqualTo(TimeDuration.FromTurns(100)));
        }

        [Test]
        public void MagazineTypeLoader() {
            var json = "{" +
                       " 'ammo_type': '9mm'," +
                       " 'capacity': 30," +
                       " 'count': 30," +
                       " 'default_ammo': '9mm'," +
                       " 'reliability': 10," +
                       " 'reload_time': '100'," +
                       "} ";

            json = json.Replace('\'', '\"');

            var jsonStr = JObject.Parse(json);

            var loader = new MagazineTypeLoader();
            var res = loader.Load(jsonStr, null);

            Assert.That(res.AmmoType, Is.EqualTo((StringID<AmmoType>) "9mm"));
            Assert.That(res.Capacity, Is.EqualTo(30));
            Assert.That(res.DefaultCount, Is.EqualTo(30));
            Assert.That(res.DefaultAmmo, Is.EqualTo((StringID<ItemType>) "9mm"));
            Assert.That(res.Reliability, Is.EqualTo(10));
            Assert.That(res.ReloadTime, Is.EqualTo(TimeDuration.FromTurns(100)));
        }

        [Test]
        public void BionicTypeLoader() {
            var json = "{" +
                       " 'id': 'bionic_test'," +
                       " 'bionic_id': 'bionic_adv_test', " +
                       " 'difficulty': 10" +
                       "} ";

            json = json.Replace('\'', '\"');

            var jsonStr = JObject.Parse(json);

            var loader = new BionicTypeLoader();
            var res = loader.Load(jsonStr, null);

            Assert.That(res.InstallationDifficulty, Is.EqualTo(10));
            Assert.That(res.Id, Is.EqualTo((StringID<Bionic>) "bionic_test"));
            Assert.That(res.BionicId, Is.EqualTo((StringID<Bionic>)"bionic_adv_test"));
        }

        [Test]
        public void AmmoTypeLoader() {
            var json = @"{
                'count': 50,
                'ammo_type': '357sig',
                'casing': '357sig_casing',
                'range': 16,
                'damage': 28,
                'pierce': 4,
                'dispersion': 30,
                'recoil': 600,
                'effects': [ 'COOKOFF' ],
                'cooks_off': true,
                'special_cook_off': true,
                'loudness': '10dB',
                'drop_active': true,
                'drop_chance': 0.5,
                'drop': 'scrap_metal'
            }";

            json = json.Replace('\'', '\"');

            var jsonStr = JObject.Parse(json);

            var loader = new AmmoTypeLoader();
            var res = loader.Load(jsonStr, null);

            Assert.That(res.DefaultCharges, Is.EqualTo(50));
            Assert.That(res.AmmoType == "357sig");
            Assert.That(res.Casing == "357sig_casing");
            Assert.That(res.Range, Is.EqualTo(16));
            Assert.That(res.LegacyDamage, Is.EqualTo(28));
            Assert.That(res.LegacyPierce, Is.EqualTo(4));
            Assert.That(res.Dispersion, Is.EqualTo(30));
            Assert.That(res.Recoil, Is.EqualTo(600));
            Assert.That(res.AmmoEffects, Is.EquivalentTo(new[] {"COOKOFF"}));
            Assert.That(res.CooksOff, Is.True);
            Assert.That(res.SpecialCookOff, Is.True);
            Assert.That(res.Loudness == (SoundLevel) "10dB");
            Assert.That(res.DropActive, Is.True); 
            Assert.That(res.DropChance, Is.EqualTo(0.5));
            Assert.That(res.Drops, Is.EquivalentTo(new StringID<ItemType>[] {"scrap_metal"}));
        }

        [Test]
        public void SeedTypeLoader() {
            var json = @"
                        {
	                        'grow': 10,
	                        'fruit_div': 1,
	                        'plant_name': 'strawberry',
	                        'fruit': 'strawberry',
	                        'seeds': true,
	                        'byproducts': [ 'withered_plant' ],
                        }";
            json = json.Replace('\'', '\"');

            var jsonStr = JObject.Parse(json);

            var loader = new SeedTypeLoader();
            var res = loader.Load(jsonStr, null);
        }
    }
}