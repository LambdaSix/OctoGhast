using OctoGhast.Configuration;
using OctoGhast.DataStructures.Renderer;
using OctoGhast.Entity;
using OctoGhast.MapGeneration;

namespace OctoGhast.Renderer
{
	public class EngineConfiguration : IEngineConfiguration
	{
		public EngineConfiguration(IConfig config, ICamera camera, IPlayer player, ITileMapGenerator mapGenerator)
		{
			Width = config.Width;
			Height = config.Height;

			Camera = camera;
			Player = player;
			MapGenerator = mapGenerator;
		}

		public int Width { get; set; }
		public int Height { get; set; }

		public ICamera Camera { get; set; }
		public IPlayer Player { get; set; }
		public ITileMapGenerator MapGenerator { get; set; }
	}
}