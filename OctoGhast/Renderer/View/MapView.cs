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

        public string TooltipFor(Vec position) {
            var sb = new StringBuilder();
            var tile = Map[position];
            sb.AppendLine("Tile: " + tile.Glyph);
            sb.AppendLine("Transparent: " + tile.IsExplored);
            sb.AppendLine("Walkable: " + tile.IsWalkable);
            sb.Append("Explored: " + tile.IsExplored);
            return sb.ToString();
        }
    }

    public class MapViewTemplate : PanelTemplate
    {
        public override Size CalculateSize() {
            var width = Size.Width;
            var height = Size.Height;

            if (HasFrameBorder) {
                height += 2;
                width += 2;
            }

            return new Size(width, height);
        }
    }

    public class MapView : Panel
    {
        private IMapViewModel Model { get; set; }

        public IPlayer Player {
            get { return Model.Player; }
        }

        public ICamera Camera {
            get { return Model.Camera; }
        }

        public IGameMap Map {
            get { return Model.Map; }
        }

        public MapView(MapViewTemplate template, IMapViewModel model) : base(template) {
            Model = model;
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

        /// <summary>
        /// Determine screen drawing offset due to framing.
        /// </summary>
        private int DrawOffset { get { return (HasFrame) ? 2 : 0; } }

        private Random _random = new Random();

        protected override void Redraw() {
            base.Redraw();

            var lightMap = Model.CalculateLightMap();

            for (int y = DrawOffset; y < Camera.ViewFrustum.Height; y++) {
                for (int x = DrawOffset; x < Camera.ViewFrustum.Width; x++) {
                    var worldPos = toWorld(x, y, Camera.ViewFrustum);

                    if (lightMap[x, y].IsLit) {
                        var tile = Map[worldPos];
                        var color = lightMap[x, y].LightColor;

                        Canvas.PrintChar(x, y, (char) tile.Glyph,
                            new Pigment(color, new Color(TCODColor.black)));
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

            Canvas.PrintString(0, 0, "FPS: " + TCODSystem.getFps());
        }
    }
}