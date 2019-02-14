using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using OctoGhast.Framework;
using OctoGhast.Framework.Data.Loading;

namespace OctoGhast.Cataclysm.LegacyLoader {
    [DataObject("ItemTemplateFactory", "Provide hydration of template data from storage")]
    public class ItemTemplateFactory : TemplateFactoryBase<ItemType, ItemTypeLoader> {
        private Dictionary<string, IEnumerable<ItemType>> _itemGroups;

        public Dictionary<string, IEnumerable<ItemType>> ItemGroups =>
            _itemGroups ?? (_itemGroups = ItemTemplates.GroupBy(s => s.Value.Type ?? "Unknown")
                .ToDictionary(key => key.Key, value => value.Select(s => s.Value)));

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
            JsonDataLoader.RegisterConverter(typeof(UseActionData), (token, type) => {
                if (token.Type == JTokenType.Object || token.Type == JTokenType.String) {
                    return new UseActionData(token);
                }

                throw new LoaderException($"Unable to process {token}");
            });

            JsonDataLoader.RegisterTypeLoader(typeof(IEnumerable<UseActionData>), (jObj, name, val, types) => {
                // Raw object or string can be converted by the constructor
                if (jObj.Type == JTokenType.Object || jObj.Type == JTokenType.String) {
                    return new[] {new UseActionData(jObj)};
                }
                // And pull apart arrays to get individual objects.
                else if (jObj.Type == JTokenType.Array) {
                    var array = jObj.Children();
                    var seq = new List<UseActionData>(array.Count());

                    foreach (var item in array) {
                        if (jObj.Type == JTokenType.Object || jObj.Type == JTokenType.String) {
                            seq.Add(new UseActionData(jObj));
                        }
                    }

                    return seq;
                }

                throw new LoaderException($"Found unknown object at {jObj.Path} -- {jObj.Parent}");
            });
        }

        public void LoadAbstracts() {
            foreach (var template in BaseTemplates.Where(s => s.Key.AbstractId != null && IsLoadable(s.Key.Type))) {
                if (!(template.Value is JObject jObj))
                    throw new Exception(
                        $"Unable to convert JSON object at '{template.Key.PathInfo}' in '{template.Key.FileID}'");

                // If this abstract is copying from another item, 
                if (jObj.TryGetValue("copy-from", out var value)) {
                    var loadOrder = LoadInheritanceChain(template);

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
                    var loadOrder = LoadInheritanceChain(template);

                    foreach (var item in loadOrder) {
                        var itemInfo = FindOrLoadItem(item);
                        if (!ItemTemplates.ContainsKey(itemInfo.GetIdentifier())) {
                            ItemTemplates.Add(itemInfo.GetIdentifier(), itemInfo);
                        }
                    }
                }
                else {
                    var info = TypeLoader.Load(jObj, null);

                    if (ItemTemplates.ContainsKey(info.GetIdentifier())) {
                        continue; // We probably loaded this from a prior copy_from so don't worry about it.
                    }

                    ItemTemplates.Add(info.GetIdentifier(), info);
                }
            }

            _itemGroups = null;
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

        public IEnumerable<string> LoadInheritanceChain(KeyValuePair<BaseTemplateType, JObject> baseItem) {
            // generate inheritance chain for each object
            // (itemId -> Parent)
            var inheritanceChain = new List<(string item, string super)>();

            if (!IsLoadable(baseItem.Key.Type))
                return Enumerable.Empty<string>();

            var item = baseItem;
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

                // Check that the parent item could be found.
                if (item.Value == null || item.Key == null)
                    throw new LoaderException($"Unable to find parent item for {baseItem}");
            }

            var loadOrder = new List<string>() {inheritanceChain.Last().item};
            loadOrder.AddRange(inheritanceChain.TakeWhile(s => s.super != null).Reverse().Select(s => s.item));
            return loadOrder;
        }

        public override IEnumerable<string> LoadableTypes { get; } = ItemNamespaces.LoadableTypes;
    }

    public static class ItemNamespaces {
        public static IEnumerable<string> LoadableTypes { get; } = new[]
        {
            "ARMOR", "BIONIC_ITEM", "BOOK", "CONTAINER", "ENGINE", "FUEL", "GENERIC", "GUN", "GUNMOD", "MAGAZINE",
            "TOOL", "TOOL_ARMOR", "TOOLMOD", "WHEEL", "AMMO", "BIONIC", "COMESTIBLE"
        };
    }

    public class TemplateProviderAttribute : Attribute {
        public TemplateProviderAttribute(params string[] types) {
            
        }
    }
}