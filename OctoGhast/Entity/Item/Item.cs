using System;
using System.Collections.Generic;

namespace OctoGhast {
    public class Item {
        /// <summary>
        /// Global unique ID for this item
        /// </summary>
        public long ID { get; set; }

        public DataItemTemplate Template { get; }
        public Item(DataItemTemplate template) {
            Template = template;
        }

        public Item() {
            
        }

        public int Damage { get; set; }

        private Dictionary<string, Dictionary<string, string>> Tags { get; } = new Dictionary<string, Dictionary<string, string>>();

        /// <summary>
        /// Add a tag with no associated data
        /// </summary>
        /// <param name="tagName"></param>
        public void AddTag(string tagName) {
            if (Tags.ContainsKey(tagName)) {
                throw new Exception("Duplicate tag added");
            }

            Tags.Add(tagName, default);
        }

        /// <summary>
        /// Add data to a tag, fi the tag does not exist, it will be created and set
        /// with the initial data.
        /// </summary>
        public void AddTagData(string tagName, string dataKey, string dataValue) {
            if (Tags.ContainsKey(tagName)) {
                Tags[tagName].Add(dataKey, dataValue);
            }
            else {
                Tags.Add(tagName, new Dictionary<string, string>
                {
                    [dataKey] = dataValue
                });
            }
        }

        /// <summary>
        /// Checks if this Item has the requested tag, regardless of any data within
        /// </summary>
        public bool HasTag(string tagName) {
            return Tags.ContainsKey(tagName);
        }

        /// <summary>
        /// Get the data associated with a tag, or null
        /// </summary>
        public Dictionary<string, string> GetTagData(string tagName) {
            return Tags.TryGetValue(tagName, out var value) ? value : null;
        }

        public bool HasFlag(string flag) {
            return Template.GetComponent<GenericItemComponent>()?.HasFlag(flag) ?? default;
        }

        public int DamageLevel(int max) {
            if (Damage == 0 || max <= 0) {
                return 0;
            }
            if (ItemInfoData.DamageMax <= 1) {
                return Damage > 0 ? max : Damage;
            }
            if (Damage < 0) {
                return -((max - 1) * (-Damage - 1) / (ItemInfoData.DamageMax - 1) + 1);
            }

            return (max - 1) * (Damage - 1) / (ItemInfoData.DamageMax - 1) + 1;
        }

        public T GetComponent<T>() where T : ItemComponent {
            return Template.Info.TryGetValue(typeof(T), out var value) ? value as T : default;
        }

        public GenericItemComponent ItemData => GetComponent<GenericItemComponent>();
        public ItemInfoComponent ItemInfoData => GetComponent<ItemInfoComponent>();

        /*
        // Covers GUN
        public bool IsRanged => Template.HasComponent<RangedComponent>();
        public ItemRanged AsRanged() => this as ItemRanged;

        // Covers ARMOR
        public bool IsArmor => Template.HasComponent<ArmorComponent>();
        public ItemArmor AsArmor() => this as ItemArmor;

        // Covers GENERIC + item-inbuilt melee data
        public bool IsMelee => Template.HasComponent<MeleeComponent>();
        public MeleeItem AsMelee() => this as ItemMelee;

        */
        // IsTool, IsBook, IsModification, IsComestible, IsContainer, IsEngine, IsWheel, IsFuel, IsBionic
    }
}