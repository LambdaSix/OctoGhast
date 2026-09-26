using OctoGhast.Renderer.Screens.Game.Controls;
using OctoGhast.UserInterface.Core;

namespace OctoGhast.Renderer.Screens
{
    public class MainGame : ScreenBase
    {
        public MainGame(WorldFactory worldFactory) {
        }

        public override void OnSettingUp() {
            base.OnSettingUp();

            var windowSize = ParentWindow.ParentApplication.CurrentWindow.Size;

            /* Control Templates */
            var mapTemplate = new GameMapControlTemplate
            {
                Model = null, // WorldViewModel ?
                Size = windowSize
            };

            /* Control widgets */

            var mapControl = new GameMapControl(mapTemplate);

            ParentWindow.AddControls(mapControl);
        }
    }
}
