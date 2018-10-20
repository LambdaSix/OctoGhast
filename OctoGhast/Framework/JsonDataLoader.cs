using System;
using Newtonsoft.Json.Linq;
using OctoGhast.Extensions;
using OctoGhast.Units;

namespace OctoGhast.Framework {
    public class JsonDataLoader {
        public int ReadProperty(JObject jObject, string name, int val) {
            return ReadProperty(jObject, name, val, (v, acc) => (int) Math.Floor(v + acc),
                (v, s) => (int) Math.Floor(v * s));
        }

        public double ReadProperty(JObject jObject, string name, double val) {
            return ReadProperty(jObject, name, val, (v, acc) => v + acc, (v, s) => v * s);
        }

        public Volume ReadProperty(JObject jObject, string name, Volume val) {
            return ReadProperty(
                jObj: jObject,
                name: name,
                value: val,
                mapFunc: s => new Volume(s),
                relativeFunc: (v, acc) => (v + acc) as Volume,
                proportionalFunc: (v, acc) => (v * acc) as Volume
            );
        }

        public Mass ReadProperty(JObject jObject, string name, Mass val) {
            return ReadProperty(
                jObj: jObject,
                name: name,
                value: val,
                mapFunc: s => new Mass(s),
                relativeFunc: (v, acc) => (v + acc) as Mass,
                proportionalFunc: (v, acc) => (v * acc) as Mass
            );
        }

        public SoundLevel ReadProperty(JObject jObject, string name, SoundLevel val) {
            return ReadProperty(
                jObj: jObject,
                name: name,
                value: val,
                mapFunc: s => new SoundLevel(s),
                relativeFunc: (v, acc) => (v + acc) as SoundLevel,
                proportionalFunc: (v, acc) => (v * acc) as SoundLevel
            );
        }

        public TimeDuration ReadProperty(JObject jObject, string name, TimeDuration val) {
            return ReadProperty(
                jObj: jObject,
                name: name,
                value: val,
                mapFunc: s => new TimeDuration(UInt64.Parse(s)),
                relativeFunc: (v, acc) => v + acc,
                proportionalFunc: null
            );
        }

        /*
         * Core Functions
         */

        private T ReadProperty<T>(JObject jObj, string name, T value, Func<T, double, T> relativeFunc,
            Func<T, double, T> proportionalFunc) {
            T outValue = default;

            if (relativeFunc != null && TryRetrieveRelative(jObj, name, out double relativeValue)) {
                return relativeFunc(value, relativeValue);
            }

            if (proportionalFunc != null && TryRetrieveProportional(jObj, name, out double proportionalValue)) {
                if (proportionalValue <= 0 || Math.Abs(proportionalValue - 1) < 0.01) // v <= 0 || v == 1
                    throw new Exception("Invalid proportional scalar");

                return proportionalFunc(value, proportionalValue);
            }

            if (!jObj.TryGetValue(name, out var normalValue)) {
                throw new Exception($"No property {name} found on {jObj}");
            }
            else {
                outValue = normalValue.Value<T>();
            }

            return outValue;
        }

        private T ReadProperty<T, TValue>(JObject jObj, string name, T value, Func<string, TValue> mapFunc,
            Func<T, TValue, T> relativeFunc, Func<T, TValue, T> proportionalFunc) {
            T outValue = default;

            if (TryRetrieveRelative(jObj, name, out var relativeValue, mapFunc)) {
                return relativeFunc(value, relativeValue);
            }

            if (TryRetrieveProportional(jObj, name, out var proportionalValue, mapFunc)) {
                return proportionalFunc(value, proportionalValue);
            }

            if (!jObj.TryGetValue(name, out var normalValue)) {
                throw new Exception($"No property {name} found on {jObj}");
            }
            else {
                outValue = normalValue.Value<T>();
            }

            return outValue;
        }

        public bool TryRetrieveRelative<T>(JObject jObj, string name, out T value) {
            if (jObj.TryGetValue("relative", out var token)) {
                if (token is JObject obj) {
                    value = obj.GetValue(name).Value<T>();
                    return true;
                }
            }

            value = default;
            return false;
        }

        public bool TryRetrieveRelative<T, TOut>(JObject jObj, string name, out TOut value, Func<T, TOut> mapFunc) {
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

        public bool TryRetrieveProportional<T>(JObject jObj, string name, out T value) {
            if (jObj.TryGetValue("proportional", out var token)) {
                if (token is JObject obj) {
                    value = obj.GetValue(name).Value<T>();
                    return true;
                }
            }

            value = default;
            return false;
        }

        public bool TryRetrieveProportional<T, TOut>(JObject jObj, string name, out TOut value, Func<T, TOut> mapFunc) {
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
}