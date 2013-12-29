using OctoGhast.DataStructures.Renderer;
using OctoGhast.Entity;
using OctoGhast.MapGeneration;

namespace OctoGhast.Renderer
{
	public interface IEngineConfiguration
	{
		int Width { get; set; }
		int Height { get; set; }

		ICamera Camera { get; set; }
		IPlayer Player { get; set; }
		ITileMapGenerator MapGenerator { get; set; }
	}
}