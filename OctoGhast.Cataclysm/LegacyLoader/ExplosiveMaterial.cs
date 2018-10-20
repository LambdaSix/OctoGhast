namespace OctoGhast.Cataclysm.LegacyLoader {
    /// <summary>
    /// Defines material properties for an explosive
    /// </summary>
    public class ExplosiveMaterial {
        /// <summary>
        /// Name of the explosive material
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Density in g/cm^3 of the material
        /// </summary>
        public double Density { get; set; }

        /// <summary>
        /// Detonation velocity of the material in m/s
        /// </summary>
        public double DetonationVelocity { get; set; }

        /// <summary>
        /// Relative Effectiveness as compared to TNT.
        /// </summary>
        public double RelativeEffectiveness { get; set; }
    }
}