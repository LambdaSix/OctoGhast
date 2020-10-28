using OctoGhast.Cataclysm.Loaders.Item;

namespace OctoGhast.Cataclysm.Loaders.WorldOptions {
    [ServiceData(nameof(WorldOptionsService), "Provides configuration and retrieval of world options")]
    public class WorldOptionsService {
        public CorpseData Corpses { get; set; }
    }

    public class CorpseData {
        // TODO: JSON TypeLoader
        public TimeDuration ResurrectionDelay { get; set; } = TimeDuration.FromDays(3);
        public bool ResurrectionEnabled { get; set; } = true;
    }
}