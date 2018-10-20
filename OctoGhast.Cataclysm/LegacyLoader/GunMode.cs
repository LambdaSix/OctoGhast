using System.Collections.Generic;
using System.Linq;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class GunMode {
        public string Name { get; }

        public Item Target { get; }

        /// <summary>
        /// Burst size for firearms, melee range for melee weapons.
        /// </summary>
        public int Value { get; }

        public IEnumerable<string> Flags { get; }

        public GunMode(string name, Item target, int value, IEnumerable<string> flags) {
            Name = name;
            Target = target;
            Value = value;
            Flags = flags;
        }

        public bool IsMelee() => Flags.Contains("MELEE");
    }
}