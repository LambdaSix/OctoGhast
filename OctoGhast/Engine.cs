using libtcod;

namespace OctoGhast
{
    public class Engine
    {
        int ScreenWidth { get; set; }
        int ScreenHeight { get; set; }

        public Engine(int screenWidth, int screenHeight) {
            ScreenWidth = screenWidth;
            ScreenHeight = screenHeight;
        }

        public void Setup() {
            // TODO: Fix TCODConsole to be friendlier.
            TCODConsole.setCustomFont("celtic_garamond_10x10_gs_tc.png", (int) (TCODFontFlags.LayoutTCOD | TCODFontFlags.Grayscale));
            TCODConsole.initRoot(ScreenWidth, ScreenHeight, "OctoGhast", false);
            TCODSystem.setFps(20);
        }

        public void Tick() {
            var root = TCODConsole.root;

            root.setBackgroundColor(TCODColor.black);
            root.setForegroundColor(TCODColor.white);
            root.putChar(1, 1, '@');
            TCODConsole.flush();
        }
    }
}