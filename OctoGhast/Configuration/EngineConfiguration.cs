using OctoGhast.Configuration;
using OctoGhast.DataStructures.Renderer;
using OctoGhast.Entity;

namespace OctoGhast.Renderer
{
	public class EngineConfiguration : IEngineConfiguration
	{
		public EngineConfiguration(IConfig config, ICamera camera, IPlayer player)
		{
			Width = config.Width;
			Height = config.Height;

			Camera = camera;
			Player = player;
		}

		public int Width { get; set; }
		public int Height { get; set; }

		public ICamera Camera { get; set; }
		public IPlayer Player { get; set; }
	}
}