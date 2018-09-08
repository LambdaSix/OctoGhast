using System;
using System.Linq;
using System.Reflection;
using OctoGhast.Renderer.View;
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

            var versionNumber = (AssemblyFileVersionAttribute)
                    Assembly.GetExecutingAssembly()
                        .GetCustomAttributes(typeof (AssemblyFileVersionAttribute), true)
                        .Single();

            var windowSize = ParentWindow.ParentApplication.CurrentWindow.Size;

            var title = "OctoGhast v" + versionNumber.Version;
            var titleTemplate = new LabelTemplate
            {
                Label = title,
                UpperLeftPos = new Vec(windowSize.Width/2 - title.Length/2, 5)
            };

            var newGameButtonTemplate = new ButtonTemplate
            {
                Label = "New Game",
                MinimumWidth = 12,
                UpperLeftPos = new Vec(windowSize.Width/2 - 6, 10)
            };

            var loadGameButtonTemplate = new ButtonTemplate
            {
                MinimumWidth = 12,
                Label = "Load Game",
            };

            loadGameButtonTemplate.AlignTo(LayoutDirection.South, newGameButtonTemplate);

            var optionsButtonTemplate = new ButtonTemplate
            {
                MinimumWidth = 12,
                Label = "Options"
            };

            optionsButtonTemplate.AlignTo(LayoutDirection.South, loadGameButtonTemplate);

            var aboutButtonTemplate = new ButtonTemplate
            {
                MinimumWidth = 12,
                Label = "About",
                MouseOverHighlight = true
            };

            aboutButtonTemplate.AlignTo(LayoutDirection.South, optionsButtonTemplate);

            var quitButtonTemplate = new ButtonTemplate
            {
                MinimumWidth = 12,
                Label = "Quit"
            };

            quitButtonTemplate.AlignTo(LayoutDirection.South, aboutButtonTemplate);

            var titleLabel = new Label(titleTemplate);
            var newGameButton = new Button(newGameButtonTemplate);
            var loadGameButton = new Button(loadGameButtonTemplate);
            var optionsButton = new Button(optionsButtonTemplate);
            var aboutButton = new Button(aboutButtonTemplate);
            var quitButton = new Button(quitButtonTemplate);

            newGameButton.ButtonClick += NewGameButton_OnButtonClick;
            loadGameButton.ButtonClick += LoadGameButton_OnButtonClick;
            optionsButton.ButtonClick += OptionsButton_OnButtonClick;
            aboutButton.ButtonClick += AboutButton_OnButtonClick;
            quitButton.ButtonClick += QuitButton_OnButtonClick;

            ParentWindow.AddControls(new ControlBase[]
            {
                titleLabel, newGameButton, loadGameButton, optionsButton, aboutButton, quitButton
            });
        }

        private void QuitButton_OnButtonClick(object sender, EventArgs eventArgs) {
            ParentWindow.ParentApplication.IsQuitting = true;
        }

        private void AboutButton_OnButtonClick(object sender, EventArgs eventArgs) {
            NavigateTo(new AboutScreen());
        }

        private void OptionsButton_OnButtonClick(object sender, EventArgs eventArgs) {
            NavigateTo(new OptionsScreen());
        }

        private void LoadGameButton_OnButtonClick(object sender, EventArgs eventArgs) {
            NavigateTo(new LoadGameScreen());
        }

        private void NewGameButton_OnButtonClick(object sender, EventArgs eventArgs) {
            NavigateTo(new MainGame(new WorldFactory())); 
        }
    }
}