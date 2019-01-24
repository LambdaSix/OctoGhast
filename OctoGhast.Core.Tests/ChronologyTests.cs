using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace OctoGhast.Core.Tests
{
    /*
     * Unit tests for Chronological features.
     * - Calendar
     * - Time
     * - TimeDuration
     */

    [TestFixture]
    public class TimeDurationTests {
        [Test]
        public void Comparable() {
            var t1 = TimeDuration.FromHours(1);
            var t2 = TimeDuration.FromHours(1);

            Assert.That(t1, Is.EqualTo(t2));
            Assert.That(t1 == t2);

            t1 = t1.AddDays(1);
            Assert.That(t1, Is.Not.EqualTo(t2));
            Assert.That(t1 != t2);
        }

        [Test]
        public void Addable() {
            var t1 = TimeDuration.FromHours(1);
            var t2 = TimeDuration.FromHours(1);

            var t3 = TimeDuration.FromHours(2);

            Assert.That(t1 + t2 == t3);
        }

        [Test]
        public void Subtractable() {
            var t1 = TimeDuration.FromHours(2);
            var t2 = TimeDuration.FromHours(1);

            var t3 = TimeDuration.FromHours(1);

            Assert.That(t1 - t2 == t3);
        }

        [Test]
        public void Remainder() {
            var t1 = TimeDuration.FromHours(1);
            var t2 = TimeDuration.FromHours(1).AddMinutes(30);

            var t3 = TimeDuration.FromMinutes(30);

            Assert.That((t2 % t1), Is.EqualTo(t3));
        }

        [Test]
        public void Multiply() {
            var t1 = TimeDuration.FromHours(1);
            var t2 = t1 * 2.5;

            var t3 = TimeDuration.FromHours(2).AddMinutes(30);

            Assert.That(t2, Is.EqualTo(t3));
        }

        [Test]
        public void Division() {
            var t1 = TimeDuration.FromHours(2);
            var t2 = t1 / 2.5;

            var t3 = TimeDuration.FromMinutes(48);

            Assert.That(t2, Is.EqualTo(t3));
        }

        [Test]
        public void FromString() {
            var str = "1 day 12 hours";
            var str2 = "1 day, 12 hours";
            var str3 = "1 day 1 day 12 hours";
            var str4 = "1 year, 6 months, 4 days, 6 hours";

            
            Assert.AreEqual(TimeDuration.FromString("1 day, 12 hours").Turns, TimeDuration.FromDays(1).AddHours(12).Turns);

            var time2 = TimeDuration.FromString(str2);
            Assert.AreEqual(time2.Turns, TimeDuration.FromDays(1).AddHours(12).Turns);

            var time3 = TimeDuration.FromString(str3);
            Assert.AreEqual(time3.Turns, TimeDuration.FromDays(1).AddDays(1).AddHours(12).Turns);

            var time4 = TimeDuration.FromString(str4);
            Assert.AreEqual(time4.Turns, TimeDuration.FromYears(1).AddMonths(6).AddDays(4).AddHours(6).Turns);
        }

        [Test]
        public void Properties() {
            // Due to 1 Turn being 6 Seconds, TimeDuration doesn't deal with a smaller second granularity than 6s
            var t1 = TimeDuration.FromSeconds(6);
            Assert.That(t1.Seconds, Is.EqualTo(6));
            var t1a = t1.AddSeconds(4); // Adding 4 seconds is a noop because it's <6 seconds.
            Assert.That(t1.Seconds, Is.EqualTo(6));

            var t2 = TimeDuration.FromMinutes(1);
            Assert.That(t2.Minutes, Is.EqualTo(1));
            Assert.That(t2.TotalSeconds, Is.EqualTo(60));

            var t3 = TimeDuration.FromHours(1);
            Assert.That(t3.TotalHours, Is.EqualTo(1));
            Assert.That(t3.TotalMinutes, Is.EqualTo(60));
            Assert.That(t3.TotalSeconds, Is.EqualTo(60 * 60));

            var t4 = TimeDuration.FromDays(1);
            Assert.That(t4.TotalDays, Is.EqualTo(1));
            Assert.That(t4.TotalHours, Is.EqualTo(24));
            Assert.That(t4.TotalMinutes, Is.EqualTo(60 * 24));
            Assert.That(t4.TotalSeconds, Is.EqualTo(60 * 60 * 24));

            var t5 = TimeDuration.FromWeeks(1);
            Assert.That(t5.TotalWeeks, Is.EqualTo(1));
            Assert.That(t5.TotalDays, Is.EqualTo(7));
            Assert.That(t5.TotalHours, Is.EqualTo(24 * 7));
            Assert.That(t5.TotalMinutes, Is.EqualTo(60 * 24 * 7));
            Assert.That(t5.TotalSeconds, Is.EqualTo(60 * 60 * 24 * 7));

            var t6 = TimeDuration.FromYears(1);
            Assert.That(t6.TotalYears, Is.EqualTo(1));
            Assert.That(t6.TotalWeeks, Is.EqualTo(1 * 52));
            Assert.That(t6.TotalDays, Is.EqualTo(7 * 52));
            Assert.That(t6.TotalHours, Is.EqualTo(24 * 7 * 52));
            Assert.That(t6.TotalMinutes, Is.EqualTo(60 * 24 * 7 * 52));
            Assert.That(t6.TotalSeconds, Is.EqualTo(60 * 60 * 24 * 7 * 52));

            var t7 = TimeDuration.FromDays(91 * 2).AddHours(14).AddMinutes(45).AddSeconds(36);
            Assert.That(t7.TotalSeasons, Is.EqualTo(2));
            Assert.That(t7.HourOfDay, Is.EqualTo(14));
            Assert.That(t7.Minutes, Is.EqualTo(45));
            Assert.That(t7.Seconds, Is.EqualTo(36));

            Console.WriteLine(t7.ToString());
        }
    }

    [TestFixture]
    public class TimeTests {
        [Test]
        public void Comparable() {
            // One day past the epoch
            var t1 = new Time(TimeDuration.FromDays(1));
            var t2 = new Time(TimeDuration.FromDays(1));

            Assert.That(t1, Is.EqualTo(t2));
            Assert.That(t1 == t2);

            // One week past the epoch
            var t3 = new Time(TimeDuration.FromDays(7));

            Assert.That(t1, Is.Not.EqualTo(t3));
            Assert.That(t1 != t3);
        }

        [Test]
        public void Addable() {
            var t1 = new Time(TimeDuration.FromDays(1));
            t1 = t1.AddTime(TimeDuration.FromDays(1));

            Assert.That(t1.TotalDays, Is.EqualTo(2));
        }

        [Test]
        public void Subtractable() {
            var t1 = new Time(TimeDuration.FromDays(2));
            t1 = t1.SubtractTime(TimeDuration.FromDays(1));

            Assert.That(t1.TotalDays, Is.EqualTo(1));
        }

        [Test]
        public void Nighttime() {
            // Spring [0]
            var s_t1 = new Time(TimeDuration.FromHours(4)); // 0400, before dawn in spring
            var s_t2 = new Time(TimeDuration.FromHours(9)); // 0900, after dawn in spring
            var s_t3 = new Time(TimeDuration.FromHours(19)); // 1900, after sunset in spring, still twilight
            var s_t4 = new Time(TimeDuration.FromHours(20).AddMinutes(1)); // 2001, after sunset in spring, after sunset+twilight

            Assert.That(s_t1.CurrentRealSeason.Name == "SPRING");

            Assert.That(s_t1.IsNightTime(), Is.True);
            Assert.That(s_t2.IsNightTime(), Is.False);
            Assert.That(s_t3.IsNightTime(), Is.False);
            Assert.That(s_t4.IsNightTime(), Is.True);

            // Summer [0]
            var su_t1 = new Time(TimeDuration.FromDays(91+1).AddHours(4)); // 0400, before dawn in summer
            var su_t2 = new Time(TimeDuration.FromDays(91+1).AddHours(9)); // 0900, after dawn in summer
            var su_t3 = new Time(TimeDuration.FromDays(91+1).AddHours(21).AddMinutes(30)); // 2101, after sunset in summer, still twilight
            var su_t4 = new Time(TimeDuration.FromDays(91+1).AddHours(22)); // 2200, after sunset + twilight

            Assert.That(su_t1.CurrentRealSeason.Name, Is.EqualTo("SUMMER"));

            Assert.That(su_t1.IsNightTime(), Is.True);
            Assert.That(su_t2.IsNightTime(), Is.False);
            Assert.That(su_t3.IsNightTime(), Is.False);
            Assert.That(su_t4.IsNightTime(), Is.True);

            // Autumn [0]
            var au_t1 = new Time(TimeDuration.FromDays(91+91+1).AddHours(5)); // 0500, before dawn in autumn
            var au_t2 = new Time(TimeDuration.FromDays(91+91+1).AddHours(8)); // 0800, after dawn in autumn
            var au_t3 = new Time(TimeDuration.FromDays(91+91+1).AddHours(19).AddMinutes(1)); // 1901, after sunset in autumn, still twilight
            var au_t4 = new Time(TimeDuration.FromDays(91+91+1).AddHours(22)); // 2200, after sunset + twilight

            Assert.That(au_t1.CurrentRealSeason.Name, Is.EqualTo("AUTUMN"));

            Assert.That(au_t1.IsNightTime(), Is.True);
            Assert.That(au_t2.IsNightTime(), Is.False);
            Assert.That(au_t3.IsNightTime(), Is.False);
            Assert.That(au_t4.IsNightTime(), Is.True);

            // Winter [0]
            var wi_t1 = new Time(TimeDuration.FromDays((3*91)+1).AddHours(5)); // 0600, before dawn in winter
            var wi_t2 = new Time(TimeDuration.FromDays((3 * 91) + 1).AddHours(8)); // 0800, after dawn in winter
            var wi_t3 = new Time(TimeDuration.FromDays((3 * 91) + 1).AddHours(17)); // 1700, after sunset in winter, still twilight
            var wi_t4 = new Time(TimeDuration.FromDays((3 * 91) + 1).AddHours(19).AddMinutes(1)); // 1701, after sunset + twilight

            Assert.That(wi_t1.CurrentRealSeason.Name, Is.EqualTo("WINTER"));

            Assert.That(wi_t1.IsNightTime(), Is.True);
            Assert.That(wi_t2.IsNightTime(), Is.False);
            Assert.That(wi_t3.IsNightTime(), Is.False);
            Assert.That(wi_t4.IsNightTime(), Is.True);
        }

        [Test]
        public void DaylightLevel() {
            var lightCurve = new double[Calendar.GetYearLength()];
            for (int i = 0; i < Calendar.GetYearLength(); i++) {
                var time = new Time(TimeDuration.FromDays(i));
                lightCurve[i] = Calendar.GetDaylightLevel(time);
            }

            Assert.That(lightCurve, Is.Not.Empty);
        }

        [Test]
        public void LightLevel() {
            var lightCurve = new double[24];
            for (int i = 0; i < 24; i++) {
                var time = new Time(TimeDuration.FromDays(30).AddHours(i));
                lightCurve[i] = Calendar.GetLightLevel(time);
            }

            Assert.That(lightCurve, Is.Not.Empty);
        }

        [Test]
        public void TestStringify() {
            var t1 = new Time(TimeDuration.FromHours(9).AddMinutes(45).AddSeconds(36));
            var t2 = new Time(TimeDuration.FromHours(19).AddMinutes(45).AddSeconds(24));

            var t1_military = t1.ToTimeString(TimeFormat.Military);
            var t2_military = t2.ToTimeString(TimeFormat.Military);

            Assert.That(t1_military, Is.EqualTo("0945.36"));
            Assert.That(t2_military, Is.EqualTo("1945.24"));

            var t1_24Hour = t1.ToTimeString(TimeFormat.TwentyFourHour);
            var t2_24Hour = t2.ToTimeString(TimeFormat.TwentyFourHour);

            Assert.That(t1_24Hour, Is.EqualTo("09:45:36"));
            Assert.That(t2_24Hour, Is.EqualTo("19:45:24"));

            var t1_civilian = t1.ToTimeString(TimeFormat.Civilian);
            var t2_civilian = t2.ToTimeString(TimeFormat.Civilian);

            Assert.That(t1_civilian, Is.EqualTo("9:45:36 AM"));
            Assert.That(t2_civilian, Is.EqualTo("7:45:24 PM"));
        }

        [Test]
        public void DaysOfWeek() {
            // The Epoch starts is a thursday.
            var t1 = new Time();
            Assert.That(t1.DayOfWeek() == Weekday.Thursday);

            // Then just check it continues to work for >1 week ahead
            t1 += TimeDuration.FromDays(1);
            Assert.That(t1.DayOfWeek() == Weekday.Friday);

            t1 += TimeDuration.FromDays(1);
            Assert.That(t1.DayOfWeek() == Weekday.Saturday);

            t1 += TimeDuration.FromDays(1);
            Assert.That(t1.DayOfWeek() == Weekday.Sunday);

            t1 += TimeDuration.FromDays(1);
            Assert.That(t1.DayOfWeek() == Weekday.Monday);

            t1 += TimeDuration.FromDays(1);
            Assert.That(t1.DayOfWeek() == Weekday.Tuesday);

            t1 += TimeDuration.FromDays(1);
            Assert.That(t1.DayOfWeek() == Weekday.Wednesday);

            t1 += TimeDuration.FromDays(1);
            Assert.That(t1.DayOfWeek() == Weekday.Thursday);

            t1 += TimeDuration.FromDays(1);
            Assert.That(t1.DayOfWeek() == Weekday.Friday);
        }
    }

    [TestFixture]
    public class CalendarTests {

    }

    [TestFixture]
    public class SeasonTests {
        [Test]
        public void SunriseSetCurve()
        {
            Console.WriteLine("day,rise_hour,rise_minute,set_hour,set_minute,season");
            int seasonIndex = 0;

            foreach (var season in Season.Seasons)
            {
                var origin = new Time(); // New date at the epoch, year 0, day 0, spring
                var riseCurve = new (int hour, int minute)[season.Length];
                var setCurve = new (int hour, int minute)[season.Length];

                var riseBounds = season.Sunrise;
                var setBounds = season.Sunset;

                for (int i = 0; i < season.Length; i++)
                {
                    var rise = season.GetSunrise(origin);
                    var set = season.GetSunset(origin);

                    riseCurve[i] = (rise.HourOfDay, rise.Minutes);
                    setCurve[i] = (set.HourOfDay, set.Minutes);

                    origin = origin.AddTime(TimeDuration.FromDays(1));
                }

                var riseSet = riseCurve.Zip(setCurve, (rise, set) => new { Rise = rise, Set = set });
                foreach (var pair in riseSet.Select((s, i) => (s, (i + (seasonIndex * 91)))))
                {
                    var (item, index) = pair;
                    var (rise, set) = (item.Rise, item.Set);

                    Console.WriteLine($"{index},{rise.hour},{rise.minute},{set.hour},{set.minute},{season.Name}");
                }

                seasonIndex++;

                // Assert that the curves at least fall within the bounds, examine the exported CSV data to check further
                Assert.That(riseCurve.First().hour, Is.GreaterThanOrEqualTo(riseBounds.Begin));
                Assert.That(riseCurve.Last().hour, Is.LessThanOrEqualTo(riseBounds.End));

                Assert.That(setCurve.First().hour, Is.GreaterThanOrEqualTo(setBounds.Begin));
                Assert.That(setCurve.Last().hour, Is.LessThanOrEqualTo(setBounds.End));
            }
        }
    }
}
