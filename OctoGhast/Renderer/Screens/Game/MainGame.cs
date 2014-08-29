using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using libtcod;
using OctoGhast.Entity;
using OctoGhast.Map;
using OctoGhast.MapGeneration.Dungeons;
using OctoGhast.Renderer.Screens.Game.Controls;
using OctoGhast.Renderer.View;
using OctoGhast.Spatial;
using OctoGhast.UserInterface.Controls;
using OctoGhast.UserInterface.Core;
using OctoGhast.UserInterface.Core.Messages;

namespace OctoGhast.Renderer.Screens
{
    public class MainGame : ScreenBase
    {
        private IMapViewModel MapModel { get; set; }

        public MainGame() {
            RegisterAction((int)GameActions.GameMap_MoveNorth, () => MapModel.Player.MoveTo(MapModel.Player.Position.Offset(0, -1)));
            RegisterAction((int)GameActions.GameMap_MoveSouth, () => MapModel.Player.MoveTo(MapModel.Player.Position.Offset(0, +1)));
            RegisterAction((int)GameActions.GameMap_MoveLeft, () => MapModel.Player.MoveTo(MapModel.Player.Position.Offset(-1, 0)));
            RegisterAction((int)GameActions.GameMap_MoveRight, () => MapModel.Player.MoveTo(MapModel.Player.Position.Offset(+1, 0)));
            RegisterAction((int) GameActions.GameMap_ShowLighting, () => MapModel.DrawLighting = !MapModel.DrawLighting);
        }

        public override void OnSettingUp() {
            base.OnSettingUp();

            var windowSize = ParentWindow.ParentApplication.CurrentWindow.Size;

            MapModel = new MapViewModel()
            {
                Camera = new Camera(Vec.Zero, windowSize),
                Map = new GameMap(windowSize.Height, windowSize.Width),
                Player = new Player(Vec.Zero, '@', TCODColor.amber),
            };

            MapModel.Camera.BindTo(MapModel.Player);

            var mapGenerator = new BSPDungeonGenerator();
            mapGenerator.PlayerPlacementFunc = rect => {
                Console.WriteLine("Putting player at {0}", rect.Center);
                MapModel.Player.MoveTo(rect.Center);
            };
            mapGenerator.MobilePlacementFunc =
                rect => Console.WriteLine("Wanted to place a Mobile at ({0})", rect.TopLeft.ToString());
            mapGenerator.TileFactory = () => new Tile {Glyph = '#'};

            mapGenerator.GenerateMap(new Size(70, 70));

            for (int x = 0; x < mapGenerator.Dimensions.Bounds.TopRight.X; x++) {
                for (int y = 0; y < mapGenerator.Dimensions.Bounds.BottomLeft.Y; y++) {
                    MapModel.Map[x, y] = mapGenerator.Map[x, y];
                }
            }

            /* Control Templates */
            var mapTemplate = new GameMapControlTemplate()
            {
                Model = MapModel,
                Size = windowSize
            };

            /* Control widgets */

            var mapControl = new GameMapControl(mapTemplate);

            ParentWindow.AddControls(new ControlBase[]
            {
                mapControl
            });
        }
    }
}