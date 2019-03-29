using System;
using System.Collections.Generic;
using OctoGhast.Framework;

namespace OctoGhast.Cataclysm.LegacyLoader {
    /// <summary>
    /// Skeletal loader structure, not related to <seealso cref="TemplateType"/>
    /// </summary>
    public class BaseTemplateType : IEquatable<BaseTemplateType>, IComparable<BaseTemplateType>, IComparable {
        public string FileID { get; set; }
        public string PathInfo { get; set; }

        [LoaderInfo("id", true)]
        public string Id { get; }

        [LoaderInfo("abstract", false)]
        public string AbstractId { get; }

        [LoaderInfo("type", true)]
        public string Type { get; }

        public bool IsAbstract { get; set; }
        public bool IsOverride { get; set; }
        public bool IsCore { get; set; }

        public BaseTemplateType(string id, string abstractId, string templateType) {
            Id = id;
            AbstractId = abstractId;
            Type = templateType;
        }

        public bool Equals(BaseTemplateType other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Id, other.Id) && string.Equals(AbstractId, other.AbstractId) && string.Equals(Type, other.Type);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((BaseTemplateType) obj);
        }

        public override int GetHashCode() {
            unchecked {
                var hashCode = (Id != null ? Id.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (AbstractId != null ? AbstractId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Type != null ? Type.GetHashCode() : 0);
                return hashCode;
            }
        }

        public int CompareTo(BaseTemplateType other) {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            var idComparison = string.Compare(Id, other.Id, StringComparison.Ordinal);
            if (idComparison != 0) return idComparison;
            var abstractIdComparison = string.Compare(AbstractId, other.AbstractId, StringComparison.Ordinal);
            if (abstractIdComparison != 0) return abstractIdComparison;
            return string.Compare(Type, other.Type, StringComparison.Ordinal);
        }

        public int CompareTo(object obj) {
            if (ReferenceEquals(null, obj)) return 1;
            if (ReferenceEquals(this, obj)) return 0;
            return obj is BaseTemplateType other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(BaseTemplateType)}");
        }

        public static bool operator <(BaseTemplateType left, BaseTemplateType right) {
            return Comparer<BaseTemplateType>.Default.Compare(left, right) < 0;
        }

        public static bool operator >(BaseTemplateType left, BaseTemplateType right) {
            return Comparer<BaseTemplateType>.Default.Compare(left, right) > 0;
        }

        public static bool operator <=(BaseTemplateType left, BaseTemplateType right) {
            return Comparer<BaseTemplateType>.Default.Compare(left, right) <= 0;
        }

        public static bool operator >=(BaseTemplateType left, BaseTemplateType right) {
            return Comparer<BaseTemplateType>.Default.Compare(left, right) >= 0;
        }
    }
}