using OctoGhast.DataStructures.Lighting;
using OctoGhast.DataStructures.Map;
using OctoGhast.DataStructures.Renderer;
using OctoGhast.Spatial;

namespace OctoGhast.Renderer.View
{
    public class MapViewModel : IMapViewModel
    {
        public Vec PlayerPosition { get; set; }
        public ICamera Camera { get; set; }
        public IGameMap Map { get; set; }
        public bool DrawLighting { get; set; }

        public MapViewModel()
        {
            PlayerPosition = Vec.Zero;
            DrawLighting = true;
        }

        private Vec ToView(Vec position, Rect constraint)
        {
            var cartCenter = constraint.Center;
            var xs = (constraint.Width / 2) + (position.X - cartCenter.X);
            var ys = (constraint.Height / 2) + (position.Y - cartCenter.Y);
            return new Vec(xs, ys);
        }

        public LightMap<TileLightInfo> CalculateLightMap()
        {
            return Map.CalculateFov(Camera.ViewFrustum.Center, 8,
                (x, y) => ToView(new Vec(x, y), Camera.ViewFrustum));
        }

        public string TooltipFor(Vec position)
        {
            return string.Empty;
        }
    }
}