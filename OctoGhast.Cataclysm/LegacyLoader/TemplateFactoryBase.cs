using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using OctoGhast.Framework;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public abstract class TemplateFactoryBase<TTemplateType, TTemplateLoader> where TTemplateLoader: class, new() {
        // TODO: Support multiple loaders, per LoadableType?
        protected TTemplateLoader TypeLoader = new TTemplateLoader();

        public ICollection<TTemplateType> Abstracts { get; protected set; } = new List<TTemplateType>();
        public IDictionary<string, TTemplateType> ItemTemplates { get; } = new Dictionary<string, TTemplateType>();
        public Dictionary<BaseTemplateType, JObject> BaseTemplates { get; } = new Dictionary<BaseTemplateType, JObject>();

        protected virtual IEnumerable<string> LoadableTypes { get; } = new string[0];

        public virtual void LoadFrom(string directory) {
            foreach (var file in Directory.EnumerateFiles(directory, "*.json", SearchOption.AllDirectories)) {
                try {
                    var json = JArray.Parse(File.ReadAllText(file));

                    foreach (var obj in json.Values<JObject>()) {
                        BaseTemplateType baseTemplate = null;

                        var type = obj.ReadProperty(() => baseTemplate.Type);

                        // Don't bother loading types we don't care about.
                        if (!IsLoadable(type)) {
                            continue;
                        }

                        if (obj.ContainsKey("abstract") && obj.ContainsKey("id")) {
                            throw new LoaderException($"Item (type:{type}) '{obj["abstract"]}/{obj["id"]}' has both abstract & id defined");
                        }

                        var id = obj.ReadProperty(() => baseTemplate.Id);
                        var abstractId = obj.ReadProperty(() => baseTemplate.AbstractId);
                        bool isAbstract = false;

                        if (abstractId != null && id == null) {
                            id = abstractId;
                            isAbstract = true;
                        }

                        baseTemplate = new BaseTemplateType(id, abstractId, type)
                        {
                            FileID = file,
                            PathInfo = obj.Path,
                            IsAbstract = isAbstract
                        };

                        BaseTemplates.Add(baseTemplate, obj);
                    }
                }
                catch (Exception e) {
                    Console.WriteLine($"Ex: {e}");
                }
            }
        }

        protected bool IsLoadable(string type) => LoadableTypes.Contains(type);
    }
}