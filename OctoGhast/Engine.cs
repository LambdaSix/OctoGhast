using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using libtcod;

namespace OctoGhast
{
    public class Engine
    {
        int ScreenWidth { get; set; }
        int ScreenHeight { get; set; }

        private Object _player;

        TCODConsole Buffer { get; set; }
        private IList<Object> _objects = new List<Object>();

        public int MapHeight { get; set; }
        public int MapWidth { get; set; }

        private static TCODColor colorDarkWall = new TCODColor(0, 0, 100);
        private static TCODColor colorDarkGround = new TCODColor(50, 50, 150);
        private int[,] _map;

        public Engine(int screenWidth, int screenHeight) {
            ScreenWidth = screenWidth;
            ScreenHeight = screenHeight;

            MapWidth = 20;
            MapHeight = 15;

            _player = new Object(ScreenWidth/2, ScreenHeight/2, '@', TCODColor.white);
            var npc = new Object(ScreenWidth/2 - 5, ScreenHeight/2, '@', TCODColor.yellow);

            _objects.Add(_player);
            _objects.Add(npc);
            _map = MakeMap();
        }

        private int[,] MakeMap() {
            return new int[,] {
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
                {0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
                {0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0},
                {0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0},
                {0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0},
                {0, 0, 0, 1, 1, 0, 1, 1, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0},
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0},
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0},
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
            };
        }

        public void Setup() {
            // TODO: Fix TCODConsole to be friendlier.
            TCODConsole.setCustomFont("celtic_garamond_10x10_gs_tc.png", (int) (TCODFontFlags.LayoutTCOD | TCODFontFlags.Grayscale));
            TCODConsole.initRoot(ScreenWidth, ScreenHeight, "OctoGhast", false);
            TCODSystem.setFps(20);
            Buffer = new TCODConsole(ScreenWidth, ScreenHeight);
        }

        public void Tick() {
            Draw(TCODConsole.root);

            foreach (var obj in _objects) {
                obj.Clear(Buffer);
            }

            HandleInput();
        }

        public void Draw(TCODConsole root) {
            for (int x = 0; x < MapHeight; ++x) {
                for (int y = 0; y < MapWidth; ++y) {
                    Buffer.putChar(x, y, _map[x, y] == 0 ? '.' : '#');
                }
            }

            foreach (var obj in _objects)
            {
                obj.Draw(Buffer);
            }

            TCODConsole.blit(Buffer, 0, 0, ScreenWidth, ScreenHeight, root, 0, 0);
            TCODConsole.flush();
        }

        public void HandleInput() {
            var key = TCODConsole.waitForKeypress(true);

            // TODO: Replace with a mapping system.
            switch (key.KeyCode) {
                case TCODKeyCode.Up: {
                    _player.Move(0, -1);
                    break;
                }
                case TCODKeyCode.Down: {
                    _player.Move(0, 1);

                    break;
                }
                case TCODKeyCode.Left: {
                    _player.Move(-1, 0);

                    break;
                }
                case TCODKeyCode.Right: {
                    _player.Move(1, 0);

                    break;
                }
            }
        }
    }
}