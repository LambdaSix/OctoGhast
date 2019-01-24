using System;
using System.Collections.Generic;
using OctoGhast.Entity;
using OctoGhast.Framework;
using OctoGhast.Units;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class SlotAmmo : CommonRangedData {
        /// <summary>
        /// The type of ammo that fits into this.
        /// </summary>
        [LoaderInfo("ammo_type")]
        public StringID<AmmoType> AmmoType { get;set; }
        
        [LoaderInfo("casing")]
        public StringID<ItemType> Casing { get; set; } = StringID<ItemType>.NullId;

        /// <summary>
        /// Type of item, if any, dropped at ranged target.
        /// </summary>
        [LoaderInfo("drop")]
        public IEnumerable<StringID<ItemType>> Drops { get; set; }

        [LoaderInfo("drop_chance")]
        public double DropChance { get; set; } = 1.0f;

        [LoaderInfo("drop_active")]
        public bool DropActive { get; set; } = true;

        [LoaderInfo("count")]
        public long DefaultCharges { get; set; } = 1;

        /// <summary>
        /// Noise-level of ammunition in decibels, defaults to 140dB, a small .22 caliber rifle
        /// </summary>
        [LoaderInfo("loudness", false, "140dB")]
        public SoundLevel Loudness { get; set; }

        [LoaderInfo("recoil")]
        public int Recoil { get; set; } = -1;

        [LoaderInfo("cooks_off")]
        public bool CooksOff { get; set; } = false;

        [LoaderInfo("special_cook_off")]
        public bool SpecialCookOff { get; set; } = false;

        [LoaderInfo("effects")]
        public IEnumerable<string> AmmoEffects { get; set; }

        [LoaderInfo("damage")][Obsolete]
        public int? LegacyDamage { get; set; }
    }
}