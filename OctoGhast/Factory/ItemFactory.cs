using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace OctoGhast {
    public class TemplateLoaderAttribute : Attribute {
        public IEnumerable<string> LoadableTypes { get; set; }
        public TemplateLoaderAttribute(string type) {
            LoadableTypes = new[] {type};
        }

        public TemplateLoaderAttribute(params string[] types) {
            LoadableTypes = types.AsEnumerable();
        }
    }

    public interface ITemplateLoader {

        /// <summary>
        /// Load a single Type into a template
        /// </summary>
        void LoadTemplate(string type, JObject data);

        /// <summary>
        /// Check the consistency of loaded data
        /// </summary>
        bool CheckData();

        /// <summary>
        /// Finalize any data
        /// </summary>
        /// <returns></returns>
        bool FinalizeData();
    }

    // Base loader for just about all kinds of items
    [TemplateLoader("AMMO", "GUN", "ARMOR", "TOOL", "TOOLMOD", "TOOL_ARMOR", "BOOK", "CONTAINER", "ENGINE", "WHEEL",
            "FUEL", "GUNMOD", "MAGAZINE", "GENERIC", "BIONIC_ITEM")]
    public class ItemFactory : ITemplateLoader {
        /// <inheritdoc />
        public void LoadTemplate(string type, JObject data) {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public bool CheckData() {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public bool FinalizeData() {
            throw new NotImplementedException();
        }
    }

}