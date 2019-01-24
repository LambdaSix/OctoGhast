using OctoGhast.DataStructures.Lighting;
using OctoGhast.Spatial;

namespace OctoGhast.Renderer.View
{
    public interface IGameViewModel
    {
        World World { get; set; }

        LightMap<TileLightInfo> CalculateLightMap();
        string TooltipFor(Vec position);
    }
}