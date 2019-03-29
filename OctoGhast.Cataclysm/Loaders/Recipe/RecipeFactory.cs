using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using OctoGhast.Cataclysm.Loaders.Construction;
using OctoGhast.Framework.Data.Loading;

namespace OctoGhast.Cataclysm.Loaders.Recipe {
    public static class RecipeNamespaces {
        public static IEnumerable<string> Types = new[] {"RECIPE", "UNCRAFT"};
    }

    [ServiceData("RecipeFactory", "Provide hydration of recipe data from storage")]
    public class RecipeFactory : ITemplateFactory {
        public Dictionary<string,RecipeType> Recipes { get; }

        public RecipeFactory() {
            Recipes = new Dictionary<string, RecipeType>();
        }

        public IEnumerator<TemplateType> GetEnumerator() {
            throw new System.NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public IEnumerable<string> LoadableTypes { get; } = RecipeNamespaces.Types;

        /// <summary>
        /// Return the fully qualified identifier for this object.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="jObj"></param>
        /// <returns></returns>
        public string GetIdentifier(string type, JObject jObj) {
            switch (type.ToLowerInvariant()) {
                case "recipe":
                    return $"recipe::{FindRecipeId(jObj)}";
                case "uncraft":
                    return $"uncraft::{FindDisassemblyId(jObj)}";
                default:
                    throw new InvalidDataException($"Expected type of [{String.Join(",", LoadableTypes)}], but got {type}");
            }
        }

        public string GetAbstractIdentifier(string type, JObject json) {
            // These types have no abstracts
            return GetIdentifier(type, json);
        }

        private string FindRecipeId(JObject jObj) {
            return FindCommonId(jObj);
        }

        private string FindDisassemblyId(JObject jObj) {
            return FindCommonId(jObj);
        }

        private string FindCommonId(JObject jObj) {
            // if the recipe has an explicit 'id' field use that, it overrides the result/id_suffix
            if (jObj.TryGetValue("id", out var idToken)) {
                return idToken.Type == JTokenType.String
                    ? idToken.Value<string>()
                    : throw new InvalidDataException(FormatTypeError("id", idToken.Type, JTokenType.String));
            }

            var hasResultToken = jObj.TryGetValue("result", out var resultToken);
            var hasSuffixToken = jObj.TryGetValue("id_suffix", out var suffixToken);

            if (!hasResultToken) {
                throw new InvalidDataException($"Expected 'result' field to be present.");
            }

            if (resultToken.Type != JTokenType.String) {
                throw new InvalidDataException(FormatTypeError("result", resultToken.Type, JTokenType.String));
            }

            // Else, if there's a 'id_suffix' field, append that to 'result'
            if (hasSuffixToken) {
                return suffixToken.Type == JTokenType.String
                    ? $"{resultToken.Value<string>()}_{suffixToken.Value<string>()}"
                    : throw new InvalidDataException(FormatTypeError("id_suffix", suffixToken.Type, JTokenType.String));
            }

            // Otherwise, just use 'result' on it's own
            return resultToken.Value<string>();
        }

        private string FormatTypeError(string field, JTokenType actual, JTokenType expected) {
            return $"Expected '{field}' to be of type '{expected}' but found {expected}";
        }

        public TemplateType GetTemplate(string identifier) {
            throw new System.NotImplementedException();
        }

        public ITemplateFactory<T> AsTyped<T>() where T : TemplateType {
            throw new System.NotImplementedException();
        }
    }
}