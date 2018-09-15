using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using OctoGhast.Extensions;

namespace OctoGhast {
    public class ArmorComponent : ItemComponent {
        public IEnumerable<string> Covers { get; set; }
        public int Coverage { get; set; }
        public int MaterialThickness { get; set; }
        public int Encumbrance { get; set; }
        public int Warmth { get; set; }
        public int EnvironmentalProtection { get; set; }

        public static ArmorComponent Deserialize(JObject data) {
            return new ArmorComponent()
            {
                Covers = data.GetArray<string>("covers"),
                Coverage = data.GetValueOr("coverage", 0),
                MaterialThickness = data.GetValueOr("material_thickness", 0),
                Encumbrance = data.GetValueOr("encumbrance", 0),
            };
        }
    }
}