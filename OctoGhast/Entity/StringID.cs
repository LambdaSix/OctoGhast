using System;

namespace OctoGhast.Entity {
    public class StringID<T> : IComparable<StringID<T>>, IComparable, IEquatable<StringID<T>> {
        private string Id { get; }

        public StringID(string id, int cid = -1) {            
            Id = id;
        }

        public StringID() : this("") {

        }

        public static StringID<T> NullId = new StringID<T>(null);

        public string AsString() => Id;

        public string AsQualifiedString() => $"{Type().Name}::{Id}";

        public Type Type() => typeof(T);

        public static bool operator <(StringID<T> lhs, StringID<T> rhs) => String.CompareOrdinal(lhs.Id,rhs.Id) < 0;

        public static bool operator >(StringID<T> lhs, StringID<T> rhs) => String.CompareOrdinal(lhs.Id, rhs.Id) > 0;

        public static bool operator ==(StringID<T> lhs, StringID<T> rhs) => String.CompareOrdinal(lhs.Id, rhs.Id) == 0;
        public static bool operator !=(StringID<T> lhs, StringID<T> rhs) => !(lhs == rhs);

        public static implicit operator StringID<T>(string val) => String.IsNullOrWhiteSpace(val) ? NullId : new StringID<T>(val);

        public static implicit operator String(StringID<T> val) => val.AsString();

        /// <inheritdoc />
        public int CompareTo(StringID<T> other) {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return string.Compare(Id, other.Id, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public int CompareTo(object obj) {
            if (ReferenceEquals(null, obj)) return 1;
            if (ReferenceEquals(this, obj)) return 0;

            return obj is StringID<T> other
                ? CompareTo(other)
                : throw new ArgumentException($"Object must be of type {nameof(StringID<T>)}");
        }

        /// <inheritdoc />
        public bool Equals(StringID<T> other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Id, other.Id);
        }

        /// <inheritdoc />
        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((StringID<T>) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode() {
            return (Id != null ? Id.GetHashCode() : 0);
        }
    }
}