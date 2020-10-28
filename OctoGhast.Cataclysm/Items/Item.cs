using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using InfiniMap;
using OctoGhast.Cataclysm.LegacyLoader;
using OctoGhast.Cataclysm.Loaders.Creature;
using OctoGhast.Cataclysm.Loaders.Item;
using OctoGhast.Cataclysm.Loaders.WorldOptions;
using OctoGhast.Entity;
using OctoGhast.Framework;
using OctoGhast.Framework.Mobile;
using OctoGhast.Units;

namespace OctoGhast.Cataclysm.Items {
    public class BaseItem : RLObject<ItemType> {
        /// <summary>
        /// The time this item was created.
        /// </summary>
        public Time Birthday { get; }

        /// <summary>
        /// The age of this item.
        /// </summary>
        public TimeDuration Age => Calendar.Now - Birthday;

        public HashSet<string> Flags { get; }

        public BaseItem(ItemType data) : base(data) {
            Flags = new HashSet<string>(TemplateData.Flags);
        }

        public BaseItem(StringID<ItemType> ItemType, Time birthday) : this(FindType(ItemType)) {
            Birthday = birthday;
        }

        public static ItemType FindType(StringID<ItemType> type) {
            return World.Instance.Retrieve<ItemTemplateFactory>().RetrieveType(type.AsString());
        }
        
        /// <summary>
        /// Return the JSON-specified type of this instance.
        /// </summary>
        public string Type => TemplateData.Type;

        /// <summary>
        /// The base weight of a single item of this type, excluding contained items.
        /// </summary>
        /// <returns></returns>
        public virtual Mass BaseWeight() {
            if (HasFlag("NO_DROP"))
                return Mass.FromGrams(0);

            // The weight is either the weight as modified by runtime processes, or as defined in the template.
            var currentWeight = RuntimeData.Get("weight", TemplateData.Weight);

            if (HasFlag("REDUCED_WEIGHT"))
                currentWeight *= 0.75;

            return currentWeight;
        }

        /// <summary>
        /// The weight of the item, including any contained items.
        /// </summary>
        /// <returns></returns>
        public virtual Mass Weight() {
            var currentWeight = BaseWeight();

            var container = new Container(this);
            currentWeight += container.ContentWeight();

            return currentWeight;
        }

        /// <summary>
        /// The base volume of a single item of this type, excluding contained items or modifications.
        /// </summary>
        /// <returns></returns>
        public virtual Volume BaseVolume() {
            return RuntimeData.Get("volume", TemplateData.Volume);
        }

        /// <summary>
        /// The volume of the item, including any contained items and modifications. Includes magazines/barrel length mods
        /// </summary>
        /// <returns></returns>
        public virtual Volume Volume() {
            var currentVolume = BaseVolume();

            var container = new Container(this);
            currentVolume += container.ContentVolume();

            var rangedWeapon = new RangedWeapon(this);
            currentVolume += rangedWeapon.ModifierVolume();

            return currentVolume;
        }

        public bool HasFlag(string flag) => Flags.Contains(flag);
        public bool AddFlag(string flag) => Flags.Add(flag);
        public bool HasFlags(params string[] flags) => Flags.IsSupersetOf(flags);

        /*
         * Functions to ascertain what this item can be treated as.
         * An item may satisfy the conditions for multiple situations.
         * Functions so that an extension class in a mod can provide extra IsX methods.
         */
        
        /// <summary>
        /// Returns true if this item has Magazine data to use.
        /// </summary>
        public bool IsMagazine() => TemplateData.Magazine != null;

        /// <summary>
        /// Returns true if this item has Gun data to use.
        /// </summary>
        public bool IsRangedWeapon() => TemplateData.Gun != null;

        /// <summary>
        /// Returns true if this item has GunMod data to use.
        /// </summary>
        public bool IsGunMod() => TemplateData.GunMod != null;

        /// <summary>
        /// Returns true if this item has Bionic data to use.
        /// </summary>
        public bool IsBionic() => TemplateData.Bionic != null;

        public bool IsAmmunition() => TemplateData.Ammo != null;

        public bool IsComestible() => TemplateData.Comestible != null;

        public bool IsFood() => IsComestible() &&
                                (TemplateData.Comestible.ComestibleType == "FOOD" ||
                                 TemplateData.Comestible.ComestibleType == "FOOD");
    }

    /// <summary>
    /// Corpse is an item that holds a reference to a Creature.
    /// 
    /// </summary>
    public class Corpse : BaseItem {
        public BaseCreature CreatureType { get; set; }
        public int CorpseQuality { get; set; }

        /// <inheritdoc />
        public Corpse(StringID<ItemType> itemType, Time birthday) : base(itemType, birthday) { }

        public static Corpse MakeFrom(BaseCreature creature, Time birthday) {
            var corpse = new Corpse("corpse", Calendar.Now)
            {
                CreatureType = creature,
            };
            
            var worldOpts = World.Instance.Retrieve<WorldOptionsService>().Corpses;
            if (worldOpts.ResurrectionEnabled) {
                var resurrectionDelay = worldOpts.ResurrectionDelay;
                corpse.RuntimeData.Set("resurrection_time", Calendar.Now.AddTime(resurrectionDelay));
            }

            // Move all the top-level containers over to this, any sub-container items will
            // be moved due to being inside the top level stuff.
            creature.GetInventory()?.CopyTo(new Container(corpse));

            return corpse;
        }

        public void Resurrect() {
            // Recreate the originating creatureType with the appropriate flags
            // TODO: CreatureFlags.IsResurrected ? (Mod-support?)
            var creature = new BaseCreature(CreatureType.TemplateData);
            creature.RuntimeData.Set("isResurrected", true);
            // Anything currently in the corpse-container should be considered worn/carried by the creature
            var corpseContainer = new Container(this);
            // Should be a fresh container, so just copy over
            var creatureContainer = creature.GetInventory().AddRange(corpseContainer);
            // TODO: Reduce resulting creature's health and status by the condition of the corpse
            // TODO: Implement easier access to creature health, etc
            (creature.TemplateData as CreatureType).Stats.HitPoints *= CorpseQuality;

        }

        public override Mass Weight() {
            var container = new Container(this);
            var currentWeight = container.ContentWeight();

            // TODO: Pull the Weight data from the creature we're the corpse for.
            var creatureData = (CreatureType.TemplateData as CreatureType);
            return (currentWeight + (creatureData.Weight));
        }

        public override Volume Volume() {
            var creatureData = (CreatureType.TemplateData as CreatureType);
            return (creatureData.Volume);
        }
    }

    public class ItemManipulator {
        public ItemType TemplateData { get; set; }
        public RuntimeData RuntimeData { get; set; }

        public BaseItem Item { get; }

        public ItemManipulator(BaseItem item) {
            TemplateData = item.TemplateData;
            RuntimeData = item.RuntimeData;
            Item = item;
        }

        public ItemManipulator(ItemType templateData, RuntimeData runtimeData) {
            TemplateData = templateData;
            RuntimeData = runtimeData;
        }

        public ItemManipulator(ItemManipulator other) : this(other.TemplateData,other.RuntimeData)  {

        }

        public static T As<T>(ItemManipulator other) where T : ItemManipulator => (T) new ItemManipulator(other);
        public virtual T As<T>() where T : ItemManipulator => (T)new ItemManipulator(this);
    }

    public class Book : ItemManipulator
    {
        /// <inheritdoc />
        public Book(BaseItem item) : base(item) { }

        public int TotalChapters => TemplateData.Book?.Chapters ?? 0;

        public int RemainingChapters(Player player) {
            var data = RuntimeData.Get($"remaining-chapters-{player.Serial}", 0);
            return data;
        }

        public void ReadChapter(Player player) {
            var remaining = Math.Max(0, RemainingChapters(player) - 1);
            RuntimeData.Set($"remaining-chapters-{player.Serial}", remaining);
        }
    }

    /// <summary>
    /// Allows access to the Brewable components of an item and manipulation of that data.
    /// </summary>
    public class Brewable : ItemManipulator {
        /// <inheritdoc />
        public Brewable(BaseItem item) : base(item) { }

        public TimeDuration BrewingTime => TemplateData.Brewable?.Time ?? TimeDuration.FromTurns(-1);

        public IEnumerable<StringID<ItemType>> BrewingResults => TemplateData.Brewable?.Results ?? Enumerable.Empty<StringID<ItemType>>();
    }

    public class Wheel : ItemManipulator {
        /// <inheritdoc />
        public Wheel(BaseItem item) : base(item) { }

        public float WheelArea => (TemplateData.Wheel?.Diameter * TemplateData.Wheel?.Width) ?? 0;
    }

    public class Armor : ItemManipulator {
        public Armor(BaseItem item) : base(item) {
        }

        public bool IsSided => throw new NotImplementedException();

        public EquipmentSide CurrentEquipmentSide => throw new NotImplementedException();

        public int Warmth => CalculateWarmth(TemplateData.Armor?.Warmth ?? 0);

        public int Thickness => TemplateData.Armor?.Thickness ?? 0;

        public GearLayer GearLayer
        {
            get
            {
                if (Item.HasFlag("SKINTIGHT"))
                    return GearLayer.Underwear;
                if (Item.HasFlag("WAIST"))
                    return GearLayer.Waist;
                if (Item.HasFlag("OUTER"))
                    return GearLayer.Outer;
                if (Item.HasFlag("BELTED"))
                    return GearLayer.Belted;

                return GearLayer.Regular;
            }
        }

        public int Coverage => TemplateData.Armor?.Coverage ?? 0;

        public int Encumbrance => TemplateData.Armor?.Encumbrance ?? 0;

        // TODO: Move this to a derived property of a Container
        public Volume StorageVolume => TemplateData.Armor?.Storage ?? Volume.FromMilliliters(0);

        public int EnvironmentalResistance => throw new NotImplementedException();

        public int EnvironmentalResistanceWithFilter => throw new NotImplementedException();

        [Obsolete("Pre-emptive deprecation in favour of proposed power armour system")]
        public bool IsPowerArmor => throw new NotImplementedException("Deprecate in favour of proposed real power armour system");

        public int CalculateWarmth(int baseWarmth) {
            var total = baseWarmth;

            if (RuntimeData.Get("furred", false))
                total += 35 * (Coverage / 100);
            if (RuntimeData.Get("wooled", false))
                total += 20 * (Coverage / 100);

            return total;
        }

        public bool Covers(BodyPart part)
        {
            throw new NotImplementedException();
        }

        public BodyPart CoveredBodyParts(EquipmentSide equipmentSide = EquipmentSide.Both)
        {
            throw new NotImplementedException();
        }

        public bool SetSide(EquipmentSide equipmentSide)
        {
            throw new NotImplementedException();
        }

        public bool SwapSide()
        {
            throw new NotImplementedException();
        }
    }

    public class RangedWeapon : ItemManipulator {
        private Container _container;

        /// <inheritdoc />
        public RangedWeapon(BaseItem item) : base(item) {
            _container = new Container(Item);
        }

        public BaseItem CurrentMagazine() => _container.FirstOrDefault(s => s.IsMagazine());

        public IEnumerable<BaseItem> GunMods => _container.Where(s => s.IsGunMod());
        public bool HasGunMod(string gunmod) => GunMods.Any(s => s.TemplateData.Id == gunmod);

        /// <summary>
        /// Return the extra volume of this weapon added by magazine protrusion, collapsible stocks and mods.
        /// </summary>
        /// <returns></returns>
        public Volume ModifierVolume() {
            var currentVolume = Item.BaseVolume();

            // Add the protrusion of the magazine out of the well.
            // That is, some magazine sit flush and some do not, so remove the volume of the magazine from the volume of the weapons magazine well.
            var magazine = CurrentMagazine();
            if (magazine != null) {
                currentVolume += Volume.FromMilliliters(Math.Max((magazine.BaseVolume() - TemplateData.MagazineWell).Milliliters, 0));
            }

            foreach (var mod in GunMods) {
                currentVolume += mod.BaseVolume();
            }

            if (Item.HasFlag("COLLAPSIBLE_STOCK")) {
                // TODO: 'stock_length' for guns
                currentVolume *= 0.3; // For now reduce the volume by 1/3 
            }

            // Small Barrel, reduce by half the stock barrel.
            if (HasGunMod("barrel_small")) {
                currentVolume -= (TemplateData.Gun.BarrelLength / 2);
            }

            return currentVolume;
        }
    }

    public class MeleeWeapon : ItemManipulator {
        /// <inheritdoc />
        public MeleeWeapon(BaseItem item) : base(item) { }
    }

    [Obsolete("Use Container instead")]
    public class ReloadableItem : ItemManipulator
    {
        public bool Reload(Player player, BaseItem reloadItem, int quantity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public ReloadableItem(BaseItem item) : base(item) { }
    }

    /// <summary>
    /// Allows access to the Container/Storage elements of an item and manipulation of it's contents.
    /// 
    /// This roughly covers:
    ///  - Item Containers (i.e, a backpack)
    ///  - Liquid Containers (i.e, a jerry can)
    ///  - Reloadable items (Batteries, ammunition, etc)
    /// And various subsets of the above (gas containers, corpses, etc)
    /// </summary>
    public class Container : ItemManipulator, IEnumerable<BaseItem> {
        public ICollection<BaseItem> Contents { get; }

        /// <inheritdoc />
        public Container(BaseItem item) : base(item) {
            Contents = RuntimeData.Get("storage_contents", new List<BaseItem>());
        }

        /// <summary>
        /// Return the weight of all the items contained in this item-container.
        /// Excluding the weight of the container.
        /// </summary>
        /// <returns></returns>
        public Mass ContentWeight() {
            Mass currMass = Mass.FromGrams(0);
            foreach (var item in Contents) {
                currMass += item.Weight();
            }

            return currMass;
        }

        /// <summary>
        /// Return the volume of contained items.
        /// If the container is RIGID, this returns 0 as container does not expand.
        /// </summary>
        /// <returns></returns>
        public Volume ContentVolume() {
            Volume currVolume = Volume.FromMilliliters(0);

            if (!TemplateData.Rigid) {
                foreach (var item in Contents) {
                    currVolume += item.Volume();
                }
            }

            return currVolume;
        }

        /// <inheritdoc />
        public IEnumerator<BaseItem> GetEnumerator() {
            return Contents.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() {
            return ((IEnumerable) Contents).GetEnumerator();
        }
    }

    public class UsableItem : ItemManipulator {
        public UsableItem(BaseItem item) : base(item) { }

        public void Activate(Player player) {
            if (Item.TemplateData.HasUse()) {
                var uses = Item.TemplateData.UseActions;
            }
        }
    }

    /// <summary>
    /// Allows access to the Tool elements of an item and manipulation of it's qualities
    /// </summary>
    public class ToolItem : ItemManipulator
    {
        /// <inheritdoc />
        public ToolItem(BaseItem item) : base(item) {

        }

        public void Reload(Player player, BaseItem reloadWith, int quantity) {
            var reloadable = As<ReloadableItem>();
            reloadable.Reload(player, reloadWith, quantity);
        }
    }
}