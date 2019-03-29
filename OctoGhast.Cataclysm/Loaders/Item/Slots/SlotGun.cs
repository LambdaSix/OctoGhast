using System.Collections.Generic;
using OctoGhast.Cataclysm.Loaders.Item.DataContainers;
using OctoGhast.Cataclysm.Loaders.Item.Types;
using OctoGhast.Entity;
using OctoGhast.Framework;
using OctoGhast.Units;

namespace OctoGhast.Cataclysm.Loaders.Item.Slots {
    public class SlotGun : CommonRangedData {
        [LoaderInfo("skill")]
        public StringID<Skill> SkillUsed { get; set; }

        [LoaderInfo("ammo")]
        public StringID<AmmoType> Ammo { get; set; }

        [LoaderInfo("durability")]
        public int Durability { get; set; }

        [LoaderInfo("integral_magazine_volume")]
        public Volume IntegralMagazineSize { get; set; }

        [LoaderInfo("reload", false, "36 seconds")]
        public TimeDuration ReloadTime { get; set; }

        [LoaderInfo("reload_noise", defaultValue: "click")]
        public string ReloadNoise { get; set; }

        [LoaderInfo("reload_noise_volume")]
        public SoundLevel ReloadNoiseVolume { get; set; }

        [LoaderInfo("sight_dispersion", false, 30)]
        public int SightDispersion { get; set; }

        [LoaderInfo("loudness", false, "0dB")]
        public SoundLevel Loudness { get; set; }

        [LoaderInfo("ups_charges")]
        public int UPSCharges { get; set; }

        [LoaderInfo("barrel_length", false, "0L")]
        public Volume BarrelLength { get; set; }

        [LoaderInfo("ammo_effects")]
        public IEnumerable<string> AmmoEffects { get; set; }

        /// <summary>
        /// Key is the (untranslated) location, value is the number of mods can have installed there.
        /// </summary>
        [LoaderInfo("valid_mod_locations")]
        public Dictionary<string, int> ValidModLocations { get; set; }

        /// <summary>
        /// Built in mods, these mods cannot be removed (IRREMOVABLE)
        /// </summary>
        [LoaderInfo("built_in_mods")]
        public IEnumerable<StringID<ItemType>> IntegralModifications { get; set; }

        /// <summary>
        /// Default mods, these are removable.
        /// </summary>
        [LoaderInfo("default_mods")]
        public IEnumerable<StringID<ItemType>> DefaultMods { get; set; }

        /*
         * "modes": [ [ "DEFAULT", "auto", 8 ], [ "SEMI", "semi-auto", 1 ] ],
         */
        /// <summary>
        /// Firing modes supported by the weapon, should always have at least DEFAULT mode
        /// </summary>
        [LoaderInfo("modes")]
        public Dictionary<string, GunModifierData> ModeModifier { get; set; }

        /// <summary>
        /// Burst size for AUTO mode, legacy field for items not migrated to specify modes
        /// </summary>
        [LoaderInfo("burst")]
        public int Burst { get; set; }

        /// <summary>
        /// How easy is the weapon to control (weight, recoil, etc). If unset, value derived from weapon type.
        /// </summary>
        [LoaderInfo("handling", false, -1)]
        public int Handling { get; set; }

        /// <summary>
        /// Additional recoil applied per shot, before effects of handling are considered.
        /// Useful for adding recoil effect to weapons that otherwise consume no ammunition.
        /// </summary>
        [LoaderInfo("recoil")]
        public int Recoil { get; set; }
    }
}