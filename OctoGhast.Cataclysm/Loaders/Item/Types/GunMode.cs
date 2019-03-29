using System.Collections.Generic;
using System.Linq;
using OctoGhast.Cataclysm.Items;

namespace OctoGhast.Cataclysm.Loaders.Item.Types {
    public class GunMode {
        public string Name { get; }

        public BaseItem Target { get; }

        /// <summary>
        /// Burst size for firearms, melee range for melee weapons.
        /// </summary>
        public int Value { get; }

        public IEnumerable<string> Flags { get; }

        public GunMode(string name, BaseItem target, int value, IEnumerable<string> flags) {
            Name = name;
            Target = target;
            Value = value;
            Flags = flags;
        }

        public bool IsMelee() => Flags.Contains("MELEE");
    }
}