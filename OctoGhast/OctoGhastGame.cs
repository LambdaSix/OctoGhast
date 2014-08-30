using System.Collections.Generic;
using libtcod;
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
    }

    public class OctoWindow : Window
    {
        public OctoWindow(WindowTemplate template) : base(template) {
            Screens = new Stack<ScreenBase>();

            EnqueueScreen(new TitleScreen());

            // Register our keybindings -> actions
            RegisterKey(TCODKeyCode.Up, (int) GameActions.GameMap_MoveNorth);
            RegisterKey(TCODKeyCode.Down, (int) GameActions.GameMap_MoveSouth);
            RegisterKey(TCODKeyCode.Left, (int) GameActions.GameMap_MoveLeft);
            RegisterKey(TCODKeyCode.Right, (int) GameActions.GameMap_MoveRight);
            RegisterKey(TCODKeyCode.F1, (int) GameActions.GameMap_ShowLighting);
        }
    }
}