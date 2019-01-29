﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static OctoGhast.Translation.Translation;
using MiscUtil;
using OctoGhast.Cataclysm.LegacyLoader;

namespace OctoGhast.Units {
    public class VolumeInMillilitersTag { }
    public class MassInGramsTag { }
    public class LoudnessInKilopascalsTag { }
    public class PressureInKiloPascalsTag { }
    public class EnergyInJoulesTag { }

    public class Quantity<TValue, TUnit> : IEquatable<Quantity<TValue, TUnit>> {
        public TValue Value { get; }

        /// <summary>
        /// A material for this Quantity, defaults to water.
        /// </summary>
        public Material Material = CoreMaterials.Water;

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
            var res = new Quantity<TValue, TUnit>(Operator.Convert<float, TValue>(
                Operator.MultiplyAlternative(Operator.Convert<TValue, float>(lhs.Value), rhs)
            ));
            return res;
        }

        public static Quantity<TValue, TUnit> operator /(Quantity<TValue, TUnit> lhs, float rhs) {
            var res = new Quantity<TValue, TUnit>(Operator.Convert<double, TValue>(
                Operator.DivideAlternative(Operator.Convert<TValue, double>(lhs.Value), rhs)
            ));
            return res;
        }

        public static Quantity<TValue, TUnit> operator *(Quantity<TValue, TUnit> lhs, double rhs) {
            var res = new Quantity<TValue, TUnit>(Operator.Convert<double, TValue>(
                Operator.MultiplyAlternative(Operator.Convert<TValue, double>(lhs.Value), rhs)
            ));
            return res;
        }

        public static Quantity<TValue, TUnit> operator /(Quantity<TValue, TUnit> lhs, double rhs) {
            var res = new Quantity<TValue, TUnit>(Operator.Convert<double, TValue>(
                Operator.DivideAlternative(Operator.Convert<TValue, double>(lhs.Value), rhs)
            ));
            return res;
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

    public class Pressure : Quantity<double, PressureInKiloPascalsTag> {
        public static Pressure Min = new Pressure(Double.NegativeInfinity);
        public static Pressure Max = new Pressure(Double.PositiveInfinity);

        public static Pressure Atmosphere => new Pressure(atmConversionFactor);

        /// <summary>
        /// 1 Atm -> kPa
        /// </summary>
        private static double atmConversionFactor = 101.325;

        /// <summary>
        /// 1 Psi -> kPa
        /// </summary>
        private static double psiConversionFactor = 6.895;

        public Pressure(double kiloPascals) : base(kiloPascals) {
            Material = CoreMaterials.Air;
        }

        public Pressure(string value) : this(((Pressure)value).Value) { }
        public Pressure(Quantity<double, PressureInKiloPascalsTag> val) : this(val.Value) { }

        public static Pressure FromAtmospheres(double atmospheres) => new Pressure(atmospheres * atmConversionFactor);
        public static Pressure FromPsi(double psi) => new Pressure(psi * psiConversionFactor);

        public double Pascals => Value * 1000;
        public double KiloPascals => Value;
        public double Atmospheres => KiloPascals / atmConversionFactor;
        public double Psi => KiloPascals / psiConversionFactor;

        public SoundLevel AsDecibels() => new SoundLevel(KiloPascals);

        // Support:
        // pa, pascals
        // kpa, kilopascals
        // atmospheres, atm
        // psi
        private static Regex _compiledRegex =
            new Regex(@"([-]?[\d.]+)((pa)|(kpa)|(pascals)|(kilopascals)|(atmospheres)|(atm)|(psi))", RegexOptions.Compiled | RegexOptions.ECMAScript | RegexOptions.IgnoreCase);

        public static implicit operator Pressure(string value)
        {
            var matches = _compiledRegex.Match(value);
            if (matches.Success)
            {
                var (val, unit) = (matches.Groups[1].Value, matches.Groups[2].Value.ToLower());
                switch (unit)
                {
                    case "pa":
                    case "pascals":
                        return new Pressure(Double.Parse(val) / 1000);
                    case "kpa":
                    case "kilopascals":
                        return new Pressure(Double.Parse(val));

                    case "atmospheres":
                    case "atm":
                        return Pressure.FromAtmospheres(double.Parse(val));

                    case "psi":
                        return Pressure.FromPsi(double.Parse(val));
                }
            }

            throw new ArgumentException("Unable to match value against known quantity", nameof(value));
        }
    }

    public class SoundLevel : Quantity<double, LoudnessInKilopascalsTag> {
        /// <summary>
        ///  Reference sound pressure - 20μPa (2*10^-5 Pa)
        /// </summary>
        private const double p0 = 0.00002d;

        public static SoundLevel Min = new SoundLevel(Int32.MinValue);
        public static SoundLevel Max = new SoundLevel(101325.0d);

        /// <inheritdoc />
        public SoundLevel(double kiloPascalValue) : base(kiloPascalValue) {
            Material = CoreMaterials.Air;
        }

        public SoundLevel(string value) : this(((SoundLevel) value).Value) { }
        public SoundLevel(Quantity<double, LoudnessInKilopascalsTag> val) : this(val.Value) { }

        public double Decibels => PascalsToDecibel(Value);
        public double Pascals => Value * 1000;
        public double KiloPascals => Value;

        public static SoundLevel FromDecibels(double value) => new SoundLevel(DecibelToKiloPascals(value));
        public static SoundLevel FromPascals(double value) => new SoundLevel(value * 1000);
        public static SoundLevel FromKiloPascals(double value) => new SoundLevel(value);
        public static SoundLevel FromPressure(Pressure value) => new SoundLevel(value.KiloPascals);

        private static double PascalsToDecibel(double value) {
            return 20 * Math.Log10(value / p0);
        }

        private static double DecibelToKiloPascals(double value) {
            return p0 * Math.Pow(10, (value / 20.0f));
        }

        public SoundLevel AddRaw(SoundLevel other) => new SoundLevel(other.Value + Value);
        public SoundLevel MultiplyRaw(double scalar) => new SoundLevel(Value * scalar);

        public SoundLevel AtMeters(float distance) {
            var p0 = Value;
            var r0 = 1;
            var r1 = distance;
            var p1 = (r0 / r1) * p0;
            return FromPascals(p1);
        }

        private static Regex _compiledRegex =
            new Regex(@"([-]?[\d.]+)((dB)|(kpa)|(pa)|(pascals)|(decibels)|(kilopascals))", RegexOptions.Compiled | RegexOptions.ECMAScript | RegexOptions.IgnoreCase);

        // Based entirely on the base cataclysm data files defining a .22 rifle as 'loudness: 25', and a real-life .22 rifle being 134dB
        // 134 / 25 = 5.36
        private static float scalingFactor = 5.36f;
        public static implicit operator SoundLevel(string value)
        {
            if (Int32.TryParse(value, out var level)) {
                return FromDecibels(level * scalingFactor);
            }

            var matches = _compiledRegex.Match(value);
            if (matches.Success) {
                var (val, unit) = (matches.Groups[1].Value, matches.Groups[2].Value.ToLower());
                switch (unit) {
                    case "db":
                    case "decibels":
                        return FromDecibels(Double.Parse(val));

                    case "kpa":
                    case "kilopascals":
                        return FromKiloPascals(Double.Parse(val));

                    case "pa:":
                    case "pascals:":
                        return FromPascals(double.Parse(val));
                }
            }

            throw new ArgumentException("Unable to match value against known quantity", nameof(value));
        }

        // TODO: Overload the + * - operators to provide logarithmic correct values.
    }

    public class Volume : Quantity<double, VolumeInMillilitersTag> {
        public static Volume Min = new Volume(Int32.MinValue);
        public static Volume Max = new Volume(Int32.MaxValue);

        public Volume(int value) : base(value) { }
        public Volume(double value) : base(value) { }

        public Volume(string value) : base(((Volume) value).Value) { }
        public Volume(Quantity<double, VolumeInMillilitersTag> val) : this(val.Value) { }

        /// <summary>
        /// Density of the contained volume in g/cm³
        /// e.x. Water is 1 g/cm³
        /// </summary>
        public float VolumeDensity => Material.Density;
        private int Density(double value) {
            // m = V(ml) x ρ(g/cm³)
            return (int) (Milliliters * VolumeDensity);
        }

        public Mass Mass() => new Mass(Density(Value)); // 1KG per 1L

        public double Milliliters => Value;
        public double Liters => Value / 1000.0;

        public static Volume FromMilliliters(double value) => new Volume(value);
        public static Volume FromLiters(double value) => FromMilliliters((int) (value * 1000.0f));

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
            new Regex(@"([-]?[\d.]+)((ml)|(milliliter)|(L)|(liter)|(KL)|(kiloliter)|(cc)?)", RegexOptions.Compiled | RegexOptions.ECMAScript | RegexOptions.IgnoreCase);

        public static implicit operator Volume(string value)
        {
            var matches = _compiledRegex.Match(value);
            if (matches.Success) {
                var (val, unit) = (matches.Groups[1].Value, matches.Groups[2].Value.ToLower());
                switch (unit) {
                    case "ml":
                    case "milliliter":
                    case "cc":
                        return Volume.FromMilliliters(Double.Parse(val));
                    case "l":
                    case "liter": {
                        return FromLiters(Double.Parse(val));
                    }
                    case "kl":
                    case "kiloliter": {
                        return FromLiters(Double.Parse(val) * 1000);
                    }
                }

                // Without a unit we're assuming it's being specified in multiples of 250 milliliters
                // HACK: Legacy support code, phase out
                if (String.IsNullOrWhiteSpace(unit)) {
                    var rawVal = Double.Parse(val);
                    return FromMilliliters(rawVal*250);
                }
            }

            throw new ArgumentException("Unable to match value against known quantity", nameof(value));
        }

        public static Volume operator *(Volume lhs, double rhs) {
            return new Volume(lhs.Value * rhs);
        }

        public static Volume operator /(Volume lhs, double rhs)
        {
            return new Volume(lhs.Value / rhs);
        }

        public static Volume operator +(Volume lhs, Volume rhs) {
            return new Volume(lhs.Value + rhs.Value);
        }

        public static Volume operator -(Volume lhs, Volume rhs)
        {
            return new Volume(lhs.Value - rhs.Value);
        }
    }

    public class Mass : Quantity<double, MassInGramsTag> {
        public static Mass Min = new Mass(Int32.MinValue);
        public static Mass Max = new Mass(Int32.MaxValue);

        public Mass(double value) : base(value) { }

        public Mass(string value) : base(((Mass)value).Value) { }

        public Mass(Quantity<int, MassInGramsTag> value) : this(value.Value) { }

        public int Grams => (int) Math.Ceiling(Value);
        public double Kilograms => Value / 1000.0f;
        public double Tons => Kilograms / 1000.0f;

        public static Mass FromGrams(double value) => new Mass(value);
        public static Mass FromKilograms(double value) => FromGrams(value * 1000.0f);

        /// <summary>
        /// Density of the mass in g/cm³
        /// e.x. Water is 1 g/cm³
        /// Ice is 0.9340g/cm³
        /// </summary>
        public float VolumeDensity => Material.Density;
        private double Density(double value)
        {
            // V(ml) = m(g) / ρ(g/cm³)
            return Grams / VolumeDensity;
        }

        public Volume Volume() => new Volume(Density(Value)); // 1KG per 1L
        public Volume Volume(float density) => new Volume(Density(Value));

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
            new Regex(@"([-]?[\d.]+)((kg)|(kilogram)|[G|K|T]|(gram)|(ton)?)", RegexOptions.Compiled | RegexOptions.ECMAScript | RegexOptions.IgnoreCase);

        public static implicit operator Mass(string value) {
            var matches = _compiledRegex.Match(value);
            if (matches.Success) {
                var (val, unit) = (matches.Groups[1].Value, matches.Groups[2].Value.ToLower());

                // Match grams, or a unitless value.
                if (unit == "g" || unit == "gram" || String.IsNullOrWhiteSpace(unit)) {
                    return Mass.FromGrams(Double.Parse(val));
                }

                if (unit == "kg" || unit == "kilogram") {
                    return FromKilograms(Double.Parse(val));
                }

                if (unit == "t" || unit == "ton") {
                    return FromKilograms(Double.Parse(val) * 1000.0f);
                }
            }

            throw new ArgumentException("Unable to match value against known quantity", nameof(value));
        }

        public static Mass operator *(Mass lhs, double rhs) {
            return new Mass(lhs.Value * rhs);
        }

        public static Mass operator +(Mass lhs, double rhs) {
            return new Mass(lhs.Value + rhs);
        }

        public static Mass operator +(Mass lhs, Mass rhs) {
            return new Mass(lhs.Value + rhs.Value);
        }
    }

    public class Energy : Quantity<double, EnergyInJoulesTag> {
        private static double CalorieConversionFactor = 4.1868;
        private static double HorsepowerConversionFactor = 745;

        /// <inheritdoc />
        public Energy(double joules) : base(joules) { }

        public Energy(string value) : base(((Energy)value).Value) { }

        public double Joules => Value;
        public double Kilojoules => Value / 1000;
        public double Megajoules => Value / 1_000_000;

        /// <summary>
        /// Energy in Watt-Seconds
        /// </summary>
        public double Watts => Value;

        /// <summary>
        /// Energy in Kilowatt-Seconds
        /// </summary>
        public double Kilowatts => Watts / 1000;

        public double WattHours => Value / 3600;
        public double KilowattHours => Value / 3_600_000;

        public double Calories => Value / CalorieConversionFactor;
        public double Kilocalories => Calories / 1000;
        public double Horsepower => Value / HorsepowerConversionFactor;

        public static Energy FromJoules(double joules) => new Energy(joules);
        public static Energy FromKilojoules(double kJoules) => new Energy(kJoules * 1000);
        public static Energy FromMegajoules(double mJoules) => new Energy(mJoules * 1_000_000);
        public static Energy FromWatts(double watts) => new Energy(watts);
        public static Energy FromKilowatts(double kWatts) => new Energy(kWatts * 1000);
        public static Energy FromWattHours(double wattHours) => new Energy(wattHours * 3600);
        public static Energy FromKilowattHours(double kWattHours) => new Energy(kWattHours * 3_600_000);
        public static Energy FromCalories(double calories) => new Energy(calories * CalorieConversionFactor);
        public static Energy FromKilocalories(double kiloCalories) => new Energy((kiloCalories * 1000) * CalorieConversionFactor);
        public static Energy FromHorsepower(double horsepower) => new Energy(horsepower * HorsepowerConversionFactor);

        public override string ToString()
        {
            if (Value >= 1_000_000)
            {
                return _($"{Megajoules}MJ");
            }

            if (Kilojoules >= 1)
            {
                return _($"{Kilojoules}kJ");
            }

            return _($"{Value}j");
        }

        /// <summary>
        /// Return the peak amount of energy in a volume of material with a given energy density (MJ/L).
        /// </summary>
        public static Energy FromMaterial(Volume volume, double energyDensity) {
            return Energy.FromMegajoules(energyDensity * volume.Liters);
        }

        /* Support (case-insensitive):
             J - Joules
             kJ - Kilojoules (1000J)
             MJ - Megajoules (1,000,000J / 1000kJ)
             W - Watt Seconds (1J)
             kW - Kilowatt Seconds (1000W)
             Wh - Watt Hours (3600W)
             kWh - Kilowatt hours (1000 Wh / 3,600,000W)
             Cal - Calories (4.1868J)
             KCal - Kilocalories (1000Cal)
             HP - Horsepower (746W / 746J)
         */
        private static readonly Regex _compiledRegex =
            new Regex(@"([-]?[\d.]+)((wh)|(kwh)|(j)|(joules)|(kJ)|(kilojoules)|(mj)|(megajoules)|(w)|(watts)|(kw)|(kilowatts)|(cal)|(kcal)|(hp)?)", RegexOptions.Compiled | RegexOptions.ECMAScript | RegexOptions.IgnoreCase);

        public Energy(Quantity<double, EnergyInJoulesTag> value) : base(value.Value) {
        }

        public static implicit operator Energy(string value)
        {
            var matches = _compiledRegex.Match(value);
            if (matches.Success)
            {
                var (val, unit) = (matches.Groups[1].Value, matches.Groups[2].Value.ToLower());

                // Match grams, or a unitless value.
                if (unit == "j" || unit == "joules" || String.IsNullOrWhiteSpace(unit)) {
                    return Energy.FromJoules(double.Parse(val));
                }

                switch (unit) {
                    case "kj":
                    case "kilojoules":
                        return FromKilojoules(double.Parse(val));
                    case "mj":
                    case "megajoules":
                        return FromMegajoules(double.Parse(val));
                    case "w":
                    case "watts":
                        return FromWatts(double.Parse(val));
                    case "kw":
                    case "kilowatts":
                        return FromKilowatts(double.Parse(val));
                    case "wh":
                        return FromWattHours(double.Parse(val));
                    case "kwh":
                        return FromKilowattHours(double.Parse(val));
                    case "cal":
                        return FromCalories(double.Parse(val));
                    case "kcal":
                        return FromKilocalories(double.Parse(val));
                    case "hp":
                        return FromHorsepower(double.Parse(val));
                }
            }

            throw new ArgumentException("Unable to match value against known quantity", nameof(value));
        }
    }
}
