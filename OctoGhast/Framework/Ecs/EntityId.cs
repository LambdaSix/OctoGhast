using System;

namespace OctoGhast.Framework.Ecs {
    /// <summary>
    /// Durable identity for an authoritative world entity. This is deliberately
    /// independent of Capsicum's pool-local, recyclable Entity instance.
    /// </summary>
    public readonly struct EntityId : IEquatable<EntityId>, IComparable<EntityId> {
        public Guid Value { get; }
        public bool IsValid => Value != Guid.Empty;

        public EntityId(Guid value) {
            if (value == Guid.Empty) {
                throw new ArgumentException("An entity ID cannot be empty.", nameof(value));
            }

            Value = value;
        }

        public static EntityId Parse(string value) {
            if (!Guid.TryParseExact(value, "N", out var parsed)) {
                throw new FormatException("Entity IDs must be 32 hexadecimal digits in N format.");
            }

            return new EntityId(parsed);
        }

        public static bool TryParse(string value, out EntityId entityId) {
            entityId = default(EntityId);
            if (!Guid.TryParseExact(value, "N", out var parsed) || parsed == Guid.Empty) {
                return false;
            }

            entityId = new EntityId(parsed);
            return true;
        }

        public int CompareTo(EntityId other) => Value.CompareTo(other.Value);

        public bool Equals(EntityId other) => Value.Equals(other.Value);

        public override bool Equals(object obj) => obj is EntityId other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();

        /// <summary>The durable canonical representation, independent of CLR type names.</summary>
        public override string ToString() => Value.ToString("N");

        public static bool operator ==(EntityId left, EntityId right) => left.Equals(right);
        public static bool operator !=(EntityId left, EntityId right) => !left.Equals(right);
        public static bool operator <(EntityId left, EntityId right) => left.CompareTo(right) < 0;
        public static bool operator >(EntityId left, EntityId right) => left.CompareTo(right) > 0;
    }
}
