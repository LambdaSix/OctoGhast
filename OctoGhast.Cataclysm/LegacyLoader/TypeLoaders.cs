using System;
using Newtonsoft.Json.Linq;
using OctoGhast.Framework;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public interface ITypeLoader<T> : ITypeLoader {
        T Load(JObject jObj, T existing = default(T));
    }

    public class ContainerTypeLoader : ITypeLoader<SlotContainer> {
        public SlotContainer Load(JObject jObj, SlotContainer existing) {
            return new SlotContainer()
            {
                Contains = jObj.ReadProperty(() => existing.Contains),
                Seals = jObj.ReadProperty(() => existing.Seals),
                Watertight = jObj.ReadProperty(() => existing.Watertight),
                Preserves = jObj.ReadProperty(() => existing.Preserves),
                UnsealsInto = jObj.ReadProperty(() => existing.UnsealsInto)
            };
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            return Load(data, existing as SlotContainer);
        }
    }

    public class ToolTypeLoader : ITypeLoader<SlotTool> {
        public SlotTool Load(JObject jObj, SlotTool existing) {
            return new SlotTool()
            {
                AmmoId = jObj.ReadProperty(() => existing.AmmoId),
                RevertsTo = jObj.ReadProperty(() => existing.RevertsTo),
                RevertMessage = jObj.ReadProperty(() => existing.RevertMessage),
                SubType = jObj.ReadProperty(() => existing.SubType),
                MaxCharges = jObj.ReadProperty(() => existing.MaxCharges),
                DefaultCharges = jObj.ReadProperty(() => existing.DefaultCharges),
                RandomCharges = jObj.ReadProperty(() => existing.RandomCharges),
                TurnsPerCharge = jObj.ReadProperty(() => existing.TurnsPerCharge),
            };
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            return Load(data, existing as SlotTool);
        }
    }

    public class VitaminLoader : ITypeLoader<VitaminInfo>
    {
        /// <inheritdoc />
        public VitaminInfo Load(JObject jObj, VitaminInfo existing = default(VitaminInfo)) {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            throw new NotImplementedException();
        }
    }
}