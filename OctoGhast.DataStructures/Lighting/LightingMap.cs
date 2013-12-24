using System.Collections.Generic;
using libtcod;
using OctoGhast.DataStructures.Map;
using OctoGhast.Spatial;

namespace OctoGhast.DataStructures.Lighting
{
    public class LightingMap
    {
        private TCODMap _map;

        public LightingMap(int width, int height) {
            _map = new TCODMap(width, height);
        }

        public void RefreshMapFrom(Array2D<Tile> tiles) {
            for (int y = 0; y < tiles.Height; y++) {
                for (int x = 0; x < tiles.Width; x++) {
                    var tile = tiles[x, y];
                    _map.setProperties(x, y, tile.IsTransparent, tile.IsWalkable);
                }
            }
        }

        public void ComputeFov(Vec playerPos, ILightSource lightSource) {
            _map.computeFov(playerPos.X, playerPos.Y, lightSource.Intensity, true, TCODFOVTypes.DiamondFov);
        }

        public bool IsVisible(int x, int y) {
            return _map.isInFov(x, y);
        }

        public void RecalculateLighting(IEnumerable<ILightSource> lights) {
            foreach (var light in lights) {
                _map.computeFov(light.Origin.X, light.Origin.Y, light.Intensity, true, TCODFOVTypes.ShadowFov);
            }
        }
    }
}