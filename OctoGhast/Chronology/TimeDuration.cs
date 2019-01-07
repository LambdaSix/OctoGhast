using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using OctoGhast.Extensions;
using static OctoGhast.Translation.Translation;

namespace OctoGhast {
    /// <summary>
    /// A duration of time, ie. 64 days, 7 hours and 4 minutes.
    /// </summary>
    public class TimeDuration : IEquatable<TimeDuration> {
        public long Turns { get; }
        private long YearLength => TimeDuration.FromDays(Season.GetTotalLength()).Turns;

        public TimeDuration(long turns) {
            Turns = turns;
        }

        public TimeDuration() {
            Turns = 0;
        }

        public TimeDuration(string str) {
            Turns = TimeDuration.FromString(str).Turns;
        }

        /*
         * TODO: Implement TimeDuration(string) to decompose something like "5M 22d 14h 12m" into 5 months, 22 days, 14 hours and 12 minutes.
         */

        public int TotalSeconds => (int) (Turns * 6);
        public int Seconds => (TotalSeconds % 60);

        public int TotalMinutes => (Turns < 10) ? 0 : (int) (Turns / 10);
        public int Minutes => (TotalMinutes % 60);

        public int TotalHours => (Turns < 600) ? 0 : (int) (Turns / 600);
        public int HourOfDay => (TotalHours % 24);

        public int TotalDays => (Turns < 14400) ? 0 : (int) (Turns / 14400);

        public int TotalWeeks => (Turns < 100800) ? 0 : (int) (Turns / 100800);

        public int TotalMonths => (Turns < 403200) ? 0 : (int) (Turns / 403200);

        public int TotalSeasons
        {
            get
            {
                var totalDays = TotalDays;
                var season = 0;
                while (totalDays > 0) {
                    totalDays -= Season.GetSeasonLength(season % Season.Seasons.Count);
                    season++;
                }

                return season;
            }
        }

        public int TotalYears => (Turns < YearLength) ? 0 : (int) (Turns / YearLength);

        public TimeDuration AddTurns(long turns) => new TimeDuration(Turns + turns);
        public TimeDuration AddSeconds(int seconds) => new TimeDuration(Turns + FromSeconds(seconds).Turns);
        public TimeDuration AddMinutes(int minutes) => new TimeDuration(Turns + FromMinutes(minutes).Turns);
        public TimeDuration AddHours(int hours) => new TimeDuration(Turns + FromHours(hours).Turns);
        public TimeDuration AddDays(int days) => new TimeDuration(Turns + FromDays(days).Turns);
        public TimeDuration AddWeeks(int weeks) => new TimeDuration(Turns + FromWeeks(weeks).Turns);
        public TimeDuration AddMonths(int months) => new TimeDuration(Turns + FromWeeks(months * 4).Turns);
        public TimeDuration AddYears(int years) => new TimeDuration(Turns + (YearLength * years));

        /// <inheritdoc />
        public override string ToString() {
            // TODO: Pluralising
            if (Turns >= TimeConstants.IndefinitelyLong.Turns)
                return _($"forever");

            if (this < TimeDuration.FromMinutes(1)) {
                return _($"{Seconds} second", $"{Seconds} seconds", Seconds);
            }

            if (this < TimeDuration.FromHours(1)) {
                return _($"{Minutes} minute", $"{Minutes} minutes", Minutes);
            }

            if (this < TimeDuration.FromDays(1)) {
                return _($"{TotalHours} minute", $"{TotalHours} minutes", TotalHours);
            }

            if (this < TimeDuration.FromWeeks(1)) {
                return _($"{TotalDays} days", $"{TotalDays} days", (TotalDays));
            }

            if (this < TimeDuration.FromMonths(1))
            {
                return _($"{TotalDays / 7} week", $"{TotalDays / 7} weeks", (TotalDays / 7));
            }

            if (this < TimeDuration.FromYears(1)) {
                
                return _($"{TotalMonths} months", $"{TotalMonths} months", TotalMonths);
            }

            return _($"{TotalYears} year", $"{TotalYears} years", TotalYears);
        }

        public static TimeDuration FromString(string str) {
            // Decompose a string like "1 day 12 hours" into TimeDuration.AddDays(1).AddHours(12)
            var pairs = str.Split(new []{' ',','}, StringSplitOptions.RemoveEmptyEntries).Pair((a, b) => (value: Int32.Parse(a), increment: b));

            var map = new Dictionary<(string singular, string plural), Func<int, TimeDuration>>
            {
                [("turn", "turns")] = v => TimeDuration.FromTurns(v),
                [("second", "seconds")] = TimeDuration.FromSeconds,
                [("minute", "minutes")] = TimeDuration.FromMinutes,
                [("hour", "hours")] = TimeDuration.FromHours,
                [("day", "days")] = TimeDuration.FromDays,
                [("week", "weeks")] = TimeDuration.FromWeeks,
                [("month", "months")] = TimeDuration.FromMonths,
                [("season","seasons")] = TimeDuration.FromSeasons,
                [("year", "years")] = TimeDuration.FromYears
            };

            var duration = new TimeDuration(0);

            foreach (var pair in pairs) {
                var foundKey = map.Keys.SingleOrDefault(s => s.singular == pair.increment || s.plural == pair.increment);

                if (foundKey.singular != null || foundKey.plural != null) {
                    duration += map[foundKey].Invoke(pair.value);
                }
            }

            return duration;
        }

        public static bool operator ==(TimeDuration lhs, TimeDuration rhs) => lhs.Turns == rhs.Turns;
        public static bool operator !=(TimeDuration lhs, TimeDuration rhs) => !(lhs == rhs);

        public static bool operator <(TimeDuration lhs, TimeDuration rhs) => lhs.Turns < rhs.Turns;
        public static bool operator >(TimeDuration lhs, TimeDuration rhs) => lhs.Turns > rhs.Turns;

        public static bool operator <=(TimeDuration lhs, TimeDuration rhs) => lhs.Turns <= rhs.Turns;
        public static bool operator >=(TimeDuration lhs, TimeDuration rhs) => lhs.Turns >= rhs.Turns;

        public static TimeDuration operator +(TimeDuration self, TimeDuration other) =>
            new TimeDuration(self.Turns + other.Turns);

        public static TimeDuration operator -(TimeDuration self, TimeDuration other) =>
            new TimeDuration(self.Turns - other.Turns);

        public static TimeDuration operator *(TimeDuration self, double other) =>
            new TimeDuration((long)(self.Turns * other));

        public static TimeDuration operator /(TimeDuration self, double other) =>
            new TimeDuration((long)(self.Turns / other));

        public static double operator *(TimeDuration self, TimeDuration other) =>
            (self.Turns * (double)other.Turns);

        public static double operator /(TimeDuration self, TimeDuration other) =>
            (self.Turns / (double)other.Turns);

        public static TimeDuration operator %(TimeDuration self, TimeDuration other) =>
            new TimeDuration(self.Turns % other.Turns);

        public static implicit operator TimeDuration(int val) => new TimeDuration(val);
        public static implicit operator TimeDuration(Int64 val) => new TimeDuration(val);

        /// <inheritdoc />
        public bool Equals(TimeDuration other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Turns == other.Turns;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TimeDuration)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Turns.GetHashCode();
        }

        public static TimeDuration Random(TimeDuration low, TimeDuration high) {
            // TODO: Refactor RenderLike.Random to support this
            throw new NotImplementedException();
        }

        public static TimeDuration FromTurns(long turns) => new TimeDuration(turns);
        public static TimeDuration FromSeconds(int seconds) => new TimeDuration((seconds < 6) ? 0 : (long) seconds / 6);
        public static TimeDuration FromMinutes(int minutes) => new TimeDuration((long) minutes * 10);
        public static TimeDuration FromHours(int hours) => FromMinutes(60 * hours);
        public static TimeDuration FromDays(int days) => FromHours(24 * days);
        public static TimeDuration FromWeeks(int weeks) => FromDays(7 * weeks);

        public static TimeDuration FromMonths(int months) => FromWeeks(4 * months);

        // nb. A Month is 4 weeks, a year is 12 months, a season is a variable length construct that doesn't impact time tracking.

        public static TimeDuration FromYears(int years) => FromMonths(12 * years); // 1 year == 12 months

        [Obsolete("Consider using FromMonths or FromYears")]
        public static TimeDuration FromSeasons(int seasons) {
            int totalDays = 0;
            for (int i = 0; i < seasons; i++) {
                totalDays += Season.GetSeasonLength(i % 4);
            }

            return FromDays(totalDays);
        }
    }
}