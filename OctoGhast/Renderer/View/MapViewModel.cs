using System;
using System.Text;
using libtcod;
using OctoGhast.DataStructures.Lighting;
using OctoGhast.DataStructures.Map;
using OctoGhast.DataStructures.Renderer;
using OctoGhast.Entity;
using OctoGhast.Spatial;
using OctoGhast.UserInterface.Controls;
using OctoGhast.UserInterface.Core;
using OctoGhast.UserInterface.Theme;

namespace OctoGhast.Renderer.View
{
    public class MapViewModel : IMapViewModel
    {
        public IPlayer Player { get; set; }
        public ICamera Camera { get; set; }
        public IGameMap Map { get; set; }
        public bool DrawLighting { get; set; }

        public MapViewModel() {
            DrawLighting = true;
        }

        private Vec toView(Vec position, Rect constraint)
        {
            var cartCenter = constraint.Center;

            var Xs = (constraint.Width / 2) + (position.X - cartCenter.X);
            var Ys = (constraint.Height / 2) + (position.Y - cartCenter.Y);
            return new Vec(Xs, Ys);
        }

        public LightMap<TileLightInfo> CalculateLightMap() {
            return Map.CalculateFov(Camera.ViewFrustum.Center, 8, (x, y) => toView(new Vec(x, y), Camera.ViewFrustum));
        }

        public string TooltipFor(Vec position) {
            return "";
        }
    }
}