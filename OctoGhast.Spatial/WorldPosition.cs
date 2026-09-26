using System;

namespace OctoGhast.Spatial
{
    /// <summary>
    /// An absolute position in a world. Coordinates use deterministic decimal values; they are
    /// logical world units, not presentation pixels, and this type imposes no tile or map size.
    /// </summary>
    [Serializable]
    public struct WorldPosition : IEquatable<WorldPosition>
    {
        public decimal X { get; private set; }
        public decimal Y { get; private set; }
        public decimal Z { get; private set; }

        public WorldPosition(decimal x, decimal y, decimal z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public bool Equals(WorldPosition other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public override bool Equals(object obj)
        {
            return obj is WorldPosition && Equals((WorldPosition)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = X.GetHashCode();
                hash = (hash * 397) ^ Y.GetHashCode();
                return (hash * 397) ^ Z.GetHashCode();
            }
        }

        public override string ToString()
        {
            return string.Format("({0}, {1}, {2})", X, Y, Z);
        }

        public static bool operator ==(WorldPosition left, WorldPosition right) { return left.Equals(right); }
        public static bool operator !=(WorldPosition left, WorldPosition right) { return !left.Equals(right); }
    }
}
