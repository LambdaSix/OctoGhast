using System.Collections.Generic;

namespace OctoGhast.Cataclysm.Loaders.Terrain {
    public class SeasonHarvestData
    {
        public IEnumerable<string> Seasons { get; set; }

        /// <summary>
        /// Collection of harvest entries for this season.
        /// </summary>
        public IEnumerable<HarvestEntry> Entries { get; set; }
    }
}