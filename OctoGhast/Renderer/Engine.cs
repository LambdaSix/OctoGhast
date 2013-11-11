using System;
using libtcod;
using OctoGhast.Entity;
using OctoGhast.Map;
using OctoGhast.Spatial;

namespace OctoGhast.Renderer
{
    public class Engine
    {
        private Map.Map _map;

        private TCODConsole Screen { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }

        public Player Player { get; set; }

        public Engine(int width, int height) {
            Height = height;
            Width = width;

            _map = new Map.Map(64*3, 64*3);
        }

        public void Setup() {
            _map.MapArray.Fill(new Tile() { Glyph = '.', IsVisible = true, IsWalkable = true });

            for (int x = 0; x < _map.MapArray.Width; x++) {
                _map.MapArray[x, 0] = new Tile() {Glyph = x % 2 == 0 ? '-' : '[', IsVisible = true, IsWalkable = false};
                _map.MapArray[x, _map.MapArray.Height-1] = new Tile() {Glyph = x % 2 == 0 ? '-' : '[', IsVisible = true, IsWalkable = false};
            }

            for (int y = 0; y < _map.MapArray.Width; y++) {
                _map.MapArray[0, y] = new Tile {Glyph = y % 2 == 0 ? '|' : ']', IsVisible = true, IsWalkable = false};
                _map.MapArray[_map.MapArray.Width - 1, y] = new Tile() { Glyph = y % 2 == 0 ? '|' : ']', IsVisible = true, IsWalkable = false };
            }

            TCODConsole.setCustomFont("celtic_garamond_10x10_gs_tc.png", (int) TCODFontFlags.LayoutTCOD);
            TCODConsole.initRoot(Width, Height, "OctoGhast", false);

            Screen = TCODConsole.root;

            Screen.setForegroundColor(TCODColor.black);
            Screen.putChar(5, 5, '@');

            Player = new Player {Position = new Vec(0, 0)};

            _camera = new Camera(Player.Position, new Rect(80, 25), _map.MapArray.Bounds);
        }

        public void Update() {
            Render(Screen);
            var key = TCODConsole.waitForKeypress(false);
            ProcessKey(key);
        }

        public void ProcessKey(TCODKey key) {
            if (key.KeyCode == TCODKeyCode.Right) {
                Player.Position = new Vec(Player.Position.X + 1, Player.Position.Y);
            }
            if (key.KeyCode == TCODKeyCode.Left) {
                Player.Position = new Vec(Player.Position.X - 1, Player.Position.Y);
            }
            if (key.KeyCode == TCODKeyCode.Up) {
                Player.Position = new Vec(Player.Position.X, Player.Position.Y - 1);
            }
            if (key.KeyCode == TCODKeyCode.Down) {
                Player.Position = new Vec(Player.Position.X, Player.Position.Y + 1);
            }
        }

        private Camera _camera;

        public void Render(TCODConsole buffer) {
            var fovChanged = _camera.MoveTo(Player.Position);

            var frustumView = _map.GetFrustumView(_camera);

            for (int x = 0; x < _camera.Width; x++) {
                for (int y = 0; y < _camera.Height; y++) {
                    buffer.putCharEx(x, y, frustumView[x, y].Glyph, TCODColor.amber, TCODColor.black);
                }
            }

            buffer.setForegroundColor(TCODColor.white);
            var playerVis = _camera.ToViewCoords(Player.Position);

            buffer.print(0, 24, String.Format("P: {0},{1}; VP: {2},{3}", Player.Position.X, Player.Position.Y,
                playerVis.Y, playerVis.X));

            buffer.print(0, 23, String.Format("FL: {0}", TCODSystem.getLastFrameLength()));

            buffer.putCharEx(playerVis.X, playerVis.Y, '@', TCODColor.green, TCODColor.black);

            TCODConsole.flush();
        }
    }
}