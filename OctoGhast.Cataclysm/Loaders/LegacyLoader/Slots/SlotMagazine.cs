using OctoGhast.Entity;
using OctoGhast.Framework;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class SlotMagazine {
        [LoaderInfo("ammo_type")]
        public StringID<AmmoType> AmmoType { get; set; }

        [LoaderInfo("capacity")]
        public int Capacity { get; set; }

        [LoaderInfo("count")]
        public int DefaultCount { get; set; }

        [LoaderInfo("default_ammo")]
        public StringID<ItemType> DefaultAmmo { get; set; }

        [LoaderInfo("reliability")]
        public int Reliability { get; set; }

        /// <summary>
        /// How long it takes to load each unit of ammunition into the magazine.
        /// Defaults to 36 seconds (6 turns)
        /// </summary>
        [LoaderInfo("reload_time", defaultValue: "36 seconds")]
        public TimeDuration ReloadTime { get; set; }

        /// <summary>
        /// For ammo-belts, one linkage of given type is dropped for each unit of ammunition consumed
        /// </summary>
        [LoaderInfo("linkage")]
        public StringID<ItemType> Linkage { get; set; }

        /// <summary>
        /// Will the magazine protect any contents if affected by fire?
        /// </summary>
        public bool ProtectContents { get; set; }
    }
}