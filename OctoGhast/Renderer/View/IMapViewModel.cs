using OctoGhast.DataStructures.Lighting;
using OctoGhast.DataStructures.Map;
using OctoGhast.DataStructures.Renderer;
using OctoGhast.Entity;
using OctoGhast.Spatial;

namespace OctoGhast.Renderer.View
{
    public interface IMapViewModel
    {
        IPlayer Player { get; set; }
        ICamera Camera { get; set; }
        IGameMap Map { get; set; }
        bool DrawLighting { get; set; }

        LightMap<TileLightInfo> CalculateLightMap();
        string TooltipFor(Vec position);
    }
}