using OctoGhast.DataStructures.Map;
using OctoGhast.DataStructures.Renderer;
using OctoGhast.Entity;
using OctoGhast.Renderer.View;
using OctoGhast.Spatial;
using OctoGhast.UserInterface.Controls;
using OctoGhast.UserInterface.Core;
using OctoGhast.UserInterface.Theme;
using XColor = Microsoft.Xna.Framework.Color;

namespace OctoGhast.Renderer.Screens.Game.Controls
{
    public class GameMapControlTemplate : PanelTemplate
    {
        public IMapViewModel Model { get; set; }

        public GameMapControlTemplate() {
            Size = new Size(80, 24);
        }
    }

    public class GameMapControl : Panel
    {
        IMapViewModel Model { get; set; }

        public IPlayer Player {
            get { return Model.Player; }
        }

        public ICamera Camera {
            get { return Model.Camera; }
        }

        public IGameMap Map {
            get { return Model.Map; }
        }

        public GameMapControl(GameMapControlTemplate template) : base(template) {
            Model = template.Model;
            Size = template.CalculateSize();
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

            for (int y = 0; y < Camera.ViewFrustum.Height; y++)
            {
                for (int x = 0; x < Camera.ViewFrustum.Width; x++)
                {
                    var worldPos = toWorld(x, y, Camera.ViewFrustum);

                    if (!Model.DrawLighting || lightMap[x, y].IsLit)
                    {
                        var tile = Map[worldPos];
                        var color = lightMap[x, y].LightColor ?? new Color(Microsoft.Xna.Framework.Color.Gray);

                        Canvas.PrintChar(x, y, (char) tile.Glyph, new Pigment(color, new Color(XColor.Black)));
                    }
                    else {
                        Canvas.PrintChar(x, y, ' ', new Pigment(new Color(XColor.Black), new Color(XColor.Black)));
                    }
                }
            }

            var playerFrustum = Rect.FromCenter(Player.Position, Camera.Size);

            var playerX = playerFrustum.TopRight.X - Player.Position.X;
            var playerY = playerFrustum.BottomLeft.Y - Player.Position.Y;
            var distanceFromCamera = playerFrustum.TopLeft - Camera.ViewFrustum.TopLeft;

            Canvas.PrintChar(playerX + distanceFromCamera.X, playerY + distanceFromCamera.Y, '@',
                new Pigment(new Color(Microsoft.Xna.Framework.Color.Brown), new Color(XColor.Black)));

            Canvas.PrintString(0, 0, "FPS: " + Framework.Game.FrameCounter.CurrentFramesPerSecond);
        }
    }
}