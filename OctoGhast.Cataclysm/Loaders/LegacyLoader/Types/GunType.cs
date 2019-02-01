using System;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class GunType : IComparable<GunType>, IComparable {
        private string Name { get; set; }

        public GunType(string name) {
            Name = name;
        }

        public static bool operator ==(GunType rhs, GunType lhs) => String.CompareOrdinal(lhs.Name, rhs.Name) == 0;
        public static bool operator !=(GunType rhs, GunType lhs) => !(rhs == lhs);

        public static bool operator <(GunType rhs, GunType lhs) => String.CompareOrdinal(lhs.Name, rhs.Name) < 0;
        public static bool operator >(GunType rhs, GunType lhs) => String.CompareOrdinal(lhs.Name, rhs.Name) > 0;

        /// <inheritdoc />
        public int CompareTo(GunType other) {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return string.Compare(Name, other.Name, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public int CompareTo(object obj) {
            if (ReferenceEquals(null, obj)) return 1;
            if (ReferenceEquals(this, obj)) return 0;
            return obj is GunType other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(GunType)}");
        }
    }
}