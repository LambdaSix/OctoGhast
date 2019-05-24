using System;
using Newtonsoft.Json.Linq;

namespace OctoGhast.Framework.Items.Actions {
    public class UseActionData : IEquatable<UseActionData>
    {
        public string Name { get; }

        /// <summary>
        /// Defines the type of the iuse.
        /// Native is a built-in IUSE handler.
        /// Otherwise it's the UseAction handler.
        /// </summary>
        public UseActionType Type { get; }
        public string HandlerType { get; }
        public JObject Data { get; }

        public UseActionData(JToken data)
        {
            if (data == null)
                return;

            if (data.Type == JTokenType.String)
            {
                Name = data.Value<string>();
                Type = UseActionType.Native;
            }
            else if (data.Type == JTokenType.Object)
            {
                data = data as JObject
                       ?? throw new ArgumentException($"data was a JObject but could not be cast to a JObject");

                Type = UseActionType.Handler;
                HandlerType = data.HasValues ? data["type"].Value<string>() : null;
                Name = HandlerType;
                Data = (JObject)data;
            }
        }

        public UseActionData(string name, UseActionType type = UseActionType.Native, JObject data = null) : this(data)
        {
            Name = name;
            Type = type;
        }

        public bool Equals(UseActionData other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return string.Equals(Name, other.Name)
                   && Type == other.Type
                   && string.Equals(HandlerType, other.HandlerType)
                   && Equals(Data?.ToString(), other.Data?.ToString());
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((UseActionData)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int)Type;
                hashCode = (hashCode * 397) ^ (HandlerType != null ? HandlerType.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Data != null ? Data.ToString().GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}