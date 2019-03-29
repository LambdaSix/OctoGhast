using System.Collections.Generic;

namespace OctoGhast.Cataclysm.Loaders.Item.DataContainers {
    public class GunModifierData {
        public string Name { get; }
        public int Quantity { get;  }
        public IEnumerable<string> Flags { get; set; }

        public GunModifierData(string name, int quantity, IEnumerable<string> flags) {
            Name = name;
            Quantity = quantity;
            Flags = flags;
        }
    }
}