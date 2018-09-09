using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OctoGhast.Extensions;

namespace OctoGhast {
    public class AmmoType {
        public string Type { get; }

        public AmmoType(string type) {
            Type = type;
        }

        /// <inheritdoc />
        public override string ToString() {
            return Type;
        }
    }

    public class Material : BaseTemplate {
        public string Name { get; set; }
        public string Id { get; set; }
        public int BashResist { get; set; }
        public int CutResist { get; set; }
        public int AcidResist { get; set; }
        public int ElectricalResist { get;set; }
        public int FireResist { get; set; }
        public int ChipResistance { get; set; }

        public int Density { get; set; }
        public string SalvagedInto { get; set; }
        public string RepairedWith { get; set; }
        public bool IsEdible { get; set; }
        public bool IsSoft { get; set; }

        public IEnumerable<VitaminInfo> Vitamins { get; set; }
        public IEnumerable<string> DamageAdjectives { get; set; }
        public string BashDamageVerb { get; set; }
        public string CutDamageVerb { get; set; }
        public IEnumerable<BurnData> BurnData { get; set; }
        public IEnumerable<(string Name,int Amount)> BurnProducts { get; set; }
        public IEnumerable<string> CompactsInto { get; set; }
        public IEnumerable<string> CompactAccepts { get; set; }

        /// <inheritdoc />
        public Material(JObject data) : base(data) {
            Name = data.GetValueOr("name", default(string));
            Id = data.GetValueOr("ident", default(string));

            BashResist = data.GetValueOr("bash_resist", 0);
            CutResist = data.GetValueOr("cut_resist", 0);
            AcidResist = data.GetValueOr("acid_resist", 0);
            ElectricalResist = data.GetValueOr("elec_resist", 0);
            ChipResistance = data.GetValueOr("chip_resist", 0);

            Density = data.GetValueOr("density", 0);
            SalvagedInto = data.GetValueOr("salvaged_into", default(string));
            RepairedWith = data.GetValueOr("repaired_with", default(string));
            IsEdible = data.GetValueOr("edible", default(bool));
            IsSoft = data.GetValueOr("soft", default(bool));

            Vitamins = data.GetArrayOfPairs("vitamins", (name, val) => new VitaminInfo()
            {
                VitaminName = name.Value<string>(),
                VitaminValue = val.Value<float>()
            }).ToList();

            DamageAdjectives = data.GetArray<string>("dmg_adj").ToList();
            BashDamageVerb = data.GetValueOr("bash_dmg_verb", default(string));
            CutDamageVerb = data.GetValueOr("cut_dmg_verb", default(string));

            BurnData = data.GetArrayOf<BurnData>("burn_data");
            BurnProducts = data.GetArrayOfPairs("burn_products", (name, amount) =>
                (name.Value<string>(), amount.Value<int>()));

            CompactsInto = data.GetArray<string>("compacts_into");
            CompactAccepts = data.GetArray<string>("compact_accepts");
        }

        public Material() : base(null) { }

        public int GetResistance(DamageType type) {
            switch (type) {
                case DamageType.Debug:
                case DamageType.Biological:
                    return 0;
                case DamageType.Bash:
                    return BashResist;
                case DamageType.Cut:
                    return CutResist;
                case DamageType.Acid:
                    return AcidResist;
                case DamageType.Stab:
                    return CutResist; // TODO: check?
                case DamageType.Heat:
                    return FireResist;
                case DamageType.Cold:
                    return 0; // TODO: Cold resistance?
                case DamageType.Electric:
                    return ElectricalResist;
            }
            return -1;
        }

        public string DamageAdjective(int damage) {
            if (damage <= 0)
                return String.Empty;
            return DamageAdjectives.ElementAt(Math.Min(4,))
        }
    }

    public class VitaminInfo {
        public string VitaminName { get; set; }
        public float VitaminValue { get; set; }
    }

    public class BurnData {
        public int Fuel { get; set; }
        public int Smoke { get; set; }
        public int Burn { get; set; }
        public string VolumePerTurn { get; set; }

        public BurnData() {
            
        }

        public BurnData(bool isFlammable) {
            if (isFlammable)
                Burn = 1;
        }

        public BurnData(JObject data) {
            Fuel = data.GetValueOr("fuel", 0);
            Smoke = data.GetValueOr("smoke", 0);
            Burn = data.GetValueOr("burn", 0);
            VolumePerTurn = data.GetValueOr("volume_per_turn", default(string));
        }
    }
}