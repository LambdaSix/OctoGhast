using System;
using System.Collections.Generic;
using System.ComponentModel;
using OctoGhast.Entity;

namespace OctoGhast {

    public enum MaterialPhase {
        None,
        Solid,
        Liquid,
        Gas,
        Plasma
    }

    public class Material : DataTemplate {
        public string Name { get; set; }
        public StringID<Material> Id { get; set; }
        public int BashResist { get; set; }
        public int CutResist { get; set; }
        public int AcidResist { get; set; }
        public int ElectricalResist { get;set; }
        public int FireResist { get; set; }
        public int ChipResistance { get; set; }

        public float Density { get; set; }
        public string SalvagedInto { get; set; }
        public string RepairedWith { get; set; }
        public bool IsEdible { get; set; }
        public bool IsSoft { get; set; }

        public IEnumerable<VitaminInfo> Vitamins { get; set; }
        public List<string> DamageAdjectives { get; set; }
        public string BashDamageVerb { get; set; }
        public string CutDamageVerb { get; set; }
        public IEnumerable<BurnData> BurnData { get; set; }
        public IEnumerable<(string Name,int Amount)> BurnProducts { get; set; }
        public IEnumerable<string> CompactsInto { get; set; }
        public IEnumerable<string> CompactAccepts { get; set; }

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
            return damage <= 0 ? String.Empty : DamageAdjectives[Math.Min(damage, DamageAdjectives.Count) - 1];
        }
    }
}