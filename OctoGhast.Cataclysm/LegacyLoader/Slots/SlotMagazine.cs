using OctoGhast.Entity;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class SlotMagazine {
        public StringID<AmmoType> Type { get; set; } = StringID<AmmoType>.NullId;
        public int Capacity { get; set; } = 0;
        public int DefaultCount { get; set; }
        public StringID<ItemType> DefaultAmmo { get; set; } = StringID<ItemType>.NullId;
        public int Reliability { get; set; } = 0;

        /// <summary>
        /// How long it takes to load each unit of ammunition into the magazine.
        /// Defaults to 36 seconds (6 turns)
        /// </summary>
        public TimeDuration ReloadTime { get; set; } = TimeDuration.FromSeconds(36);

        /// <summary>
        /// For ammo-belts, one linkage of given type is dropped for each unit of ammunition consumed
        /// </summary>
        public StringID<ItemType> Linkage { get; set; } = StringID<ItemType>.NullId;

        /// <summary>
        /// Will the magazine protect any contents if affected by fire?
        /// </summary>
        public bool ProtectContents { get; set; } = false;
    }
}