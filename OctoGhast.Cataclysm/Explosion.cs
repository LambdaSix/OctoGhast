using System;
using System.Collections.Generic;
using System.Numerics;
using InfiniMap;
using OctoGhast.Cataclysm.Loaders.Item.DataContainers;
using OctoGhast.Cataclysm.Loaders.Item.Types;
using OctoGhast.Spatial;
using OctoGhast.Units;
using static OctoGhast.Translation.Translation;

namespace OctoGhast.Cataclysm {
    public class FragmentCloud : IComparable<FragmentCloud>, IComparable {
        /// <summary>
        /// Velocity in m/s
        /// </summary>
        public double Velocity { get; set; }

        /// <summary>
        /// Density in fragments/m^3
        /// </summary>
        public double Density { get; set; }

        public FragmentCloud(double velocity, double density) {
            Density = density;
            Velocity = velocity;
        }

        /// <summary>
        /// Minimum velocity resulting in skin perforation according to https://www.ncbi.nlm.nih.gov/pubmed/7304523
        /// </summary>
        public const double MinEffectiveVelocity = 70.0f;
        /// <summary>
        /// Pretty arbitrary minimum density.  1/1,000 change of a fragment passing through the given square.
        /// </summary>
        public const double MinFragmentDensity = 0.0001;

        public static bool ShrapnelCheck(FragmentCloud cloud) {
            return cloud.Density > 0.0f 
                   && cloud.Velocity > MinEffectiveVelocity && cloud.Density > MinFragmentDensity;
        }

        public static FragmentCloud AccumulateCloud(FragmentCloud originCloud, int distance) {
            // Velocity is the cumulative and continuous decay of speed, so accumulate the same way as light attenuation
            var velocity = ((distance - 1) * originCloud.Velocity) / distance;
            // Density is the accumulation of discrete attenuation events, each term is added to the series via multiplication.
            var density = originCloud.Density / distance;

            return new FragmentCloud(velocity, density);
        }

        /// <inheritdoc />
        public int CompareTo(FragmentCloud other) {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            var velocityComparison = Velocity.CompareTo(other.Velocity);
            if (velocityComparison != 0) return velocityComparison;
            return Density.CompareTo(other.Density);
        }

        /// <inheritdoc />
        public int CompareTo(object obj) {
            if (ReferenceEquals(null, obj)) return 1;
            if (ReferenceEquals(this, obj)) return 0;
            return obj is FragmentCloud other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(FragmentCloud)}");
        }

        public static bool operator <(FragmentCloud left, FragmentCloud right) {
            return Comparer<FragmentCloud>.Default.Compare(left, right) < 0;
        }

        public static bool operator >(FragmentCloud left, FragmentCloud right) {
            return Comparer<FragmentCloud>.Default.Compare(left, right) > 0;
        }

        public static bool operator <=(FragmentCloud left, FragmentCloud right) {
            return Comparer<FragmentCloud>.Default.Compare(left, right) <= 0;
        }

        public static bool operator >=(FragmentCloud left, FragmentCloud right) {
            return Comparer<FragmentCloud>.Default.Compare(left, right) >= 0;
        }

        public static bool operator ==(FragmentCloud left, FragmentCloud right) {
            return Comparer<FragmentCloud>.Default.Compare(left, right) == 0;
        }

        public static bool operator !=(FragmentCloud left, FragmentCloud right) {
            return !(left == right);
        }

        protected bool Equals(FragmentCloud other)
        {
            return Velocity.Equals(other.Velocity) && Density.Equals(other.Density);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FragmentCloud)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Velocity.GetHashCode() * 397) ^ Density.GetHashCode();
            }
        }
    }

    public interface IExplosion {
        void DoExplosion();
        Map2D<FragmentCloud> CalculateFragments(WorldSpace2D origin);
    }

    public class Explosion : IExplosion {
        private readonly ExplosiveData _data;

        public Explosion(ExplosiveData data) {
            _data = data;
        }

        public void DoExplosion() {
            EmitNoise();
        }

        public Map2D<FragmentCloud> CalculateFragments(WorldSpace2D origin) {
            if (_data.Shrapnel is null)
                throw new Exception($"Unable to compute fragmentation data for explosive, missing fragmentation data");

            float fragmentV = GurneySpherical(_data.ExplosiveMaterial); // Velocity
            var fragmentM = _data.Shrapnel.FragmentMass; // Mass
            var fragmentDensity = _data.Shrapnel.CasingMass.Grams / fragmentM.Grams; // Density == CasingMass / FragmentMass

            var cloudMap = new Map2D<FragmentCloud>();
            var initialCloud = new FragmentCloud(fragmentV, fragmentDensity);
            cloudMap[origin] = initialCloud;

            var originV = new Vector3(origin.X, origin.Y, 0);

            double distance(Vector3 v1, Vector3 v2) {
                double lengthX = Math.Abs(v1.X - v2.X);
                double lengthY = Math.Abs(v1.X - v2.X);
                return Math.Sqrt((lengthX * lengthX) + (lengthY + lengthY));
            }

            // Generate a point cloud of V/d values.
            // When (V < MinEffectiveVelocity || d < MinFragmentDensity) stop
            ShadowCaster.ComputeFieldOfViewWithShadowCasting(0, 0, 32,
                (x, y) => true, 
                (x, y) => cloudMap[x, y] = FragmentCloud.AccumulateCloud(initialCloud, (int) distance(new Vector3(x, y, 0), originV)));

            return cloudMap;
        }

        private void EmitNoise() {
            var noiseLevel = GetNoiseLevel(AsTnt(_data.Mass, _data.ExplosiveMaterial.RelativeEffectiveness), 1.0);
            throw new NotImplementedException("Implement Sound emitting");
        }

        /// <summary>
        /// Calculate the noise level for an explosion of a specified <paramref name="tntMass"/> of TNT at a <paramref name="distance"/> in meters.
        /// </summary>
        /// <param name="tntMass">Mass of explosive material as TNT</param>
        /// <param name="distance">Distance from epicenter</param>
        /// <returns></returns>
        private SoundLevel GetNoiseLevel(Mass tntMass, double distance, bool airBurst = false) {
            var overPressure = airBurst
                ? ExplosionOverpressureGroundLevel(tntMass, distance)
                : ExplosionOverpressureAirburst(tntMass, distance);

            return SoundLevel.FromPascals(overPressure.Pascals);
        }

        /// <summary>
        /// Given a weight and relative effectiveness of a given explosive, convert it as if it was
        /// actually TNT.
        /// </summary>
        /// <param name="weight">Weight of the alternative explosive material to convert</param>
        /// <param name="relativeEffectiveness">The R.E value of the alternative explosive material</param>
        /// <returns>The equivalent weight of TNT to match the explosive power of the supplied material</returns>
        public static Mass AsTnt(Mass weight, double relativeEffectiveness) {
            var relative = 1.0 / relativeEffectiveness;
            var mass = (int) Math.Round(weight.Grams / relative, MidpointRounding.ToEven);
            return Mass.FromGrams(mass);
        }

        /// <summary>
        /// Given a <paramref name="mass"/> of TNT, and a distance from epicenter, return the Overpressure at that location in atmospheres.
        /// </summary>
        /// <param name="mass">Mass of TNT to detonate in kg</param>
        /// <param name="distance">Distance from detonation</param>
        /// <returns></returns>
        public static Pressure ExplosionOverpressureGroundLevel(Mass mass, double distance) {
            const double c0 = 0.95;
            const double c1 = 3.9;
            const double c2 = 13.0;

            return ExplosionOverPressure(mass.Kilograms, distance, c0, c1, c2);
        }

        /// <summary>
        /// Velocity of casing material in m/s
        /// </summary>
        /// <param name="material"></param>
        /// <returns></returns>
        private static float GurneySpherical(ExplosiveMaterial material)
        {
            // Use an approximation here of GurneyConstant = 1/3 of explosive detonation velocity
            return (float)material.DetonationVelocity / 3.0f;
        }

        /// <summary>
        /// Calculate the cross-sectional area of a steel sphere in cm^2 based on the mass
        /// </summary>
        /// <param name="fragmentMass"></param>
        /// <returns></returns>
        private static double MassToArea(Mass fragmentMass)
        {
            var fragmentVolume = fragmentMass.Volume(7.85f); // Convert the mass to a volume assuming it's made from steel.
            var radius = MathExt.CubeRoot(fragmentVolume.Milliliters * 3.0) / (4.0 * Math.PI);
            return radius * radius * Math.PI;
        }

        /// <summary>
        /// Given a <paramref name="mass"/> of TNT, and a distance from epicenter, return the Overpressure at that location in atmospheres.
        /// </summary>
        /// <param name="mass">Mass of TNT to detonate</param>
        /// <param name="distance">Distance from detonation</param>
        /// <returns></returns>
        public static Pressure ExplosionOverpressureAirburst(Mass mass, double distance)
        {
            const float c0 = 0.84f;
            const float c1 = 2.7f;
            const float c2 = 7.0f;
            return ExplosionOverPressure(mass.Kilograms, distance, c0, c1, c2);
        }

        public static Pressure ExplosionOverPressure(double mass, double distance, double c0, double c1, double c2) {
            var oP = (c0 * (MathExt.CubeRoot(mass) / distance)) + (c1 * (MathExt.CubeRoot(Math.Pow(mass, 2)) / Math.Pow(distance, 2))) + (c2 * (mass / Math.Pow(distance, 3)));
            return Pressure.FromAtmospheres(oP);
        }
    }

    public static class MathExt {
        public static double CubeRoot(double value) => System.Math.Pow(value, (1.0 / 3.0));
    }
}