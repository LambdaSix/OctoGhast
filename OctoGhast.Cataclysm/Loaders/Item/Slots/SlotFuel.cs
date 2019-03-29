using OctoGhast.Framework;
using OctoGhast.Units;

namespace OctoGhast.Cataclysm.Loaders.Item.Slots {
    public class SlotFuel {
        /// <summary>
        /// Energy of the fuel in kJ per charge.
        /// </summary>
        // TODO: Move to MJ/Litre by Material?
        [LoaderInfo("energy")]
        public Energy Energy { get; set; }
    }
}