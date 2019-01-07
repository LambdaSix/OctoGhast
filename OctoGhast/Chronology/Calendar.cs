using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;

namespace OctoGhast {
    public static class TimeConstants {
        /// <summary>
        /// Day 0, first day of the game.
        /// </summary>
        public static Time DefaultEpoch = new Time(TimeDuration.FromDays(0));

        /// <summary>
        /// A 'forever' value.
        /// </summary>
        public static TimeDuration IndefinitelyLong = new TimeDuration(Int64.MaxValue / 100);

        public static double MoonlightPerQuarter = 2.25;
    }

    public enum MoonPhase {
        /// <summary>
        /// New (completely dark) Moon
        /// </summary>
        New,

        /// <summary>
        /// One quarter lit, amount lit increasing daily
        /// </summary>
        WaxingCrescent,

        /// <summary>
        /// One half lit, amount lit increasing daily
        /// </summary>
        HalfMoonWaxing,

        /// <summary>
        /// Three quarters lit, amount lit increasing daily
        /// </summary>
        WaxingGibbous,

        /// <summary>
        /// Full moon, completely lit
        /// </summary>
        Full,

        /// <summary>
        /// Three quarters lit, amount lit decreasing daily
        /// </summary>
        WaningGibbous,

        /// <summary>
        /// One half lit, amount lit decreasing daily
        /// </summary>
        HalfMoonWaning,

        /// <summary>
        /// One quarter lit, amount lit decreasing daily
        /// </summary>
        WaningCrescent
    }

    public class Season {
        public static List<Season> Seasons = new List<Season>()
        {
            // Sunset/Sunrise is a scaling function, so in spring it starts at 6AM and moves back to 5AM
            // then summer it starts at 5AM and moves forward to 6 and so on in autumn and winter before returning to spring.
            new Season("SPRING")
            {
                Sunrise = (6, 5), Sunset = (19, 21), Length = 91,
                ModifierFunc = (percent, deviation) => 1.0 + (percent * deviation)
            },
            new Season("SUMMER")
            {
                Sunrise = (5, 6), Sunset = (21, 19), Length = 91,
                ModifierFunc = (percent, deviation) => (1.0 + deviation) - (percent * deviation)
            },
            new Season("AUTUMN")
            {
                Sunrise = (6, 7), Sunset = (19, 17), Length = 91,
                ModifierFunc = (percent, deviation) => (1.0 - (percent * deviation))
            },
            new Season("WINTER")
            {
                Sunrise = (7, 6), Sunset = (17, 19), Length = 91,
                ModifierFunc = (percent, deviation) => (1.0 - deviation) + (percent * deviation)
            },
        };

        public string Name { get; set; }
        public (int Begin, int End) Sunrise { get; set; }
        public (int Begin, int End) Sunset { get; set; }
        public int Length { get; set; }
        public Func<double,double,double> ModifierFunc { get; set; }

        public Season(string name) {
            Name = name;
        }

        /// <summary>
        /// Returns the hour and minute of the sunrise for the given timepoints day and season
        /// </summary>
        /// <param name="timePoint"></param>
        /// <returns></returns>
        public Time GetSunrise(Time timePoint) {
            var percent = (double)timePoint.DayOfSeason / Length; // day 14.0 / 91
            var time = Sunrise.Begin * (1.0 - percent) + Sunrise.End * percent;

            // Time will be something like (for early spring) 5.98901098901099, so take the 5 for the hour
            // then leave the fractional component and multiply by 60 to retrieve minutes past the hour.
            var newHour = (int)time;
            time -= (int)time;
            var newMinute = (int) (time * 60);

            var sunriseTime = new Time(newHour, newMinute);
            // Feed back a new Time instance with the sunrise.
            return sunriseTime;
        }

        /// <summary>
        /// Returns the hour & minute of the sunset for the given timepoints day and season
        /// </summary>
        /// <param name="timePoint"></param>
        /// <returns></returns>
        public Time GetSunset(Time timePoint) {
            var percent = (double)timePoint.DayOfSeason / Length; // day 14.0 / 91
            var time = Sunset.Begin * (1.0 - percent) + Sunset.End * percent;

            // The same as Sunrise, just it's later in the evening normally.
            var newHour = (int)time;
            time -= (int)time;
            var newMinute = (int)(time * 60);

            var sunsetTime = new Time(newHour, newMinute);
            // Feed back a new Calendar instance with the sunrise.
            return sunsetTime;
        }

        public double GetLightModifier(double percent, double deviation) {
            return ModifierFunc?.Invoke(percent, deviation)
                   ?? throw new Exception($"Season {Name} has no LightModifier function defined");
        }


        public static Season Get(string seasonName) {
            return Seasons.SingleOrDefault(season => season.Name == seasonName);
        }

        public static void AddSeason(Season info) {
            if (Seasons.Any(s => s.Name == info.Name)) {
                throw new Exception($"Season '{info.Name}' already registered");
            }

            Seasons.Add(info);
        }

        public static bool RemoveSeason(string seasonName) {
            var season = Get(seasonName);

            if (season != null) {
                Seasons.Remove(season);
                return true;
            }

            return false;
        }

        public static void ChangeSeasonLength(int length) {
            foreach (var item in Seasons) {
                item.Length = length;
            }
        }

        public static int GetAverageSeasonLength() => (int)Math.Round(Seasons.Average(s => s.Length));
        public static int GetSeasonLength(string seasonName) => Get(seasonName).Length;
        public static int GetSeasonLength(int seasonIndex) => Seasons[seasonIndex].Length;
        public static int GetTotalLength() => Seasons.Sum(s => s.Length);
    }

    public class Calendar {
        private Time _currentTime;

        /// <summary>
        /// Set this on starting a new game, it's important for calculating the actual time.
        /// </summary>
        public static Season StartSeason;

        /// <summary>
        /// The current time of the game world.
        /// </summary>
        public static Time Now { get; set; }

        /// <summary>
        /// Advance time by a specified number of turns.
        /// This is is designed to be called from the main game loop once per logical turn.
        /// </summary>
        /// <param name="turns"></param>
        /// <returns>The new time index</returns>
        public static Time Advance(long turns) => Now = Now.AddTime(TimeDuration.FromTurns(turns));

        /// <summary>
        /// Advance time by a time interval.
        /// </summary>
        /// <param name="timeDelta"></param>
        /// <returns>The new time index</returns>
        public static Time Advance(TimeDuration timeDelta) => Now = Now.AddTime(timeDelta);

        public Season CurrentSeason { get; set; }

        public Calendar() {
            _currentTime = new Time(0L);
            CurrentSeason = Season.Seasons[0];
            StartSeason = CurrentSeason;
            Now = _currentTime;
        }

        public Calendar(long turn) {
            _currentTime = new Time(turn);
            CurrentSeason = _currentTime.CurrentRealSeason;
            Now = _currentTime;
        }

        public Calendar(int minute, int hour, int day, Season season, int year) {
            var totalDays = TotalDaysFromSeason(season, year);

            // Current time is then the addition of Minutes, Hours, Days and days from season+year above
            _currentTime = new Time(
                TimeDuration.FromMinutes(minute)
                + TimeDuration.FromHours(hour)
                + TimeDuration.FromDays(day)
                + TimeDuration.FromDays(totalDays));

            Now = _currentTime;
        }

        /// <summary>
        /// Given a Current Season and the Year, work out how many days have passed since the beginning of the game, only deals with whole seasons.
        /// That is, add one any extra days/hours/minutes afterwards.
        /// </summary>
        /// <param name="season"></param>
        /// <param name="year"></param>
        /// <returns></returns>
        public static int TotalDaysFromSeason(Season season, int year) {
            // Attempt to reliably work out the current day based on the year & given season.
            // So for Autumn in the 4th year, the number of passed seasons is 16. (3+4*4)  (assuming 4 seasons default)
            // Then we sum all the season lengths between the starting season and now, to give a total number of days passed.
            var currentSeason = Season.Seasons.IndexOf(season) + 1; // Autumn:2 -> Autumn:3
            var startSeason = Season.Seasons.IndexOf(StartSeason) + 1; // Spring:0 -> Spring:1

            var totalSeasons = (currentSeason + (year * Season.Seasons.Count));
            int totalDays = 0;

            for (int i = startSeason; i <= totalSeasons; i++) {
                totalDays += Season.GetSeasonLength(i % Season.Seasons.Count);
            }

            return totalDays;
        }

        /// <summary>
        /// Get the average length of a year, that is the average all season lengths.
        /// </summary>
        /// <returns>Average Year length in days</returns>
        public static int GetAverageYearLength() => Season.GetAverageSeasonLength() * Season.Seasons.Count;

        /// <summary>
        /// Get the length of a year as the summation of all seasons lengths.
        /// </summary>
        /// <returns>Year length in days</returns>
        public static int GetYearLength() => Season.GetTotalLength();

        public static MoonPhase GetMoonPhase(Time time) {
            // One full phase every 2 months
            var moonPhaseDuration = time.CurrentRealSeason.Length * 2.0 / 3.0;

            var currentDay = time.DayOfSeason;
            double phaseChange = currentDay / moonPhaseDuration;
            var moonPhases = Enum.GetNames(typeof(MoonPhase)).Count();
            int currentPhase = (int)Math.Round(phaseChange * moonPhases) % moonPhases;
            return (MoonPhase) currentPhase;
        }

        public static double GetDaylightLevel(Time time) {
            double percent = Math.Max((double)(time.DayOfSeason+1) / time.CurrentRealSeason.Length, 0.01098901098901098901098901098901);
            double modifier = default;
            double deviation = 0.25;

            modifier = time.CurrentRealSeason.GetLightModifier(percent, deviation);

            return modifier * 100.0;
        }

        public static double GetLightLevel(Time time) {
            var now = new Time(time.HourOfDay, time.Minutes);
            var sunrise = time.CurrentRealSeason.GetSunrise(time);
            var sunset = time.CurrentRealSeason.GetSunset(time);

            double daylightLevel = GetDaylightLevel(time);

            var moonPhase = GetMoonPhase(time);
            var moonlight = 1 + ((int)moonPhase * TimeConstants.MoonlightPerQuarter);
            var twilightDuration = TimeDuration.FromHours(1);

            if (time.IsNightTime())
                return moonlight;

            if (now >= sunrise && now <= sunrise + twilightDuration) {
                var percent = (now - sunrise) / twilightDuration;
                return moonlight * (1.0 - percent) + daylightLevel * percent;
            }

            if (now >= sunset && now <= sunset + twilightDuration) {
                double percent = (now - sunset) / twilightDuration;
                return daylightLevel * (1.0 - percent) + moonlight * percent;
            }

            return daylightLevel;

        }
    }
}