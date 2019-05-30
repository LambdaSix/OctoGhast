using System.Collections.Generic;
using OctoGhast.Framework;

namespace OctoGhast.Cataclysm.Loaders.Terrain {
    public class HarvestableData {

        /// <summary>
        /// Determines what should replace this after the harvest action is completed.
        /// </summary>
        [LoaderInfo("transforms_into")]
        public string HarvestedResult { get; set; }

        [LoaderInfo("seasons", TypeLoader = typeof(SeasonHarvestDataTypeLoader))]
        public IEnumerable<SeasonHarvestData> Seasons { get; set; }
    }
}