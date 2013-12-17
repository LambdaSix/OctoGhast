using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using libtcod;
using OctoGhast.DataStructures.Entity;
using OctoGhast.DataStructures.Map;
using OctoGhast.Entity;
using OctoGhast.MapGeneration;
using OctoGhast.MapGeneration.Dungeons;
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

            var playerPosition = new Vec(0, 0);

            var mapGen = new BSPDungeonGenerator {
                PlayerPlacementFunc = (rect) => playerPosition = rect.Center,
                MobilePlacementFunc = (rect) => _objects.Add(new GameObject(rect.Center, 'c', TCODColor.orange)),
            };

            _map = new GameMap(Width*3, Height*3);
            mapGen.GenerateMap(_map.MapArray.Bounds);
            _map.MapArray = mapGen.Map;
            _map.InvalidateMap();

            Player = new Player(playerPosition, '@', TCODColor.amber);
            Player.MoveTo(playerPosition, _map);
        }

        public void Setup() {
            TCODConsole.setCustomFont("celtic_garamond_10x10_gs_tc.png", (int) TCODFontFlags.LayoutTCOD);
            TCODConsole.initRoot(Width, Height, "OctoGhast", false);

            Screen = TCODConsole.root;

            _objects.Add(Player);

            _camera = new Camera(Player.Position, new Rect(80, 25), _map.MapArray.Bounds);
        }

        public void Update() {
            Render(Screen);
            var key = TCODConsole.waitForKeypress(false);
            ProcessKey(key);
        }

        private bool _dirtyFov;

        public void ProcessKey(TCODKey key) {
            if (key.KeyCode == TCODKeyCode.Right) {
                Player.MoveTo(new Vec(Player.Position.X + 1, Player.Position.Y), _map);
                _dirtyFov = true;
            }
            if (key.KeyCode == TCODKeyCode.Left) {
                _dirtyFov = Player.MoveTo(new Vec(Player.Position.X - 1, Player.Position.Y), _map);
            }
            if (key.KeyCode == TCODKeyCode.Up) {
                _dirtyFov = Player.MoveTo(new Vec(Player.Position.X, Player.Position.Y - 1), _map);
            }
            if (key.KeyCode == TCODKeyCode.Down) {
                _dirtyFov = Player.MoveTo(new Vec(Player.Position.X, Player.Position.Y + 1), _map);
            }
        }

        private Camera _camera;

        public void Render(TCODConsole buffer) {
            var fovChanged = _camera.MoveTo(Player.Position);

            if (_dirtyFov) {
                _map.CalculateFov(Player.Position, 8);
                _dirtyFov = false;
            }

            //if (fovChanged)
                //_map.InvalidateMap();

            var frustumView = _map.GetFrustumView(_camera);

            for (int x = 0; x < _camera.Width; x++) {
                for (int y = 0; y < _camera.Height; y++) {
                    var worldCoords = _camera.ToWorldCoords(new Vec(x, y));

                    buffer.putCharEx(x, y, ' ', TCODColor.black, TCODColor.black);

                    if (_map.IsExplored(worldCoords.X, worldCoords.Y))
                    {
                        buffer.putCharEx(x, y, frustumView[x, y].Glyph, TCODColor.darkGrey, TCODColor.black);
                    }

                    if (_map.IsVisible(worldCoords.X, worldCoords.Y)) {
                        buffer.putCharEx(x, y, frustumView[x, y].Glyph, TCODColor.flame, TCODColor.black);
                    }


                    //if (!_map.IsTransparent(worldCoords.X, worldCoords.Y)) {
                    //    buffer.putCharEx(x, y, '0', TCODColor.azure, TCODColor.lightBlue);
                    //}
                }
            }

            foreach (var obj in _objects) {
                var worldCoords = _camera.ToWorldCoords(obj.Position);

                if (_map.IsVisible(worldCoords.X, worldCoords.Y) && obj != Player) {
                    obj.Draw(buffer, _camera.ToViewCoords(obj.Position));
                }
            }

            Player.Draw(buffer, _camera.ToViewCoords(Player.Position));

            buffer.setForegroundColor(TCODColor.white);
            var playerVis = _camera.ToViewCoords(Player.Position);

            buffer.print(0, 24, String.Format("P: {0},{1}; VP: {2},{3}", Player.Position.X, Player.Position.Y,
                playerVis.Y, playerVis.X));

            buffer.print(0, 23, String.Format("FL: {0}", TCODSystem.getFps()));

            TCODConsole.flush();
        }
    }
}