using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MiscUtil;
using Newtonsoft.Json.Linq;
using OctoGhast.Entity;
using OctoGhast.Extensions.FastExpressionCompiler;
using OctoGhast.Units;

namespace OctoGhast.Framework {
    public interface ITypeLoader {
        object Load(JObject data, object existing);
    }

    public static class JsonDataLoader {
        private static Dictionary<Type, Func<JObject, string, object, Type[], object>> _conversionMap =
            new Dictionary<Type, Func<JObject, string, object, Type[], object>>()
            {
                [typeof(int)] = (jObj, name, val, _) => ReadProperty(jObj, name, (int) val),
                [typeof(long)] = (jObj, name, val, _) => ReadProperty(jObj, name, (long) val),
                [typeof(double)] = (jObj, name, val, _) => ReadProperty(jObj, name, (double) val),
                [typeof(float)] = (jObj, name, val, _) => ReadProperty(jObj, name, (float) val),
                [typeof(bool)] = (jObj, name, val, _) => ReadProperty(jObj, name, (bool) val),
                [typeof(string)] = (jObj, name, val, _) => ReadProperty(jObj, name),
                [typeof(Volume)] = (jObj, name, val, _) => ReadProperty<Volume>(jObj, name, val as Volume),
                [typeof(Mass)] = (jObj, name, val, _) => ReadProperty<Mass>(jObj, name, val as Mass),
                [typeof(Energy)] = (jObj, name, val, _) => ReadProperty<Energy>(jObj, name, val as Energy),
                [typeof(SoundLevel)] = (jObj, name, val, _) => ReadProperty<SoundLevel>(jObj, name, val as SoundLevel),
                [typeof(TimeDuration)] = (jObj, name, val, _) => ReadProperty<TimeDuration>(jObj, name, val as TimeDuration),
                [typeof(Nullable<>)] = (jObj, name, val, types) => ReadNullable(jObj, name, val, typeof(Nullable<>), types),
                // Just handle all kinds of IEnumerable with an open type.
                [typeof(IEnumerable<>)] = (jObj, name, val, types) => ReadEnumerable(jObj, name, val, typeof(IEnumerable<>), types),
                [typeof(Dictionary<object, IEnumerable<object>>)] = (jObj, name, val, types) => ReadNestedDictionary(jObj, name, val, typeof(Dictionary<,>), types),
                // Defined as an open type to handle most 'pure' dictionarys, string:string, string:int, etc.
                [typeof(Dictionary<,>)] = (jObj, name, val, types) => ReadDictionary(jObj, name, val, typeof(Dictionary<,>), types),
                // Special case, string converts to a given StringID<>, so just make it open to not need a constructed type for every variant.
                [typeof(StringID<>)] = (jObj, name, val, types) => ReadProperty(jObj, name, val, typeof(StringID<>), types)
            };

        private static Dictionary<Type,Func<JToken, Type, object>> _castingMap = new Dictionary<Type, Func<JToken, Type, object>>()
        {
            [typeof(StringID<>)] = ConvertStringID,
            [typeof(Volume)] = ConvertVolume,
            [typeof(Mass)] = ConvertMass,
            [typeof(Energy)] = ConvertEnergy,
            [typeof(SoundLevel)] = ConvertSoundLevel,
            [typeof(TimeDuration)] = ConvertTimeDuration,
        };

        public static void RegisterTypeLoader(Type typeDef, Func<JObject, string, object, Type[], object> func) {
            if (_conversionMap.ContainsKey(typeDef))
                throw new ArgumentException($"Type Loader already contains loader for {typeDef.Name}");
            _conversionMap.Add(typeDef, func);
        }

        public static void RegisterConverter(Type typeDef, Func<JToken, Type, object> func) {
            if (_castingMap.ContainsKey(typeDef))
                throw new ArgumentException($"Converter already contains conversion for {typeof(JToken)} -> {typeDef}");
            _castingMap.Add(typeDef, func);
        }

        private static object ConvertStringID(JToken input, Type type) {
            if (input.Value<string>() is string str && type.GetGenericTypeDefinition().IsAssignableFrom(typeof(StringID<>))) {
                var ctor = type.GetConstructor(new Type[] {typeof(string)});
                return Convert.ChangeType(ctor.Invoke(new object[] {str}), type);
            }

            return input;
        }

        private static object ConvertVolume(JToken input, Type type) {
            if (input.Value<string>() is string str && type.IsAssignableFrom(typeof(Volume))) {
                return new Volume(str);
            }

            return input;
        }

        private static object ConvertMass(JToken input, Type type) {
            if (input.Value<string>() is string str && type.IsAssignableFrom(typeof(Mass))) {
                return new Mass(str);
            }

            return input;
        }

        private static object ConvertEnergy(JToken input, Type type) {
            if (input.Value<string>() is string str && type.IsAssignableFrom(typeof(Energy))) {
                return new Energy(str);
            }

            return input;
        }

        private static object ConvertSoundLevel(JToken input, Type type) {
            if (input.Value<string>() is string str && type.IsAssignableFrom(typeof(SoundLevel))) {
                return new SoundLevel(str);
            }

            return input;
        }

        private static object ConvertTimeDuration(JToken input, Type type) {
            if (input.Type == JTokenType.Integer && input.Value<long>() is long val && type.IsAssignableFrom(typeof(TimeDuration))) {
                return new TimeDuration(val);
            } else if (input.Type == JTokenType.String && input.Value<string>() is string str && type.IsAssignableFrom(typeof(TimeDuration))) {
                return new TimeDuration(str);
            }

            return input;
        }

        public static int ReadProperty(this JObject jObject, string name, int val) {
            var res = ReadProperty<int?>(jObject, name, val, (v, acc) => (int?) (v + acc), (v, s) => (int?) (v * s));
            return res ?? val;
        }

        public static double ReadProperty(this JObject jObject, string name, double val) {
            var res = ReadProperty<double?>(jObject, name, val, (v, acc) => v + acc, (v, s) => v * s);
            return res ?? val;
        }

        public static float ReadProperty(this JObject jObject, string name, float val)
        {
            var res = ReadProperty<double?>(jObject, name, val, (v, acc) => v + acc, (v, s) => v * s);
            return (float) (res ?? val);
        }

        public static long ReadProperty(this JObject jObject, string name, long val) {
            var res = ReadProperty<long?>(jObject, name, val, (v, acc) => (int?) (v + acc), (v, s) => (int?) (v * s));
            return res ?? val;
        }

        public static bool ReadProperty(this JObject jObject, string name, bool val) {
            // Bool has no relative/proportional support.
            return jObject.TryGetValue(name, out var value)
                ? value.Value<bool>()
                : val;
        }

        public static bool HasObject(this JObject jObject, string name) {
            // Sometimes in cata data files, arrays are secretly objects.. basically we want something that isn't a value-type here.
            return jObject.TryGetValue(name, out var value) && (value.Type == JTokenType.Object || value.Type == JTokenType.Array);
        }

        public static T ReadProperty<T>(this JObject jObject, string name, T val) {
            (Func<JObject, string, object, Type[], object> func, Type type, Type[] arguments) args = (null, null, null);

            var tType = typeof(T);
            var isGeneric = tType.IsGenericType;
            var isSequenceType = isGeneric && tType.GetGenericTypeDefinition().IsAssignableFrom(typeof(IEnumerable<>));
            var isDictionary = isGeneric && tType.GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<,>));
            var isNullable = isGeneric && tType.GetGenericTypeDefinition().IsAssignableFrom(typeof(Nullable<>));

            var genericArgs = isGeneric ? tType.GetGenericArguments() : new Type[0];

            var isNestedSequenceType = isGeneric && genericArgs.Length == 2
                                       && genericArgs[1].IsGenericType
                                       && genericArgs[1].GetGenericTypeDefinition().IsAssignableFrom(typeof(IEnumerable<>));

            // Special case IEnumerable<>, Dictionary<,>, and Dictionary<,IEnumerable<>>
            if (isSequenceType) {
                args = (_conversionMap[typeof(IEnumerable<>)], tType, tType.GetGenericArguments());
            } else if (isDictionary && !isNestedSequenceType) {
                args = (_conversionMap[typeof(Dictionary<,>)], tType, tType.GetGenericArguments());
            }
            else if (isDictionary && isNestedSequenceType) {
                args =(_conversionMap[typeof(Dictionary<object,IEnumerable<object>>)], tType, tType.GetGenericArguments());
            }
            else {
                foreach (var (type, value) in _conversionMap.Select(s => (s.Key, s.Value))) {
                    // Exact type matches short circuit. No need to work anything else out.
                    if (type == tType) {
                        args = (value, tType, new Type[0]);
                        break;
                    }

                    if (isGeneric && tType.GetGenericTypeDefinition().IsAssignableFrom(type)) {
                        args = (value, tType, tType.GetGenericArguments());
                        break;
                    }
                }
            }

            if (args == (null, null, null))
                throw new ArgumentException($"Unable to deduce built-in loader for type: '{tType}");

            var result = args.func(jObject, name, val, args.arguments);

            if (result == null)
                return default;

            if (!args.type.IsGenericType) {
                return (T) result;
            }

            // Handle sequences
            var typeOfT = args.type.GetGenericTypeDefinition().MakeGenericType(args.arguments);
            var isInterface = args.type.IsInterface;

            if (args.type.IsGenericType && result.GetType().IsAssignableFrom(typeOfT)) {
                return (T) result;
            }
            else if (isInterface && (result.GetType().GetInterface(typeOfT.Name) != null)) {
                return (T) result;
            }
            else if (isNullable && result.GetType().IsValueType) {
                return (T) result;
            }
            else {
                var newType = typeOfT;
                return (T) Convert.ChangeType(result, newType);
            }
        }

        public static T ReadNullable<T>(this JObject jObject, string name, T existingValue, Type realType, Type[] types) {
            var paramTypes = new[] {typeof(JObject), typeof(string), typeof(Nullable<>)};

            if (realType.IsGenericType && types.Any()) {
                var method = FindGenericMethod(nameof(ReadNullable), paramTypes);
                var genericMethod = method.MakeGenericMethod(types);
                return (T) genericMethod.Invoke(null, new object[] {jObject, name, existingValue});
            }

            return default;
        }

        public static T? ReadNullable<T>(this JObject jObj, string name, T? existingValue) where T:struct {
            var innerTypes = typeof(T).GenericTypeArguments;
            if (innerTypes.Length > 1)
                throw new Exception($"Invalid number of generic arguments to Nullable<T>: {String.Join(",", innerTypes.AsEnumerable())}");

            // Find one the regular ReadProperty(JObject, string, [int,string,bool,...] methods
            var paramTypes = new[] {typeof(JObject), typeof(string), typeof(T)};
            var method = FindMethod(nameof(ReadProperty), paramTypes);

            // Then lift the regular type to the nullable type.
            return (T?) method.Invoke(null, new object[] {jObj, name, existingValue});
        }

        public static object ReadDictionary<T>(this JObject jObject, string name, T existingValue, Type realType, Type[] types) {
            var paramTypes = new[] {typeof(JObject), typeof(string), typeof(Dictionary<,>)};

            if (realType.IsGenericTypeDefinition && types.Any())
            {
                // Construct a version of ReadEnumerable<T> : IEnumerable<T> where T == type
                var method = FindGenericMethod(nameof(ReadDictionary), paramTypes, genericArgs: 2);

                var genericMethod = method.MakeGenericMethod(types);
                return genericMethod.Invoke(null, new object[] { jObject, name, existingValue });
            }

            return default;
        }

        public static Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(JObject jObj, string name, Dictionary<TKey,TValue> existingValue) {
            Dictionary<TInnerKey, TInnerValue> RetrieveDictionary<TInnerKey,TInnerValue>(JContainer input) {
                IEnumerable<(TInnerKey key,TInnerValue value)> ExtractPairs(JToken jToken, Func<JToken,TInnerKey> keyMap = null,
                    Func<JToken, TInnerValue> valueMap = null) {

                    if (jToken.First is JArray) {
                        foreach (var subArray in jToken) {
                            switch (subArray.Count()) {
                                case 2:
                                    yield return (subArray[0].Value<TInnerKey>(), subArray[1].Value<TInnerValue>());
                                    break;
                                case 1:
                                    yield return (subArray[0].Value<TInnerKey>(), default);
                                    break;
                            }
                        }
                    }
                    else {
                        yield return (
                            keyMap != null
                                ? keyMap(jToken.ElementAtOrDefault(0))
                                : jToken.ElementAtOrDefault(0).Value<TInnerKey>(),
                            valueMap != null
                                ? valueMap(jToken)
                                : jToken.ElementAtOrDefault(1).Value<TInnerValue>()
                        );
                    }
                }

                var res = Enumerable.Empty<(TInnerKey key, TInnerValue value)>();

                Func<JToken, TInnerKey> keyConverter = null;
                Func<JToken, TInnerValue> valueConverter = null;

                if (_castingMap.TryGetValue(OpenType<TInnerKey>(), out var keyFunc))
                    keyConverter = t => (TInnerKey) keyFunc(t, typeof(TInnerKey));
                if (_castingMap.TryGetValue(OpenType<TInnerValue>(), out var valueFunc))
                    valueConverter = t => (TInnerValue) valueFunc(t, typeof(TInnerValue));
                    

                switch (input) {
                    case JProperty rootProperty:
                    {
                        res = rootProperty.Children().Children().SelectMany(t => ExtractPairs(t, keyConverter, valueConverter));
                        break;
                    }
                    case JArray rootObject:
                    {
                        res = rootObject.Children().SelectMany(t => ExtractPairs(t, keyConverter, valueConverter));
                        break;
                    }
                }

                return res.ToDictionary(pair => pair.key, pair => pair.value);
            }

            Dictionary<TKey,TValue> dict = null;

            if (jObj.TryGetValue(name, out var token) && token.Type != JTokenType.Array)
                dict = jObj.GetValue(name).ToObject<Dictionary<TKey, TValue>>();

            dict = existingValue ?? dict ?? RetrieveDictionary<TKey,TValue>(jObj.GetValue(name)?.Parent);

            if (TryRetrieveDeletesObject(jObj, name, out var deletionSeq, RetrieveDictionary<TKey, TValue>)) {
                foreach (var (key, value) in deletionSeq.Select(s => (s.Key, s.Value))) {
                    if (!dict.ContainsKey(key))
                        throw new Exception($"Attempted to delete value {key} from {name} but was not present");

                    if (dict.ContainsKey(key)) {
                        dict.Remove(key);
                    }
                }
            }

            if (TryRetrieveExtendsObject(jObj, name, out var extendSeq, RetrieveDictionary<TKey, TValue>)) {
                foreach (var (key, value) in extendSeq.Select(s => (s.Key, s.Value))) {
                    if (dict.ContainsKey(key))
                        throw new Exception($"Attempted to 'extend' dictionary object that already contains value");

                    if (!dict.ContainsKey(key))
                        dict.Add(key, value);
                }
            }

            if (TryRetrieveRelativeObject(jObj, name, out var relativeValues, RetrieveDictionary<TKey,TValue>)) {
                foreach (var (key,value) in relativeValues.Select(s => (s.Key,s.Value))) {
                    if (dict.ContainsKey(key)) {
                        dict[key] = Operator.Add(dict[key], value);
                    }
                }
            }

            if (TryRetrieveProportionalObject(jObj, name, out var proportionalValues, RetrieveDictionary<TKey,double>)) {
                foreach (var (key, value) in proportionalValues.Select(s => (s.Key, s.Value))) {
                    if (dict.ContainsKey(key)) {
                        var scaledValue = Operator.MultiplyAlternative(Operator.Convert<TValue,double>(dict[key]), value);
                        dict[key] = Operator.Convert<double, TValue>(scaledValue);
                    }
                }
            }

            return dict;
        }

        public static object ReadNestedDictionary<T>(this JObject jObject, string name, T existingValue, Type realType, Type[] types) {
            var paramTypes = new[] {typeof(JObject), typeof(string)};

            if (realType.IsGenericTypeDefinition && types.Any()) {
                var method = FindGenericMethod(nameof(ReadNestedDictionary), paramTypes, 2);
                var innerTypes = new[]
                {
                    types[0],
                    types[1].GenericTypeArguments[0]
                };

                var genericMethod = method.MakeGenericMethod(innerTypes);
                return genericMethod.Invoke(null, new object[] {jObject, name});
            }

            return default;
        }

        public static Dictionary<TKey, IEnumerable<TValue>> ReadNestedDictionary<TKey, TValue>(JObject jObj, string name) {
            Dictionary<TKey, IEnumerable<TValue>> workingSet = new Dictionary<TKey, IEnumerable<TValue>>();
            Func<JToken, Type, object> keyConverter = null;
            Func<JToken, Type, object> valueConverter = null;

            if (typeof(TKey).IsGenericType && !typeof(TKey).IsValueType && !_castingMap.TryGetValue(OpenType<TKey>(), out keyConverter))
                throw new Exception($"Unable to find key converter from {typeof(string)} to {OpenType<TKey>()}");

            if (typeof(TValue).IsGenericType && !typeof(TValue).IsValueType && !_castingMap.TryGetValue(OpenType<TValue>(), out valueConverter))
                throw new Exception($"Unable to find value converter from {typeof(string)} to {OpenType<TValue>()}");


            var rootObject = jObj.GetValue(name);
            if (rootObject is JArray) {
                var pairs = rootObject.Zip(rootObject.Skip(1), Tuple.Create);

                foreach (var (jKey, jValue) in pairs.Where(s => s.Item1.Type != JTokenType.Array)) {
                    var values = jValue;
                    var set = Enumerable.Empty<TValue>();

                    if (values is JArray subArray) {
                        var tempSet = subArray.Values<string>();
                        set = tempSet.Select(s => (TValue) keyConverter(s, typeof(TValue)));
                    }

                    var key = (TKey)keyConverter(jKey.Value<string>(), typeof(TKey));


                    workingSet.Add(key, set.ToList());
                }
            }

            return workingSet;
        }

        private static bool ShouldLoad(JObject jObj, string propertyName) {
            if (jObj.ContainsKey(propertyName))
                return true;

            // Check if there are Proportional/Relative/Extend/Delete properties
            if (jObj.TryGetValue("relative", out var relative))
                if (((JObject) relative).ContainsKey(propertyName))
                    return true;
            if (jObj.TryGetValue("proportional", out var proportional))
                if (((JObject) proportional).ContainsKey(propertyName))
                    return true;
            if (jObj.TryGetValue("extend", out var extend))
                if (((JObject) extend).ContainsKey(propertyName))
                    return true;
            if (jObj.TryGetValue("delete", out var delete))
                if (((JObject) delete).ContainsKey(propertyName))
                    return true;

            return false;
        }

        public static TOut ReadProperty<T, TOut>(this JObject jObject, Expression<Func<T>> expression, Func<T, TOut> mapFunc) {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            if (mapFunc == null) throw new ArgumentNullException(nameof(mapFunc));

            return mapFunc.Invoke(jObject.ReadProperty(expression));
        }

        public static T ReadProperty<T>(this JObject jObject, Expression<Func<T>> expression) {
            var propertyInfo = expression.GetProperty();
            var attr = propertyInfo.GetCustomAttribute<LoaderInfoAttribute>();

            if (attr == null)
                throw new Exception($"Missing attribute on {expression.GetRootObject()}{propertyInfo.Name}");

            T defaultValue = default(T);
            if (attr.DefaultValue is string str && propertyInfo.PropertyType == typeof(string)) {
                // Bit of a hack but if we're a string, then be an actual string for conversion.
                defaultValue = Operator.Convert<string, T>(str);
            }
            else if (attr.DefaultValue is string && propertyInfo.PropertyType != typeof(string)) {
                // Try and use one of the conversion methods
                if (_castingMap.TryGetValue(OpenType<T>(), out var func))
                {
                    var token = new JValue(attr.DefaultValue);
                    defaultValue = Operator.Convert<object, T>(func(token, propertyInfo.PropertyType));
                }
            }
            else if (attr.DefaultValue != null && _castingMap.TryGetValue(typeof(T), out var func)) {
                defaultValue = (T) func(new JObject(new JProperty("default", attr.DefaultValue))["default"], typeof(T));
            }
            else {
                if (attr.DefaultValue != null) {
                    defaultValue = Operator.Convert<object, T>(attr.DefaultValue);
                }
                else {
                    defaultValue = default(T);
                }
            }

            // If the property pointed to already has a value, use that as a default.
            var value = expression.GetRootObject() != null
                ? expression.CompileFast().GetValue(defaultValue)
                : defaultValue;

            if (attr.TypeLoader != null) {
                var loader = Activator.CreateInstance(attr.TypeLoader) as ITypeLoader;
                return (T) loader?.Load(jObject, value);

            }

            if (!ShouldLoad(jObject, attr.FieldName))
                return value;

            return ReadProperty(jObject, attr.FieldName, value);
        }

        public static string ReadProperty(this JObject jObject, string name) {
            // No relative/proportional for strings. (?)
            if (jObject.TryGetValue(name, out var value)) {
                return value.Value<string>();
            }

            return default;
        }

        public static object ReadEnumerable<T>(this JObject jObject, string name, T existingValue, Type realType, Type[] types) {
            var paramTypes = new[] {typeof(JObject), typeof(string), typeof(IEnumerable<>)};

            if (realType.IsGenericTypeDefinition && types.Any()) {
                // Construct a version of ReadEnumerable<T> : IEnumerable<T> where T == type
                var method = FindGenericMethod(nameof(ReadEnumerable), paramTypes);

                var genericMethod = method.MakeGenericMethod(types[0]);
                return genericMethod.Invoke(null, new object[] {jObject, name, existingValue});
            }

            return default(T);
        }

        public static IEnumerable<T> ReadEnumerable<T>(this JObject jObj, string name, IEnumerable<T> existingValue) {
            Func<JToken, T> mapFunc = default;

            if (_castingMap.TryGetValue(OpenType<T>(), out var func)) {
                mapFunc = (token) => (T) func(token.Value<string>(), typeof(T));
            }
            return ReadEnumerable(jObj, name, existingValue, mapFunc);
        }

        private static Type OpenType<T>() {
            var openTypeOfT = typeof(T).IsGenericType ? typeof(T).GetGenericTypeDefinition() : typeof(T);
            return openTypeOfT;
        }

        public static IEnumerable<T> ReadEnumerable<T>(this JObject jObj, string name, IEnumerable<T> existingValue, Func<JToken,T> mapFunc) {
            if (existingValue is null)
                existingValue = Enumerable.Empty<T>();

            // TODO: Extend & Delete
            if (jObj.TryRetrieveExtends<T>(name, out var extends)) {
                return existingValue.Concat(extends);
            }

            if (jObj.TryRetrieveDeletes<T>(name, out var deletes)) {
                return existingValue.Except(deletes);
            }

            if (jObj.TryGetValue(name, out var value)) {
                if (value is JArray arr) {
                    return mapFunc != null
                        ? arr.Select(mapFunc)
                        : arr.Values<T>().ToList().AsEnumerable();
                }

                if (value.Type != JTokenType.Array && value.Type != JTokenType.Object) {
                    return mapFunc != null
                        ? new[] {mapFunc(value)}
                        : new[] {value.Value<T>()};
                }
            }

            return existingValue;
        }

        public static object ReadProperty<T>(this JObject jObject, string name, T val, Type realType, Type[] types) {
            var paramTypes = new[] {typeof(JObject), typeof(string)};

            if (realType.IsGenericTypeDefinition && types.Any()) {
                var method = FindGenericMethod(nameof(ReadProperty), paramTypes);

                var genericMethod = method.MakeGenericMethod(types[0]);
                return genericMethod.Invoke(null, new object[] {jObject, name});
            }

            return default(T);
        }

        public static StringID<T> ReadProperty<T>(this JObject jObject, string name) {
            // No relative/proportional for strings.
            if (jObject.TryGetValue(name, out var value) && (value.Type != JTokenType.Array && value.Type != JTokenType.Object)) {
                return new StringID<T>(value.Value<string>());
            }

            return default;
        }

        /*
         * The following methods define a unused generic type then constrain it so the compiler knows how to redirect
         * the invocations from the generic ReadProperty<T>(jObject,string,T) properly rather than going into an infinite loop on itself.
         */
        public static Volume ReadProperty<T>(this JObject jObject, string name, Volume val) where T : Volume {
            return ReadProperty(
                jObj: jObject,
                name: name,
                existingValue: val,
                mapFunc: s => new Volume(s),
                relativeFunc: (v, acc) => new Volume(v + acc),
                proportionalFunc: (v, acc) => new Volume(v * acc)
            );
        }

        public static Mass ReadProperty<T>(this JObject jObject, string name, Mass val) where T : Mass {
            return ReadProperty(
                jObj: jObject,
                name: name,
                existingValue: val,
                mapFunc: s => new Mass(s),
                relativeFunc: (v, acc) => (v + acc),
                proportionalFunc: (v, acc) => (v * acc)
            );
        }

        public static Energy ReadProperty<T>(this JObject jObject, string name, Energy val) where T : Energy
        {
            return ReadProperty(
                jObj: jObject,
                name: name,
                existingValue: val,
                mapFunc: s => new Energy(s),
                relativeFunc: (v, acc) => new Energy((v + acc)),
                proportionalFunc: (v, acc) => new Energy(v * acc)
            );
        }

        public static SoundLevel ReadProperty<T>(this JObject jObject, string name, SoundLevel val) where T:SoundLevel{
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
                relativeFunc: (v, acc) => v.AddRaw(acc),
                proportionalFunc: (v, acc) => v.MultiplyRaw(acc)
            );
        }

        public static TimeDuration ReadProperty<T>(this JObject jObject, string name, TimeDuration val) where T:TimeDuration{
            return ReadProperty(
                jObj: jObject,
                name: name,
                existingValue: val,
                mapFunc: s => new TimeDuration(Int64.Parse(s)),
                relativeFunc: (v, acc) => v + acc,
                proportionalFunc: (v, acc) => v * acc
            );
        }

        /*
         * Core Functions
         */

        private static T ReadProperty<T>(this JObject jObj, string name, T existingValue,
            Func<T, double, T> relativeFunc, Func<T, double, T> proportionalFunc, bool required = false) {
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
            else if (jObj.ContainsKey(name) && normalValue.Type != JTokenType.Array && normalValue.Type != JTokenType.Object) {
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

        public static TValue ReadProperty<T, TValue>(this JObject jObj, string name, T existingValue,
            Func<string, TValue> mapFunc,
            Func<T, TValue, TValue> relativeFunc, Func<T, double, TValue> proportionalFunc, bool required = false) {
            TValue newValue = default;

            if (TryRetrieveRelative(jObj, name, out var relativeValue, mapFunc)) {
                newValue = relativeFunc(existingValue, relativeValue);
                return newValue;
            }

            if (TryRetrieveProportional<string, double>(jObj, name, out var proportionalValue, double.Parse)) {
                newValue = proportionalFunc(existingValue, proportionalValue);
                return newValue;
            }

            // When there are no Proportional/Relative tags, just load the property normally.
            if (!jObj.TryGetValue(name, out var normalValue) && required) {
                throw new Exception($"No property {name} found on {jObj}");
            }
            else {
                try {
                    if (normalValue != null)
                        newValue = mapFunc(normalValue.Value<string>());
                }
                catch (FormatException ex) {
                    throw new FormatException(
                        $"Unable to convert value '{normalValue.Value<string>()}' to type of {typeof(T)}", ex);
                }
            }

            return newValue;
        }

        public static bool TryRetrieveExtends<T>(this JObject jObj, string name, out IEnumerable<T> values) {
            return TryRetrieveEnumerable(jObj, name, "extend", out values);
        }

        public static bool TryRetrieveDeletes<T>(this JObject jObj, string name, out IEnumerable<T> values) {
            return TryRetrieveEnumerable(jObj, name, "delete", out values);
        }

        public static bool TryRetrieveRelativeObject<TOut>(this JObject jObj, string name, out TOut value, Func<JContainer, TOut> objectFunc) {
            return TryRetrieveObject(jObj, name, "relative", out value, objectFunc);
        }

        public static bool TryRetrieveProportionalObject<TOut>(this JObject jObj, string name, out TOut value, Func<JContainer, TOut> objectFunc) {
            return TryRetrieveObject(jObj, name, "proportional", out value, objectFunc);
        }

        public static bool TryRetrieveExtendsObject<TOut>(this JObject jObj, string name, out TOut value, Func<JContainer, TOut> objectFunc) {
            return TryRetrieveObject(jObj, name, "extend", out value, objectFunc);
        }

        public static bool TryRetrieveDeletesObject<TOut>(this JObject jObj, string name, out TOut value, Func<JContainer, TOut> objectFunc)
        {
            return TryRetrieveObject(jObj, name, "delete", out value, objectFunc);
        }

        public static bool TryRetrieveRelative<T>(this JObject jObj, string name, out T value) {
            return TryRetrieve<T, T>(jObj, name, "relative", out value);
        }

        public static bool TryRetrieveRelative<T, TOut>(this JObject jObj, string name, out TOut value,
            Func<T, TOut> mapFunc) {
            return TryRetrieve(jObj, name, "relative", out value, mapFunc);
        }

        public static bool TryRetrieveProportional<T>(this JObject jObj, string name, out T value) {
            return TryRetrieve<T, T>(jObj, name, "proportional", out value);
        }

        public static bool TryRetrieveProportional<T, TOut>(this JObject jObj, string name, out TOut value,
            Func<T, TOut> mapFunc) {
            return TryRetrieve(jObj, name, "proportional", out value, mapFunc);
        }

        private static bool TryRetrieveObject<TOut>(this JObject jObj, string name, string type, out TOut value, Func<JContainer, TOut> objectFunc) {
            if (jObj.TryGetValue(type, out var token)) {
                if (token is JObject obj) {
                    var v = obj.GetValue(name);
                    if (v != null) {
                        value = objectFunc(v.Parent);
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }

        private static bool TryRetrieve<T, TOut>(this JObject jObj, string name, string type, out TOut value,
            Func<T, TOut> mapFunc = null) {
            if (jObj.TryGetValue(type, out var token)) {
                if (token is JObject obj) {
                    var v = obj.GetValue(name);
                    if (v != null) {
                        value = mapFunc != null ? mapFunc(v.Value<T>()) : v.Value<TOut>();
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }

        private static bool TryRetrieveEnumerable<T>(this JObject jObj, string name, string type,
            out IEnumerable<T> values) {
            if (jObj.TryGetValue(type, out var token)) {
                if (token is JObject obj) {
                    var v = obj.GetValue(name);
                    if (v != null) {
                        values = v.Values<T>();
                        return true;
                    }
                }
            }

            values = default;
            return false;
        }

        private static Dictionary<(string, Type[],int), MethodInfo> genericMethodCache = new Dictionary<(string, Type[],int), MethodInfo>();
        public static void ClearGenericMethodCache() => genericMethodCache.Clear();
        private static MethodInfo FindGenericMethod(string name, Type[] paramTypes, int genericArgs = 1)
        {
            if (genericMethodCache.TryGetValue((name, paramTypes, genericArgs), out var value)) {
                return value;
            }

            var methodInfos = typeof(JsonDataLoader).GetMethods()
                .Where(s => s.Name == name)
                .Select(m => new
                {
                    Method = m,
                    Params = m.GetParameters().Select(s => s.ParameterType),
                    Args = m.GetGenericArguments()
                });
            var methodInfo = methodInfos
                .Where(x => x.Params.Count() == paramTypes.Length
                            && x.Args.Length == genericArgs
                            && x.Params.SequenceEqual(paramTypes, new SimpleTypeComparer())
                )
                .Select(x => x.Method);
            var method = methodInfo.First();

            genericMethodCache.Add((name, paramTypes, genericArgs), method);
            return method;
        }

        private static Dictionary<(string, Type[]), MethodInfo> methodCache = new Dictionary<(string, Type[]), MethodInfo>();
        public static void ClearMethodCache() => methodCache.Clear();

        private static MethodInfo FindMethod(string name, Type[] paramTypes) {
            if (methodCache.TryGetValue((name,paramTypes), out var value)) {
                return value;
            }

            var methodInfos = typeof(JsonDataLoader).GetMethods()
                .Where(s => s.Name == name)
                .Select(m => new
                {
                    Method = m,
                    Params = m.GetParameters().Select(s => s.ParameterType)
                });
            var methodInfo = methodInfos
                .Where(x => x.Params.Count() == paramTypes.Length
                            && x.Params.SequenceEqual(paramTypes, new SimpleTypeComparer())
                )
                .Select(x => x.Method);
            var method = methodInfo.First();

            methodCache.Add((name, paramTypes), method);
            return method;
        }
    }

    internal class SimpleTypeComparer : IEqualityComparer<Type> {
        /// <inheritdoc />
        public bool Equals(Type x, Type y) {
            return x?.Name == y?.Name;
        }

        /// <inheritdoc />
        public int GetHashCode(Type obj) {
            return obj.GetHashCode();
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
        public static T GetValue<T>(this Func<T> accessor, T defaultValue = default(T))
        {
            var type = typeof(T);
            bool isNullable = !type.IsValueType;
            T value;
            if (isNullable)
            {
                var val = accessor();
                value = val != null ? val : defaultValue;
            }
            else
            {
                value = accessor();
            }
            return value;
        }

        /// <summary>
        /// Decompose an Expression Tree into parts and return the root object of the expression.
        /// That is, for an expression <code>() => MyFoo.Property.SubValue.Value</code> return a reference
        /// to <code>MyFoo</code>
        /// </summary>
        /// <param name="propertyExpression">Lambda pointing to the property to retrieve the root object from</param>
        /// <returns></returns>
        public static object GetRootObject<T>(this Expression<Func<T>> propertyExpression)
        {
            MemberExpression body;
            switch (propertyExpression.Body.NodeType)
            {
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    body = ((propertyExpression.Body is UnaryExpression ue) ? ue.Operand : null) as MemberExpression;
                    break;
                default:
                    body = propertyExpression.Body as MemberExpression;
                    break;
            }

            while (body.Expression is MemberExpression)
                body = (MemberExpression)body.Expression;

            if (!(body.Expression is ConstantExpression rootObject))
                return null;

            if (body.Member.MemberType == MemberTypes.Property)
            {
                var propInfo = body.Member as PropertyInfo;
                return propInfo != null
                    ? propInfo.GetValue(rootObject.Value)
                    : null;
            }

            if (body.Member.MemberType == MemberTypes.Field)
            {
                var fieldInfo = body.Member as FieldInfo;
                return fieldInfo != null
                    ? fieldInfo.GetValue(rootObject.Value)
                    : null;
            }

            return null;
        }
    }
}