using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OctoGhast {
    public class TemplateLoader {
        public Dictionary<string, List<JObject>> Templates { get; set; } = new Dictionary<string, List<JObject>>();
        public Dictionary<string, ITemplateLoader> Loaders { get; set; } = new Dictionary<string, ITemplateLoader>();

        public void LoadFromPath(string path) {
            var typeDefs = new List<(string type, JObject itemDef)>();

            foreach (var file in Directory.EnumerateFiles(path, "*.json", SearchOption.AllDirectories)) {
                var fileHnd = new JsonTextReader(new StreamReader(File.OpenRead(file)));
                JToken container;
                try {
                    container = JToken.Load(fileHnd);
                }
                catch (JsonReaderException ex) {
                    // TODO: Replace this with a better exception type for pulling apart later
                    throw new Exception($"In file: {file} // {ex.Message}", ex);
                }

                typeDefs.AddRange(LoadObject(container, typeDefs));
            }

            var byType = typeDefs.GroupBy(s => s.type);
            if (!Templates.Any()) {
                Templates = byType.ToDictionary(grp => grp.Key, grp => grp.Select(s => s.itemDef).ToList());
            }
            else {
                // Probably loading a mod?
                // Throw the templates in, the loader handling the types can work out if there are duplicates we care about.
                foreach (var item in byType) {
                    Templates.Add(item.Key, item.Select(s => s.itemDef).ToList());
                }
            }
        }

        private IEnumerable<(string,JObject)> LoadObject(JToken container, List<(string type, JObject itemDef)> typeDefs) {
            foreach (var item in container.Children<JObject>()) {
                if (!item.ContainsKey("type")) // TODO: Capture the line numbers?
                    throw new Exception("Entity with missing 'type' information");

                var typeName = item.GetValue("type").Value<string>();
                yield return (typeName, item);
            }
        }

        private void SetupCoreLoadHandlers() {
            var assembly = Assembly.GetExecutingAssembly();
            var loaders = assembly.GetTypes().Where(s => s.IsAnsiClass)
                .Where(s => s.GetInterfaces().Contains(typeof(ITemplateLoader)));

            foreach (var loader in loaders) {
                var attribute = loader.GetCustomAttribute<TemplateLoaderAttribute>();
                if (attribute is null)
                    throw new Exception("Class inheriting from ITemplateLoader lacks TemplateLoaderAttribute");

                var instance = Activator.CreateInstance(loader) as ITemplateLoader;
                foreach (var type in attribute.LoadableTypes) {
                    RegisterHandler(type, instance);
                }
            }
        }

        public void RegisterHandler(string type, ITemplateLoader loader) {
            if (Loaders.ContainsKey(type))
                throw new Exception($"Attempted to register duplicate type loader for {type}");

            Loaders.Add(type, loader);
        }
    }
}