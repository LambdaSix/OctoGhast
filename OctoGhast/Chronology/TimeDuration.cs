using System;
using System.CodeDom;
using static OctoGhast.Translation.Translation;

namespace OctoGhast {
    /// <summary>
    /// A duration of time, ie. 64 days, 7 hours and 4 minutes.
    /// </summary>
    public class TimeDuration : IEquatable<TimeDuration> {
        public ulong Turns { get; }
        private ulong YearLength => TimeDuration.FromDays(Season.GetTotalLength()).Turns;

        public TimeDuration(ulong turns) {
            Turns = turns;
        }

        public TimeDuration() {
            Turns = 0;
        }

        public int TotalSeconds => (int) (Turns * 6);
        public int Seconds => (TotalSeconds % 60);

        public int TotalMinutes => (Turns < 10) ? 0 : (int) (Turns / 10);
        public int Minutes => (TotalMinutes % 60);

        public int TotalHours => (Turns < 600) ? 0 : (int) (Turns / 600);
        public int HourOfDay => (TotalHours % 24);

        public int TotalDays => (Turns < 14400) ? 0 : (int) (Turns / 14400);

        public int TotalWeeks => (Turns < 100800) ? 0 : (int) (Turns / 100800);

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

        public TimeDuration AddTurns(int turns) => new TimeDuration(Turns + (ulong) turns);
        public TimeDuration AddSeconds(int seconds) => new TimeDuration(Turns + FromSeconds(seconds).Turns);
        public TimeDuration AddMinutes(int minutes) => new TimeDuration(Turns + FromMinutes(minutes).Turns);
        public TimeDuration AddHours(int hours) => new TimeDuration(Turns + FromHours(hours).Turns);
        public TimeDuration AddDays(int days) => new TimeDuration(Turns + FromDays(days).Turns);
        public TimeDuration AddWeeks(int weeks) => new TimeDuration(Turns + FromWeeks(weeks).Turns);
        public TimeDuration AddYears(int years) => new TimeDuration(Turns + (YearLength * (ulong)years));

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

            if (this < TimeDuration.FromSeasons(1)) {
                return _($"{TotalDays / 7} week", $"{TotalDays / 7} weeks", (TotalDays / 7));
            }

            if (this < TimeDuration.FromYears(1)) {
                return _($"{TotalSeasons} season", $"{TotalSeasons} seasons", TotalSeasons);
            }

            return _($"{TotalYears} year", $"{TotalYears} years", TotalYears);
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
            new TimeDuration((ulong)(self.Turns * other));

        public static TimeDuration operator /(TimeDuration self, double other) =>
            new TimeDuration((ulong)(self.Turns / other));

        public static double operator *(TimeDuration self, TimeDuration other) =>
            (self.Turns * (double)other.Turns);

        public static double operator /(TimeDuration self, TimeDuration other) =>
            (self.Turns / (double)other.Turns);

        public static TimeDuration operator %(TimeDuration self, TimeDuration other) =>
            new TimeDuration((ulong) self.Turns % other.Turns);

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

        public static TimeDuration FromTurns(int turns) => new TimeDuration((ulong) turns * 6);
        public static TimeDuration FromSeconds(int seconds) => new TimeDuration((seconds < 6) ? 0 : (ulong) seconds / 6);
        public static TimeDuration FromMinutes(int minutes) => new TimeDuration((ulong) minutes * 10);
        public static TimeDuration FromHours(int hours) => FromMinutes(60 * hours);
        public static TimeDuration FromDays(int days) => FromHours(24 * days);
        public static TimeDuration FromWeeks(int weeks) => FromDays(7 * weeks);

        public static TimeDuration FromSeasons(int seasons) {
            int totalDays = 0;
            for (int i = 0; i < seasons; i++) {
                totalDays += Season.GetSeasonLength(i % 4);
            }

            return FromDays(totalDays);
        }

        /// <summary>
        /// Give a TimeDuration calculated for n whole years assuming it starts at Season[0] and progresses to Season[Max]
        /// </summary>
        /// <param name="years"></param>
        /// <returns></returns>
        public static TimeDuration FromYears(int years) {
            int totalDays = 0;

            // One year
            for (int i = 0; i < Season.Seasons.Count; i++) {
                totalDays += Season.GetSeasonLength(i % 4);
            }

            return FromDays(totalDays * years);
        }
    }
}