using OctoGhast.DataStructures.Lighting;
using OctoGhast.Spatial;
using OctoGhast.World;

namespace OctoGhast.Renderer.View
{
    public interface IGameViewModel
    {
        WorldInstance World { get; set; }

        LightMap<TileLightInfo> CalculateLightMap();
        string TooltipFor(Vec position);
    }
}