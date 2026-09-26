using System;

namespace OctoGhast.Spatial
{
    /// <summary>
    /// A stable key for a bucket in a spatial index. Its coordinate scale is defined by the
    /// index's <see cref="ISpatialCellMapper"/>, not by the core spatial library.
    /// </summary>
    [Serializable]
    public struct SpatialCell : IEquatable<SpatialCell>
    {
        public long X { get; private set; }
        public long Y { get; private set; }
        public long Z { get; private set; }

        public SpatialCell(long x, long y, long z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public bool Equals(SpatialCell other) { return X == other.X && Y == other.Y && Z == other.Z; }
        public override bool Equals(object obj) { return obj is SpatialCell && Equals((SpatialCell)obj); }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = X.GetHashCode();
                hash = (hash * 397) ^ Y.GetHashCode();
                return (hash * 397) ^ Z.GetHashCode();
            }
        }

        public override string ToString() { return string.Format("({0}, {1}, {2})", X, Y, Z); }
        public static bool operator ==(SpatialCell left, SpatialCell right) { return left.Equals(right); }
        public static bool operator !=(SpatialCell left, SpatialCell right) { return !left.Equals(right); }
    }

    /// <summary>Maps logical positions to index buckets. Profiles own grid dimensions and rounding rules.</summary>
    public interface ISpatialCellMapper
    {
        SpatialCell GetCell(WorldPosition position);
    }

}
