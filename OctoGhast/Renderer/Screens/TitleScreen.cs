using System;
using System.Text;
using OctoGhast.Spatial;
using OctoGhast.UserInterface.Controls;
using OctoGhast.UserInterface.Core;
using OctoGhast.UserInterface.Templates;

namespace OctoGhast.Renderer.Screens
{
    public class TitleScreen : ScreenBase
    {
        public override void OnSettingUp()
        {
            base.OnSettingUp();

            var windowSize = ParentWindow.ParentApplication.CurrentWindow.Size;

            var newGameButtonTemplate = new ButtonTemplate
            {
                Label = "New Game",
                UpperLeftPos = new Vec(windowSize.Width/2, 10)
            };

            var loadGameButtonTemplate = new ButtonTemplate
            {
                Label = "Load Game",
            };

            loadGameButtonTemplate.AlignTo(LayoutDirection.South, newGameButtonTemplate);

            var optionsButtonTemplate = new ButtonTemplate
            {
                Label = "Options"
            };

            optionsButtonTemplate.AlignTo(LayoutDirection.South, loadGameButtonTemplate);

            var aboutButtonTemplate = new ButtonTemplate
            {
                Label = "About"
            };

            aboutButtonTemplate.AlignTo(LayoutDirection.South, optionsButtonTemplate);

            var quitButtonTemplate = new ButtonTemplate
            {
                Label = "Quit"
            };

            quitButtonTemplate.AlignTo(LayoutDirection.South, aboutButtonTemplate);

            var newGameButton = new Button(newGameButtonTemplate);
            var loadGameButton = new Button(loadGameButtonTemplate);
            var optionsButton = new Button(optionsButtonTemplate);
            var aboutButton = new Button(aboutButtonTemplate);
            var quitButton = new Button(quitButtonTemplate);

            newGameButton.ButtonClick += (sender, args) => Console.WriteLine("New Game Clicked");
            loadGameButton.ButtonClick += (sender, args) => Console.WriteLine("Load Game Clicked");
            optionsButton.ButtonClick += (sender, args) => Console.WriteLine("Options Clicked");
            aboutButton.ButtonClick += (sender, args) => Console.WriteLine("About Clicked");
            quitButton.ButtonClick += (sender, args) => Console.WriteLine("Quit Clicked");

            ParentWindow.AddControls(new[]
            {
                newGameButton, loadGameButton, optionsButton, aboutButton, quitButton
            });

        }
    }
}