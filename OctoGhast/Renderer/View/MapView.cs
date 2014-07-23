using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
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
    public interface IMapViewModel
    {
        IPlayer Player { get; set; }
        ICamera Camera { get; set; }
        IGameMap Map { get; set; }

        LightMap<TileLightInfo> CalculateLightMap();
        string TooltipFor(Vec position);
    }

    public class MapViewModel : IMapViewModel
    {
        public IPlayer Player { get; set; }
        public ICamera Camera { get; set; }
        public IGameMap Map { get; set; }

        private Vec toView(Vec position, Rect constraint)
        {
            var cartCenter = constraint.Center;

            var Xs = (constraint.Width / 2) + (position.X - cartCenter.X);
            var Ys = (constraint.Height / 2) + (position.Y - cartCenter.Y);
            return new Vec(Xs, Ys);
        }

        public LightMap<TileLightInfo> CalculateLightMap() {
            return Map.CalculateFov(Camera.ViewFrustum.Center, 4, (x, y) => toView(new Vec(x, y), Camera.ViewFrustum));
        }
    }

    public class MapViewTemplate : PanelTemplate
    {
        public MapViewTemplate() {
        }

        public override Size CalculateSize() {
            return Size;
        }
    }

    public class MapView : Panel
    {
        private IMapViewModel Model { get; set; }

        public IPlayer Player { get; set; }
        public ICamera Camera { get; set; }
        public IGameMap Map { get; set; }

        public MapView(MapViewTemplate template, IMapViewModel model) : base(template) {
            Player = model.Player;
            Camera = model.Camera;
            Map = model.Map;
        }

        private Vec toWorld(int x, int y, Rect constraint) {
            return new Vec(constraint.TopLeft.X + x, constraint.TopLeft.Y + y);
        }

        private Vec toWorld(Vec pos, Rect constraint) {
            return constraint.TopLeft + pos;
        }

        protected override string DetermineTooltipText() {
            return Model.TooltipFor(toWorld(ScreenToLocal(CurrentMousePosition), Camera.ViewFrustum));
        }

        protected override void Redraw() {
            base.Redraw();

            var lightMap = Model.CalculateLightMap();

            for (int y = 0; y < Camera.ViewFrustum.Height; y++) {
                for (int x = 0; x < Camera.ViewFrustum.Width; x++) {
                    var worldPos = toWorld(x, y, Camera.ViewFrustum);

                    if (lightMap[x, y].IsLit) {
                        var tile = Map[worldPos];

                        Canvas.PrintChar(x, y, (char) tile.Glyph,
                            new Pigment(new Color(TCODColor.amber), new Color(TCODColor.black)));
                    }
                    else {
                        Canvas.PrintChar(x, y, ' ', new Pigment(new Color(TCODColor.black), new Color(TCODColor.black)));
                    }
                }
            }

            var playerFrustum = Rect.FromCenter(Player.Position, Camera.Size);

            var playerX = playerFrustum.TopRight.X - Player.Position.X;
            var playerY = playerFrustum.BottomLeft.Y - Player.Position.Y;
            var distanceFromCamera = playerFrustum.TopLeft - Camera.ViewFrustum.TopLeft;

            Canvas.PrintChar(playerX + distanceFromCamera.X, playerY + distanceFromCamera.Y, '@',
                new Pigment(new Color(TCODColor.brass), new Color(TCODColor.black)));
        }
    }
}