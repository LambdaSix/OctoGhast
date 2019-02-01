using OctoGhast.Framework;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class SlotWheel {
        /// <summary>
        /// Diameter of wheel in inches
        /// </summary>
        [LoaderInfo("diameter")]
        public int Diameter { get; set; }

        /// <summary>
        /// Width of wheel in inches
        /// </summary>
        [LoaderInfo("width")]
        public int Width { get; set; }
    }
}