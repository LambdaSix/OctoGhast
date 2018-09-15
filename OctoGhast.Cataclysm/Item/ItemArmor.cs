using System;
using System.Linq;

namespace OctoGhast {
    public enum ArmorLayer
    {
        Underwear,
        Regular,
        Waist,
        Outer,
        Belted
    }

    public class ItemArmor : Item {
        public int Thickness => Component.MaterialThickness;
        public int Warmth => Component.Warmth;
        public int Coverage => Component.Coverage;

        public ArmorComponent Component => Template.Info[typeof(ArmorComponent)] as ArmorComponent;
        private ItemArmor() { }

        private int ResistType(DamageType type, bool toSelf) {
            // HACK: This entire type is a little messy because of Materials + paddin
            if (toSelf && (type == DamageType.Acid || type == DamageType.Heat))
                return Int32.MaxValue;

            if (type == DamageType.Acid || type == DamageType.Heat) {
                /*
                float resistSpecial = 0;
                foreach (var material in base.ItemInfoData.Materials) {
                    // TODO: Material[] not String[]
                    //resistSpecial += material.GetResistance(type);
                }
                resistSpecial /= ItemInfoData.Materials.Count();

                var env = Component.EnvironmentalProtection;
                resistSpecial += env / 10.0f;
                return (int) Math.Round(resistSpecial);
                */
            }

            int baseThickness = Thickness;
            float resist = 0;
            float padding = 0;
            int effectiveThickness = 1;

            // This should probably be replaced with looking up the Leather & Kevlar Materials
            // and apply their thickness
            int leatherBonus = HasTag("leather_padded") ? baseThickness : 0;

            // Replace with the Material system. But basically Kevlar guards more against CUT (bullets deal CUT)
            var kevlarMult = HasTag("kevlar_padding")
                ? (type == DamageType.Cut)
                    ? 2
                    : 1
                : 0;
           
            int kevlarBonus = baseThickness * kevlarMult;
            padding += leatherBonus + kevlarBonus;

            // Bash
            int dmg = DamageLevel(4);
            int effectiveDamage = toSelf ? Math.Min(dmg, 0) : Math.Max(dmg, 0);
            effectiveThickness = Math.Max(1, Thickness - effectiveDamage);

            /*
            // Apply material bonuses:
            foreach (var material in ItemInfoData.Materials) {
                // TODO: Process ItemData.Materials as a Material[] rather than String[]
                //resist += material.GetResistance(type);
            }
            resist /= ItemInfoData.Materials.Count();
            */

            var result = (int) Math.Round(resist * effectiveThickness + padding);
            if (type == DamageType.Stab) {
                return (int) (0.8f * result);
            }

            return result;
        }

        public int DamageResist(DamageType type, bool toSelf) {
            switch (type) {
                case DamageType.Debug:
                case DamageType.Biological:
                case DamageType.Electric:
                case DamageType.Cold:
                    return toSelf ? Int32.MaxValue : 0; // ???
                case DamageType.Bash:
                case DamageType.Cut:
                case DamageType.Acid:
                case DamageType.Stab:
                case DamageType.Heat:
                    return ResistType(type, toSelf);
            }

            return -1;
        }

        public virtual int GetWarmth() {
            int furBonus = 0;
            int woolBonus = 0;

            int baseWarmth = Warmth;
            // TODO: var bonus = GetTagData("warmthBonus"); (item.AddTagData("warmthBonus", "furred", "35");)
            // Mod thing?
            if (HasTag("furred")) {
                furBonus = (35 * Coverage) / 100;
            }
            if (HasTag("wooled")) {
                woolBonus = (20 * Coverage) / 100;
            }

            return baseWarmth + furBonus + woolBonus;
        }

        public virtual ArmorLayer GetLayer() {
            // Should this be a mod thing?
            if (HasFlag("SKINTIGHT")) {
                return ArmorLayer.Underwear;
            }
            if (HasFlag("WAIST")) {
                return ArmorLayer.Waist;
            }
            if (HasFlag("OUTER")) {
                return ArmorLayer.Outer;
            }
            if (HasFlag("BELTED")) {
                return ArmorLayer.Belted;
            }

            return ArmorLayer.Regular;
        }
    }
}