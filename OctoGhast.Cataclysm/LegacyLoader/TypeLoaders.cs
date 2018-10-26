using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using OctoGhast.Framework;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public interface ITypeLoader<T> : ITypeLoader {
        T Load(JObject jObj, T existing = default(T));
    }

    public class ContainerTypeLoader : ITypeLoader<SlotContainer> {
        public SlotContainer Load(JObject jObj, SlotContainer existing) {
            return new SlotContainer()
            {
                Contains = jObj.ReadProperty(() => existing.Contains),
                Seals = jObj.ReadProperty(() => existing.Seals),
                Watertight = jObj.ReadProperty(() => existing.Watertight),
                Preserves = jObj.ReadProperty(() => existing.Preserves),
                UnsealsInto = jObj.ReadProperty(() => existing.UnsealsInto)
            };
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            return Load(data, existing as SlotContainer);
        }
    }

    public class ToolTypeLoader : ITypeLoader<SlotTool> {
        public SlotTool Load(JObject jObj, SlotTool existing) {
            return new SlotTool()
            {
                AmmoId = jObj.ReadProperty(() => existing.AmmoId),
                RevertsTo = jObj.ReadProperty(() => existing.RevertsTo),
                RevertMessage = jObj.ReadProperty(() => existing.RevertMessage),
                SubType = jObj.ReadProperty(() => existing.SubType),
                MaxCharges = jObj.ReadProperty(() => existing.MaxCharges),
                DefaultCharges = jObj.ReadProperty(() => existing.DefaultCharges),
                RandomCharges = jObj.ReadProperty(() => existing.RandomCharges),
                ChargesPerUse = jObj.ReadProperty(() => existing.ChargesPerUse),
                TurnsPerCharge = jObj.ReadProperty(() => existing.TurnsPerCharge),
            };
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            return Load(data, existing as SlotTool);
        }
    }

    public class DamageInfoTypeLoader : ITypeLoader<DamageInfo> {
        /// <inheritdoc />
        public DamageInfo Load(JObject jObj, DamageInfo existing = default(DamageInfo)) {
            string type = null;
            if (jObj.ContainsKey("ranged_damage"))
                type = "ranged_damage";
            else if (jObj.ContainsKey("thrown_damage"))
                type = "thrown_damage";
            else
                throw new Exception("Unable to find 'ranged_damage' or 'thrown_damage' property");

            return new DamageInfo()
            {
                DamageUnits = jObj.ReadEnumerable(type, existing?.DamageUnits ?? new List<DamageUnit>(),
                    (token) => new DamageUnit(
                        DamageUnit.FromString(token.Value<string>("damage_type")),
                        token.Value<float>("amount"))
                ).ToList(),
            };
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            return Load(data, existing as DamageInfo);
        }
    }

    public class ComestibleTypeLoader : ITypeLoader<SlotComestible> {
        /// <inheritdoc />
        public SlotComestible Load(JObject jObj, SlotComestible existing = default(SlotComestible)) {
            return new SlotComestible()
            {
                ComestibleType = jObj.ReadProperty(() => existing.ComestibleType),
                Tool = jObj.ReadProperty(() => existing.Tool),
                DefaultCharges = jObj.ReadProperty(() => existing.DefaultCharges),
                Quench = jObj.ReadProperty(() => existing.Quench),
                Nutrition = jObj.ReadProperty(() => existing.Nutrition),
                Calories = jObj.ReadProperty(() => existing.Calories),
                SpoilsIn = jObj.ReadProperty(() => existing.SpoilsIn),
                AddictionFactor = jObj.ReadProperty(() => existing.AddictionFactor),
                AddictionType = jObj.ReadProperty(() => existing.AddictionType),
                Fun = jObj.ReadProperty(() => existing.Fun),
                StimulantFactor = jObj.ReadProperty(() => existing.StimulantFactor),
                HealthyFactor = jObj.ReadProperty(() => existing.HealthyFactor),
                ParasiteFactor = jObj.ReadProperty(() => existing.ParasiteFactor),
                Vitamins = jObj.ReadProperty(() => existing.Vitamins),
                RotSpawn = jObj.ReadProperty(() => existing.RotSpawn),
                RotSpawnChance = jObj.ReadProperty(() => existing.RotSpawnChance),
            };
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            return Load(data, existing as SlotComestible);
        }
    }

    public class BrewableTypeLoader : ITypeLoader<SlotBrewable> {
        /// <inheritdoc />
        public SlotBrewable Load(JObject jObj, SlotBrewable existing = default(SlotBrewable)) {
            return new SlotBrewable()
            {
                Results = jObj.ReadProperty(() => existing.Results),
                Time = jObj.ReadProperty(() => existing.Time)
            };
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            return Load(data, existing as SlotBrewable);
        }
    }

    public class ArmorTypeLoader : ITypeLoader<SlotArmor> {
        /// <inheritdoc />
        public SlotArmor Load(JObject jObj, SlotArmor existing = default(SlotArmor)) {
            return new SlotArmor()
            {
                Covers = jObj.ReadProperty(() => existing.Covers),
                Sided = jObj.ReadProperty(() => existing.Sided),
                Encumbrance = jObj.ReadProperty(() => existing.Encumbrance),
                Thickness = jObj.ReadProperty(() => existing.Thickness),
                EnvResist = jObj.ReadProperty(() => existing.EnvResist),
                EnvResistWithFilter = jObj.ReadProperty(() => existing.EnvResistWithFilter),
                Warmth = jObj.ReadProperty(() => existing.Warmth),
                Storage = jObj.ReadProperty(() => existing.Storage),
                PowerArmour = jObj.ReadProperty(() => existing.PowerArmour)
            };
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            return Load(data, existing as SlotArmor);
        }
    }

    public class BookTypeLoader : ITypeLoader<SlotBook> {
        /// <inheritdoc />
        public SlotBook Load(JObject jObj, SlotBook existing = default(SlotBook)) {
            return new SlotBook()
            {
                Skill = jObj.ReadProperty(() => existing.Skill),
                RequiredLevel = jObj.ReadProperty(() => existing.RequiredLevel),
                Time = jObj.ReadProperty(() => existing.Time),
                Fun = jObj.ReadProperty(() => existing.Fun),
                Intel = jObj.ReadProperty(() => existing.Intel),
                MaxLevel = jObj.ReadProperty(() => existing.MaxLevel),
                Chapters = jObj.ReadProperty(() => existing.Chapters)
            };
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            return Load(data, existing as SlotBook);
        }
    }

    public class ModTypeLoader : ITypeLoader<SlotMod> {
        /// <inheritdoc />
        public SlotMod Load(JObject jObj, SlotMod existing = default(SlotMod)) {
            return new SlotMod()
            {
                AcceptableAmmo = jObj.ReadProperty(() => existing.AcceptableAmmo),
                AmmoModifier = jObj.ReadProperty(() => existing.AmmoModifier),
                CapacityMultiplier = jObj.ReadProperty(() => existing.CapacityMultiplier),
                MagazineAdapter = jObj.ReadProperty(() => existing.MagazineAdapter),
            };
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            return Load(data, existing as SlotMod);
        }
    }

    public class EngineTypeLoader : ITypeLoader<SlotEngine> {
        /// <inheritdoc />
        public SlotEngine Load(JObject jObj, SlotEngine existing = default(SlotEngine)) {
            return new SlotEngine()
            {
                Displacement = jObj.ReadProperty(() => existing.Displacement),
                Faults = jObj.ReadProperty(() => existing.Faults)
            };
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            return Load(data, existing as SlotEngine);
        }
    }

    public class WheelTypeLoader : ITypeLoader<SlotWheel> {
        /// <inheritdoc />
        public SlotWheel Load(JObject jObj, SlotWheel existing = default(SlotWheel)) {
            return new SlotWheel()
            {
                Width = jObj.ReadProperty(() => existing.Width),
                Diameter = jObj.ReadProperty(() => existing.Diameter)
            };
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            return Load(data, existing as SlotWheel);
        }
    }

    public class FuelTypeLoader : ITypeLoader<SlotFuel> {
        /// <inheritdoc />
        public SlotFuel Load(JObject jObj, SlotFuel existing = default(SlotFuel)) {
            return new SlotFuel()
            {
                Energy = jObj.ReadProperty(() => existing.Energy)
            };
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            return Load(data, existing as SlotFuel);
        }
    }

    public class GunTypeLoader : ITypeLoader<SlotGun> {
        /// <inheritdoc />
        public SlotGun Load(JObject jObj, SlotGun existing = default(SlotGun)) {
            return new SlotGun()
            {
                Ammo = jObj.ReadProperty(() => existing.Ammo),
                Range = jObj.ReadProperty(() => existing.Range),
                AmmoEffects = jObj.ReadProperty(() => existing.AmmoEffects),
                BarrelLength = jObj.ReadProperty(() => existing.BarrelLength),
                Burst = jObj.ReadProperty(() => existing.Burst),
                Damage = jObj.HasObject("ranged_damage") ? jObj.ReadProperty(() => existing.Damage) : null,
                DefaultMods = jObj.ReadProperty(() => existing.DefaultMods),
                Dispersion = jObj.ReadProperty(() => existing.Dispersion),
                Durability = jObj.ReadProperty(() => existing.Durability),
                Handling = jObj.ReadProperty(() => existing.Handling),
                IntegralMagazineSize = jObj.ReadProperty(() => existing.IntegralMagazineSize),
                IntegralModifications = jObj.ReadProperty(() => existing.IntegralModifications),
                LegacyDamage = !jObj.HasObject("ranged_damage") ? jObj.ReadProperty(() => existing.LegacyDamage) : null,
                LegacyPierce = !jObj.HasObject("pierce") ? jObj.ReadProperty(() => existing.LegacyPierce) : null,
                Loudness = jObj.ReadProperty(() => existing.Loudness),
                ModeModifier = jObj.ReadProperty(() => existing.ModeModifier),
                Recoil = jObj.ReadProperty(() => existing.Recoil),
                ReloadNoise = jObj.ReadProperty(() => existing.ReloadNoise),
                ReloadNoiseVolume = jObj.ReadProperty(() => existing.ReloadNoiseVolume),
                ReloadTime = jObj.ReadProperty(() => existing.ReloadTime),
                SightDispersion = jObj.ReadProperty(() => existing.SightDispersion),
                SkillUsed = jObj.ReadProperty(() => existing.SkillUsed),
                UPSCharges = jObj.ReadProperty(() => existing.UPSCharges),
                ValidModLocations = jObj.ReadProperty(() => existing.ValidModLocations),
            };
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            return Load(data, existing as SlotGun);
        }
    }

    public class GunModTypeLoader : ITypeLoader<SlotGunMod> {
        /// <inheritdoc />
        public SlotGunMod Load(JObject jObj, SlotGunMod existing = default(SlotGunMod)) {
            return new SlotGunMod()
            {
                Ammo = jObj.ReadProperty(() => existing.Ammo),
                Damage = jObj.HasObject("ranged_damage") ? jObj.ReadProperty(() => existing.Damage) : null,
                Burst = jObj.ReadProperty(() => existing.Burst),
                Range = jObj.ReadProperty(() => existing.Range),
                Dispersion = jObj.ReadProperty(() => existing.Dispersion),
                ModeModifier = jObj.ReadProperty(() => existing.ModeModifier),
                LegacyDamage = !jObj.HasObject("ranged_damage") ? jObj.ReadProperty(() => existing.LegacyDamage) : null,
                LegacyPierce = !jObj.HasObject("pierce") ? jObj.ReadProperty(() => existing.LegacyPierce) : null,
                SkillUsed = jObj.ReadProperty(() => existing.SkillUsed),
                ValidModLocations = jObj.ReadProperty(() => existing.ValidModLocations),
                ReloadNoise = jObj.ReadProperty(() => existing.ReloadNoise),
                SightDispersion = jObj.ReadProperty(() => existing.SightDispersion),
                UPSCharges = jObj.ReadProperty(() => existing.UPSCharges),
                Loudness = jObj.ReadProperty(() => existing.Loudness),
                HandlingModifier = jObj.ReadProperty(() => existing.HandlingModifier),
                AmmoEffects = jObj.ReadProperty(() => existing.AmmoEffects),
                Recoil = jObj.ReadProperty(() => existing.Recoil),
                Durability = jObj.ReadProperty(() => existing.Durability),
                DefaultMods = jObj.ReadProperty(() => existing.DefaultMods),
                IntegralMagazineSize = jObj.ReadProperty(() => existing.IntegralMagazineSize),
                ReloadTime = jObj.ReadProperty(() => existing.ReloadTime),
                BarrelLength = jObj.ReadProperty(() => existing.BarrelLength),
                ReloadNoiseVolume = jObj.ReadProperty(() => existing.ReloadNoiseVolume),
                IntegralModifications = jObj.ReadProperty(() => existing.IntegralModifications),
                AimSpeed = jObj.ReadProperty(() => existing.AimSpeed),
                InstallationTime = jObj.ReadProperty(() => existing.InstallationTime),
                Location = jObj.ReadProperty(() => existing.Location),
                ModTargets = jObj.ReadProperty(() => existing.ModTargets),
            };
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            return Load(data, existing as SlotGunMod);
        }
    }

    public class MagazineTypeLoader : ITypeLoader<SlotMagazine> {
        /// <inheritdoc />
        public SlotMagazine Load(JObject jObj, SlotMagazine existing = default(SlotMagazine)) {
            return new SlotMagazine()
            {
                AmmoType = jObj.ReadProperty(() => existing.AmmoType),
                ReloadTime = jObj.ReadProperty(() => existing.ReloadTime),
                Capacity = jObj.ReadProperty(() => existing.Capacity),
                DefaultAmmo = jObj.ReadProperty(() => existing.DefaultAmmo),
                DefaultCount = jObj.ReadProperty(() => existing.DefaultCount),
                Linkage = jObj.ReadProperty(() => existing.Linkage),
                Reliability = jObj.ReadProperty(() => existing.Reliability)
            };
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
             return Load(data, existing as SlotMagazine);
        }
    }

    public class BionicTypeLoader : ITypeLoader<SlotBionic> {
        /// <inheritdoc />
        public SlotBionic Load(JObject jObj, SlotBionic existing = default(SlotBionic)) {
            return new SlotBionic()
            {
                Id = jObj.ReadProperty(() => existing.Id),
                BionicId = jObj.ReadProperty(() => existing.BionicId),
                InstallationDifficulty = jObj.ReadProperty(() => existing.InstallationDifficulty)
            };
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            return Load(data, existing as SlotBionic);
        }
    }

    public class AmmoTypeLoader : ITypeLoader<SlotAmmo> {
        /// <inheritdoc />
        public SlotAmmo Load(JObject jObj, SlotAmmo existing = default(SlotAmmo)) {
            return new SlotAmmo()
            {
                AmmoType = jObj.ReadProperty(() => existing.AmmoType),
                DefaultCharges = jObj.ReadProperty(() => existing.DefaultCharges),
                Range = jObj.ReadProperty(() => existing.Range),
                Damage = jObj.HasObject("damage") ? jObj.ReadProperty(() => existing.Damage) : null,
                Dispersion = jObj.ReadProperty(() => existing.Dispersion),
                LegacyDamage = !jObj.HasObject("damage") ? jObj.ReadProperty(() => existing.LegacyDamage) : null,
                LegacyPierce = !jObj.HasObject("pierce") ? jObj.ReadProperty(() => existing.LegacyPierce) : null,
                Loudness = jObj.ReadProperty(() => existing.Loudness),
                Recoil = jObj.ReadProperty(() => existing.Recoil),
                AmmoEffects = jObj.ReadProperty(() => existing.AmmoEffects),
                Casing = jObj.ReadProperty(() => existing.Casing),
                CooksOff = jObj.ReadProperty(() => existing.CooksOff),
                DropActive = jObj.ReadProperty(() => existing.CooksOff),
                DropChance = jObj.ReadProperty(() => existing.DropChance),
                Drops = jObj.ReadProperty(() => existing.Drops),
                SpecialCookOff = jObj.ReadProperty(() => existing.SpecialCookOff)
            };
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            return Load(data, existing as SlotAmmo);
        }
    }

    public class SeedTypeLoader : ITypeLoader<SlotSeed> {
        /// <inheritdoc />
        public SlotSeed Load(JObject jObj, SlotSeed existing = default(SlotSeed)) {
            return new SlotSeed()
            {
                ByProducts = jObj.ReadProperty(() => existing.ByProducts),
                Fruit = jObj.ReadProperty(() => existing.Fruit),
                FruitDivisor = jObj.ReadProperty(() => existing.FruitDivisor),
                GrowthTime = jObj.ReadProperty(() => existing.GrowthTime),
                PlantName = jObj.ReadProperty(() => existing.PlantName),
                SpawnSeeds = jObj.ReadProperty(() => existing.SpawnSeeds)
            };
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            return Load(data, existing as SlotSeed);
        }
    }
}