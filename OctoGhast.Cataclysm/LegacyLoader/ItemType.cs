using System;
using System.Collections.Generic;
using System.Linq;
using OctoGhast.Entity;
using OctoGhast.Framework;
using OctoGhast.Units;
using static OctoGhast.Translation.Translation;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class ItemType {
        [LoaderInfo("container_data", TypeLoader = typeof(ContainerTypeLoader))]
        public SlotContainer Container { get; set; }
        
        [LoaderInfo("tool_data", TypeLoader = typeof(ToolTypeLoader))]
        public SlotTool Tool { get; set; }

        [LoaderInfo("comestible_data", TypeLoader = typeof(ComestibleTypeLoader))]
        public SlotComestible Comestible { get; set; }

        [LoaderInfo("brewable", TypeLoader = typeof(BrewableTypeLoader))]
        public SlotBrewable Brewable { get; set; }

        [LoaderInfo("armor_data", TypeLoader = typeof(ArmorTypeLoader))]
        public SlotArmor Armor { get; set; }

        [LoaderInfo("book_data", TypeLoader = typeof(BookTypeLoader))]
        public SlotBook Book { get; set; }

        [LoaderInfo("mod_data", TypeLoader = typeof(ModTypeLoader))]
        public SlotMod Mod { get; set; }

        [LoaderInfo("engine_data", TypeLoader = typeof(EngineTypeLoader))]
        public SlotEngine Engine { get; set; }

        [LoaderInfo("wheel_data", TypeLoader = typeof(WheelTypeLoader))]
        public SlotWheel Wheel { get; set; }

        [LoaderInfo("fuel_data", TypeLoader = typeof(FuelTypeLoader))]
        public SlotFuel Fuel { get; set; }

        [LoaderInfo("gun_data", TypeLoader = typeof(GunTypeLoader))]
        public SlotGun Gun { get; set; }

        [LoaderInfo("gunmod_data", TypeLoader = typeof(GunModTypeLoader))]
        public SlotGunMod GunMod { get; set; }

        [LoaderInfo("magazine_data", TypeLoader = typeof(MagazineTypeLoader))]
        public SlotMagazine Magazine { get; set; }

        [LoaderInfo("bionic_data", TypeLoader = typeof(BionicTypeLoader))]
        public SlotBionic Bionic { get; set; }

        [LoaderInfo("ammo_data", TypeLoader=typeof(AmmoTypeLoader))]
        public SlotAmmo Ammo { get; set; }

        [LoaderInfo("seed_data", TypeLoader = typeof(SeedTypeLoader))]
        public SlotSeed Seed { get; set; }
        // TODO: Artifacts

        /// <summary>
        /// String identifier for this type
        /// </summary>
        [LoaderInfo("id", true, "null")]
        private string ID { get; set; } = "null";

        [LoaderInfo("name", true, "none")]
        public string Name { get; set; } = "none";
        [LoaderInfo("name_plural", false, "none")]
        public string PluralName { get; set; } = "none";

        [LoaderInfo("looks_like")]
        public StringID<ItemType> LooksLike { get; set; }

        [LoaderInfo("snippet_category")]
        public string SnippetCategory { get; set; }

        [LoaderInfo("description")]
        public string Description { get; set; }

        [LoaderInfo("container")]
        public StringID<ItemType> DefaultContainer { get; set; }

        [LoaderInfo("qualities")]
        public Dictionary<string,int> Qualities { get; set; }

        [LoaderInfo("properties")]
        public Dictionary<string,string> Properties { get; set; }

        [LoaderInfo("materials")]
        public IEnumerable<StringID<Material>> Materials { get; set; }

        [LoaderInfo("use_methods")]
        public Dictionary<string,Action> UseMethods { get; set; }

        [LoaderInfo("countdown_interval", false, 0)]
        public int CountdownInterval { get; set; }

        [LoaderInfo("countdown_action")]
        // TODO: Implement UseMethods/UseFunction (func{t,t,t} ?)
        public string CountdownAction { get; set; }

        [LoaderInfo("countdown_destroy", false, false)]
        public bool CountDownDestroy { get; set; }

        [LoaderInfo("drop_action")]
        public string DropAction { get; set; }

        [LoaderInfo("emits")]
        public IEnumerable<StringID<Emit>> Emits { get; set; }

        [LoaderInfo("item_tags")]
        public IEnumerable<string> ItemTags { get; set; }

        [LoaderInfo("techniques")]
        public IEnumerable<StringID<MaterialArtsTechnique>> Techniques { get; set; }

        [LoaderInfo("min_str", false, 0)]
        public int MinimumStrength { get; set; }

        [LoaderInfo("min_dex", false, 0)]
        public int MinimumDexterity { get; set; }
        [LoaderInfo("min_int", false, 0)]
        public int MinimumIntelligence { get; set; }

        [LoaderInfo("min_per", false, 0)]
        public int MinimumPerception { get; set; }

        [LoaderInfo("min_skills")]
        public Dictionary<StringID<Skill>, int> MinimumSkills { get; set; }

        [LoaderInfo("explode_in_fire", false, false)]
        public bool ExplodesInFire { get; set; }

        [LoaderInfo("explosion_data")]
        public ExplosionData Explosion { get; set; }

        [LoaderInfo("phase", false, "Solid")]
        public string Phase { get; set; }

        [LoaderInfo("stackable", false, false)]
        public bool Stackable { get; set; } = false;

        [LoaderInfo("weight", true, "0.0KG")]
        public Mass Weight { get; set; }

        [LoaderInfo("volume", true, "0.0L")]
        public Volume Volume { get; set; }

        [LoaderInfo("price", false, 0)]
        public int Price { get; set; }
        [LoaderInfo("price_postapoc", false, 0)]
        public int PriceAfterEpoch { get; set; }

        [LoaderInfo("stack_size", false, 0)]
        public int StackSize { get; set; }

        [LoaderInfo("integral_volume", false, "-1ML")]
        public Volume IntegralVolume { get; set; }

        [LoaderInfo("rigid", false, true)]
        public bool Rigid { get; set; }

        [LoaderInfo("melee_data")]
        public Dictionary<string,int> MeleeDamageTypes { get; set; }

        [LoaderInfo("thrown_damage", TypeLoader = typeof(DamageInfoTypeLoader))]
        public DamageInfo ThrownDamage { get; set; }

        /// <summary>
        /// To hit bonus for melee combat, -5 to +5 is reasonable
        /// </summary>
        [LoaderInfo("to_hit")]
        public int ToHit { get; set; } = 0;

        /// <summary>
        /// Exactly the same as item_Tags LIGHT_*, this is for the lightmap
        /// </summary>
        public int LightEmission { get; set; } = 0;

        [LoaderInfo("category")]
        public StringID<ItemCategory> Category { get; set; } = null;

        /// <summary>
        /// Color on the map
        /// </summary>
        [LoaderInfo("color")]
        public string Color { get; set; } = "white"; // TODO: Enum for default colours?

        [LoaderInfo("symbol")]
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
        [LoaderInfo("magazines")]
        public Dictionary<StringID<AmmoType>, IEnumerable<StringID<ItemType>>> Magazines { get; set; }

        /// <summary>
        /// Default magazine for each ammotype that can be used to reload this item.
        /// </summary>
        /// <remarks>
        /// This is generally the first magazine in Magazines for the given ammo-type
        /// </remarks>
        public Dictionary<StringID<AmmoType>, StringID<ItemType>> MagazineDefault { get; set; }

        [LoaderInfo("magazine_well")]
        public Volume MagazineWell { get; set; } = "0.0L";

        [Obsolete("Query the item for what it can support")]
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

        public StringID<ItemType> GetDefaultMagazine(StringID<AmmoType> caliber) {
            if (Magazines.TryGetValue(caliber, out var defaultMagazine)) {
                return defaultMagazine.First();
            }

            return StringID<ItemType>.NullId;
        }

        public string GetName(int quantity = 1) {
            return _($"{Name}", $"{PluralName}", quantity);
        }

        public string GetId() => ID;
        public bool CountByCharges() => Stackable;

        public int ChargesDefault() => (int) (Tool?.DefaultCharges ?? Comestible?.DefaultCharges ?? Ammo?.DefaultCharges ?? (Stackable ? 1 : 0));

        public int ChargesToUse() => Tool?.ChargesPerUse ?? 1;

        public int MaximumCharges() => (int) (Tool?.MaxCharges ?? 1);

        public bool HasUse() => UseMethods.Any();

        public bool CanUse(string useName) => GetUse(useName) != null;

        public Action GetUse(string useName) {
            return UseMethods.TryGetValue(useName, out var action)
                ? action
                : null;
        }
    }
}