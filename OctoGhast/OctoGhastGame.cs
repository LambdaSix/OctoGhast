using libtcod;
using OctoGhast.Entity;
using OctoGhast.Framework;
using OctoGhast.Map;
using OctoGhast.Renderer;
using OctoGhast.Renderer.View;
using OctoGhast.Spatial;
using OctoGhast.UserInterface.Controls;
using OctoGhast.UserInterface.Core;

namespace OctoGhast
{
    public class OctoghastGame : Game
    {
        protected override void Setup(GameInfo info) {
            base.Setup(info);

            var template = new WindowTemplate(info.ScreenSize);
            var window = new OctoWindow(template)
            {
                ParentApplication = this,
                TooltipBGAlpha = 0.2f,
                TooltipFGAlpha = 1.0f,
            };

            SetWindow(window);
        }
    }

    public class OctoWindow : Window
    {
        public OctoWindow(WindowTemplate template) : base(template) {
        }

        public override void OnSettingUp() {
            base.OnSettingUp();

            var quitButtonTemplate = new ButtonTemplate()
            {
                Label = "QUIT",
                Tooltip = "Quit the application",
                HasFrameBorder = true,
                UpperLeftPos = new Vec(5, 5),
            };

            var windowSize = ParentApplication.CurrentWindow.Size.Offset(-25, -15);
            var mapViewTemplate = new MapViewTemplate
            {
                Size = windowSize,
                Title = "MapView",
                HasFrameBorder = true,
                UpperLeftPos = new Vec(20,10)
            };

            var controlSize = mapViewTemplate.CalculateSize();

            var mapView = new MapView(mapViewTemplate,
                new MapViewModel()
                {
                    Camera = new Camera(Vec.Zero, windowSize),
                    Map = new GameMap(windowSize.Height, windowSize.Width),
                    Player = new Player(Vec.Zero, '@', TCODColor.amber)
                });

            var quitButton = new Button(quitButtonTemplate);
            quitButton.ButtonClick += (o, e) => ParentApplication.IsQuitting = true;
            AddControl(quitButton);
            AddControl(mapView);
        }

        protected override void Redraw() {
            base.Redraw();

            Canvas.PrintString(0, 0, "Hello, OctoGhast!");
        }
    }
}