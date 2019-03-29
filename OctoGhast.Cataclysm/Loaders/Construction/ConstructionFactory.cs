using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using OctoGhast.Framework.Data.Loading;

namespace OctoGhast.Cataclysm.Loaders.Construction {
    public static class ConstructionNamespaces
    {
        public static IEnumerable<string> Types = new[] { "CONSTRUCTION" };
    }

    [ServiceData("ConstructionFactory", "Provide hydration of construction data from storage")]
    public class ConstructionFactory : ITemplateFactory
    {
        public Dictionary<string, ConstructionType> Constructions { get; }

        public ConstructionFactory()
        {
            Constructions = new Dictionary<string, ConstructionType>();
        }

        public IEnumerator<TemplateType> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerable<string> LoadableTypes { get; }
        public string GetIdentifier(string type, JObject jObj)
        {
            return $"construction::{FindConstructionId(jObj)}";
        }

        public string GetAbstractIdentifier(string type, JObject json)
        {
            throw new NotImplementedException();
        }

        private string FindConstructionId(JObject jObj)
        {
            // if the construction has an explicit 'id' field use that.
            if (jObj.TryGetValue("id", out var idToken))
            {
                return idToken.Type == JTokenType.String
                    ? idToken.Value<string>()
                    : throw new InvalidDataException(FormatTypeError("id", idToken.Type, JTokenType.String));
            }

            var hasDescription = jObj.TryGetValue("description", out var descriptionToken);
            var hasCategory = jObj.TryGetValue("category", out var categoryToken);

            if (hasDescription && hasCategory)
            {
                var descStr = descriptionToken.Value<string>().Replace(' ', '_').ToLowerInvariant();
                var categoryStr = categoryToken.Value<string>().ToLowerInvariant();

                // Return something like 'furn_deconstruct_simple_furniture' as a fallback
                return $"{categoryStr}_{descStr}";
            }

            throw new InvalidDataException($"Expected 'category' and 'description', or 'id' field to be present");
        }

        private string FormatTypeError(string field, JTokenType actual, JTokenType expected)
        {
            return $"Expected '{field}' to be of type '{expected}' but found {expected}";
        }

        public TemplateType GetTemplate(string identifier)
        {
            throw new NotImplementedException();
        }

        public ITemplateFactory<T> AsTyped<T>() where T : TemplateType
        {
            throw new NotImplementedException();
        }
    }
}