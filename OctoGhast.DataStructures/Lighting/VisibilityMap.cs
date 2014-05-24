using System;
using System.Collections.Generic;
using System.Linq;
using libtcod;
using OctoGhast.DataStructures.Map;
using OctoGhast.Spatial;

namespace OctoGhast.DataStructures.Lighting
{
    /// <summary>
    /// Provides calculations for FOV across a visible set.
    /// </summary>
    public class VisibilityMap<T> where T: ITile
    {
        private TCODMap _map;

        public VisibilityMap(int screenWidth, int screenHeight) {
            _map = new TCODMap(screenWidth, screenWidth);
        }

        public void RefreshMapFrom(IEnumerable<T> tiles, int stride) {
            var pvs = tiles.ToList();
            int xMax = pvs.Count()/stride;
            for (int x = 0; x < xMax; x++) {
                for (int y = 0; y < stride; y++) {
                    var tile = pvs.ElementAt((y*stride) + x);
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