using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace OctoGhast.Server
{
    public class ServerConstants
    {
        public static double TickRate = TimeSpan.FromMilliseconds(20).TotalMilliseconds;
        public static double TickTime = 1000 / TickRate;
        public static double CombatTickRate = TimeSpan.FromSeconds(1).TotalMilliseconds;
        public static int TicksPerSecond = 20;
    }

    class Program
    {
        public static DateTime CurrentTime { get; set; }
        public static bool _closing;


        static void Main(string[] args) {
            var worldPath = "./DefaultWorld/";

            var World = new World(worldPath);

            var worldTick = 0UL;
            var now = DateTime.UtcNow;

            while (!_closing)
            {
                CurrentTime = DateTime.UtcNow;
                // Process game loop
                World.Tick(worldTick);

                // Process background services and such

                var nextTickDue = false;

                while (!nextTickDue)
                {
                    var elapsedTime = DateTime.UtcNow;
                    var span = elapsedTime - CurrentTime;

                    if (span.TotalMilliseconds > 1000 / ServerConstants.TicksPerSecond)
                        nextTickDue = false;
                    else
                    {
                        int sleepTime = (int)((1000 / ServerConstants.TicksPerSecond) - span.TotalMilliseconds);
                        Thread.Sleep(sleepTime);
                        nextTickDue = true;
                    }
                }

                Console.SetCursorPosition(0, 0);
                
                Console.WriteLine($"World time: Day {Calendar.Now.DayOfWeek()} - Season: {Calendar.Now.CurrentRealSeason.Name} - Year {2015 + Calendar.Now.TotalYears} - {Calendar.Now.ToTimeString(TimeFormat.Military)}");
                Console.WriteLine($"\t Moon phase: {Calendar.GetMoonPhase(Calendar.Now)}                           ");
                Console.WriteLine($"\t Game Turn: {Calendar.Now.Turns}");
                Console.WriteLine($"\t Cycle Time: {(DateTime.Now - CurrentTime).Milliseconds}                             ");
                Console.WriteLine("--------------------------------------------------------------");
                Console.WriteLine($"DataObject Count: {World.DataObjects.Count}");
                foreach (var dataObject in World.DataObjects) {
                    Console.WriteLine($"------ {dataObject.Value.GetType().FullName} ------");
                    var attrib = dataObject.Value.GetType().GetCustomAttribute<ServiceDataAttribute>();
                    Console.WriteLine($"\t{attrib.Name} -- {attrib.Description}");
                }

                worldTick++;
            }

        }
    }
}
