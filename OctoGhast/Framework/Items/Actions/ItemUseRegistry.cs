using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OctoGhast.Extensions.FastExpressionCompiler;

namespace OctoGhast.Framework.Items.Actions {
    public class ItemUseRegistry {
        public Dictionary<string, ItemUseDelegate> ItemUses { get; set; }

        public ItemUseRegistry() {
            ItemUses = LoadItemUsages();
        }

        private Dictionary<string, ItemUseDelegate> LoadItemUsages() {
            var dictionary = new Dictionary<string, ItemUseDelegate>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                foreach (var type in assembly.GetTypes().Where(s => s.IsClass())) {
                    if (type.GetCustomAttribute<ItemUseAttribute>() is ItemUseAttribute attr) {
                        if (Activator.CreateInstance(type) is ItemUse<TemplateType> usage) {
                            dictionary.Add(attr.UseName, usage.Use);
                        }
                        else {
                            throw new Exception($"Unable to create instance of {type.Name} as {nameof(ItemUse<TemplateType>)}");
                        }
                    }
                }
            }

            return dictionary;
        }

        public ItemUseDelegate Retrieve(string useName) => ItemUses.TryGetValue(useName ?? "NULL", out var obj)
            ? obj
            : throw new KeyNotFoundException($"Unable to find handler for {useName}");
    }
}