using System;
using OctoGhast.DataStructures.Lighting;
using OctoGhast.DataStructures.Renderer;
using OctoGhast.Entity;
using OctoGhast.Spatial;

namespace OctoGhast.Renderer.View
{
    public class GameViewModel : IGameViewModel
    {
        public World World { get; set; }

        // public Player Player => World.Player;
        public bool DrawLighting { get; set; }

        public GameViewModel() {
            DrawLighting = true;
        }

        public LightMap<TileLightInfo> CalculateLightMap() {
            throw new NotImplementedException();
            // return World.CalculateFov(Camera.ViewFrustum.Center, 8, (x, y) => new Vec(x, y).ToView(Camera.ViewFrustum));
        }

        public string TooltipFor(Vec position) {
            return "";
        }
    }

    public static class VecExtensions {
        public static Vec ToView(this Vec self, Rect constraint) {
            var cartCenter = constraint.Center;

            var Xs = (constraint.Width / 2) + (self.X - cartCenter.X);
            var Ys = (constraint.Height / 2) + (self.Y - cartCenter.Y);
            return new Vec(Xs, Ys);
        }
    }
}