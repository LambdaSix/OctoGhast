﻿using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace OctoGhast.Cataclysm.Loaders.Recipe {
    public class RecipeComponentGroup : IEnumerable<RecipeComponent> {
        public IEnumerable<RecipeComponent> Components { get; set; }

        public RecipeComponentGroup(JArray component) {
            Components = new[] { new RecipeComponent(component) };
        }

        public RecipeComponentGroup(IEnumerable<JToken> components)
        {
            Components = LoadComponents(components).ToList();
        }

        private IEnumerable<RecipeComponent> LoadComponents(IEnumerable<JToken> components) {
            return components.Where(s => s.Type == JTokenType.Array).Select(component => new RecipeComponent(component));
        }

        public IEnumerator<RecipeComponent> GetEnumerator() {
            return Components.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return ((IEnumerable) Components).GetEnumerator();
        }
    }

    [DebuggerDisplay("{ComponentItemType}::{Quantity}")]
    public class RecipeComponent {
        public string ComponentItemType { get; set; }
        public int Quantity { get; set; }

        /// <summary>
        /// Indicates that <see cref="ComponentItemType"/> defines a Material List, rather than a specific item
        /// </summary>
        public bool UseMaterialList { get; set; }

        /// <summary>
        /// Indicates that the component should always be consumed on crafting failure.
        /// </summary>
        public bool ConsumeOnFail { get; set; }

        public RecipeComponent(JToken jObj) {
            if (jObj.Type == JTokenType.Array) {
                var jArr = jObj as JArray;
                Debug.Assert(jArr != null);

                var componentName = jArr[0].Value<string>();

                ComponentItemType = componentName;
                Quantity = jArr[1].Value<int>();
                

                // TODO: Deprecate this in favor of requirement:: qualified names.
                if (jObj.Count() > 2) {
                    UseMaterialList = jArr[2].Value<string>().ToLowerInvariant() == "list";
                }
            }
        }

        public RecipeComponent(string componentItemType, int quantity, bool useMaterialList) {
            ComponentItemType = componentItemType;
            Quantity = quantity;
            UseMaterialList = useMaterialList;
        }
    }
}