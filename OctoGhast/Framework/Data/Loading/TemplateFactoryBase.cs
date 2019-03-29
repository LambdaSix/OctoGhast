using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using OctoGhast.Cataclysm.LegacyLoader;
using OctoGhast.Extensions.FastExpressionCompiler;

namespace OctoGhast.Framework.Data.Loading {
    public class DataFactory {
        public Dictionary<string, ITemplateFactory> TypeMap { get; }

        public Dictionary<BaseTemplateType, JObject> BaseTemplates { get; } = new Dictionary<BaseTemplateType, JObject>();
        public Dictionary<string, Dictionary<BaseTemplateType, JObject>> BaseTemplateGroups => BaseTemplates
            .GroupBy(s => s.Key.Type ?? "Unknown").ToDictionary(key => key.Key,
                value => value.Select(s => s).ToDictionary(s => s.Key, innerVal => innerVal.Value));

        public DataFactory() {
            TypeMap = FindFactories();
        }

        private Dictionary<string, ITemplateFactory> FindFactories() {
            bool findFactory(Type s) => s.IsClass() && s.HasInterface(typeof(ITemplateFactory));
            var dict = new Dictionary<string, ITemplateFactory>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                foreach (var type in assembly.GetTypes().Where(findFactory)) {
                    if (Activator.CreateInstance(type) is ITemplateFactory factory) {
                        
                        // Build up a mapping of json type -> factory
                        // ex.
                        // ["ARMOR"] = (ItemTemplateFactory)factory
                        // Where a template factory handles multiple data types, there should be multiple references to the same instance.
                        foreach (var loadableType in factory.LoadableTypes) {
                            dict.Add(loadableType, factory);
                        }
                    }
                    else {
                        throw new Exception($"Unable to create instance of {type.Name} as {nameof(ITemplateFactory)}");
                    }
                }
            }
            return dict;
        }

        public virtual void LoadFrom(string directory, bool isCore = false) {
            foreach (var file in Directory.EnumerateFiles(directory, "*.json", SearchOption.AllDirectories)) {
                var json = JObject.Parse(File.ReadAllText(file));

                if (json.Type != JTokenType.Array)
                    throw new LoaderException($"Found file not rooted with an array - {file}");

                foreach (var jObj in json.Values<JObject>()) {
                    if (!jObj.TryGetValue("type", out var jType))
                        throw new LoaderException($"Object at {jObj.Path} in {file} lacks 'type' field.");

                    var strType = jType.Value<string>().ToLowerInvariant();

                    if (TypeMap.TryGetValue(jType.Value<string>(), out var factory)) {
                        var identifier = factory.GetIdentifier(strType, jObj);
                        var abstractIdentifier = factory.GetAbstractIdentifier(strType, jObj);

                        /*
                         * Overrides are handled as part of the multi-pass loading.
                         * We determine which templates are overrides here, then at a later stage when we've loaded
                         * all of the available templates, each item factory should handle discarding old templates
                         * in favor of overriden ones.
                         *
                         * Precedence order should be Core is overridden by mods, mods should order themselves by
                         * dependencies.
                         *
                         * If a mod specifies that it should be loaded after another, that ensures any overrides by
                         * A of B are loaded in the correct order, otherwise load ordering isn't guaranteed.
                         */
                        bool useOverride = false;
                        if (jObj.TryGetValue("override", out var overrideToken)) {
                            // Just to handle a situation where someone specifies `'override': false`..
                            useOverride = overrideToken.Value<bool>();
                        }

                        var baseTemplate = new BaseTemplateType(identifier, abstractIdentifier, strType)
                        {
                            FileID = file,
                            IsAbstract = abstractIdentifier != null,
                            PathInfo = jObj.Path,
                            IsOverride = useOverride,
                            IsCore = isCore,
                        };

                        BaseTemplates.Add(baseTemplate, jObj);
                    }
                    else {
                        throw new LoaderException($"Unknown object type {jType}");
                    }
                }
            }
        }

        // Match the {type}::{id} format
        public TemplateType Get(string type, string id) {
            // Work which Factory we should retrieve from, then retrieve the template via it's FQN.
            return TypeMap.TryGetValue(type, out var factory)
                ? factory.GetTemplate($"{type}::{id}")
                : null;
        }

        /// <summary>
        /// Attempt to retrieve a reference to a template by it's qualified ID.
        /// </summary>
        /// <example>
        /// var woodMaterial = Get{Material}("material::wood");
        /// </example>
        /// <param name="qualifiedId"></param>
        public TemplateType GetQualifiedItem<T>(string qualifiedId) where T: TemplateType {
            var (type, id) = EntityNamespacing.TransformQualifiedId(qualifiedId);

            return Get(type, id) as T;
        }
    }

    /// <summary>
    /// Indicates the Types a factory is capable of providing.
    /// </summary>
    public class TemplateProviderAttribute : Attribute {
        public string[] Types { get; }

        public TemplateProviderAttribute(params string[] types) {
            Types = types;
        }
    }

    public abstract class TemplateFactoryBase<TTemplateType, TTemplateLoader> where TTemplateLoader: class, new() {
        // TODO: Support multiple loaders, per LoadableType?
        protected TTemplateLoader TypeLoader = new TTemplateLoader();

        public ICollection<TTemplateType> Abstracts { get; protected set; } = new List<TTemplateType>();
        public IDictionary<string, TTemplateType> ItemTemplates { get; } = new Dictionary<string, TTemplateType>();
        public Dictionary<BaseTemplateType, JObject> BaseTemplates { get; } = new Dictionary<BaseTemplateType, JObject>();

        public Dictionary<string, Dictionary<BaseTemplateType, JObject>> BaseTemplateGroups => BaseTemplates
            .GroupBy(s => s.Key.Type ?? "Unknown").ToDictionary(key => key.Key,
                value => value.Select(s => s).ToDictionary(s => s.Key, innerVal => innerVal.Value));

        public virtual IEnumerable<string> LoadableTypes { get; } = new string[0];

        public virtual void LoadFrom(string directory) {
            foreach (var file in Directory.EnumerateFiles(directory, "*.json", SearchOption.AllDirectories)) {
                try {
                    var json = JArray.Parse(File.ReadAllText(file));

                    foreach (var obj in json.Values<JObject>()) {
                        BaseTemplateType baseTemplate = null;

                        var type = obj.ReadProperty(() => baseTemplate.Type);

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

                        try {
                            BaseTemplates.Add(baseTemplate, obj);
                        }
                        catch (ArgumentException e) {
                            throw new LoaderException($"Duplicate item {id}", e);
                        }
                    }
                }
                catch (Exception e) {
                    Console.WriteLine($"Error loading {file} - Ex: {e}");
                }
            }
        }

        protected bool IsLoadable(string type) => LoadableTypes.Contains(type);
    }

    public class LoaderException : Exception
    {
        /// <inheritdoc />
        public LoaderException() { }

        /// <inheritdoc />
        public LoaderException(string message) : base(message) { }

        /// <inheritdoc />
        public LoaderException(string message, Exception innerException) : base(message, innerException) { }
    }
}