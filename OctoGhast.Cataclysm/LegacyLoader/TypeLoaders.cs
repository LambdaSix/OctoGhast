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

    public class DamageInfoTypeLoader : ITypeLoader<DamageInfo> {
        /// <inheritdoc />
        public DamageInfo Load(JObject jObj, DamageInfo existing = default(DamageInfo)) {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            throw new NotImplementedException();
        }
    }

    public class ComestibleTypeLoader : ITypeLoader<SlotComestible> {
        /// <inheritdoc />
        public SlotComestible Load(JObject jObj, SlotComestible existing = default(SlotComestible)) {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            throw new NotImplementedException();
        }
    }

    public class BrewableTypeLoader : ITypeLoader<SlotBrewable> {
        /// <inheritdoc />
        public SlotBrewable Load(JObject jObj, SlotBrewable existing = default(SlotBrewable)) {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            throw new NotImplementedException();
        }
    }

    public class ArmorTypeLoader : ITypeLoader<SlotArmor> {
        /// <inheritdoc />
        public SlotArmor Load(JObject jObj, SlotArmor existing = default(SlotArmor)) {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            throw new NotImplementedException();
        }
    }

    public class BookTypeLoader : ITypeLoader<SlotBook> {
        /// <inheritdoc />
        public SlotBook Load(JObject jObj, SlotBook existing = default(SlotBook)) {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            throw new NotImplementedException();
        }
    }

    public class ModTypeLoader : ITypeLoader<SlotMod> {
        /// <inheritdoc />
        public SlotMod Load(JObject jObj, SlotMod existing = default(SlotMod)) {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            throw new NotImplementedException();
        }
    }

    public class EngineTypeLoader : ITypeLoader<SlotEngine> {
        /// <inheritdoc />
        public SlotEngine Load(JObject jObj, SlotEngine existing = default(SlotEngine)) {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            throw new NotImplementedException();
        }
    }

    public class WheelTypeLoader : ITypeLoader<SlotWheel> {
        /// <inheritdoc />
        public SlotWheel Load(JObject jObj, SlotWheel existing = default(SlotWheel)) {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            throw new NotImplementedException();
        }
    }

    public class FuelTypeLoader : ITypeLoader<SlotFuel> {
        /// <inheritdoc />
        public SlotFuel Load(JObject jObj, SlotFuel existing = default(SlotFuel)) {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            throw new NotImplementedException();
        }
    }

    public class GunTypeLoader : ITypeLoader<SlotGun> {
        /// <inheritdoc />
        public SlotGun Load(JObject jObj, SlotGun existing = default(SlotGun)) {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            throw new NotImplementedException();
        }
    }

    public class GunModTypeLoader : ITypeLoader<SlotGunMod> {
        /// <inheritdoc />
        public SlotGunMod Load(JObject jObj, SlotGunMod existing = default(SlotGunMod)) {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            throw new NotImplementedException();
        }
    }

    public class MagazineTypeLoader : ITypeLoader<SlotMagazine> {
        /// <inheritdoc />
        public SlotMagazine Load(JObject jObj, SlotMagazine existing = default(SlotMagazine)) {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
             throw new NotImplementedException();
        }
    }

    public class BionicTypeLoader : ITypeLoader<SlotBionic> {
        /// <inheritdoc />
        public SlotBionic Load(JObject jObj, SlotBionic existing = default(SlotBionic)) {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            throw new NotImplementedException();
        }
    }

    public class AmmoTypeLoader : ITypeLoader<SlotAmmo> {
        /// <inheritdoc />
        public SlotAmmo Load(JObject jObj, SlotAmmo existing = default(SlotAmmo)) {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            throw new NotImplementedException();
        }
    }

    public class SeedTypeLoader : ITypeLoader<SlotSeed> {
        /// <inheritdoc />
        public SlotSeed Load(JObject jObj, SlotSeed existing = default(SlotSeed)) {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            throw new NotImplementedException();
        }
    }
}