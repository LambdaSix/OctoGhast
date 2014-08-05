using System.Collections;
using System.Collections.Generic;
using libtcod;
using OctoGhast.Entity;
using OctoGhast.Framework;
using OctoGhast.Map;
using OctoGhast.Renderer;
using OctoGhast.Renderer.Screens;
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
        private Stack<ScreenBase> Screens { get; set; }

        public OctoWindow(WindowTemplate template) : base(template) {
            Screens = new Stack<ScreenBase>();

            Screens.Push(new TitleScreen());
        }

        public override void OnSettingUp() {
            base.OnSettingUp();

            var screen = Screens.Pop();
            AddManager(screen);
            screen.OnSettingUp();
        }

        protected override void Redraw() {
            base.Redraw();

            Canvas.PrintString(0, 0, "Hello, OctoGhast!");
        }
    }
}