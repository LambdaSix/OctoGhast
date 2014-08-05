using System;
using libtcod;
using OctoGhast.Renderer.Screens;
using OctoGhast.Renderer.View;
using OctoGhast.Spatial;
using OctoGhast.UserInterface.Controls;
using OctoGhast.UserInterface.Core;
using OctoGhast.UserInterface.Core.Messages;

namespace OctoGhast.Renderer
{
    public class MapViewController : ScreenBase
    {
        public MapView MapView { get; set; }

        public MapViewController(MapView mapView) {
            MapView = mapView;
        }

        private Random _random = new Random();

        public override void OnSettingUp() {
            base.OnSettingUp();

            Button quitButton = new Button(new ButtonTemplate()
            {
                Label = "QUIT",
                UpperLeftPos = new Vec(3, 0),
            });

            ParentWindow.AddControls(new[] {quitButton});

            quitButton.ButtonClick += (o, e) => ParentWindow.ParentApplication.IsQuitting = true;
        }

        public override void OnKeyPressed(KeyboardData keyData) {
            base.OnKeyPressed(keyData);

            switch (keyData.KeyCode) {
                case TCODKeyCode.Up:
                    MapView.Player.Position.Offset(0, -1);
                    Console.WriteLine("Up!");
                    break;
                case TCODKeyCode.Down:
                    MapView.Player.Position.Offset(0, 1);
                    break;
                case TCODKeyCode.Right:
                    MapView.Player.Position.Offset(1, 0);
                    break;
                case TCODKeyCode.Left:
                    MapView.Player.Position.Offset(-1, 0);
                    break;
            }
        }
    }
}