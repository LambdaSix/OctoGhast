using System.Collections.Generic;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class SlotEngine {
        /// <summary>
        /// For combustion engines, the displacement in Cubic Centimeters
        /// </summary>
        public int Displacement { get; set; } = 0;

        public IEnumerable<string> Faults { get; set; }
    }
}