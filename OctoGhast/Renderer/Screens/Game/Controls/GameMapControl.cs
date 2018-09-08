using System;
using InfiniMap;
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
        public IGameViewModel Model { get; set; }

        public GameMapControlTemplate() {
            Size = new Size(80, 24);
        }
    }

    /// <summary>
    /// Handles the Drawing of the World to the screen.
    /// No handling of game logic is done here, that should be handled by WorldInstance and Systems
    /// </summary>
    public class GameMapControl : Panel
    {
        /// <summary>
        /// Access to the WorldInstance is handled via the ViewModel
        /// </summary>
        IGameViewModel Model { get; set; }

        // Shortcuts
        public IPlayer Player => Model.World.Player;
        public ICamera Camera => Model.World.Camera;
        public Map2D<ITile> Map => Model.World.Map;

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

            // Should the Lightmap calculations move here or be handled by a System?
            var lightMap = Model.CalculateLightMap();

            for (int y = 0; y < Camera.ViewFrustum.Height; y++)
            {
                for (int x = 0; x < Camera.ViewFrustum.Width; x++)
                {
                    var worldPos = toWorld(x, y, Camera.ViewFrustum);

                    if (lightMap[x, y].IsLit) {
                        var tile = Map[worldPos.X, worldPos.Y];
                        var color = lightMap[x, y].LightColor ?? new Color(XColor.Gray);

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
                new Pigment(new Color(XColor.Brown), new Color(XColor.Black)));

            Canvas.PrintString(0, 0, "FPS: " + Framework.Game.FrameCounter.CurrentFramesPerSecond);
        }
    }
}