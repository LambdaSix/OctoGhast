using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace OctoGhast.Framework.Data.Loading {
    public interface ITemplateFactory : IEnumerable<TemplateType>
    {
        /// <summary>
        /// List of 'type' identifiers this factory is able to load.
        /// Recommended that this is shared in a static instance for use by <see cref="TemplateType.NamespaceName"/> and
        /// <see cref="TemplateType.IsAlias"/> for identification of namespacing.
        /// </summary>
        IEnumerable<string> LoadableTypes { get; }

        /// <summary>
        /// Return a unique identifier within this template types namespace.        
        /// </summary>
        /// <param name="type">Item type based on 'type' field in data</param>
        /// <param name="jObj">Container object for raw data</param>
        /// <returns></returns>
        string GetIdentifier(string type, JObject jObj);

        /// <summary>
        /// Return a unique identifier within this template types namespace for an abstract item.
        /// Abstract items behave like abstract classes and cannot be directly created, only used
        /// as part of an inheritance chain.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="json"></param>
        /// <returns></returns>
        string GetAbstractIdentifier(string type, JObject json);

        /// <summary>
        /// Return a hydrated template from this factory.
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        TemplateType GetTemplate(string identifier);

        /// <summary>
        /// Return this factory instance as a generic instance of itself.
        /// </summary>
        /// <typeparam name="T">Descendent of TemplateType this factory can provide</typeparam>
        /// <returns></returns>
        ITemplateFactory<T> AsTyped<T>() where T : TemplateType;
    }

    public interface ITemplateFactory<T> : ITemplateFactory where T : TemplateType
    {
        T LoadTemplate(JObject jObj);
    }
}