using System;
using OctoGhast.Map;
using OctoGhast.MapGeneration.Dungeons;
using OctoGhast.Renderer.Screens.Game.Controls;
using OctoGhast.Renderer.View;
using OctoGhast.Spatial;
using OctoGhast.UserInterface.Controls;
using OctoGhast.UserInterface.Core;

namespace OctoGhast.Renderer.Screens
{
    public class MainGame : ScreenBase
    {
        private IMapViewModel MapModel { get; set; }

        public MainGame()
        {
            RegisterAction((int)GameActions.GameMap_ShowLighting,
                () => MapModel.DrawLighting = !MapModel.DrawLighting);
        }

        public override void OnSettingUp()
        {
            base.OnSettingUp();

            var windowSize = ParentWindow.ParentApplication.CurrentWindow.Size;

            MapModel = new MapViewModel
            {
                Camera = new Camera(Vec.Zero, windowSize),
                Map = new GameMap(windowSize.Height, windowSize.Width),
                PlayerPosition = Vec.Zero
            };

            var mapGenerator = new BSPDungeonGenerator();
            mapGenerator.PlayerPlacementFunc = rect =>
            {
                Console.WriteLine("Putting player at {0}", rect.Center);
                MapModel.PlayerPosition = rect.Center;
                MapModel.Camera.MoveTo(rect.Center);
            };
            mapGenerator.MobilePlacementFunc =
                rect => Console.WriteLine("Wanted to place a Mobile at ({0})", rect.TopLeft);
            mapGenerator.TileFactory = () => new Tile { Glyph = '#' };

            mapGenerator.GenerateMap(new Size(70, 70));

            for (int x = 0; x < mapGenerator.Dimensions.Bounds.TopRight.X; x++)
            {
                for (int y = 0; y < mapGenerator.Dimensions.Bounds.BottomLeft.Y; y++)
                {
                    MapModel.Map[x, y] = mapGenerator.Map[x, y];
                }
            }

            var mapTemplate = new GameMapControlTemplate
            {
                Model = MapModel,
                Size = windowSize
            };

            var mapControl = new GameMapControl(mapTemplate);

            ParentWindow.AddControls(new ControlBase[] { mapControl });
        }
    }
}