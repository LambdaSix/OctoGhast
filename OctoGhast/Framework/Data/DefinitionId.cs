using System;

namespace OctoGhast.Framework.Data {
    /// <summary>
    /// Stable typed identity for immutable content. The domain prevents IDs
    /// from unrelated definition registries being accidentally interchanged.
    /// </summary>
    public readonly struct DefinitionId : IEquatable<DefinitionId>, IComparable<DefinitionId> {
        public string Domain { get; }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Domain) && !string.IsNullOrWhiteSpace(Value);

        public DefinitionId(string domain, string value) {
            ValidatePart(domain, nameof(domain));
            ValidatePart(value, nameof(value));
            Domain = domain;
            Value = value;
        }

        public static DefinitionId Parse(string qualifiedId) {
            if (qualifiedId == null) throw new ArgumentNullException(nameof(qualifiedId));
            var separator = qualifiedId.IndexOf("::", StringComparison.Ordinal);
            if (separator <= 0 || separator + 2 >= qualifiedId.Length ||
                qualifiedId.IndexOf("::", separator + 2, StringComparison.Ordinal) >= 0) {
                throw new FormatException("Definition IDs must have the form 'domain::value'.");
            }

            return new DefinitionId(qualifiedId.Substring(0, separator), qualifiedId.Substring(separator + 2));
        }

        public int CompareTo(DefinitionId other) {
            var domain = string.Compare(Domain, other.Domain, StringComparison.Ordinal);
            return domain != 0 ? domain : string.Compare(Value, other.Value, StringComparison.Ordinal);
        }

        public bool Equals(DefinitionId other) =>
            string.Equals(Domain, other.Domain, StringComparison.Ordinal) &&
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is DefinitionId other && Equals(other);

        public override int GetHashCode() {
            unchecked {
                return ((Domain != null ? StringComparer.Ordinal.GetHashCode(Domain) : 0) * 397) ^
                       (Value != null ? StringComparer.Ordinal.GetHashCode(Value) : 0);
            }
        }

        public override string ToString() => IsValid ? Domain + "::" + Value : string.Empty;

        public static bool operator ==(DefinitionId left, DefinitionId right) => left.Equals(right);
        public static bool operator !=(DefinitionId left, DefinitionId right) => !left.Equals(right);
        public static bool operator <(DefinitionId left, DefinitionId right) => left.CompareTo(right) < 0;
        public static bool operator >(DefinitionId left, DefinitionId right) => left.CompareTo(right) > 0;

        private static void ValidatePart(string value, string parameterName) {
            if (string.IsNullOrWhiteSpace(value)) {
                throw new ArgumentException("Definition ID parts cannot be empty or whitespace.", parameterName);
            }
            if (value.IndexOf("::", StringComparison.Ordinal) >= 0) {
                throw new ArgumentException("Definition ID parts cannot contain the '::' separator.", parameterName);
            }
        }
    }
}
