using System;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class GunType {
        private string Name { get; set; }

        public GunType(string name) {
            Name = name;
        }

        public static bool operator ==(GunType rhs, GunType lhs) => String.CompareOrdinal(lhs.Name, rhs.Name) == 0;
        public static bool operator !=(GunType rhs, GunType lhs) => !(rhs == lhs);

        public static bool operator <(GunType rhs, GunType lhs) => String.CompareOrdinal(lhs.Name, rhs.Name) < 0;
        public static bool operator >(GunType rhs, GunType lhs) => String.CompareOrdinal(lhs.Name, rhs.Name) > 0;
    }
}