using System;
using Newtonsoft.Json.Linq;
using OctoGhast.Extensions;

namespace OctoGhast {
    public static class BurnableLoader {
        public static BurnData Deserialize(this BurnData self, JObject data) {
            return new BurnData()
            {
                Fuel = data.GetValueOr("fuel", 0),
                Smoke = data.GetValueOr("smoke", 0),
                Burn = data.GetValueOr("burn", 0),
                VolumePerTurn = data.GetValueOr("volume_per_turn", default(string)),
            };
        }

        public static JObject Serialize(this BurnData data) {
            throw new NotImplementedException();
        }
    }
}