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
		private bool _dirtyFov;

		public Engine(IEngineConfiguration serviceConfiguration) {
			Height = serviceConfiguration.Height;
			Width = serviceConfiguration.Width;

			var playerPosition = Vec.Zero;

			// Do the setup for placement because of closure variables.
			serviceConfiguration.MapGenerator.PlayerPlacementFunc = (rect) => playerPosition = rect.Center;
			serviceConfiguration.MapGenerator.MobilePlacementFunc = (rect) => {
				_objects.Add(new Mobile(rect.Center, 'c', TCODColor.orange, "A Smelly Orcses"));
			};

			_map = new GameMap(Width*3, Height*3);
			serviceConfiguration.MapGenerator.GenerateMap(_map.Bounds);
			_map.SetFrom(serviceConfiguration.MapGenerator.Map);
			_map.InvalidateMap();

			Player = serviceConfiguration.Player;
			Player.MoveTo(playerPosition, _map, Enumerable.Empty<IMobile>());
			_map.CalculateFov(playerPosition, 8);

			_camera = serviceConfiguration.Camera;
			_camera.MoveTo(Player.Position);
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

		private IEnumerable<IMobile> GetMobilesInView() {
			return _objects.OfType<IMobile>().Where(mob => _camera.ViewFrustum.Contains(mob.Position));
		}

		private void ProcessKey(TCODKey key) {
			// System bindings

			if (key.KeyCode == TCODKeyCode.Escape) {
				Shutdown();
			}

			// If we're holding down Shift, then just scroll the camera around and ignore the player.
			if (key.Shift && key.KeyCode == TCODKeyCode.Right) {
				_camera.MoveTo(_camera.CameraCenter.OffsetX(+1));
				return;
			}
			if (key.Shift && key.KeyCode == TCODKeyCode.Left) {
				_camera.MoveTo(_camera.CameraCenter.OffsetX(-1));
				return;
			}
			if (key.Shift && key.KeyCode == TCODKeyCode.Up) {
				_camera.MoveTo(_camera.CameraCenter.OffsetY(-1));
				return;
			}
			if (key.Shift && key.KeyCode == TCODKeyCode.Down) {
				_camera.MoveTo(_camera.CameraCenter.OffsetY(+1));
				return;
			}

			// Otherwise, move the player directly.
			if (key.KeyCode == TCODKeyCode.Right) {
				_dirtyFov = Player.MoveTo(Player.Position.OffsetX(+1), _map, GetMobilesInView());
			}
			if (key.KeyCode == TCODKeyCode.Left) {
				_dirtyFov = Player.MoveTo(Player.Position.OffsetX(-1), _map, GetMobilesInView());
			}
			if (key.KeyCode == TCODKeyCode.Up) {
				_dirtyFov = Player.MoveTo(Player.Position.OffsetY(-1), _map, GetMobilesInView());
			}
			if (key.KeyCode == TCODKeyCode.Down) {
				_dirtyFov = Player.MoveTo(Player.Position.OffsetY(+1), _map, GetMobilesInView());
			}

			// If we managed to move the player, then update the camera to the current location.
			if (_dirtyFov) {
				_camera.MoveTo(Player.Position);
			}
		}

		public void Render(TCODConsole buffer) {
			if (_dirtyFov) {
				_map.CalculateFov(Player.Position, 8);
				_dirtyFov = false;
			}

			Array2D<Tile> frustumView = _map.GetFrustumView(_camera.ViewFrustum);

			for (int x = 0; x < _camera.Width; x++) {
				for (int y = 0; y < _camera.Height; y++) {
					Vec worldCoords = _camera.ToWorldCoords(new Vec(x, y));

					buffer.putCharEx(x, y, ' ', ColorBlack, ColorBlack);

					if (_map.IsExplored(worldCoords.X, worldCoords.Y)) {
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
			Vec playerVis = _camera.ToViewCoords(Player.Position);

			buffer.print(0, Height-1, String.Format("P: {0},{1}; VP: {2},{3}", Player.Position.X, Player.Position.Y,
				playerVis.Y, playerVis.X));
			buffer.print(0, Height-2, String.Format("C: {0},{1}", _camera.CameraCenter.X, _camera.CameraCenter.Y));

			buffer.print(0, Height-3, String.Format("FL: {0}", TCODSystem.getFps()));

			TCODConsole.flush();
		}
	}
}