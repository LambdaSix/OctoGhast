using System;
using static OctoGhast.Translation.Translation;

namespace OctoGhast {
    public enum TimeFormat {
        /// <summary>
        /// Military time: 0700.44, 1745.22
        /// HourMinute.Second
        /// </summary>
        Military,

        /// <summary>
        /// Twenty Four hour time: 07:45:22, 17:45:34
        /// Hour:Minute:Second
        /// </summary>
        TwentyFourHour,

        /// <summary>
        /// AM/PM style time: 7:45:38 AM, 5:38:12 PM
        /// Hour:Minute:Second (AM|PM)
        /// </summary>
        Civilian,
    }

    public enum Weekday {
        Sunday,
        Monday,
        Tuesday,
        Wednesday,
        Thursday,
        Friday,
        Saturday
    }

    /// <summary>
    /// A single point in time.
    /// ie. 1423 on Day 38 of Spring, Year 8
    /// </summary>
    public class Time : IEquatable<Time> {
        private TimeDuration epoch;

        public long Turns { get; }

        /// <summary>
        /// Create a new instance of time starting on the first day.
        /// </summary>
        public Time() {
            //epoch = new TimeDuration(TimeConstants.DefaultEpoch.Turns);
            epoch = new TimeDuration();
            Turns = epoch.Turns;
        }

        public Time(long turnNumber) {
            Turns = turnNumber;
            epoch = new TimeDuration(Turns);
        }

        /// <summary>
        /// Construct a new instance in time based on an absolute time duration from the start of the game.
        /// </summary>
        public Time(TimeDuration duration) {
            Turns = duration.Turns;
            epoch = duration;
        }

        /// <summary>
        /// Construct a Time instance with only an hour and minute component.
        /// </summary>
        public Time(int hour, int minute) {
            if (hour > 23 || hour < 0)
                throw new ArgumentOutOfRangeException(nameof(hour));
            if (minute > 59 || minute < 0)
                throw new ArgumentOutOfRangeException(nameof(minute));

            epoch = TimeDuration.FromHours(hour).AddMinutes(minute);
            Turns = epoch.Turns;
        }

        public Time AddTime(TimeDuration duration) {
            return new Time(Turns + duration.Turns);
        }

        public Time SubtractTime(TimeDuration duration) {
            return new Time(Turns - duration.Turns);
        }

        public bool IsNightTime()
        {
            var now = new Time(HourOfDay, Minutes);
            var sunrise = CurrentRealSeason.GetSunrise(this);
            var sunset = CurrentRealSeason.GetSunset(this);

            var twilightDuration = TimeDuration.FromHours(1);

            // now is the hours past midnight as a Time
            // Sunrise is the hour/minute component
            // Sunset is the hour/minute component

            return now > (sunset + twilightDuration) || now < sunrise;
        }

        /// <inheritdoc />
        public string ToTimeString(TimeFormat format) {
            if (format == TimeFormat.Military) {
                return _($"{HourOfDay:00}{Minutes:00}.{Seconds:00}");
            }

            if (format == TimeFormat.TwentyFourHour) {
                return _($"{HourOfDay:00}:{Minutes:00}:{Seconds:00}");
            }

            if (format == TimeFormat.Civilian) {
                var indicator = (HourOfDay < 12) ? "AM" : "PM";
                var hourClipped = HourOfDay % 12;
                return _($"{hourClipped}:{Minutes:00}:{Seconds:00} {indicator}");
            }

            return _($"{HourOfDay:00}{Minutes:00}.{Seconds:00}");
        }

        /// <inheritdoc />
        public override string ToString() {
            // TODO: Eternal season?
            // TODO: Configure the default timeformat somehow?
            var day = DayOfSeason + 1;
            return _($"Year {TotalYears}, {CurrentRealSeason.Name}, day {day} {ToTimeString(TimeFormat.Military)}");
        }

        public int TotalSeconds => epoch.TotalSeconds;
        public int Seconds => epoch.Seconds;

        public int TotalMinutes => epoch.TotalMinutes;
        public int Minutes => epoch.Minutes;

        public int TotalHours => epoch.TotalHours;
        public int HourOfDay => epoch.HourOfDay;

        public int TotalDays => epoch.TotalDays;
        public int DayOfYear => (TotalDays % Season.GetTotalLength());

        public Weekday DayOfWeek() {
            var daysSinceCataclysm = this - TimeConstants.DefaultEpoch;
            var startDay = Weekday.Thursday;
            int result = daysSinceCataclysm.AddDays((int) startDay).TotalDays;
            return (Weekday) (result % 7);
        }

        public int DayOfSeason => (TotalDays % Season.GetSeasonLength(CurrentSeason));

        public int CurrentSeason {
            get {
                var res = Math.Max(0, (epoch.TotalSeasons % Season.Seasons.Count)-1);

                // Hack: Make winter work properly (4 % 4 == 0)
                if (res == 0 && epoch.TotalSeasons > 3)
                    return 3;
                else
                    return res;
            }
        }

        public Season CurrentRealSeason => Season.Seasons[CurrentSeason];

        public int TotalYears => epoch.TotalYears;

        public static bool operator ==(Time lhs, Time rhs) => lhs.Turns == rhs.Turns;
        public static bool operator !=(Time lhs, Time rhs) => !(lhs == rhs);

        public static Time operator +(Time lhs, TimeDuration rhs) =>
            new Time(lhs.Turns + rhs.Turns);
        public static Time operator -(Time lhs, TimeDuration rhs) =>
            new Time(lhs.Turns - rhs.Turns);

        public static TimeDuration operator -(Time lhs, Time rhs) =>
            new TimeDuration(lhs.Turns - rhs.Turns);

        public static bool operator >(Time lhs, Time rhs) =>
            lhs.Turns > rhs.Turns;

        public static bool operator <(Time lhs, Time rhs) =>
            lhs.Turns < rhs.Turns;

        public static bool operator >=(Time lhs, Time rhs) =>
            lhs.Turns >= rhs.Turns;

        public static bool operator <=(Time lhs, Time rhs) =>
            lhs.Turns <= rhs.Turns;

        /// <inheritdoc />
        public bool Equals(Time other)
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
            return Equals((Time)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Turns.GetHashCode();
        }
    }
}