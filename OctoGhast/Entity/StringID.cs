using System;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class StringID<T> {
        private string Id { get; }

        public StringID(string id, int cid = -1) {
            Id = id;
        }

        public StringID() : this("") {

        }

        public static StringID<T> NullId = new StringID<T>(null);

        public string AsString() => Id;

        public static bool operator <(StringID<T> lhs, StringID<T> rhs) => String.CompareOrdinal(lhs.Id,rhs.Id) < 0;

        public static bool operator >(StringID<T> lhs, StringID<T> rhs) => String.CompareOrdinal(lhs.Id, rhs.Id) > 0;

        public static bool operator ==(StringID<T> lhs, string rhs) => String.CompareOrdinal(lhs.Id, rhs) == 0;
        public static bool operator !=(StringID<T> lhs, string rhs) => !(lhs == rhs);

        public static bool operator ==(StringID<T> lhs, StringID<T> rhs) => String.CompareOrdinal(lhs.Id, rhs.Id) == 0;
        public static bool operator !=(StringID<T> lhs, StringID<T> rhs) => !(lhs == rhs);

        public static implicit operator StringID<T>(string val) => String.IsNullOrWhiteSpace(val) ? NullId : new StringID<T>(val);

        public static implicit operator String(StringID<T> val) => val.AsString();
    }
}