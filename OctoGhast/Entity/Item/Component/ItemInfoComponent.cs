using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using OctoGhast.Extensions;

namespace OctoGhast {
    /// <summary>
    /// Extended information about a particular item
    /// </summary>
    public class ItemInfoComponent : ItemComponent {
        public int DamageMin = -1000;
        public int DamageMax = 4000;

        public string Name { get; set; }
        public string Description { get; set; }
        public string Symbol { get; set; }
        public string Color { get; set; }
        public string Category { get; set; }
        public int Weight { get; set; }
        public int Volume { get; set; }

        /// <inheritdoc />
        public static ItemInfoComponent Deserialize(JObject data) {
            return new ItemInfoComponent()
            {
                Name = data.GetValueOr("name", default(string)),
                Description = data.GetValueOr("description", default(string)),
                Symbol = data.GetValueOr("symbol", default(string)),
                Color = data.GetValueOr("color", default(string)),
                Category = data.GetValueOr("category", default(string)),
                Weight = data.GetValueOr("weight", 0),
                Volume = data.GetValueOr("volume", 0),
            };
        }
    }
}