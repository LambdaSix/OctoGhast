using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using OctoGhast.DataStructures.Map;
using OctoGhast.Spatial;
using libtcod;

namespace OctoGhast.MapGeneration
{
    public class SimpleMapGenerator : IMapGenerator<Tile>
    {
        private TCODRandom _random;
        private int _mapWidth;
        private int _mapHeight;

        public Array2D<Tile> Map { get; private set; }

        public UInt32 Seed { get; set; }

        public SimpleMapGenerator(UInt32 seedValue) {
            Seed = seedValue;
        }

        public void GenerateMap(Rect dimensions) {
            _mapWidth = dimensions.Width;
            _mapHeight = dimensions.Height;

            _random = new TCODRandom(Seed, TCODRandomType.MersenneTwister);
            var map = BuildMap();

            Map = ProcessMap(map, dimensions);
            // TODO: Transform the heightmap into a Array2D of tile.
            // This also requires consideration of walkable/visible. This being a simple generator, true for both.
            // TODO: Generate FOV/Walkable/resource/etc meshes as appropriate (low priority)
        }

        private static Array2D<Tile> ProcessMap(TCODHeightMap map, Rect dimensions) {
            var arrayMap = new Array2D<Tile>(dimensions.Width, dimensions.Height);
            arrayMap.Fill(vec => new Tile {Glyph = (char) (48 + (map.getValue(vec.X, vec.Y)*10)), IsWalkable = true});
            return arrayMap;
        } 

        private void AddHill(TCODHeightMap map, int hillCount, float baseRadius, float radiusVariation, float height) {
            const float pi = (float) Math.PI;
            for (int i = 0; i < hillCount; i++) {
                var hillMinRadius = baseRadius*(1.0f - radiusVariation);
                var hillMaxRadius = baseRadius*(1.0f + radiusVariation);

                var radius = _random.getFloat(hillMinRadius, hillMaxRadius);
                var theta = _random.getFloat(0.0f, pi*2.0f);

                var distance = _random.getFloat(0.0f, Math.Min(_mapWidth, _mapHeight)/2.0f - radius);
                var xH = (float) (_mapWidth/2.0f + Math.Cos(theta)*distance);
                var yH = (float) (_mapHeight/2.0f + Math.Sin(theta)*distance);
                map.addHill(xH, yH, radius, height);
            }
        }

        private TCODHeightMap BuildMap() {
            var map = new TCODHeightMap(_mapWidth, _mapHeight);
            AddHill(map,
                hillCount: 25,
                baseRadius: 10.0f,
                radiusVariation: 0.5f,
                height: 0.5f);
            map.normalize(0, 1);
            return map;
        }

        /// <summary>
        /// Test method, draw the generated map to an offscreen buffer and return the buffer handle
        /// </summary>
        /// <param name="map"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        internal TCODConsole Draw(TCODHeightMap map, TCODConsole buffer) {
            for (int x = 0; x < _mapWidth; x++) {
                for (int y = 0; y < _mapHeight; y++) {
                    var z = map.getValue(x, y);
                    var value = (int) z*255 & 0xFF;
                    var colour = new TCODColor(value, value, value);
                    buffer.setCharBackground(x, y, colour, TCODBackgroundFlag.Set);
                }
            }
            return buffer;
        }
    }
}
