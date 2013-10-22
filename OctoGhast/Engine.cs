using libtcod;

namespace OctoGhast
{
    public class Engine
    {
        int ScreenWidth { get; set; }
        int ScreenHeight { get; set; }

        int PlayerX { get; set; }
        int PlayerY { get; set; }

        public Engine(int screenWidth, int screenHeight) {
            ScreenWidth = screenWidth;
            ScreenHeight = screenHeight;

            PlayerX = ScreenWidth/2;
            PlayerY = ScreenHeight/2;
        }

        public void Setup() {
            // TODO: Fix TCODConsole to be friendlier.
            TCODConsole.setCustomFont("celtic_garamond_10x10_gs_tc.png", (int) (TCODFontFlags.LayoutTCOD | TCODFontFlags.Grayscale));
            TCODConsole.initRoot(ScreenWidth, ScreenHeight, "OctoGhast", false);
            TCODSystem.setFps(20);
        }

        public void Tick() {
            Draw(TCODConsole.root);
            TCODConsole.root.putChar(PlayerX, PlayerY, ' ');
            HandleInput();
        }

        public void Draw(TCODConsole root) {
            root.setBackgroundColor(TCODColor.black);
            root.setForegroundColor(TCODColor.white);
            root.putChar(PlayerX, PlayerY, '@');
            TCODConsole.flush();
        }

        public void HandleInput() {
            var key = TCODConsole.waitForKeypress(true);

            // TODO: Replace with a mapping system.
            switch (key.KeyCode)
            {
                case TCODKeyCode.Up:
                    {
                        PlayerY -= 1;
                        break;
                    }
                case TCODKeyCode.Down:
                    {
                        PlayerY += 1;
                        break;
                    }
                case TCODKeyCode.Left:
                    {
                        PlayerX -= 1;
                        break;
                    }
                case TCODKeyCode.Right:
                    {
                        PlayerX += 1;
                        break;
                    }
            }
        }
    }
}