using InfiniMap;
using Newtonsoft.Json.Linq;
using OctoGhast.Framework.Mobile;

namespace OctoGhast.Framework.Items.Actions {
    /// <summary>
    /// Defines a base 'use action' function.
    /// These are intended to be a generic action type like 'transform' that takes json parametrization.
    /// </summary>
    public class UseAction
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public UseActionData Data { get; set; }

        public UseAction(string type) {
            Type = type;
        }

        public UseAction(UseActionData data) {
            Data = data;
            Name = data.Name;
            Type = data.HandlerType;
        }

        public virtual (bool success, string message) CanInvoke<T>(UseActionData actionData, BaseCreature player, RLObject<T> item, bool turnTick, WorldSpace position) where T : TemplateType
        {
            return (true, null);
        }

        public virtual int Invoke<T>(UseActionData actionData, BaseCreature player, RLObject<T> item, bool turnTick, WorldSpace position) where T : TemplateType
        {
            return -1;
        }

        public virtual void Load(JObject jObj) { }
    }
}