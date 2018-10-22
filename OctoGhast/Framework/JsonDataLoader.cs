using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MiscUtil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Ninject.Infrastructure.Language;
using OctoGhast.Extensions;
using OctoGhast.Units;
using OctoGhast.UserInterface.Controls;

namespace OctoGhast.Framework {
    public interface ITypeLoader {
        object Load(JObject data, object existing);
    }

    public static class JsonDataLoader {
        private static readonly Dictionary<Type, Func<JObject, string, object, object>> _conversionMap =
            new Dictionary<Type, Func<JObject, string, object, object>>()
            {
                [typeof(int)] = (jObj, name, val) => ReadProperty(jObj, name, (int) val),
                [typeof(double)] = (jObj, name, val) => ReadProperty(jObj, name, (double) val),
                [typeof(float)] = (jObj, name, val) => ReadProperty(jObj, name, (float) val),
                [typeof(string)] = (jObj, name, val) => ReadProperty(jObj, name),
                [typeof(Volume)] = (jObj, name, val) => ReadProperty<Volume>(jObj, name, val as Volume),
                [typeof(Mass)] = (jObj, name, val) => ReadProperty<Mass>(jObj, name, val as Mass),
                [typeof(Dictionary<string, string>)] = (jObj, name, val) => ReadDictionary<string, string>(jObj, name),
                [typeof(Dictionary<string, int>)] = (jObj, name, val) => ReadDictionary<string, int>(jObj, name),
                [typeof(Dictionary<string,IEnumerable<string>>)] = (jObj, name, val) => ReadNestedDictionary<string,string>(jObj, name),
                [typeof(IEnumerable<string>)] = (jObj, name, val) => ReadEnumerable<string>(jObj, name),
            };

        public static int ReadProperty(this JObject jObject, string name, int val) {
            var res = ReadProperty<int?>(jObject, name, val, (v, acc) => (int?)(v + acc), (v, s) => (int?)(v * s));
            return res ?? val;
        }

        public static double ReadProperty(this JObject jObject, string name, double val) {
            var res = ReadProperty<double?>(jObject, name, val, (v, acc) => v + acc, (v, s) => v * s);
            return res ?? val;
        }

        public static bool ReadProperty(this JObject jObject, string name, bool val) {
            // Bool has no relative/proportional support.
            return jObject.TryGetValue(name, out var value)
                ? value.Value<bool>()
                : val;
        }

        public static T ReadProperty<T>(this JObject jObject, string name, T val) {
            if (_conversionMap.TryGetValue(typeof(T), out var func)) {
                return (T) func(jObject, name, val);
            }

            throw new ArgumentException($"Unable to deduce built-in loader for type: '{typeof(T)}");
        }

        public static Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(JObject jObj, string name) {
            var dict = new Dictionary<TKey, TValue>();

            if (jObj.GetValue(name) is JArray rootObject) {
                foreach (var array in rootObject.Children().Children()) {
                    dict.Add(array[0].Value<TKey>(), array[1].Value<TValue>());
                }
            }
            else {
                dict = jObj.GetValue(name).ToObject<Dictionary<TKey, TValue>>();    
            }

            return dict;
        }

        public static Dictionary<TKey, IEnumerable<TValue>> ReadNestedDictionary<TKey, TValue>(JObject jObj, string name) {
            Dictionary<TKey, IEnumerable<TValue>> workingSet = new Dictionary<TKey, IEnumerable<TValue>>();
            var rootObject = jObj.GetValue(name);
            if (rootObject is JArray) {
                var pairs = rootObject.Zip(rootObject.Skip(1), Tuple.Create);

                foreach (var (key, value) in pairs.Where(s => s.Item1.Type != JTokenType.Array)) {
                    var values = value;
                    IEnumerable<TValue> set = Enumerable.Empty<TValue>();

                    if (values is JArray subArray) {
                        set = subArray.Values<TValue>();
                    }

                    workingSet.Add(key.Value<TKey>(), set.ToList());
                }
            }

            return workingSet;
        }

        public static IEnumerable<T> ReadEnumerable<T>(JObject jObj, string name) {
            if (jObj.TryGetValue(name, out var value)) {
                if (value is JArray arr) {
                    return arr.Values<T>();
                }
            }
            return default;
        }

        public static T ReadProperty<T>(this JObject jObject, Expression<Func<T>> expression) {
            var propertyInfo = expression.GetProperty();
            var attr = propertyInfo.GetCustomAttribute<LoaderInfoAttribute>();

            T defaultValue = default(T);
            if (attr.DefaultValue is string str) {
                // Bit of a hack but if we're a string, then be an actual string for conversion.
                defaultValue = Operator.Convert<string, T>(str);
            }
            else {
                defaultValue = (T) attr.DefaultValue;
            }

            var value = expression.Compile().GetValue(defaultValue);

            if (attr.TypeLoader != null) {
                var loader = Activator.CreateInstance(attr.TypeLoader) as ITypeLoader;
                return (T) loader?.Load(jObject, value);

            }

            return ReadProperty(jObject, attr.FieldName, value);
        }

        public static string ReadProperty(this JObject jObject, string name) {
            // No relative/proportional for strings.
            if (jObject.TryGetValue(name, out var value)) {
                return value.Value<string>();
            }

            return default;
        }

        public static Volume ReadProperty<T>(this JObject jObject, string name, Volume val) where T:Volume {
            return ReadProperty(
                jObj: jObject,
                name: name,
                existingValue: val,
                mapFunc: s => new Volume(s),
                relativeFunc: (v, acc) => (v + acc) as T,
                proportionalFunc: (v, acc) => (v * acc) as T
            );
        }

        public static Mass ReadProperty<T>(this JObject jObject, string name, Mass val) where T:Mass{
            return ReadProperty(
                jObj: jObject,
                name: name,
                existingValue: val,
                mapFunc: s => new Mass(s),
                relativeFunc: (v, acc) => (v + acc) as T,
                proportionalFunc: (v, acc) => (v * acc) as T
            );
        }

        public static SoundLevel ReadProperty(this JObject jObject, string name, SoundLevel val) {
            /*
             * Naive implementation of the Relative/Proportional tags here.
             * 6dB + 6dB == 9dB, not 12dB because decibels are a logarithmic scale.
             * For ease of content authors, we implement the naive 6+6=12 scale here.
             */
            return ReadProperty(
                jObj: jObject,
                name: name,
                existingValue: val,
                mapFunc: s => new SoundLevel(s),
                relativeFunc: (v, acc) => (v + acc) as SoundLevel,
                proportionalFunc: (v, acc) => (v * acc) as SoundLevel
            );
        }

        public static TimeDuration ReadProperty(this JObject jObject, string name, TimeDuration val) {
            return ReadProperty(
                jObj: jObject,
                name: name,
                existingValue: val,
                mapFunc: s => new TimeDuration(UInt64.Parse(s)),
                relativeFunc: (v, acc) => v + acc,
                proportionalFunc: (v, acc) => v * acc
            );
        }

        /*
         * Core Functions
         */

        private static T ReadProperty<T>(this JObject jObj, string name, T existingValue, Func<T, double, T> relativeFunc, Func<T, double, T> proportionalFunc, bool required = false) {
            T newValue = default;

            if (relativeFunc != null && TryRetrieveRelative(jObj, name, out double relativeValue)) {
                return relativeFunc(existingValue, relativeValue);
            }

            if (proportionalFunc != null && TryRetrieveProportional(jObj, name, out double proportionalValue)) {
                if (proportionalValue <= 0 || Math.Abs(proportionalValue - 1) < 0.01) // v <= 0 || v == 1
                    throw new Exception("Invalid proportional scalar");

                return proportionalFunc(existingValue, proportionalValue);
            }

            // When there are no Proportional or Relative tags, just load the property normally.
            if (!jObj.TryGetValue(name, out var normalValue) && required) {
                throw new Exception($"No property {name} found on {jObj}");
            }
            else {
                try {
                    newValue = normalValue.Value<T>();
                }
                catch (FormatException ex) {
                    throw new FormatException(
                        $"Unable to convert value '{normalValue.Value<string>()}' to type of {typeof(T)}", ex);
                }
            }

            return newValue;
        }

        private static TValue ReadProperty<T, TValue>(this JObject jObj, string name, T existingValue, Func<string, TValue> mapFunc,
            Func<T, TValue, TValue> relativeFunc, Func<T, double, TValue> proportionalFunc, bool required = false) {
            TValue newValue = default;

            if (TryRetrieveRelative(jObj, name, out var relativeValue, mapFunc)) {
                return relativeFunc(existingValue, relativeValue);
            }

            if (TryRetrieveProportional<string,double>(jObj, name, out var proportionalValue, double.Parse)) {
                return proportionalFunc(existingValue, proportionalValue);
            }

            // When there are no Proportional/Relative tags, just load the property normally.
            if (!jObj.TryGetValue(name, out var normalValue) && required) {
                throw new Exception($"No property {name} found on {jObj}");
            }
            else {
                try {
                    newValue = mapFunc(normalValue.Value<string>());
                }
                catch (FormatException ex) {
                    throw new FormatException(
                        $"Unable to convert value '{normalValue.Value<string>()}' to type of {typeof(T)}", ex);
                }
            }

            return newValue;
        }

        public static bool TryRetrieveRelative<T>(this JObject jObj, string name, out T value) {
            /*
             * Looking for:
             * "relative": { "weight": 10, "volume": 2" },
             */
            if (jObj.TryGetValue("relative", out var token)) {
                if (token is JObject obj) {
                    value = obj.GetValue(name).Value<T>();
                    return true;
                }
            }

            value = default;
            return false;
        }

        public static bool TryRetrieveRelative<T, TOut>(this JObject jObj, string name, out TOut value, Func<T, TOut> mapFunc) {
            /*
             * Looking for:
             * "proportional": { "weight": 2.0, "volume": 1.5 },
             */
            if (jObj.TryGetValue("relative", out var token)) {
                if (token is JObject obj) {
                    var v = obj.GetValue(name).Value<T>();
                    value = mapFunc(v);
                    return true;
                }
            }

            value = default;
            return false;
        }

        public static bool TryRetrieveProportional<T>(this JObject jObj, string name, out T value) {
            if (jObj.TryGetValue("proportional", out var token)) {
                if (token is JObject obj) {
                    value = obj.GetValue(name).Value<T>();
                    return true;
                }
            }

            value = default;
            return false;
        }

        public static bool TryRetrieveProportional<T, TOut>(this JObject jObj, string name, out TOut value, Func<T, TOut> mapFunc) {
            if (jObj.TryGetValue("proportional", out var token)) {
                if (token is JObject obj) {
                    var v = obj.GetValue(name).Value<T>();
                    value = mapFunc(v);
                    return true;
                }
            }

            value = default;
            return false;
        }
    }

    public static class ExpressionExtensions {
        /// <summary>
        /// Decompose an Expression Tree into parts and return a PropertyInfo if the expression
        /// resolved to a property on a class.
        /// </summary>
        /// <typeparam name="T">Type of the property</typeparam>
        /// <param name="propertyExpression">Lambda pointing to the property</param>
        /// <returns>A PropertyInfo object describing the target</returns>
        public static PropertyInfo GetProperty<T>(this Expression<Func<T>> propertyExpression) {
            if (propertyExpression == null)
                throw new ArgumentNullException(nameof(propertyExpression));

            var body = propertyExpression.Body as MemberExpression;

            if (body == null)
                throw new ArgumentException("Invalid Expression Body", nameof(propertyExpression));

            var property = body.Member as PropertyInfo;

            if (property == null)
                throw new ArgumentException("Argument body is not a property", nameof(propertyExpression));

            return property;
        }

        /// <summary>
        /// Get the value from an expression, allowing for types that could be null.
        /// </summary>
        /// <typeparam name="T">Type of the object to retrieve</typeparam>
        /// <param name="accessor">Lambda to retrieve the object</param>
        /// <param name="defaultValue">A default value to return instead of null</param>
        /// <returns>The objects value or the default value</returns>
        public static T GetValue<T>(this Func<T> accessor, T defaultValue = default(T)) {
            var type = typeof(T);
            bool isNullable = !type.IsValueType || (Nullable.GetUnderlyingType(type) != null);
            T value;
            if (isNullable) {
                var val = accessor();
                value = val != null ? val : defaultValue;
            }
            else {
                value = accessor();
            }

            return value;
        }
    }
}