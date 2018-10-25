using System.Collections.Generic;
using OctoGhast.Framework;
using OctoGhast.Units;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class SlotEngine {
        /// <summary>
        /// For combustion engines, the displacement in Cubic Centimeters
        /// </summary>
        [LoaderInfo("displacement", false, "0L")]
        public Volume Displacement { get; set; }

        [LoaderInfo("faults")]
        public IEnumerable<string> Faults { get; set; }
    }
}