using System.Diagnostics;
using Newtonsoft.Json;

namespace OctoGhast {
    public class AmmoType {
        public string Type { get; }

        public AmmoType(string type) {
            Type = type;
        }

        /// <inheritdoc />
        public override string ToString() {
            return Type;
        }
    }
}