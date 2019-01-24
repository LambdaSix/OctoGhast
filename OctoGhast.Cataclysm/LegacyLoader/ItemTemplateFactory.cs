using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
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

    [DataObject("TemplateFactory", "Provide hydration of template data from storage")]
    public class ItemTemplateFactory : TemplateFactoryBase<ItemType, ItemTypeLoader> {
        public ItemTemplateFactory() {
            // Inform the JsonDataLoader we have custom types and how to load them.
            JsonDataLoader.RegisterConverter(typeof(GunModifierData), (token, type) =>
                token.Count() == 4
                    ? new GunModifierData(token[1].Value<string>(), token[2].Value<int>(), token.Skip(3).TakeWhile(s => s.Value<JToken>() != null).Select(s => s.Value<string>()))
                    : new GunModifierData(token[1].Value<string>(), token[2].Value<int>(), Enumerable.Empty<string>())
            );

            JsonDataLoader.RegisterConverter(typeof(GunType), (token, t) =>
                new GunType(token.Value<string>())
            );

            JsonDataLoader.RegisterTypeLoader(typeof(GunModLocation), (token, t, v, _) =>
                token.ContainsKey("location")
                    ? new GunModLocation(token.GetValue("location").Value<string>())
                    : null
            );

            JsonDataLoader.RegisterConverter(typeof(SnippetCategory), (token, t) => {
                if (token.Type == JTokenType.Object) {
                    // Reading an object
                    var id = token[0].Value<string>();
                    var text = token[1].Value<string>();
                    return new SnippetCategory() {Id = id, Text = text};
                }

                if (token.Type == JTokenType.String) {
                    return new SnippetCategory() {Id = token.Value<string>()};
                }

                return null;
            });
        }

        public void LoadAbstracts() {
            foreach (var template in BaseTemplates.Where(s => s.Key.AbstractId != null && IsLoadable(s.Key.Type))) {
                if (!(template.Value is JObject jObj))
                    throw new Exception(
                        $"Unable to convert JSON object at '{template.Key.PathInfo}' in '{template.Key.FileID}'");

                // If this abstract is copying from another item, 
                if (jObj.TryGetValue("copy-from", out var value)) {
                    var loadOrder = LoadInheritanceChain(template.Key.Id);

                    foreach (var item in loadOrder) {
                        var itemInfo = FindOrLoadAbstract(item);
                        if (!Abstracts.Contains(itemInfo)) {
                            Abstracts.Add(itemInfo);
                        }
                    }
                }
                else {
                    Abstracts.Add(TypeLoader.Load(jObj, null));
                }
            }
        }

        public void LoadItemTemplates() {
            foreach (var template in BaseTemplates.Where(s => s.Key.Id != null && IsLoadable(s.Key.Type))) {
                if (!(template.Value is JObject jObj))
                    throw new Exception(
                        $"Unable to convert JSON object at '{template.Key.PathInfo}' in '{template.Key.FileID}'");

                // If this abstract is copying from another item, 
                if (jObj.TryGetValue("copy-from", out _)) {
                    var loadOrder = LoadInheritanceChain(template.Key.Id);

                    foreach (var item in loadOrder) {
                        var itemInfo = FindOrLoadItem(item);
                        if (!ItemTemplates.ContainsKey(itemInfo.Id)) {
                            ItemTemplates.Add(itemInfo.Id, itemInfo);
                        }
                    }
                }
                else {
                    var info = TypeLoader.Load(jObj, null);
                    ItemTemplates.Add(info.Id, info);
                }
            }
        }

        public ItemType RetrieveType(string id) => LoadItemTemplate(id);

        public ItemType LoadItemTemplate(string id) {
            var item = FindOrLoadItem(id);
            if (item != null) {
                ItemTemplates.Add(id, item);
                return item;
            }

            throw new Exception($"Unable to retrieve template for '{id}'");
        }

        private ItemType FindOrLoadItem(string id) {
            // Look for an existing item to fulfill this request
            if (ItemTemplates.TryGetValue(id, out var existingItem))
                return existingItem;

            // Or it might be an Abstract
            var existingAbstract = Abstracts.FirstOrDefault(s => s.Abstract == id);
            if (existingAbstract != null)
                return existingAbstract;

            // We don't have it loaded, go load it.
            var template = BaseTemplates.FirstOrDefault(s => s.Key.Id == id);
            if (template.Value != null) {
                if (template.Value.TryGetValue("copy-from", out var token)) {
                    return TypeLoader.Load(template.Value, FindOrLoadItem(token.Value<string>()));
                }
                else {
                    return TypeLoader.Load(template.Value, null);
                }
            }

            return null;
        }

        private ItemType FindOrLoadAbstract(string id) {
            var existing = Abstracts.FirstOrDefault(s => s.Abstract == id);
            if (existing != null) {
                return existing;
            }

            var template = BaseTemplates.FirstOrDefault(s => s.Key.Id == id);
            if (template.Value != null) {
                if (template.Value.TryGetValue("copy-from", out var token)) {
                    return TypeLoader.Load(template.Value, FindOrLoadAbstract(token.Value<string>()));
                }
                else {
                    return TypeLoader.Load(template.Value, null);
                }
            }

            return null;
        }

        public IEnumerable<string> LoadInheritanceChain(string id) {
            // generate inheritance chain for each object
            // (itemId -> Parent)
            var inheritanceChain = new List<(string item, string super)>();

            var item = BaseTemplates.FirstOrDefault(s => s.Key.Id == id);

            if (!IsLoadable(item.Key.Type))
                return Enumerable.Empty<string>();

            while (true) {
                if (item.Value.TryGetValue("copy-from", out var token)) {
                    var parent = token.Value<string>();
                    inheritanceChain.Add((item.Key.Id, parent));
                    item = BaseTemplates.FirstOrDefault(s => s.Key.Id == parent);
                }
                else {
                    inheritanceChain.Add((item.Key.Id, null));
                    break;
                }
            }

            var loadOrder = new List<string>() {inheritanceChain.Last().item};
            loadOrder.AddRange(inheritanceChain.TakeWhile(s => s.super != null).Reverse().Select(s => s.item));
            return loadOrder;
        }

        protected override IEnumerable<string> LoadableTypes { get; } = new[]
        {
            "ARMOR", "BIONIC_ITEM", "BOOK", "CONTAINER", "ENGINE", "FUEL", "GENERIC", "GUN", "GUNMOD", "MAGAZINE",
            "TOOL", "TOOL_ARMOR", "TOOLMOD", "WHEEL", "AMMO"
        };
    }
}