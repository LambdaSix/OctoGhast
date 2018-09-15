using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace OctoGhast.Extensions {
    public static class JObjectExtensions {
        public static T GetValueOr<T>(this JObject self, string propertyName, T defaultVal) {
            return self.TryGetValue(propertyName, StringComparison.InvariantCultureIgnoreCase, out var value)
                ? value.Value<T>()
                : defaultVal;
        }

        public static IEnumerable<JProperty> Matches(this JObject self, string match) {
            return self.Properties().Where(s => s.Name.Contains(match));
        }

        public static IEnumerable<T> GetArray<T>(this JObject self, string propertyName) {
            var res = self.TryGetValue(propertyName, out var value);
            if (res) {
                if (value.Type==JTokenType.Array) {
                    return value.Values<T>();
                }
            }
            return Enumerable.Empty<T>();
        }

        public static T GetObject<T>(this JObject self, string propertyName) {
            var res = self.TryGetValue(propertyName, out var value);
            if (res) {
                if (value.Type == JTokenType.Object) {
                    return value.ToObject<T>();
                }
            }

            return default;
        }

        public static IEnumerable<TOut> GetArrayOfPairs<TOut>(this JObject self, string propertyName, Func<JToken, JToken, TOut> mapFunc) {
            var res = self.TryGetValue(propertyName, out var value);
            if (res) {
                // We found a property
                if (value.Type == JTokenType.Array) {
                    // The property is an array 
                    if (value.HasValues)
                        foreach (var pair in value) {
                            yield return mapFunc(pair[0], pair[1]);
                        }
                }
            }
        }

        public static IEnumerable<TOut> GetArrayOf<TOut>(this JObject self, string propertyName) where TOut : class, new() {
            var res = self.TryGetValue(propertyName, out var value);
            if (res) {
                // We found an array
                if (value.Type == JTokenType.Array) {
                    foreach (var val in value) {
                        if (val.Type == JTokenType.Object) {
                            bool isTemplate = typeof(TOut).GetConstructors()
                                .Any(s => s.GetParameters().Any(p => p.ParameterType == typeof(JObject)));

                            if (isTemplate) { 
                                var instance = Activator.CreateInstance(typeof(TOut), val as JObject) as TOut;
                                yield return instance;
                            }
                            else {
                                yield return val.ToObject<TOut>();
                            }
                        }
                    }
                }
            }
        }
    }
}