using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using OctoGhast.Extensions;

namespace OctoGhast {
    public static class MaterialLoader {
        public static Material Deserialize(this Material self, JObject data) {
            return new Material()
            {
                Type = data.GetValueOr("type", default(string)),
                Name = data.GetValueOr("name", default(string)),
                Id = data.GetValueOr("ident", default(string)),

                BashResist = data.GetValueOr("bash_resist", 0),
                CutResist = data.GetValueOr("cut_resist", 0),
                AcidResist = data.GetValueOr("acid_resist", 0),
                ElectricalResist = data.GetValueOr("elec_resist", 0),
                ChipResistance = data.GetValueOr("chip_resist", 0),

                Density = data.GetValueOr("density", 0),
                SalvagedInto = data.GetValueOr("salvaged_into", default(string)),
                RepairedWith = data.GetValueOr("repaired_with", default(string)),
                IsEdible = data.GetValueOr("edible", default(bool)),
                IsSoft = data.GetValueOr("soft", default(bool)),

                Vitamins = data.GetArrayOfPairs("vitamins", (name, val) => new VitaminInfo()
                {
                    VitaminName = name.Value<string>(),
                    VitaminValue = val.Value<float>()
                }).ToList(),

                DamageAdjectives = data.GetArray<string>("dmg_adj").ToList(),
                BashDamageVerb = data.GetValueOr("bash_dmg_verb", default(string)),
                CutDamageVerb = data.GetValueOr("cut_dmg_verb", default(string)),

                BurnData = data.GetArrayOf<BurnData>("burn_data"),
                BurnProducts = data.GetArrayOfPairs("burn_products", (name, amount) =>
                    (name.Value<string>(), amount.Value<int>())),

                CompactsInto = data.GetArray<string>("compacts_into"),
                CompactAccepts = data.GetArray<string>("compact_accepts")
            };
        }

        public static JObject Serialize(this Material data) {
            throw new NotImplementedException();
        }
    }
}