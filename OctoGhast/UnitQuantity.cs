using System;
using System.CodeDom;
using System.Linq;
using System.Text.RegularExpressions;
using MiscUtil;

namespace OctoGhast.Units {
    public class VolumeInMillilitersTag { }
    public class MassInGramsTag { }

    public class Quantity<TValue, TUnit> {
        public TValue Value { get; }

        public Quantity(TValue value) {
            Value = value;
        }

        public static bool operator ==(Quantity<TValue, TUnit> lhs, Quantity<TValue, TUnit> rhs) {
            return Operator.Equal(lhs.Value, rhs.Value);
        }

        public static bool operator !=(Quantity<TValue, TUnit> lhs, Quantity<TValue, TUnit> rhs) {
            return !(lhs == rhs);
        }

        public static Quantity<TValue, TUnit> operator +(Quantity<TValue, TUnit> lhs, Quantity<TValue, TUnit> rhs) {
            return new Quantity<TValue, TUnit>(Operator.Add(lhs.Value, rhs.Value));
        }

        public static Quantity<TValue, TUnit> operator -(Quantity<TValue, TUnit> lhs, Quantity<TValue, TUnit> rhs) {
            return new Quantity<TValue, TUnit>(Operator.Subtract(lhs.Value, rhs.Value));
        }

        public static Quantity<TValue, TUnit> operator *(Quantity<TValue, TUnit> lhs, Quantity<TValue, TUnit> rhs) {
            return new Quantity<TValue, TUnit>(Operator.Multiply(lhs.Value, rhs.Value));
        }

        public static Quantity<TValue, TUnit> operator /(Quantity<TValue, TUnit> lhs, Quantity<TValue, TUnit> rhs) {
            return new Quantity<TValue, TUnit>(Operator.Divide(lhs.Value, rhs.Value));
        }

        public static Quantity<TValue, TUnit> operator *(Quantity<TValue, TUnit> lhs, float rhs) {
            return new Quantity<TValue, TUnit>(Operator.MultiplyAlternative(lhs.Value, rhs));
        }

        public static Quantity<TValue, TUnit> operator /(Quantity<TValue, TUnit> lhs, float rhs) {
            return new Quantity<TValue, TUnit>(Operator.DivideAlternative(lhs.Value, rhs));
        }

        public static bool operator >(Quantity<TValue, TUnit> lhs, Quantity<TValue, TUnit> rhs) {
            return Operator.GreaterThan(lhs.Value, rhs.Value);
        }

        public static bool operator <(Quantity<TValue, TUnit> lhs, Quantity<TValue, TUnit> rhs) {
            return Operator.LessThan(lhs.Value, rhs.Value);
        }

        public static Quantity<TValue, TUnit> operator -(Quantity<TValue,TUnit> lhs) {
            return new Quantity<TValue, TUnit>(Operator.Negate(lhs.Value));
        }
    }

    public class Volume : Quantity<int, VolumeInMillilitersTag> {
        public static Volume Min = new Volume(Int32.MinValue);
        public static Volume Max = new Volume(Int32.MaxValue);

        public Volume(int value) : base(value) { }

        public Volume(string value) : base(((Volume) value).Value) { }

        /// <summary>
        /// Density of the contained volume in g/cm³
        /// e.x. Water is 1 g/cm3
        /// </summary>
        public float VolumeDensity { get; set; } = 1;
        private int Density(int value) {
            // m = V x ρ
            return (int) (Value * VolumeDensity);
        }

        public Mass Mass() => new Mass(Density(Value)); // 1KG per 1L

        public int Milliliters => Value;
        public int Liters=> Value / 1000;

        public static Volume FromMilliliters(int value) => new Volume(value);
        public static Volume FromLiters(int value) => new Volume(value * 1000);

        /// <summary>
        /// Legacy conversion for old volume values.
        /// A multiple of 250ML.
        /// </summary>
        public static Volume FromLegacy(int quantity) => new Volume(quantity * 250);

        private static Regex _compiledRegex =
            new Regex(@"(\d+)((ml)|(milliliter)|(L)|(liter)|(KL)|(kiloliter))", RegexOptions.Compiled | RegexOptions.ECMAScript | RegexOptions.IgnoreCase);

        public static implicit operator Volume(string value)
        {
            var matches = _compiledRegex.Match(value);
            if (matches.Success)
            {
                var (val, unit) = (matches.Groups[1].Value, matches.Groups[2].Value.ToLower());
                if (unit == "ml" || unit == "milliliter")
                {
                    return Volume.FromMilliliters(Int32.Parse(val));
                }

                if (unit == "l" || unit == "liter")
                {
                    return Volume.FromLiters(Int32.Parse(val));
                }

                if (unit == "kl" || unit == "kiloliter")
                {
                    return Volume.FromLiters(Int32.Parse(val) * 1000);
                }
            }

            throw new ArgumentException("Unable to match value against known quantity", nameof(value));
        }
    }

    public class Mass : Quantity<int, MassInGramsTag> {
        public static Mass Min = new Mass(Int32.MinValue);
        public static Mass Max = new Mass(Int32.MaxValue);

        public Mass(int value) : base(value) { }

        public Mass(string value) : base(((Mass) value).Value) { }

        public int Grams => Value;
        public float Kilograms => Value / 1000.0f;
        public float Tons => Kilograms / 1000.0f;

        public static Mass FromGrams(int value) => new Mass(value);
        public static Mass FromKilograms(int value) => new Mass(value * 1000);

        private static Regex _compiledRegex =
            new Regex(@"(\d+)((kg)|(kilogram)|[G|K|T]|(gram)|(ton))", RegexOptions.Compiled | RegexOptions.ECMAScript | RegexOptions.IgnoreCase);

        public static implicit operator Mass(string value) {
            var matches = _compiledRegex.Match(value);
            if (matches.Success) {
                var (val, unit) = (matches.Groups[1].Value, matches.Groups[2].Value.ToLower());
                if (unit == "g" || unit == "gram") {
                    return Mass.FromGrams(Int32.Parse(val));
                }

                if (unit == "kg" || unit == "kilogram") {
                    return Mass.FromKilograms(Int32.Parse(val));
                }

                if (unit == "t" || unit == "ton") {
                    return Mass.FromKilograms(Int32.Parse(val) * 1000);
                }
            }

            throw new ArgumentException("Unable to match value against known quantity",nameof(value));
        }
    }
}
