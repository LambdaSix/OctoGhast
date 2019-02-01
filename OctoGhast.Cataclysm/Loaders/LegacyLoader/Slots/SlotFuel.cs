using OctoGhast.Framework;
using OctoGhast.Units;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class SlotFuel {
        /// <summary>
        /// Energy of the fuel in kJ per charge.
        /// </summary>
        // TODO: Move to MJ/Litre by Material?
        [LoaderInfo("energy")]
        public Energy Energy { get; set; }
    }
}