using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ninject;
using OctoGhast.Configuration;
using OctoGhast.DataStructures.Renderer;
using OctoGhast.Entity;
using OctoGhast.MapGeneration;
using OctoGhast.MapGeneration.Dungeons;
using OctoGhast.Spatial;
using libtcod;
using OctoGhast.Renderer;

namespace OctoGhast
{
	internal class Program
	{
		private static void Main(string[] args) {
			var mapSize = new Rect(80*3, 25*3);

			var camera = new Camera(Vec.Zero, new Rect(50, 50), mapSize);
			var player = new Player(Vec.Zero, '@', TCODColor.amber);
			var mapGenerator = new BSPDungeonGenerator();

			using (var kernel = new StandardKernel()) {
				kernel.Bind<ICamera>().ToConstant(camera);
				kernel.Bind<IPlayer>().ToConstant(player);
				kernel.Bind<ITileMapGenerator>().ToConstant(mapGenerator);
				kernel.Bind<IConfig>().To<Config>();
				kernel.Bind<IEngineConfiguration>().To<EngineConfiguration>();
				kernel.Bind<IEngine>().To<Engine>();

				var engineRoot = kernel.Get<IEngine>();
				engineRoot.Setup();

				while (engineRoot.IsRunning) {
					engineRoot.Update();
				}
			}
		}
	}
}