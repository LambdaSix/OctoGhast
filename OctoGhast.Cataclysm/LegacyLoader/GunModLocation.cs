using System;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class GunModLocation {
        public string Id { get; }

        public GunModLocation(string id) {
            Id = id;
        }

        public static bool operator ==(GunModLocation lhs, GunModLocation rhs) =>
            String.CompareOrdinal(lhs.Id, rhs.Id) == 0;
        public static bool operator !=(GunModLocation lhs, GunModLocation rhs) => !(lhs == rhs);

        public static bool operator >(GunModLocation lhs, GunModLocation rhs) =>
            String.CompareOrdinal(lhs.Id, rhs.Id) > 1;

        public static bool operator <(GunModLocation lhs, GunModLocation rhs) =>
            String.CompareOrdinal(lhs.Id, rhs.Id) < 0;
    }
}