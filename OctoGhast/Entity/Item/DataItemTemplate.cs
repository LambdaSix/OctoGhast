using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace OctoGhast {
    /// <summary>
    /// 
    /// </summary>
    public class DataItemTemplate : DataTemplate {
        public Dictionary<Type,ItemComponent> Info { get; set; }

        public bool HasComponent<T>() where T: ItemComponent {
            return Info.ContainsKey(typeof(T));
        }

        public T GetComponent<T>() where T: ItemComponent {
            return Info.TryGetValue(typeof(T), out var value)
                ? value as T
                : default;
        }

        public void AddComponent<T>(T info) where T : ItemComponent {
            if (Info.ContainsKey(typeof(T))) {
                // TODO: Overwrite for now, maybe exception throw?
                Info[typeof(T)] = info;
            }
            else {
                Info.Add(typeof(T), info);
            }
        }
    }

    public class ItemComponent {

    }
}