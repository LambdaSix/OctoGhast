using System;
using System.Collections.Generic;
using System.Linq;
using InfiniMap;
using OctoGhast.Entity;
using OctoGhast.Units;
using static OctoGhast.Translation.Translation;

namespace OctoGhast.Cataclysm.LegacyLoader {
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
        public string Color { get; set; } = "white"; // TODO: Enum for default colours?

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

        public string GetName(int quantity = 1) {
            return Translation.Translation._($"{Name}", $"{PluralName}", quantity);
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