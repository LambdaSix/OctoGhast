using System.Collections.Generic;
using OctoGhast.Framework;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class SlotGunMod : SlotGun {
        
        [LoaderInfo("location")]
        public GunModLocation Location { get; set; }

        [LoaderInfo("mod_targets")]
        public IEnumerable<GunType> ModTargets { get; set; }

        /// <summary>
        /// If this value is set, this gunmod functions as a sight.
        /// A sight is only usable to aim by a character whose current Character::recoil is at or below this value.
        /// </summary>
        [LoaderInfo("sight_dispersion")]
        public new int? SightDispersion { get; set; } = default;

        /// <summary>
        /// For Sights, this value affects time cost of aiming.
        /// Higher is better, in case of multiple usable sighs, the one with the highest aim speed is used.
        /// </summary>
        [LoaderInfo("aim_speed")]
        public int? AimSpeed { get; set; }

        [LoaderInfo("loudness_modifier")]
        public new int Loudness { get; set; }

        [LoaderInfo("install_time")]
        public TimeDuration InstallationTime { get; set; }

        /// <summary>
        /// Increase base gun UPS consumption by this amount per shot.
        /// </summary>
        [LoaderInfo("ups_charges")]
        public new int UPSCharges { get; set; }

        /// <summary>
        /// Firing modes added to or replacing those of the base gun.
        /// </summary>
        [LoaderInfo("mode_modifier")]
        public new Dictionary<GunMode, GunModifierData> ModeModifier { get; set; }

        [LoaderInfo("ammo_effects")]
        public IEnumerable<string> AmmoEffects { get; set; }

        [LoaderInfo("handling_modifier")]
        public new int Handling { get; set; } = 0;
    }
}