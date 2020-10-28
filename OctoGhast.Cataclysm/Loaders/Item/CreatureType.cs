using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using OctoGhast.Cataclysm.Items;
using OctoGhast.Cataclysm.Loaders.Item;
using OctoGhast.Entity;
using OctoGhast.Framework;
using OctoGhast.Framework.Mobile;
using OctoGhast.Units;

namespace OctoGhast.Cataclysm.Loaders.Creature {
    public static class MobileExtensions {
        public static Container GetInventory(this BaseCreature self) {
            return self.RuntimeData.Get<Container>("inventory", null);
        }

        public static Container GetInventory<T>(this Mobile<T> self) where T : CreatureType
        {
            return self.RuntimeData.Get<Container>("inventory", null);
        }
    }

    public class MobileAttackEffectData {
        [LoaderInfo("id")]
        public string Id { get; set; }

        [LoaderInfo("duration")]
        public TimeDuration Duration { get; set; }

        [LoaderInfo("affect_hit_bp")]
        public bool HitBodyPartOnly { get; set; }

        [LoaderInfo("bp")]
        public StringID<BodyPartInfo> BodyPart { get; set; }

        [LoaderInfo("permanent")]
        public bool IsPermanent { get; set; }

        [LoaderInfo("chance", false, 100)]
        public double Chance { get; set; }

        public MobileAttackEffectData(JObject jsonData) {
            Id = jsonData.ReadProperty(() => Id);
            Duration = jsonData.ReadProperty(() => Duration);
            HitBodyPartOnly = jsonData.ReadProperty(() => HitBodyPartOnly);
            BodyPart = jsonData.ReadProperty(() => BodyPart);
            IsPermanent = jsonData.ReadProperty(() => IsPermanent);
            Chance = jsonData.ReadProperty(() => Chance);
        }

        public BodyPartInfo GetBodyPart() => throw new NotImplementedException("Implement AnatomyManager");

        /// <inheritdoc />
        public MobileAttackEffectData(string id, TimeDuration duration, bool hitBodyPartOnly, StringID<BodyPartInfo> bodyPart, bool isPermanent, double chance) {
            Id = id;
            Duration = duration;
            HitBodyPartOnly = hitBodyPartOnly;
            BodyPart = bodyPart;
            IsPermanent = isPermanent;
            Chance = chance;
        }
    }

    public class SlotMobileStats {
        [LoaderInfo("hp")]
        public int HitPoints { get; set; }

        [LoaderInfo("speed")]
        public int Speed { get; set; }

        [LoaderInfo("aggression")]
        public int Aggression { get; set; }

        [LoaderInfo("morale")]
        public int Morale { get; set; }

        public int VisionDay { get; set; }
        public int VisionNight { get; set; }

        public double Luminance { get; set; }
    }

    public class SlotMobileCombat {
        public int AttackCost { get; set; }
        public int MeleeSkill { get; set; }
        public int MeleeDice { get; set; }
        public int MeleeDiceSides { get; set; }
        public int Dodge { get; set; }

        // TODO: Dictionary<DamageType type, float resistance>()
        public int ArmourBash { get; set; }
        public int ArmourCut { get; set; }
        public int ArmourStab { get; set; }
        public int ArmourAcid { get; set; }
        public int ArmourFire { get; set; }

        public IEnumerable<MobileAttackEffectData> AttackEffects { get; set; }

        public DamageInfo MeleeDamage { get; set; }

        public int MeleeCut { get; set; }

        public int BashSkill { get; set; }

        public IEnumerable<string> AngerTriggers { get; set; }
        public IEnumerable<string> PlacateTriggers { get; set; }
        public IEnumerable<string> FearTriggers { get; set; }
    }

    public class SlotMobilePathing {
        public int MaxDistance { get; set; }
        public int MaxLength { get; set; }
        public int BashStrength { get; set; }
        public bool AllowOpeningDoors { get; set; }
        public bool AllowClimbingStairs { get; set; }
        public bool AvoidTraps { get; set; }
    }

    /// <summary>
    /// Data about what happens when a Mobile dies
    /// </summary>
    public class SlotMobileDeath {

        /// <summary>
        /// Which Death Function to run on dying
        /// </summary>
        public string DeathFunction { get; set; }

        /// <summary>
        /// What to drop when this creature dies.
        /// </summary>
        // TODO: Implement the ItemGroupFactory stuff to hydrate this properly
        public string DeathDrops { get; set; }
    }

    public class SlotMobileButcherData {
        /// <summary>
        /// What harvest group (if any) to use for butchering this creature
        /// </summary>
        public string Harvest { get; set; }
    }

    public class SlotMobileReproduction {
        /// <summary>
        /// Average size of a litter for this creature
        /// </summary>
        [LoaderInfo("baby_count")]
        public int LitterSize { get; set; }

        /// <summary>
        /// How frequently this creature possibly creates a litter
        /// </summary>
        [LoaderInfo("baby_timer")]
        public TimeDuration LitterTimer { get; set; }

        /// <summary>
        /// If this is a creature that has live-births (most mammals) - this is the creature spawned directly.
        /// </summary>
        [LoaderInfo("baby_monster")]
        public StringID<CreatureType> LitterType { get; set; }

        /// <summary>
        /// If this is a hatching species, the kind of item (not specifically an egg) that is spawned that will
        /// later turn into the creature defined in that item.
        /// </summary>
        [LoaderInfo("baby_egg")]
        public StringID<ItemType> LitterEggType { get; set; }

        [LoaderInfo("baby_flags")]
        public IEnumerable<string> LitterFlags { get; set; }
    }

    public class SlotMobileBiosignature {
        [LoaderInfo("biosig_timer")]
        public TimeDuration Timer { get; set; }

        public StringID<ItemType> Item { get; set; }
    }

    /// <summary>
    /// Data about how a mobile changes/evolves/upgrades over time.
    /// This covers difficulty curves and also baby creatures growing up.
    /// </summary>
    public class SlotMobileEvolution {
        [LoaderInfo("half_life")]
        public int HalfLife { get; set; }

        [LoaderInfo("age_grow")]
        public int AgeGrow { get; set; }

        [LoaderInfo("into_group")]
        public string IntoGroup { get; set; }

        [LoaderInfo("into")]
        public StringID<CreatureType> IntoCreature { get; set; }
    }

    public class CreatureType : CreatureData {
        public SlotMobileStats Stats { get; set; }
        public SlotMobileCombat Combat { get; set; }
        public SlotMobileDeath Death { get; set; }
        public SlotMobileButcherData ButcherData { get; set; }
        public SlotMobileBiosignature BiosignatureData { get; set; }
        public SlotMobileReproduction ReproductionData { get; set; }
        public SlotMobileEvolution UpgradeData { get; set; }

        public SlotMobilePathing PathingData { get; set; }

        public string Name { get; set; }
        public string NamePlural { get; set; }

        public string Description { get; set; }

        public string Phase { get; set; }

        public IEnumerable<string> Materials { get; set; }

        public IEnumerable<string> Species { get; set; }

        public IEnumerable<string> Categories { get; set; }

        public string DefaultFaction { get; set; }

        /// <summary>
        /// What the creature leaves behind if burnt.
        /// </summary>
        [LoaderInfo("burn_into")]
        public StringID<ItemType> BurnsInto { get; set; }

        public string Symbol { get; set; }

        public string LooksLike { get; set; }

        public string Color { get; set; }

        public string RevertToItem { get; set; }

        [Obsolete("Convert to using Volume/Weight markers")]
        public string MonsterSize { get; set; }
        public Volume Volume { get; set; }
        public Mass Weight { get; set; }

        public IEnumerable<string> Flags { get; set; }
    }
}