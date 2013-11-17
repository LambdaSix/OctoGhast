using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using libtcod;
using OctoGhast.DataStructures.Entity;
using OctoGhast.DataStructures.Map;
using OctoGhast.Entity;
using OctoGhast.MapGeneration;
using OctoGhast.Spatial;

namespace OctoGhast.Renderer
{
    public class Engine
    {
        private GameMap _map;

        private TCODConsole Screen { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }

        private ICollection<GameObject> _objects = new List<GameObject>();
        public Player Player { get; set; }

        public Engine(int width, int height) {
            Height = height;
            Width = width;

            var mapGen = new SimpleMapGenerator(0xDEADBEEF);
            mapGen.GenerateMap(new Rect(Width*3, Height*3));

            _map = new GameMap(Width*3, Height*3) {MapArray = mapGen.Map};
        }

        public void Setup() {
            TCODConsole.setCustomFont("celtic_garamond_10x10_gs_tc.png", (int) TCODFontFlags.LayoutTCOD);
            TCODConsole.initRoot(Width, Height, "OctoGhast", false);

            Screen = TCODConsole.root;

            Player = new Player(new Vec(0, 0), '@', TCODColor.red);
            _objects.Add(Player);

            _camera = new Camera(Player.Position, new Rect(80, 25), _map.MapArray.Bounds);
        }

        public void Update() {
            Render(Screen);
            var key = TCODConsole.waitForKeypress(false);
            ProcessKey(key);
        }

        public void ProcessKey(TCODKey key) {
            if (key.KeyCode == TCODKeyCode.Right) {
                Player.MoveTo(new Vec(Player.Position.X + 1, Player.Position.Y), _map);
            }
            if (key.KeyCode == TCODKeyCode.Left) {
                Player.MoveTo(new Vec(Player.Position.X - 1, Player.Position.Y), _map);
            }
            if (key.KeyCode == TCODKeyCode.Up) {
                Player.MoveTo(new Vec(Player.Position.X, Player.Position.Y - 1), _map);
            }
            if (key.KeyCode == TCODKeyCode.Down) {
                Player.MoveTo(new Vec(Player.Position.X, Player.Position.Y + 1), _map);
            }
        }

        private Camera _camera;

        public void Render(TCODConsole buffer) {
            var fovChanged = _camera.MoveTo(Player.Position);

            var frustumView = _map.GetFrustumView(_camera);

            for (int x = 0; x < _camera.Width; x++) {
                for (int y = 0; y < _camera.Height; y++) {
                    buffer.putCharEx(x, y, frustumView[x, y].Glyph, TCODColor.white, TCODColor.black);
                }
            }

            foreach (var obj in _objects) {
                obj.Draw(buffer, _camera.ToViewCoords(obj.Position));
            }

            buffer.setForegroundColor(TCODColor.white);
            var playerVis = _camera.ToViewCoords(Player.Position);

            buffer.print(0, 24, String.Format("P: {0},{1}; VP: {2},{3}", Player.Position.X, Player.Position.Y,
                playerVis.Y, playerVis.X));

            buffer.print(0, 23, String.Format("FL: {0}", TCODSystem.getLastFrameLength()));

            TCODConsole.flush();
        }
    }
}