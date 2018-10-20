using System.Collections.Generic;
using OctoGhast.Units;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class SlotGun : CommonRangedData {
        public StringID<Skill> SkillUsed { get; set; } = StringID<Skill>.NullId;
        public StringID<AmmoType> Ammo { get; set; } = StringID<AmmoType>.NullId;
        public int Durability { get; set; } = 0;
        public int IntegralMagazineSize { get; set; } = 0;
        public TimeDuration ReloadTime { get; set; } = new TimeDuration();
        public string ReloadNoise { get; set; } = Translation.Translation._($"click.");
        public SoundLevel ReloadNoiseVolume { get; set; } = "0dB";
        public int SightDispersion { get; set; } = 30;
        public SoundLevel Loudness { get; set; } = "0dB";
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
}