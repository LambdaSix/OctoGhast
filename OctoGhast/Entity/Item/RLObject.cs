using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OctoGhast.Framework;

namespace OctoGhast {
    public interface IDataObject {
        void Deserialize(JObject jObj);
        JObject Serialize();
    }

    [DataObject("ObjectManager", "Manages Serial allocations for RLObject<>")]
    public class ObjectManager : IDataObject {
        public int CurrentSerial { get; private set; }

        public ObjectManager() { }

        public int NextSerial() => ++CurrentSerial;

        /// <inheritdoc />
        public void Deserialize(JObject jObj) {
            if (jObj.TryGetValue("currentSerial", out var value))
            {
                CurrentSerial = value.Value<int>();
            }
        }

        /// <inheritdoc />
        public JObject Serialize() {
            throw new NotImplementedException();
        }
    }

    public class RuntimeData {
        public Dictionary<string, object> Data { get; set; }

        public T Get<T>(string key, T defaultValue) {
            if (Data.TryGetValue(key, out var value))
                return (T) value;
            return defaultValue;
        }

        public void Put<T>(string key, T value) {
            if (Data.ContainsKey(key))
                throw new ArgumentException($"Key '{key}' already present");
            Data.Add(key, value);
        }

        public void Set<T>(string key, T value) {
            if (Data.ContainsKey(key)) {
                Data[key] = value;
            }
            else {
                Data.Add(key, value);
            }
        }

        public bool Has(string key) => key != null && Data.ContainsKey(key);
    }

    /// <summary>
    /// Base ancestor for any Template's used with RLObject, provides serialization primitives.
    /// </summary>
    public class TemplateType {
        [LoaderInfo("id")]
        public string Id { get; set; }

        [LoaderInfo("abstract")]
        public string Abstract { get; set; }

        [LoaderInfo("type")]
        public string Type { get; set; }

        public virtual string GetIdentifier() => Id ?? Abstract;
        public virtual string GetName(int amount = 1) => "NULL";
    }

    /// <summary>
    /// Non-generic version of RLObject{T}.
    ///
    /// Intended to simplify passing them around from framework to game.
    /// </summary>
    public class RLObject {
        public object TemplateData { get; set; }
        public Type TemplateType { get; set; }
        public RuntimeData RuntimeData { get; }
        public int? Serial { get; }

        public RLObject(object templateData, Type templateType, RuntimeData runtimeData, int serial = -1) {
            TemplateData = templateData;
            TemplateType = templateType;
            RuntimeData = runtimeData;

            // Should someone create a RLObject for no particular reason..
            Serial = serial == -1
                ? World.Instance.Retrieve<ObjectManager>()?.NextSerial()
                : serial;
        }

        public T TemplateDataAs<T>() where T : TemplateType => (TemplateData as T);

        public static RLObject From<T>(RLObject<T> source) where T: TemplateType {
            return new RLObject(source.TemplateData, source.TemplateData.GetType(), source.RuntimeData, source.Serial);
        }

        public static RLObject<T> From<T>(T source) where T:TemplateType{
            return new RLObject<T>(source);
        }

        public RLObject<T> As<T>() where T : TemplateType => new RLObject<T>(TemplateDataAs<T>(), RuntimeData, Serial ?? -1);
    }

    /// <summary>
    /// Base object for an entity that stores runtime data or reads data from an template.
    ///
    /// TemplateData: (Ideally) Immutable data the object shuld use to decide on actions. This object is not serialized.
    /// RuntimeData: A dictionary of mutable state data, this is serialized.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RLObject<T> where T : TemplateType {
        /// <summary>
        /// Template data driving this object
        /// </summary>
        public T TemplateData { get; set; }

        /// <summary>
        /// Runtime data used by systems acting on this object.
        /// </summary>
        public RuntimeData RuntimeData { get; set; }

        /// <summary>
        /// Unique identifier for this object.
        /// </summary>
        public int Serial { get; set; }

        public RLObject(T data, int? serial = null) {
            TemplateData = data;
            Serial = serial ?? World.Instance.Retrieve<ObjectManager>().NextSerial();
        }

        public RLObject(T data, RuntimeData runtime, int serial) {
            TemplateData = data;
            RuntimeData = runtime;
            Serial = serial;
        }

        public virtual string GetName() => TemplateData.GetName();

        /// <summary>
        /// Create a JObject describing this RLObject.
        /// Preserves template information for later rehydration.
        /// </summary>
        /// <returns></returns>
        public virtual JObject Serialize() {
            var jObj = new JObject
            {
                {"serial", Serial},
                {"templateType", typeof(T).FullName},
                {"templateID", TemplateData.GetIdentifier()}
            };

            // Runtime data should contain everything required by the implementing mod
            var runtime = JObject.FromObject(RuntimeData.Data);
            jObj.Add("runtimeData", runtime);

            return jObj;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        public virtual void Deserialize(JObject source) {
            // Valid object?
            Debug.Assert(source.ContainsKey("serial") && source["serial"].Type == JTokenType.Integer);
            Debug.Assert(source.ContainsKey("templateType") && source["templateType"].Type == JTokenType.String);
            Debug.Assert(source.ContainsKey("templateId") && source["templateId"].Type == JTokenType.String);
            Debug.Assert(source.ContainsKey("runtimeData") && source["runtimeData"].Type == JTokenType.Object);

            Serial = source["serial"].Value<int>();
            var templateType = source["templateType"].Value<string>();

            // Deserialize should be called after 'new RLObject<FoobarTemplate>(templateData)'
            // So we don't need to hydrate the template, only check it's the correct type.
            if (typeof(T).FullName != templateType)
                throw new TypeLoadException(
                    $"Mismatch between expected type '{typeof(T).FullName}' and rehydrated type '{templateType}'");

            // And the ID's match.
            var templateId = source["templateId"].Value<string>();
            if (TemplateData.GetIdentifier() != templateId)
                throw new TypeLoadException(
                    $"Mismatch between expected id '{TemplateData.GetIdentifier()}' and rehydrated type '{templateId}'");

            // Then rehydate the runtime data.
            RuntimeData.Data = source["runtimeData"].ToObject<Dictionary<string, object>>();

        }

        /// <summary>
        /// Make a copy of the Data set belonging to this item.
        /// </summary>
        /// <returns>A new instance which is a Copy of Data</returns>
        public T CopyData() {
            var str = JsonConvert.SerializeObject(TemplateData);
            return JsonConvert.DeserializeObject<T>(str);
        }

        public async Task<T> CopyDataAsync() {
            return await Task.Run(CopyData);
        }

        public static explicit operator RLObject(RLObject<T> other) => RLObject.From(other);

        public static implicit operator RLObject<TemplateType>(RLObject<T> other) => new RLObject<TemplateType>(other.TemplateData, other.RuntimeData, other.Serial);
    }
}