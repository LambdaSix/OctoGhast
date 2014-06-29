using System.Collections.Generic;
using System.Linq.Expressions;
using OctoGhast.Framework;
using OctoGhast.Spatial;
using OctoGhast.UserInterface.Controls;

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

            var quitButton = new Button(quitButtonTemplate);
            quitButton.ButtonClick += (o, e) => ParentApplication.IsQuitting = true;
            AddControl(quitButton);
        }

        protected override void Redraw() {
            base.Redraw();

            Canvas.PrintString(0, 0, "Hello, OctoGhast!");
        }
    }
}