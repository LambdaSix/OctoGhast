using System.Collections.Generic;
using OctoGhast.Entity;
using OctoGhast.Units;

namespace OctoGhast.Cataclysm.LegacyLoader {
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
        public SoundLevel Loudness { get; set; } = "-1dB";
        public int Recoil { get; set; } = -1;
        public bool CooksOff { get; set; } = false;
        public bool SpecialCookOff { get; set; } = false;
    }
}