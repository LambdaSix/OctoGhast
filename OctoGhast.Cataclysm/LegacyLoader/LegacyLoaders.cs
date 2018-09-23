using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using OctoGhast.Entity;
using OctoGhast.Spatial;
using static OctoGhast.Translation.Translation;
using OctoGhast.Units;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class Skill {

    }

    public class Bionic {

    }

    public class Emit {

    }

    public class MaterialArtsTechnique {

    }

    public class MonsterGroup {

    }

    public enum AddictionType {
        None,
        Caffeine,
        Alcohol,
        Sleep,
        Painkiller,
        Speed,
        Cigarette,
        Cocaine,
        CrackCocaine,
        Mutagen,
        Diazepam,
        MarlossR,
        MarlossB,
        MarlossY
    }

    public class GunMode {
        public string Name { get; }

        public Item Target { get; }

        /// <summary>
        /// Burst size for firearms, melee range for melee weapons.
        /// </summary>
        public int Value { get; }

        public IEnumerable<string> Flags { get; }

        public GunMode(string name, Item target, int value, IEnumerable<string> flags) {
            Name = name;
            Target = target;
            Value = value;
            Flags = flags;
        }

        public bool IsMelee() => Flags.Contains("MELEE");
    }

    public class GunModifierData {
        public string Name { get; }
        public int Quantity { get;  }
        public IEnumerable<string> Flags { get; set; }

        public GunModifierData(string name, int quantity, IEnumerable<string> flags) {
            Name = name;
            Quantity = quantity;
            Flags = flags;
        }
    }

    public class GunModLocation {
        public string Id { get; }

        public GunModLocation(string id) {
            Id = id;
        }

        public static bool operator ==(GunModLocation lhs, GunModLocation rhs) =>
            String.CompareOrdinal(lhs.Id, rhs.Id) == 0;
        public static bool operator !=(GunModLocation lhs, GunModLocation rhs) => !(lhs == rhs);

        public static bool operator >(GunModLocation lhs, GunModLocation rhs) =>
            String.CompareOrdinal(lhs.Id, rhs.Id) > 1;

        public static bool operator <(GunModLocation lhs, GunModLocation rhs) =>
            String.CompareOrdinal(lhs.Id, rhs.Id) < 0;
    }

    public class SlotTool {
        public StringID<AmmoType> AmmoId { get; set; } = StringID<AmmoType>.NullId;
        public StringID<ItemType> RevertsTo { get; set; } = StringID<ItemType>.NullId;
        public string RevertMessage { get; set; }
        public string SubType { get; set; }
        public long MaxCharges { get; set; } = default;
        public long DefaultCharges { get; set; } = default;
        public IEnumerable<long> RandomCharges { get; set; }
        public int ChargesPerUse { get; set; } = default;
        public int TurnsPerCharge { get; set; } = default;
    }

    public class SlotComestible {
        public string ComestibleType { get; set; } = default;
        public StringID<ItemType> Tool { get; set; } = StringID<ItemType>.NullId;
        public long DefaultCharges { get; set; } = 1;
        public int Quench { get; set; } = default;

        // TODO: Convert to Calories and mix in Carb/Fat/Protein info
        public int Nutrition { get; set; } = default;

        public TimeDuration SpoilsIn { get; set; } = new TimeDuration(0);
        public int AddictionFactor { get; set; } = 0;
        public AddictionType AddictionType { get; set; } = AddictionType.None;
        public int Fun { get; set; } = 0;
        public int StimulantFactor { get; set; } = 0;
        public int HealthyFactor { get; set; } = 0;
        public int ParasiteFactor { get; set; } = 0;
        public Dictionary<VitaminInfo, int> Vitamins { get; set; }

        /// <summary>
        /// 1 Nutrient ~= 8.7kCal (1Nutr/5Min = 288Nutr/day at 2500kcal/day)
        /// </summary>
        private static float kCalPerNutrients = 2500.0f / (12 * 24);

        public float GetCalories() => Nutrition * kCalPerNutrients;

        public StringID<MonsterGroup> RotSpawn { get; set; } = StringID<MonsterGroup>.NullId;
        public int RotSpawnChance { get; set; } = 10;
    }

    public class SlotBrewable {
        public IEnumerable<StringID<ItemType>> Results { get; set; }
        private TimeDuration Time { get; set; } = new TimeDuration(0);
    }

    public class SlotContainer {
        public Volume Contains { get; set; } = "0.0L";
        public bool Seals { get; set; } = default;
        public bool Watertight { get; set; } = default;
        public bool Preserves { get; set; } = default;
        public StringID<ItemType> UnsealsInto { get; set; } = StringID<ItemType>.NullId;
    }

    public class SlotArmor {
        public IEnumerable<string> Covers { get; set; }
        public bool Sided { get; set; } = false;
        public int Encumber { get; set; } = default;

        public int Thickness { get; set; } = default;
        public int EnvResist { get; set; } = default;
        public int EnvResistWithFilter { get; set; } = default;
        public int Warmth { get; set; } = default;

        // TODO: Units.Volume usage
        public Volume Storage { get; set; } = "0.0L";

        public bool PowerArmour { get; set; } = default;
    }

    public class SlotBook {
        public StringID<Skill> Skill = StringID<Skill>.NullId;
        public int Level { get; set; } = default;
        public int Req { get; set; } = default;
        public int Fun { get; set; } = default;
        public int Intel { get; set; } = default;

        /// <summary>
        /// How long in in minutes (10 turns) it takes to read.
        /// 'to read' means getting 1 skill-point.
        /// </summary>
        public int Time { get; set; } = 0;

        public int Chapters { get; set; } = 0;
    }

    public class SlotMod {
        public IEnumerable<string> AcceptableAmmo { get; set; }
        public string AmmoModifier { get; set; }
        public Dictionary<string, IEnumerable<string>> MagazineAdapter { get; set; }
        public float CapacityMultiplier { get; set; } = 1.0f;
    }

    public class CommonRangedData {
        public DamageInfo Damage { get; set; }
        public int Range { get; set; } = 0;
        public int Dispersion { get; set; } = 0;

        [Obsolete]
        public int LegacyPierce { get; set; }
        [Obsolete]
        public int LegacyDamage { get; set; }
    }

    public class SlotEngine {
        /// <summary>
        /// For combustion engines, the displacement in Cubic Centimeters
        /// </summary>
        public int Displacement { get; set; } = 0;

        public IEnumerable<string> Faults { get; set; }
    }

    public class SlotWheel {
        /// <summary>
        /// Diameter of wheel in inches
        /// </summary>
        public int Diameter { get; set; } = 0;

        /// <summary>
        /// Width of wheel in inches
        /// </summary>
        public int Width { get; set; } = 0;
    }

    public class SlotFuel {
        /// <summary>
        /// Energy of the fuel in kJ per charge.
        /// </summary>
        public float Energy { get; set; }
    }

    public class SlotGun : CommonRangedData {
        public StringID<Skill> SkillUsed { get; set; } = StringID<Skill>.NullId;
        public StringID<AmmoType> Ammo { get; set; } = StringID<AmmoType>.NullId;
        public int Durability { get; set; } = 0;
        public int IntegralMagazineSize { get; set; } = 0;
        public TimeDuration ReloadTime { get; set; } = new TimeDuration();
        public string ReloadNoise { get; set; } = _("click.");
        public int ReloadNoiseVolume { get; set; } = 0;
        public int SightDispersion { get; set; } = 30;
        public int Loudness { get; set; } = default;
        public int UPSCharges { get; set; } = default;
        public Volume BarrelLength { get; set; } = "0ml";
        public IEnumerable<string> AmmoEffects { get; set; }

        /// <summary>
        /// Key is the (untranslated) location, value is the number of mods can have installed there.
        /// </summary>
        public Dictionary<GunModLocation, int> ValidModLocations { get; set; }

        /// <summary>
        /// Built in mods, these mods cannot be removed (IRREMOVABLE)
        /// </summary>
        public IEnumerable<StringID<ItemType>> IntegralModifications { get; set; }

        /// <summary>
        /// Default mods, these are removable.
        /// </summary>
        public IEnumerable<StringID<ItemType>> DefaultMods { get; set; }

        /// <summary>
        /// Firing modes supported by the weapon, should always have at least DEFAULT mode
        /// </summary>
        public Dictionary<StringID<GunMode>, GunModifierData> ModeModifier { get; set; }

        /// <summary>
        /// Burst size for AUTO mode, legacy field for items not migrated to specify modes
        /// </summary>
        public int Burst { get; set; } = 0;

        /// <summary>
        /// How easy is the weapon to control (weight, recoil, etc). If unset, value derived from weapon type.
        /// </summary>
        public int Handling { get; set; } = -1;

        /// <summary>
        /// Additional recoil applied per shot, before effects of handling are considered.
        /// Useful for adding recoil effect to weapons that otherwise consume no ammunition.
        /// </summary>
        public int Recoil { get; set; }
    }

    public class GunType {
        private string Name { get; set; }

        public GunType(string name) {
            Name = name;
        }

        public static bool operator ==(GunType rhs, GunType lhs) => String.CompareOrdinal(lhs.Name, rhs.Name) == 0;
        public static bool operator !=(GunType rhs, GunType lhs) => !(rhs == lhs);

        public static bool operator <(GunType rhs, GunType lhs) => String.CompareOrdinal(lhs.Name, rhs.Name) < 0;
        public static bool operator >(GunType rhs, GunType lhs) => String.CompareOrdinal(lhs.Name, rhs.Name) > 0;
    }

    public class SlotGunMod : SlotGun {
        public GunModLocation Location { get; set; }
        public IEnumerable<GunType> Usable { get; set; }

        /// <summary>
        /// If this value is set, this gunmod functions as a sight.
        /// A sight is only usable to aim by a character whose current Character::recoil is at or below this value.
        /// </summary>
        public new int? SightDispersion { get; set; } = default;

        /// <summary>
        /// For Sights, this value affects time cost of aiming.
        /// Higher is better, in case of multiple usable sighs, the one with the highest aim speed is used.
        /// </summary>
        public int? AimSpeed { get; set; }

        public new int Loudness { get; set; } = 0;
        public TimeDuration InstallationTime { get; set; } = new TimeDuration();
        
        /// <summary>
        /// Increase base gun UPS consumption by this amount per shot.
        /// </summary>
        public new int UPSCharges { get; set; } = 0;

        /// <summary>
        /// Firing modes added to or replacing those of the base gun.
        /// </summary>
        public new Dictionary<GunMode, GunModifierData> ModeModifier { get; set; }

        public IEnumerable<string> AmmoEffects { get; set; }

        public new int Handling { get; set; } = 0;
    }

    public class SlotMagazine {
        public StringID<AmmoType> Type { get; set; } = StringID<AmmoType>.NullId;
        public int Capacity { get; set; } = 0;
        public int DefaultCount { get; set; }
        public StringID<ItemType> DefaultAmmo { get; set; } = StringID<ItemType>.NullId;
        public int Reliability { get; set; } = 0;

        /// <summary>
        /// How long it takes to load each unit of ammunition into the magazine.
        /// Defaults to 36 seconds (6 turns)
        /// </summary>
        public TimeDuration ReloadTime { get; set; } = TimeDuration.FromSeconds(36);

        /// <summary>
        /// For ammo-belts, one linkage of given type is dropped for each unit of ammunition consumed
        /// </summary>
        public StringID<ItemType> Linkage { get; set; } = StringID<ItemType>.NullId;

        /// <summary>
        /// Will the magazine protect any contents if affected by fire?
        /// </summary>
        public bool ProtectContents { get; set; } = false;
    }

    public class SlotAmmo : CommonRangedData {
        /// <summary>
        /// The type of ammo that fits into this.
        /// </summary>
        public IEnumerable<StringID<AmmoType>> Type { get;set; }
        public StringID<ItemType> Casing { get; set; } = StringID<ItemType>.NullId;

        /// <summary>
        /// Type of item, if any, dropped at ranged target.
        /// </summary>
        public StringID<ItemType> Drops { get; set; } = StringID<ItemType>.NullId;

        public float DropChance { get; set; } = 1.0f;
        public bool DropActive { get; set; } = true;

        public long DefaultCharges { get; set; } = 1;
        public int Loudness { get; set; }= -1;
        public int Recoil { get; set; } = -1;
        public bool CooksOff { get; set; } = false;
        public bool SpecialCookOff { get; set; } = false;
    }

    public class SlotBionic {
        public int InstallationDifficulty { get; set; }
        public StringID<Bionic> Id { get; set; }
    }

    public class SlotSeed {
        public TimeDuration GrowthTime { get; set; } = new TimeDuration();
        public int FruitDivisor { get; set; } = 1;
        public string PlantName { get; set; }
        public StringID<ItemType> Fruit { get; set; }
        public bool SpawnSeeds { get; set; } = true;
        public IEnumerable<string> ByProducts { get; set; }
    }

    // TODO: Artifacts ?

    public class ItemType {
        public SlotContainer Container { get; set; }
        public SlotTool Tool { get; set; }
        public SlotComestible Comestible { get; set; }
        public SlotBrewable Brewable { get; set; }
        public SlotArmor Armor { get; set; }
        public SlotBook Book { get; set; }
        public SlotMod Mod { get; set; }
        public SlotEngine Engine { get; set; }
        public SlotWheel Wheel { get; set; }
        public SlotFuel Fuel { get; set; }
        public SlotGun Gun { get; set; }
        public SlotGunMod GunMod { get; set; }
        public SlotMagazine Magazine { get; set; }
        public SlotBionic Bionic { get; set; }
        public SlotAmmo Ammo { get; set; }
        public SlotSeed Seed { get; set; }
        // TODO: Artifacts

        /// <summary>
        /// String identifier for this type
        /// </summary>
        private string ID = "null";

        public string Name = "none";
        public string PluralName = "none";

        public StringID<ItemType> LooksLike { get; set; } = StringID<ItemType>.NullId;
        public string SnippetCategory { get; set; }
        public string Description { get; set; }
        public StringID<ItemType> DefaultContainer { get; set; }
        public Dictionary<string,int> Qualities { get; set; }
        public Dictionary<string,string> Properties { get; set; }

        public IEnumerable<StringID<Material>> Materials { get; set; }

        // TODO: Implement UseMethods/UseFunction (func{t,t,t} ?)
        public Dictionary<string,Action> UseMethods { get; set; }

        public int CountdownInterval { get; set; }= 0;

        // TODO: Implement UseMethods/UseFunction (func{t,t,t} ?)
        public Action CountdownAction { get; set; }

        public bool CountDownDestroy { get; set; } = false;

        public Action DropAction { get; set; }

        public IEnumerable<StringID<Emit>> Emits { get; set; }

        public IEnumerable<string> ItemTags { get; set; }
        public IEnumerable<StringID<MaterialArtsTechnique>> Techniques { get; set; }

        public int MinimumStrength { get; set; } = 0;
        public int MinimumDexterity { get; set; } = 0;
        public int MinimumIntelligence { get; set; } = 0;
        public int MinimumPerception { get; set; } = 0;
        public Dictionary<StringID<Skill>, int> MinimumSkills { get; set; }

        public bool ExplodesInFire { get; set; } = false;
        public ExplosionData Explosion { get; set; }

        public MaterialPhase Phase { get; set; } = MaterialPhase.Solid;

        public bool Stackable { get; set; } = false;

        public Mass Weight { get; set; } = "0.0KG";
        public Volume Volume { get; set; } = "0.0L";

        public int Price { get; set; } = 0;
        public int PriceAfterEpoch { get; set; } = -1;
        public int StackSize { get; set; } = 0;
        public Volume IntegralVolume { get; set; } = null;

        public bool Rigid { get; set; } = true;
        public IEnumerable<int> MeleeDamageTypes { get; set; }
        public DamageInfo ThrownDamage { get; set; }

        /// <summary>
        /// To hit bonus for melee combat, -5 to +5 is reasonable
        /// </summary>
        public int ToHit { get; set; } = 0;

        /// <summary>
        /// Exactly the same as item_Tags LIGHT_*, this is for the lightmap
        /// </summary>
        public int LightEmission { get; set; } = 0;

        public ItemCategory Category { get; set; } = null;

        /// <summary>
        /// Color on the map
        /// </summary>
        public string Color { get; set; } = "white";

        public string Symbol { get; set; } = "#";

        public int DamageMin { get; set; } = -1;
        public int DamageMax { get; set; } = 4;

        /// <summary>
        /// What items can repair this item?
        /// </summary>
        public IEnumerable<StringID<ItemType>> RepairWith { get; set; }

        /// <summary>
        /// Magazine types, if any, for each ammo type that can be used to reload this item.
        /// </summary>
        public Dictionary<StringID<AmmoType>, IEnumerable<StringID<ItemType>>> Magazines { get; set; }

        /// <summary>
        /// Default magazine for each ammotype that can be used to reload this item.
        /// </summary>
        public Dictionary<StringID<AmmoType>, StringID<ItemType>> MagazineDefault { get; set; }

        public Volume MagazineWell { get; set; } = "0.0L";

        public string GetItemType() {
            bool NotNull<T>(T t) => t != null;

            if (NotNull(Tool))
                return "TOOL";
            if (NotNull(Comestible))
                return "COMESTIBLE";
            if (NotNull(Container))
                return "CONTAINER";
            if (NotNull(Armor))
                return "ARMOR";
            if (NotNull(Book))
                return "BOOK";
            if (NotNull(Gun))
                return "GUN";
            if (NotNull(Bionic))
                return "BIONIC";
            if (NotNull(Ammo))
                return "AMMO";

            return "MISC";
        }

        public string GetName(int quantity) {
            return _($"{Name}", $"{PluralName}", quantity);
        }

        public string GetId() => ID;
        public bool CountByCharges() => Stackable;

        public int ChargesDefault() {
            return (int) (Tool?.DefaultCharges ?? Comestible?.DefaultCharges ?? Ammo?.DefaultCharges ?? (Stackable ? 1 : 0));
        }

        public int ChargesToUse() => Tool?.ChargesPerUse ?? 1;

        public int MaximumCharges() => (int) (Tool?.MaxCharges ?? 1);

        public bool HasUse() => UseMethods.Any();

        public bool CanUse(string useName) => GetUse(useName) != null;

        public Action GetUse(string useName) {
            return UseMethods.TryGetValue(useName, out var action)
                ? action
                : null;
        }

        public long Invoke(Player p, Item it, Vector3 pos) {
            if (!HasUse())
                return 0;
            return Invoke(p, it, pos, UseMethods.First().Key);
        }

        public long Invoke(Player p, Item it, Vector3 pos, string useName) {
            var use = GetUse(useName);
            if (use == null) {
                Debug.WriteLine($"Tried to invoke {useName} on a {GetName(1)}, which doesn't have this use_function");
                return 0;
            }

            var res = use?.Invoke(p, it, false, pos);
            if (res)
        }
        public long Tick(Player p, Item it, Vector3 pos);
    }

    public class ItemCategory { }

    public class ExplosionData { }

    public class LegacyLoaders {
    }
}