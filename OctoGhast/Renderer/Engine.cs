﻿using System;
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
        private Camera _camera;

        // Because we access black /so much/ per frame, memoize it to avoid going
        // across the P/Invoke border to libTCOD
        private static readonly TCODColor ColorBlack = TCODColor.black;

        private TCODConsole Screen { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }

        private ICollection<GameObject> _objects = new List<GameObject>();

        public Player Player { get; set; }
        private bool _dirtyFov;

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

        private IEnumerable<IMobile> GetMobilesInView() {
            return _objects.OfType<IMobile>().Where(mob => _camera.ViewFrustum.Contains(mob.Position));
        }

        private void ProcessKey(TCODKey key) {
            // If we're holding down Shift, then just scroll the camera around and ignore the player.
            if (key.Shift && key.KeyCode == TCODKeyCode.Right) {
                _camera.MoveTo(new Vec(_camera.CameraCenter.X + 1, _camera.CameraCenter.Y));
                return;
            }
            if (key.Shift && key.KeyCode == TCODKeyCode.Left) {
                _camera.MoveTo(new Vec(_camera.CameraCenter.X - 1, _camera.CameraCenter.Y));
                return;
            }
            if (key.Shift && key.KeyCode == TCODKeyCode.Up) {
                _camera.MoveTo(new Vec(_camera.CameraCenter.X, _camera.CameraCenter.Y - 1));
                return;
            }
            if (key.Shift && key.KeyCode == TCODKeyCode.Down) {
                _camera.MoveTo(new Vec(_camera.CameraCenter.X, _camera.CameraCenter.Y + 1));
                return;
            }

            // Otherwise, move the player directly.
            if (key.KeyCode == TCODKeyCode.Right) {
                _dirtyFov = Player.MoveTo(new Vec(Player.Position.X + 1, Player.Position.Y), _map, GetMobilesInView());
            }
            if (key.KeyCode == TCODKeyCode.Left) {
                _dirtyFov = Player.MoveTo(new Vec(Player.Position.X - 1, Player.Position.Y), _map, GetMobilesInView());
            }
            if (key.KeyCode == TCODKeyCode.Up) {
                _dirtyFov = Player.MoveTo(new Vec(Player.Position.X, Player.Position.Y - 1), _map, GetMobilesInView());
            }
            if (key.KeyCode == TCODKeyCode.Down) {
                _dirtyFov = Player.MoveTo(new Vec(Player.Position.X, Player.Position.Y + 1), _map, GetMobilesInView());
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

                    buffer.putCharEx(x, y, ' ', ColorBlack, ColorBlack);

                    if (_map.IsExplored(worldCoords.X, worldCoords.Y))
                    {
                        buffer.putCharEx(x, y, frustumView[x, y].Glyph, TCODColor.darkGrey, ColorBlack);
                    }

                    if (_map.IsVisible(worldCoords.X, worldCoords.Y)) {
                        buffer.putCharEx(x, y, frustumView[x, y].Glyph, TCODColor.flame, ColorBlack);
                    }
                }
            }

            // For each possibly visible mobile, see if the player can see it and draw appropriately.
            foreach (var mobile in GetMobilesInView()) {
                if (_map.IsVisible(mobile.Position.X, mobile.Position.Y) && mobile != Player) {
                    mobile.Draw(buffer, _camera.ToViewCoords(mobile.Position));
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