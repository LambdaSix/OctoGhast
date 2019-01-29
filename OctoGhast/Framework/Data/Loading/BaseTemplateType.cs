using System;
using OctoGhast.Framework;

namespace OctoGhast.Cataclysm.LegacyLoader {
    /// <summary>
    /// Skeletal loader structure, not related to <seealso cref="TemplateType"/>
    /// </summary>
    public class BaseTemplateType : IEquatable<BaseTemplateType> {
        public string FileID { get; set; }
        public string PathInfo { get; set; }

        [LoaderInfo("id", true)]
        public string Id { get; }

        [LoaderInfo("abstract", false)]
        public string AbstractId { get; }

        [LoaderInfo("type", true)]
        public string Type { get; }

        public bool IsAbstract { get; set; }

        public BaseTemplateType(string id, string abstractId, string templateType) {
            Id = id;
            AbstractId = abstractId;
            Type = templateType;
        }

        /// <inheritdoc />
        public bool Equals(BaseTemplateType other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Id, other.Id) && string.Equals(Type, other.Type);
        }

        /// <inheritdoc />
        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((BaseTemplateType) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode() {
            unchecked {
                return ((Id != null ? Id.GetHashCode() : 0) * 397) ^ (Type != null ? Type.GetHashCode() : 0);
            }
        }
    }
}