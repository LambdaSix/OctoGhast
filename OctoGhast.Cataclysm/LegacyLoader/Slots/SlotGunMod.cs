using System.Collections.Generic;

namespace OctoGhast.Cataclysm.LegacyLoader {
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
}