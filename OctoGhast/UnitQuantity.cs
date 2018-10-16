using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static OctoGhast.Translation.Translation;
using MiscUtil;

namespace OctoGhast.Units {
    public class VolumeInMillilitersTag { }
    public class MassInGramsTag { }
    public class LoudnessInPascalsTag { }

    public class Quantity<TValue, TUnit> : IEquatable<Quantity<TValue, TUnit>> {
        public TValue Value { get; }

        /// <summary>
        /// A material for this Quantity, defaults to water.
        /// </summary>
        public Material Material = new Material()
        {
            Density = 1.0f
        };

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

        public static Quantity<TValue, TUnit> operator -(Quantity<TValue, TUnit> lhs) {
            return new Quantity<TValue, TUnit>(Operator.Negate(lhs.Value));
        }

        /// <inheritdoc />
        public bool Equals(Quantity<TValue, TUnit> other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return EqualityComparer<TValue>.Default.Equals(Value, other.Value);
        }

        /// <inheritdoc />
        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Quantity<TValue, TUnit>) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode() {
            return EqualityComparer<TValue>.Default.GetHashCode(Value);
        }
    }

    public class SoundLevel : Quantity<double, LoudnessInPascalsTag> {
        /// <summary>
        ///  Reference sound pressure - 20μPa (2*10^-5 Pa)
        /// </summary>
        private const double p0 = 0.00002d;

        public static SoundLevel Min = new SoundLevel(Int32.MinValue);
        public static SoundLevel Max = new SoundLevel(101325.0d);

        /// <inheritdoc />
        public SoundLevel(double pascalValue) : base(pascalValue) { }

        public SoundLevel(int decibels) : base(DecibelToPascals(decibels)) { }

        public SoundLevel(string value) : base(((SoundLevel) value).Value) { }

        public double Decibels => PascalsToDecibel(Value);
        public double Pascals => Value;

        public static SoundLevel FromDecibels(double value) => new SoundLevel(DecibelToPascals(value));
        public static SoundLevel FromPascals(double value) => new SoundLevel(value);

        private static double PascalsToDecibel(double value) {
            return 20 * Math.Log10(value / p0);
        }

        private static double DecibelToPascals(double value) {
            return p0 * Math.Pow(10, (value / 20.0f));
        }

        public SoundLevel AtMeters(float distance) {
            var p0 = Value;
            var r0 = 1;
            var r1 = distance;
            var p1 = (r0 / r1) * p0;
            return FromPascals(p1);
        }

        private static Regex _compiledRegex =
            new Regex(@"([\d.]+)((dB)|(db))", RegexOptions.Compiled | RegexOptions.ECMAScript | RegexOptions.IgnoreCase);

        public static implicit operator SoundLevel(string value)
        {
            var matches = _compiledRegex.Match(value);
            if (matches.Success)
            {
                var (val, unit) = (matches.Groups[1].Value, matches.Groups[2].Value.ToLower());
                switch (unit) {
                    case "db":
                    case "decibels":
                        return FromDecibels(Double.Parse(val));
                    case "pa":
                    case "Pa":
                        return FromPascals(Double.Parse(val));
                }
            }

            throw new ArgumentException("Unable to match value against known quantity", nameof(value));
        }
    }

    public class Volume : Quantity<int, VolumeInMillilitersTag> {
        public static Volume Min = new Volume(Int32.MinValue);
        public static Volume Max = new Volume(Int32.MaxValue);

        public Volume(int value) : base(value) { }

        public Volume(string value) : base(((Volume) value).Value) { }

        /// <summary>
        /// Density of the contained volume in g/cm³
        /// e.x. Water is 1 g/cm³
        /// </summary>
        public float VolumeDensity => Material.Density;
        private int Density(int value) {
            // m = V(ml) x ρ(g/cm³)
            return (int) (Milliliters * VolumeDensity);
        }

        public Mass Mass() => new Mass(Density(Value)); // 1KG per 1L

        public int Milliliters => Value;
        public float Liters => Value / 1000.0f;

        public static Volume FromMilliliters(int value) => new Volume(value);
        public static Volume FromLiters(float value) => FromMilliliters((int) (value * 1000.0f));

        /// <summary>
        /// Legacy conversion for old volume values.
        /// A multiple of 250ML.
        /// </summary>
        public static Volume FromLegacy(int quantity) => new Volume(quantity * 250);

        public override string ToString()
        {
            if (Liters >= 1000) {
                return _($"{Liters / 1000}KL");
            }

            if (Liters >= 1) {
                return _($"{Liters}L");
            }

            return _($"{Milliliters}ml");
        }

        private static Regex _compiledRegex =
            new Regex(@"([\d.]+)((ml)|(milliliter)|(L)|(liter)|(KL)|(kiloliter))", RegexOptions.Compiled | RegexOptions.ECMAScript | RegexOptions.IgnoreCase);

        public static implicit operator Volume(string value)
        {
            var matches = _compiledRegex.Match(value);
            if (matches.Success) {
                var (val, unit) = (matches.Groups[1].Value, matches.Groups[2].Value.ToLower());
                if (unit == "ml" || unit == "milliliter") {
                    return Volume.FromMilliliters(Int32.Parse(val));
                }

                if (unit == "l" || unit == "liter") {
                    if (val.Contains(".")) {
                        // If there's a decimal point and it's a liter:
                        var rawVal = Single.Parse(val);
                        return FromLiters(rawVal);
                    }

                    return FromLiters(Int32.Parse(val));
                }

                if (unit == "kl" || unit == "kiloliter") {
                    if (val.Contains(".")) {
                        // If there's a decimal point and it's a kiloliter:
                        var rawVal = Double.Parse(val);
                        return FromLiters((int) (rawVal * 1000));
                    }

                    return FromLiters(Int32.Parse(val) * 1000);
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
        public static Mass FromKilograms(float value) => FromGrams((int) (value * 1000.0f));

        /// <summary>
        /// Density of the mass in g/cm³
        /// e.x. Water is 1 g/cm³
        /// Ice is 0.9340g/cm³
        /// </summary>
        public float VolumeDensity => Material.Density;
        private int Density(int value)
        {
            // V(ml) = m(g) / ρ(g/cm³)
            return (int)(Grams / VolumeDensity);
        }

        public Volume Volume() => new Volume(Density(Value)); // 1KG per 1L

        public override string ToString() {
            if (Tons >= 1) {
                return _($"{Tons}T");
            }

            if (Kilograms >= 1) {
                return _($"{Kilograms}KG");
            }

            return _($"{Grams}g");
        }

        private static readonly Regex _compiledRegex =
            new Regex(@"([\d.]+)((kg)|(kilogram)|[G|K|T]|(gram)|(ton))", RegexOptions.Compiled | RegexOptions.ECMAScript | RegexOptions.IgnoreCase);

        public static implicit operator Mass(string value) {
            var matches = _compiledRegex.Match(value);
            if (matches.Success) {
                var (val, unit) = (matches.Groups[1].Value, matches.Groups[2].Value.ToLower());
                if (unit == "g" || unit == "gram") {
                    return Mass.FromGrams(Int32.Parse(val));
                }

                if (unit == "kg" || unit == "kilogram") {
                    if (val.Contains(".")) {
                        // If there's a decimal point and it's a liter:
                        var rawVal = Single.Parse(val);
                        return FromKilograms(rawVal);
                    }

                    return FromKilograms(Int32.Parse(val));
                }

                if (unit == "t" || unit == "ton") {
                    if (val.Contains(".")) {
                        // If there's a decimal point and it's a liter:
                        var rawVal = Single.Parse(val);
                        return FromKilograms(rawVal * 1000.0f);
                    }

                    return FromKilograms(Int32.Parse(val) * 1000.0f);
                }
            }

            throw new ArgumentException("Unable to match value against known quantity", nameof(value));
        }
    }
}
