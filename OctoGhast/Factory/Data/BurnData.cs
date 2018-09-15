using Newtonsoft.Json.Linq;
using OctoGhast.Extensions;

namespace OctoGhast {
    public class BurnData {
        public int Fuel { get; set; }
        public int Smoke { get; set; }
        public int Burn { get; set; }
        public string VolumePerTurn { get; set; }

        public BurnData() {
            
        }

        public BurnData(bool isFlammable) {
            if (isFlammable)
                Burn = 1;
        }
    }
}