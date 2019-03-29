using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace OctoGhast.Cataclysm.Loaders.Recipe {
    public class RecipeToolGroup : IEnumerable<RecipeTool> {
        public IEnumerable<RecipeTool> Tools;

        public RecipeToolGroup(JArray tool) {
            Tools = new[] {new RecipeTool(tool)};
        }

        public RecipeToolGroup(IEnumerable<JToken> tools) {
            Tools = LoadTools(tools);
        }

        private static IEnumerable<RecipeTool> LoadTools(IEnumerable<JToken> tools) {
            return tools.Where(s => s.Type == JTokenType.Array).Select(tool => new RecipeTool(tool as JArray));
        }

        public IEnumerator<RecipeTool> GetEnumerator() {
            return Tools.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return ((IEnumerable) Tools).GetEnumerator();
        }
    }

    [DebuggerDisplay("{ToolName}::{Charges}")]
    public class RecipeTool {
        public string ToolName { get; set; }
        public int Charges { get; set; }
        public bool UseList { get; set; }

        public RecipeTool(JArray jObj) {
            if (jObj.Type == JTokenType.Array) {
                var jArr = jObj.Values();
                ToolName = jArr.ElementAt(0).Value<string>();
                Charges = jArr.ElementAt(1).Value<int>();

                if (jArr.Count() > 2) {
                    UseList = jArr.ElementAt(2).Value<string>().ToLowerInvariant() == "list";
                }
            }
        }

        public RecipeTool(string toolName, int charges, bool useList) {
            ToolName = toolName;
            Charges = charges;
            UseList = useList;
        }
    }
}