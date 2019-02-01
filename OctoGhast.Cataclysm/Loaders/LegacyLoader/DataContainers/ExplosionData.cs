using System;
using OctoGhast.Framework;

namespace OctoGhast.Cataclysm.LegacyLoader {
    [Obsolete("Legacy JSON - Use ExplosiveData instead")]
    public class ExplosionData {

        /// <summary>
        /// Measure of explosive power in Grams Of TNT equivalent. 
        /// </summary>
        [LoaderInfo("power", true)]
        public double Power { get; set; } = -1.0f;

        /// <summary>
        /// Power retained per traveled tile of explosion. 0..1
        /// </summary>
        [LoaderInfo("distance_factor", false, 0.8)]
        public double DistanceFactor { get; set; } = 0.8f;

        /// <summary>
        /// Is this explosion fire based?
        /// </summary>
        [LoaderInfo("fire", false, false)]
        public bool Incendiary { get; set; } = false;

        [LoaderInfo("shrapnel", TypeLoader = typeof(ShrapnelDataTypeLoader))]
        public ShrapnelData Shrapnel { get; set; }

        /// <summary>
        /// The distance at which we have the <paramref name="ratio"/> of initial power.
        /// </summary>
        public double ExpectedRange(double ratio) {
            if (Power <= 0.0f || DistanceFactor >= 1.0f || DistanceFactor <= 0.0f)
                return 0.0f;

            return Math.Log(ratio) / Math.Log(DistanceFactor / 1.1f);
        }

        public double PowerAtDistance(double distance) {
            if (Power <= 0.0f || DistanceFactor >= 1.0f || DistanceFactor <= 0.0f)
                return 0.0f;

            return Power * Math.Pow(DistanceFactor / 1.0f, distance);
        }

        public int SafeRange() {
            var ratio = 1 / Power / 2;
            return (int) (ExpectedRange(ratio) + 1);
        }
    }
}