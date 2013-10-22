using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using libtcod;

namespace OctoGhast
{
    class Program
    {
        static void Main(string[] args) {
            var engine = new Engine(80, 25);
            engine.Setup();

            while (true) {
                engine.Tick();
            }

            Console.ReadKey();
        }
    }
}
