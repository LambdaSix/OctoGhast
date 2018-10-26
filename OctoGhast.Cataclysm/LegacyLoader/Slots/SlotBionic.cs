using OctoGhast.Entity;
using OctoGhast.Framework;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class SlotBionic {

        [LoaderInfo("difficulty")]
        public int InstallationDifficulty { get; set; }

        [LoaderInfo("id")]
        public StringID<Bionic> Id { get; set; }

        [LoaderInfo("bionic_id")]
        public StringID<Bionic> BionicId { get; set; }
    }
}