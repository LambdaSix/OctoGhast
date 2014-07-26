using OctoGhast.Framework;
using OctoGhast.Spatial;

namespace OctoGhast
{
	internal class Program
	{
        /*
        // TODO: Create an application with an event loop and hooks for various things
		private static void Main(string[] args) {
			var mapSize = new Rect(80*3, 25*3);

		    var camera = new Camera(Vec.Zero, new Size(80, 25));
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
        */

	    static void Main(string[] args) {
	        GameInfo info = new GameInfo()
	        {
	            Title = "Test",
	            ScreenSize = new Size(80, 40),
	            Font = "celtic_garamond_10x10_gs_tc.png",
	        };

	        UserInterface.Core.Config.HeightFunc = () => info.ScreenSize.Height;
	        UserInterface.Core.Config.WidthFunc = () => info.ScreenSize.Width;

	        using (var application = new OctoghastGame()) {
	            application.Start(info);
	        }
	    }
	}
}