using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OctoGhast.Entity;
using OctoGhast.MapGeneration.Dungeons;
using OctoGhast.Spatial;
using libtcod;
using OctoGhast.Renderer;

namespace OctoGhast
{
    class Program
    {
        static void Main(string[] args)
        {
	        var camera = new Camera(Vec.Zero, new Rect(80, 25), new Rect(80*3, 25*3));
	        var player = new Player(Vec.Zero, '@', TCODColor.amber);
			var mapGenerator = new BSPDungeonGenerator();

	        var engine = new Engine(80, 25, camera, player, mapGenerator);
            engine.Setup();

            while (engine.IsRunning) {
                engine.Update();
            }
        }
    }
}
