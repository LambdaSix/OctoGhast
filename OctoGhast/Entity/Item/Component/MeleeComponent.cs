using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using OctoGhast.Extensions;

namespace OctoGhast {
    public class MeleeComponent : ItemComponent {
        public int Cutting { get; set; }
        public int Bashing { get; set; }
        public int ToHit { get; set; }

        public static  MeleeComponent Deserialize(JObject data) {
            return new MeleeComponent()
            {
                Cutting = data.GetValueOr("cutting", 0),
                Bashing = data.GetValueOr("bashing", 0),
                ToHit = data.GetValueOr("to_hit", 0),
            };
        }
    }
}