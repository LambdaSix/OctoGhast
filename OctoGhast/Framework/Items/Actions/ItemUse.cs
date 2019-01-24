using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using InfiniMap;
using Newtonsoft.Json.Linq;
using OctoGhast.Extensions.FastExpressionCompiler;
using OctoGhast.Framework.Mobile;

using static OctoGhast.Translation.Translation;

namespace OctoGhast.Framework.Items.Actions {
    public delegate int ItemUseDelegate(BaseCreature player, RLObject<TemplateType> item, bool turnTick, WorldSpace position);

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

        public ItemUseDelegate Retrieve(string useName) => ItemUses.TryGetValue(useName, out var obj)
            ? obj
            : throw new KeyNotFoundException($"Unable to find handler for {useName}");
    }

    /// <summary>
    /// Defines a base 'iuse'/'use_action' function.
    /// These are intended as specific actions on items that require no json parametrization.
    ///
    /// When supplied with a function delegate, it is wrapped in a UseAction for consumption.
    /// </summary>
    public class ItemUse<T> where T : TemplateType
    {
        public UseAction Action { get; set; }

        public ItemUse() { }

        public ItemUse(UseAction f)
        {
            Action = f;
        }

        public virtual int Use(BaseCreature player, RLObject<T> item, bool turnTick, WorldSpace position)
        {
            return -1;
        }

        public (bool success, string message) CanInvoke(BaseCreature player, RLObject<T> item, bool turnTick, WorldSpace position)
        {
            if (Action == null)
                return (false, _($"You can't do anything interesting with your {item.GetName()}"));

            return Action.CanInvoke(player, item, turnTick, position);
        }

        public int Invoke(BaseCreature player, RLObject<T> item, bool turnTick, WorldSpace position)
        {
            return Action.Invoke(player, item, turnTick, position);
        }
    }

    /// <summary>
    /// Defines a base 'use action' function.
    /// These are intended to be a generic action type like 'transform' that takes json parametrization.
    /// </summary>
    public class UseAction
    {
        public string Type { get; set; }
        public string Name { get; set; }

        public UseAction(string type) { }

        public virtual (bool success, string message) CanInvoke<T>(BaseCreature player, RLObject<T> item, bool turnTick, WorldSpace position) where T : TemplateType
        {
            return (true, null);
        }

        public virtual int Invoke<T>(BaseCreature player, RLObject<T> item, bool turnTick, WorldSpace position) where T : TemplateType
        {
            return -1;
        }

        public virtual void Load(JObject jObj) { }
    }
}