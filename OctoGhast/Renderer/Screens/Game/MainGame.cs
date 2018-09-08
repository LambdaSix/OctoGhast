using OctoGhast.Renderer.Screens.Game.Controls;
using OctoGhast.UserInterface.Core;

namespace OctoGhast.Renderer.Screens
{
    public class MainGame : ScreenBase
    {
        public MainGame(WorldFactory worldFactory) {
            /*
            RegisterAction((int)GameActions.GameMap_MoveNorth, () => GameModel.Player.MoveTo(GameModel.Player.Position.Offset(0, -1)));
            RegisterAction((int)GameActions.GameMap_MoveSouth, () => GameModel.Player.MoveTo(GameModel.Player.Position.Offset(0, +1)));
            RegisterAction((int)GameActions.GameMap_MoveLeft, () => GameModel.Player.MoveTo(GameModel.Player.Position.Offset(-1, 0)));
            RegisterAction((int)GameActions.GameMap_MoveRight, () => GameModel.Player.MoveTo(GameModel.Player.Position.Offset(+1, 0)));
            RegisterAction((int) GameActions.GameMap_ShowLighting, () => GameModel.DrawLighting = !GameModel.DrawLighting);
            */
        }

        public override void OnSettingUp() {
            base.OnSettingUp();

            var windowSize = ParentWindow.ParentApplication.CurrentWindow.Size;

            /* Initialize the WorldInstance from the WorldFactory we were passed.
            WorldInstance = WorldFactory.Create();
            WorldInstance.Initialize();

            GameModel = new GameViewModel()
            {
                Camera = new Camera(Vec.Zero, windowSize),
                World = new WorldInstance(windowSize.Height, windowSize.Width),
                Player = new Player(Vec.Zero, '@', new Color(XColor.Orange)),
            };
            */

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