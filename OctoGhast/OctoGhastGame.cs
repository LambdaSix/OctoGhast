using System.Collections.Generic;
using OctoGhast.Framework;
using OctoGhast.Renderer.Screens;
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
            Screens = new Stack<ScreenBase>();

            EnqueueScreen(new TitleScreen());
        }
    }
}