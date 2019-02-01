using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using OctoGhast.Framework;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public interface ITypeLoader<T> : ITypeLoader {
        T Load(JObject jObj, T existing = default(T));
    }

    public class TypeLoader {
        /// <summary>
        /// Returns either the sub-object on <paramref name="jObj"/> matching at least one of
        /// <paramref name="names"/> or the root object if none match.
        /// </summary>
        /// <param name="jObj">JObject to inspect</param>
        /// <param name="names">List of property names to look for</param>
        /// <returns>JObject of either the sub-object or root object</returns>
        public static JObject FindObject(JObject jObj, params string[] names) {
            // TODO: Throw an exception is the object has >1 of the fields matched by name.

            // Loop through all given names, and return the first match.
            foreach (var name in names) {
                if (jObj.ContainsKey(name))
                    return jObj[name] as JObject;
            }

            return jObj;
        }

        public T ReadIf<T>(Func<bool> typeMatcher, Func<bool> fieldMatcher, Func<T> output) {
            return typeMatcher() || fieldMatcher() ? output() : default(T);
        }

        public bool TypeMatches(string existingType, string matchType) =>
            String.Equals(existingType, matchType, StringComparison.CurrentCultureIgnoreCase);
    }

    public class ItemTypeLoader : TypeLoader, ITypeLoader<ItemType> {
        /// <inheritdoc />
        public ItemType Load(JObject jObj, ItemType existing = default(ItemType)) {
            var load = new ItemType();

            load.Id = jObj.ReadProperty(() => existing.Id);
            load.Abstract = jObj.ReadProperty(() => existing.Abstract);
            load.Type = jObj.ReadProperty(() => existing.Type);
            load.Container = ReadIf(
                () => TypeMatches(load.Type, "container"),
                () => jObj.ContainsKey("container_data"),
                () => jObj.ReadProperty(() => existing.Container));

            load.Tool = ReadIf(
                () => TypeMatches(load.Type, "tool"),
                () => jObj.ContainsKey("tool_data"),
                () => jObj.ReadProperty(() => existing.Tool));

            load.Comestible = ReadIf(
                () => TypeMatches(load.Type, "comestible"),
                () => jObj.ContainsKey("tool_data"),
                () => jObj.ReadProperty(() => existing.Comestible));

            load.Brewable = ReadIf(
                () => true, // Brewable isn't actually a TYPE atm..
                () => jObj.ContainsKey("brewable") || jObj.ContainsKey("brewable_data"),
                () => jObj.ReadProperty(() => existing.Brewable));

            load.Armor = ReadIf(
                () => TypeMatches(load.Type, "armor"),
                () => jObj.ContainsKey("armor_data"),
                () => jObj.ReadProperty(() => existing.Armor));

            load.Book = ReadIf(
                () => TypeMatches(load.Type, "book"),
                () => jObj.ContainsKey("book_data"),
                () => jObj.ReadProperty(() => existing.Book));

            load.ToolMod = ReadIf(
                () => TypeMatches(load.Type, "tool_mod"),
                () => jObj.ContainsKey("toolmod_data"),
                () => jObj.ReadProperty(() => existing.ToolMod));

            load.Engine = ReadIf(
                () => TypeMatches(load.Type, "engine"),
                () => jObj.ContainsKey("engine_data"),
                () => jObj.ReadProperty(() => existing.Engine));

            load.Wheel = ReadIf(
                () => TypeMatches(load.Type, "wheel"),
                () => jObj.ContainsKey("wheel_data"),
                () => jObj.ReadProperty(() => existing.Wheel));

            load.Fuel = ReadIf(
                () => TypeMatches(load.Type, "fuel"),
                () => jObj.ContainsKey("wheel_data"),
                () => jObj.ReadProperty(() => existing.Fuel));

            load.Gun = ReadIf(
                () => TypeMatches(load.Type, "gun"),
                () => jObj.ContainsKey("gun_data"),
                () => jObj.ReadProperty(() => existing.Gun));

            load.GunMod = ReadIf(
                () => TypeMatches(load.Type, "gunmod"),
                () => jObj.ContainsKey("gunmod_data"),
                () => jObj.ReadProperty(() => existing.GunMod));

            load.Magazine = ReadIf(
                () => TypeMatches(load.Type, "magazine"),
                () => jObj.ContainsKey("magazine_data"),
                () => jObj.ReadProperty(() => existing.Magazine));

            load.Bionic = ReadIf(
                () => TypeMatches(load.Type, "bionic"),
                () => jObj.ContainsKey("bionic_data"),
                () => jObj.ReadProperty(() => existing.Bionic));

            load.Ammo = ReadIf(
                () => TypeMatches(load.Type, "ammo"),
                () => jObj.ContainsKey("ammo_data"),
                () => jObj.ReadProperty(() => existing.Ammo));

            load.Seed = ReadIf(
                () => TypeMatches(load.Type, "seed"),
                () => jObj.ContainsKey("seed_data"),
                () => jObj.ReadProperty(() => existing.Seed));

            // End of optional data.

            load.Volume = jObj.ReadProperty(() => existing.Volume);
            load.Name = jObj.ReadProperty(() => existing.Name);
            load.PluralName = jObj.ReadProperty(() => existing.PluralName);
            load.Magazines = jObj.ReadProperty(() => existing.Magazines);
            load.Qualities = jObj.ReadProperty(() => existing.Qualities);
            load.Properties = jObj.ReadProperty(() => existing.Properties);
            load.CountdownInterval = jObj.ReadProperty(() => existing.CountdownInterval);
            load.UseActions = jObj.ReadProperty(() => existing.UseActions);
            load.Color = jObj.ReadProperty(() => existing.Color);
            load.CountDownDestroy = jObj.ReadProperty(() => existing.CountDownDestroy);
            load.Stackable = jObj.ReadProperty(() => existing.Stackable);
            load.Description = jObj.ReadProperty(() => existing.Description);
            load.Category = jObj.ReadProperty(() => existing.Category);
            load.Explosion = jObj.ReadProperty(() => existing.Explosion);
            load.Weight = jObj.ReadProperty(() => existing.Weight);
            load.Materials = jObj.ReadProperty(() => existing.Materials);
            load.DefaultContainer = jObj.ReadProperty(() => existing.DefaultContainer);
            load.Emits = jObj.ReadProperty(() => existing.Emits);
            load.ExplodesInFire = jObj.ReadProperty(() => existing.ExplodesInFire);
            load.IntegralVolume = jObj.ReadProperty(() => existing.IntegralVolume);
            load.ItemTags = jObj.ReadProperty(() => existing.ItemTags);
            load.LooksLike = jObj.ReadProperty(() => existing.LooksLike);
            load.MagazineWell = jObj.ReadProperty(() => existing.MagazineWell);
            load.MeleeDamageTypes = jObj.ReadProperty(() => existing.MeleeDamageTypes);
            load.MinimumDexterity = jObj.ReadProperty(() => existing.MinimumDexterity);
            load.MinimumIntelligence = jObj.ReadProperty(() => existing.MinimumIntelligence);
            load.MinimumPerception = jObj.ReadProperty(() => existing.MinimumPerception);
            load.MinimumSkills = jObj.ReadProperty(() => existing.MinimumSkills);
            load.MinimumStrength = jObj.ReadProperty(() => existing.MinimumStrength);
            load.Phase = jObj.ReadProperty(() => existing.Phase);
            load.Flags = jObj.ReadProperty(() => existing.Flags);
            load.Price = jObj.ReadProperty(() => existing.Price);
            load.PriceAfterEpoch = jObj.ReadProperty(() => existing.PriceAfterEpoch);
            load.Rigid = jObj.ReadProperty(() => existing.Rigid);
            load.SnippetCategory = jObj.ReadProperty(() => existing.SnippetCategory);
            load.StackSize = jObj.ReadProperty(() => existing.StackSize);
            load.Symbol = jObj.ReadProperty(() => existing.Symbol);
            load.Techniques = jObj.ReadProperty(() => existing.Techniques);
            load.ThrownDamage = jObj.ReadProperty(() => existing.ThrownDamage);
            load.ToHit = jObj.ReadProperty(() => existing.ToHit);
            return load;
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            return Load(data, existing as ItemType);
        }
    }

    public class ContainerTypeLoader : TypeLoader, ITypeLoader<SlotContainer> {
        public SlotContainer Load(JObject jObj, SlotContainer existing) {
            /*
             * Item data comes in two main flavours:
             *   - x_data : an object field with the data
             *   - type = x : fields are stored on the root object
             *
             * The line below handles both these situations.
             * The pattern is repeated for every type-loader except the base ItemTypeLoader
             * which always reads from the root object.
             */

            jObj = FindObject(jObj, "container_data");

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

    public class ToolTypeLoader : TypeLoader, ITypeLoader<SlotTool> {
        public SlotTool Load(JObject jObj, SlotTool existing) {
            jObj = FindObject(jObj, "tool_data");

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
            /*
             * DamageInfo is special and annoying, we don't use the FindObject here because
             * the data is either in ranged_damage or thrown_damage and both are full objects.
             */

            string type = null;
            if (jObj.ContainsKey("ranged_damage"))
                type = "ranged_damage";
            else if (jObj.ContainsKey("thrown_damage"))
                type = "thrown_damage";
            else
                return null;
            if (jObj.ContainsKey(type) && jObj.HasObject(type))
                return new DamageInfo()
                {
                    DamageUnits = jObj.ReadEnumerable(type, existing?.DamageUnits ?? new List<DamageUnit>(),
                        (token) => new DamageUnit(
                            DamageUnit.FromString(token.Value<string>("damage_type")),
                            token.Value<float>("amount"))
                    ).ToList(),
                };
            return null;
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            return Load(data, existing as DamageInfo);
        }
    }

    public class ComestibleTypeLoader : TypeLoader, ITypeLoader<SlotComestible> {
        /// <inheritdoc />
        public SlotComestible Load(JObject jObj, SlotComestible existing = default(SlotComestible)) {
            jObj = FindObject(jObj, "comestible_data");

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

    public class BrewableTypeLoader : TypeLoader, ITypeLoader<SlotBrewable> {
        /// <inheritdoc />
        public SlotBrewable Load(JObject jObj, SlotBrewable existing = default(SlotBrewable)) {
            jObj = FindObject(jObj, "brewable_data", "brewable");

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

    public class ArmorTypeLoader : TypeLoader, ITypeLoader<SlotArmor> {
        /// <inheritdoc />
        public SlotArmor Load(JObject jObj, SlotArmor existing = default(SlotArmor)) {
            jObj = FindObject(jObj, "armor_data");

            return new SlotArmor()
            {
                Coverage = jObj.ReadProperty(() => existing.Coverage),
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

    public class BookTypeLoader : TypeLoader, ITypeLoader<SlotBook> {
        /// <inheritdoc />
        public SlotBook Load(JObject jObj, SlotBook existing = default(SlotBook)) {
            jObj = FindObject(jObj, "book_data");

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

    public class ToolModTypeLoader : TypeLoader, ITypeLoader<SlotToolMod> {
        /// <inheritdoc />
        public SlotToolMod Load(JObject jObj, SlotToolMod existing = default(SlotToolMod)) {
            jObj = FindObject(jObj, "toolmod_data");

            return new SlotToolMod()
            {
                AcceptableAmmo = jObj.ReadProperty(() => existing.AcceptableAmmo),
                AmmoModifier = jObj.ReadProperty(() => existing.AmmoModifier),
                CapacityMultiplier = jObj.ReadProperty(() => existing.CapacityMultiplier),
                MagazineAdapter = jObj.ReadProperty(() => existing.MagazineAdapter),
            };
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            return Load(data, existing as SlotToolMod);
        }
    }

    public class EngineTypeLoader : TypeLoader, ITypeLoader<SlotEngine> {
        /// <inheritdoc />
        public SlotEngine Load(JObject jObj, SlotEngine existing = default(SlotEngine)) {
            jObj = FindObject(jObj, "engine_data");

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

    public class WheelTypeLoader : TypeLoader, ITypeLoader<SlotWheel> {
        /// <inheritdoc />
        public SlotWheel Load(JObject jObj, SlotWheel existing = default(SlotWheel)) {
            jObj = FindObject(jObj, "wheel_data");

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

    public class FuelTypeLoader : TypeLoader, ITypeLoader<SlotFuel> {
        /// <inheritdoc />
        public SlotFuel Load(JObject jObj, SlotFuel existing = default(SlotFuel)) {
            jObj = FindObject(jObj, "fuel_data");

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

    public class GunTypeLoader : TypeLoader, ITypeLoader<SlotGun> {
        /// <inheritdoc />
        public SlotGun Load(JObject jObj, SlotGun existing = default(SlotGun)) {
            jObj = FindObject(jObj, "gun_data");

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
                LegacyDamage = !jObj.HasObject("ranged_damage")
                    ? jObj.ReadProperty(() => existing.LegacyDamage)
                    : null,
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

    public class GunModTypeLoader : TypeLoader, ITypeLoader<SlotGunMod> {
        /// <inheritdoc />
        public SlotGunMod Load(JObject jObj, SlotGunMod existing = default(SlotGunMod)) {
            jObj = FindObject(jObj, "gunmod_data");

            return new SlotGunMod()
            {
                Ammo = jObj.ReadProperty(() => existing.Ammo),
                Damage = jObj.HasObject("ranged_damage") ? jObj.ReadProperty(() => existing.Damage) : null,
                Burst = jObj.ReadProperty(() => existing.Burst),
                Range = jObj.ReadProperty(() => existing.Range),
                Dispersion = jObj.ReadProperty(() => existing.Dispersion),
                ModeModifier = jObj.ReadProperty(() => existing.ModeModifier),
                LegacyDamage = !jObj.HasObject("ranged_damage")
                    ? jObj.ReadProperty(() => existing.LegacyDamage)
                    : null,
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

    public class ExplosionDataTypeLoader : ITypeLoader<ExplosionData> {
        /// <inheritdoc />
        public ExplosionData Load(JObject jObj, ExplosionData existing = default(ExplosionData)) {
            if (jObj.ContainsKey("explosion"))
                return new ExplosionData()
                {
                    Power = jObj.ReadProperty(() => existing.Power),
                    DistanceFactor = jObj.ReadProperty(() => existing.DistanceFactor),
                    Incendiary = jObj.ReadProperty(() => existing.Incendiary),
                    Shrapnel = jObj.ReadProperty(() => existing.Shrapnel)
                };

            return null;
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            return Load(data, existing as ExplosionData);
        }
    }

    public class ShrapnelDataTypeLoader : ITypeLoader<ShrapnelData> {

        /// <inheritdoc />
        public ShrapnelData Load(JObject jObj, ShrapnelData existing = default(ShrapnelData)) {
            if (jObj.ContainsKey("shrapnel"))
                return new ShrapnelData()
                {
                    FragmentMass = jObj.ReadProperty(() => existing.FragmentMass),
                    CasingMass = jObj.ReadProperty(() => existing.CasingMass),
                    ItemDropType = jObj.ReadProperty(() => existing.ItemDropType),
                    RecoveryChance = jObj.ReadProperty(() => existing.RecoveryChance)
                };

            return null;
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            return Load(data, existing as ShrapnelData);
        }
    }

    public class MagazineTypeLoader : TypeLoader, ITypeLoader<SlotMagazine> {
        /// <inheritdoc />
        public SlotMagazine Load(JObject jObj, SlotMagazine existing = default(SlotMagazine)) {
            jObj = FindObject(jObj, "magazine_dta");

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

    public class BionicTypeLoader : TypeLoader, ITypeLoader<SlotBionic> {
        /// <inheritdoc />
        public SlotBionic Load(JObject jObj, SlotBionic existing = default(SlotBionic)) {
            jObj = FindObject(jObj, "bionic_data");

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

    public class AmmoTypeLoader : TypeLoader, ITypeLoader<SlotAmmo> {
        /// <inheritdoc />
        public SlotAmmo Load(JObject jObj, SlotAmmo existing = default(SlotAmmo)) {
            jObj = FindObject(jObj, "ammo_data");

            return new SlotAmmo
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

    public class SeedTypeLoader : TypeLoader, ITypeLoader<SlotSeed> {
        /// <inheritdoc />
        public SlotSeed Load(JObject jObj, SlotSeed existing = default(SlotSeed)) {
            jObj = FindObject(jObj, "seed_data");

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