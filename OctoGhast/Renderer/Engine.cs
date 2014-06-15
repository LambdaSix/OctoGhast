using System;
using System.Collections.Generic;
using System.Linq;
using OctoGhast.DataStructures.Renderer;
using libtcod;
using OctoGhast.DataStructures.Entity;
using OctoGhast.DataStructures.Map;
using OctoGhast.Entity;
using OctoGhast.MapGeneration.Dungeons;
using OctoGhast.Spatial;

namespace OctoGhast.Renderer
{
    public static class CameraExtensions
    {
        public static void BindTo(this ICamera camera, IMobile mobile) {
            mobile.OnMove(camera.MoveTo);
        }
    }

    public interface IEngine
    {
        int Height { get; set; }
        int Width { get; set; }
        IPlayer Player { get; set; }
        bool IsRunning { get; }

        /// <summary>
        /// Initialize the Engine and window.
        /// </summary>
        void Setup();

        void Shutdown();

        /// <summary>
        /// Render a single frame and wait for input.
        /// </summary>
        void Update();

        void Render(TCODConsole buffer);
    }

    public class Engine : IEngine
    {
        // Because we access black /so much/ per frame, memoize it to avoid going
        // across the P/Invoke border to libTCOD
        private static readonly TCODColor ColorBlack = TCODColor.black;
        private readonly IGameMap _map;
        private readonly ICollection<IGameObject> _objects = new List<IGameObject>();
        private readonly ICamera _camera;

        public Engine(IEngineConfiguration serviceConfiguration) {
            Height = serviceConfiguration.Height;
            Width = serviceConfiguration.Width;

            var playerPosition = Vec.Zero;

            _map = new GameMap(Height, Width);

            Player = serviceConfiguration.Player;
            Player.MoveTo(playerPosition, _map, Enumerable.Empty<IMobile>());

            _camera = serviceConfiguration.Camera;
            _camera.MoveTo(Vec.Zero);
            _camera.BindTo(Player);
        }

        private TCODConsole Screen { get; set; }

        public int Height { get; set; }
        public int Width { get; set; }

        public IPlayer Player { get; set; }
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Initialize the Engine and window.
        /// </summary>
        public void Setup() {
            TCODConsole.setCustomFont("celtic_garamond_10x10_gs_tc.png", (int) TCODFontFlags.LayoutTCOD);
            TCODConsole.initRoot(Width, Height, "OctoGhast", false);

            Screen = TCODConsole.root;

            _objects.Add(Player);
            _camera.MoveTo(Player.Position);

            IsRunning = true;
        }

        public void Shutdown() {
            // TODO: Cleanup any libtcod/native resources.

            IsRunning = false;
        }

        /// <summary>
        /// Render a single frame and wait for input.
        /// </summary>
        public void Update() {
            Render(Screen);
            TCODKey key = TCODConsole.waitForKeypress(false);
            ProcessKey(key);
        }

        private void ProcessKey(TCODKey key) {
            var entityList = Enumerable.Empty<IMobile>();

            if (key.KeyCode == TCODKeyCode.Left) {
                if (key.Shift) {
                    _camera.MoveTo(_camera.Position.Offset(-1, 0));
                    return;
                }
                Player.MoveTo(Player.Position.Offset(-1, 0), _map, entityList);
            }

            if (key.KeyCode == TCODKeyCode.Right) {
                if (key.Shift) {
                    _camera.MoveTo(_camera.Position.Offset(+1, 0));
                    return;
                }
                Player.MoveTo(Player.Position.Offset(+1, 0), _map, entityList);
            }

            if (key.KeyCode == TCODKeyCode.Down) {
                if (key.Shift) {
                    _camera.MoveTo(_camera.Position.Offset(0, +1));
                    return;
                }
                Player.MoveTo(Player.Position.Offset(0, +1), _map, entityList);
            }

            if (key.KeyCode == TCODKeyCode.Up) {
                if (key.Shift) {
                    _camera.MoveTo(_camera.Position.Offset(0, -1));
                    return;
                }
                Player.MoveTo(Player.Position.Offset(0, -1), _map, entityList);
            }

            if (key.KeyCode == TCODKeyCode.Escape) {
                Shutdown();
            }
        }

        private Vec toWorld(int x, int y, Rect constraint) {
            return new Vec(constraint.TopLeft.X + x, constraint.TopLeft.Y + y);
        }

        private Vec toView(Vec position, Rect constraint) {
            var cartCenter = constraint.Center;

            var Xs = (constraint.Width/2) + (position.X - cartCenter.X);
            var Ys = (constraint.Height/2) + (position.Y - cartCenter.Y);
            return new Vec(Xs, Ys);
        }

        public void Render(TCODConsole buffer) {
            var fov = _map.CalculateFov(_camera.ViewFrustum.Center, 4,
                (x, y) => toView(new Vec(x, y), _camera.ViewFrustum));

            for (int y = 0; y < _camera.ViewFrustum.Height; y++) {
                for (int x = 0; x < _camera.ViewFrustum.Width; x++) {
                    var worldPos = toWorld(x, y, _camera.ViewFrustum);

                    if (fov[x, y]) {
                        buffer.putCharEx(x, y, _map[worldPos].Glyph, TCODColor.grey, ColorBlack);
                    }
                    else {
                        buffer.putCharEx(x, y, ' ', TCODColor.amber, ColorBlack);
                    }
                }
            }

            var playerFrustum = Rect.FromCenter(Player.Position, _camera.Size);

            var playerX = playerFrustum.TopRight.X - Player.Position.X;
            var playerY = playerFrustum.BottomLeft.Y - Player.Position.Y;
            var distanceFromCamera = playerFrustum.TopLeft - _camera.ViewFrustum.TopLeft;

            buffer.putCharEx(playerX + distanceFromCamera.X, playerY + distanceFromCamera.Y, '@', TCODColor.brass,
                ColorBlack);

            TCODConsole.blit(buffer, 0, 0, Width, Height, TCODConsole.root, 0, 0);
            TCODConsole.flush();
        }
    }
}