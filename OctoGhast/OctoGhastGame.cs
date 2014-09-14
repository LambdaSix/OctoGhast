using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using OctoGhast.Framework;
using OctoGhast.Renderer.Screens;
using OctoGhast.UserInterface.Core;
using OctoGhast.UserInterface.Core.Messages;

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

        public OctoghastGame(GameInfo info) : base(info) {
            
        }
    }

    public class OctoWindow : Window
    {
        public OctoWindow(WindowTemplate template) : base(template) {
            Screens = new Stack<ScreenBase>();

            EnqueueScreen(new TitleScreen());

            // Register our keybindings -> actions
            RegisterKey(Keys.Up, (int) GameActions.GameMap_MoveNorth);
            RegisterKey(Keys.Down, (int) GameActions.GameMap_MoveSouth);
            RegisterKey(Keys.Left, (int) GameActions.GameMap_MoveLeft);
            RegisterKey(Keys.Right, (int) GameActions.GameMap_MoveRight);
            RegisterKey(Keys.F1, (int) GameActions.GameMap_ShowLighting);
        }
    }
}