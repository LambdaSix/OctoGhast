using System;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class CommonRangedData {
        public DamageInfo Damage { get; set; }
        public int Range { get; set; } = 0;
        public int Dispersion { get; set; } = 0;

        [Obsolete]
        public int LegacyPierce { get; set; }
        [Obsolete]
        public int LegacyDamage { get; set; }
    }
}