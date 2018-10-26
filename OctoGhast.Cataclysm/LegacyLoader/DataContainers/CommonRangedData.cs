using System;
using OctoGhast.Framework;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class CommonRangedData {
        [LoaderInfo("ranged_damage", TypeLoader = typeof(DamageInfoTypeLoader))]
        public DamageInfo Damage { get; set; }

        [LoaderInfo("range")]
        public int Range { get; set; }

        [LoaderInfo("dispersion")]
        public int Dispersion { get; set; } = 0;

        [Obsolete]
        [LoaderInfo("pierce")]
        public int? LegacyPierce { get; set; }

        [Obsolete]
        [LoaderInfo("ranged_damage")]
        public int? LegacyDamage { get; set; }
    }
}