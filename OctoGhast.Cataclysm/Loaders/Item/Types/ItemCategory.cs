using System;
using System.Collections.Generic;

namespace OctoGhast.Cataclysm.Loaders.Item.Types {
    public class ItemCategory : IComparable<ItemCategory>, IComparable {
        public string Id { get; set; }
        public string Name { get; set; }
        public int SortRank { get; set; }

        /// <inheritdoc />
        public int CompareTo(ItemCategory other) {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            var sortRankComparison = SortRank.CompareTo(other.SortRank);
            if (sortRankComparison != 0) return sortRankComparison;
            var nameComparison = string.Compare(Name, other.Name, StringComparison.Ordinal);
            if (nameComparison != 0) return nameComparison;
            return string.Compare(Id, other.Id, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public int CompareTo(object obj) {
            if (ReferenceEquals(null, obj)) return 1;
            if (ReferenceEquals(this, obj)) return 0;
            return obj is ItemCategory other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(ItemCategory)}");
        }

        public static bool operator <(ItemCategory left, ItemCategory right) {
            return Comparer<ItemCategory>.Default.Compare(left, right) < 0;
        }

        public static bool operator >(ItemCategory left, ItemCategory right) {
            return Comparer<ItemCategory>.Default.Compare(left, right) > 0;
        }

        public static bool operator <=(ItemCategory left, ItemCategory right) {
            return Comparer<ItemCategory>.Default.Compare(left, right) <= 0;
        }

        public static bool operator >=(ItemCategory left, ItemCategory right) {
            return Comparer<ItemCategory>.Default.Compare(left, right) >= 0;
        }
    }
}