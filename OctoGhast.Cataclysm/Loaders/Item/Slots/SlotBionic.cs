using OctoGhast.Cataclysm.Loaders.Item.Types;
using OctoGhast.Entity;
using OctoGhast.Framework;

namespace OctoGhast.Cataclysm.Loaders.Item.Slots {
    public class SlotBionic {

        [LoaderInfo("difficulty")]
        public int InstallationDifficulty { get; set; }

        [LoaderInfo("id")]
        public StringID<Bionic> Id { get; set; }

        [LoaderInfo("bionic_id")]
        public StringID<Bionic> BionicId { get; set; }
    }
}