using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using libtcod;
using OctoGhast.DataStructures.Map;
using OctoGhast.Spatial;

namespace OctoGhast.MapGeneration.Dungeons
{
    public class BSPDungeonGenerator : IMapGenerator<Tile>
    {
        public Array2D<Tile> Map { get; private set; }

        public uint Seed { get; set; }
        public Func<Rect, bool> PlayerPlacementFunc { get; set; }
        public Func<Rect, bool> MobilePlacementFunc { get; set; }
        public Func<Rect, bool> ItemPlacementFunc { get; set; } 

        public void GenerateMap(Rect dimensions) {
            Map = new Array2D<Tile>(dimensions.Width, dimensions.Height);
            Map.Fill(new Tile {Glyph = '#', IsVisible = false, IsWalkable = false});
            Dimensions = dimensions;

            Generate();
        }

        public Rect Dimensions { get; set; }

        public void Dig(Vec a, Vec b) {
            if (b.X < a.X) {
                int tmp = b.X;
                b.X = a.X;
                a.X = tmp;
            }

            if (b.Y < a.Y) {
                int tmp = b.Y;
                b.Y = a.Y;
                a.Y = tmp;
            }

            for (int x = a.X; x <= b.X; x++) {
                for (int y = a.Y; y <= b.Y; y++) {
                    Map[x, y] = new Tile {Glyph = '.', IsVisible = true, IsWalkable = true};
                }
            }
        }

        private void MakeRoom(bool firstRoom, int x1, int y1, int x2, int y2) {
            MakeRoom(firstRoom, new Vec(x1, y1), new Vec(x2, y2));
        }

        private void MakeRoom(bool firstRoom, Vec a, Vec b) {
            Dig(a, b);

            if (firstRoom) {
                PlayerPlacementFunc(new Rect(a, b.X - a.X, b.Y - a.Y));
            }
            else {
                MobilePlacementFunc(new Rect(a, b.X - a.X, b.Y - a.Y));
            }
        }

        private void Generate() {
            var RoomMinSize = 6;
            var RoomMaxSize = 12;
            var roomNumber = 0;

            var bsp = new TCODBsp(Dimensions.X, Dimensions.Y, Dimensions.Width, Dimensions.Height);
            bsp.splitRecursive(TCODRandom.getInstance(), 8, RoomMaxSize, RoomMaxSize, 1.5f, 1.5f);

            int lastX = 0, lastY = 0;

            var callback = new BSPListener( node => {
                int x, y, w, h;

                if (node.isLeaf()) {
                    var rand = TCODRandom.getInstance();
                    w = rand.getInt(RoomMinSize, node.w - 2);
                    h = rand.getInt(RoomMinSize, node.h - 2);
                    x = rand.getInt(node.x + 1, node.x + node.w - w - 1);
                    y = rand.getInt(node.y + 1, node.y + node.h - h - 1);

                    MakeRoom(roomNumber == 0, x, y, x + w - 1, y + h - 1);

                    if (roomNumber != 0) {
                        Dig(new Vec(lastX, lastY), new Vec(x + w/2, lastY));
                        Dig(new Vec(x + w/2, lastY), new Vec(x + w/2, y + h/2));
                    }

                    lastX = x + w / 2;
                    lastY = y + h / 2;
                    roomNumber++;
                }
                return true;
            });

            bsp.traverseInvertedLevelOrder(callback);
        }

        /// <summary>
        /// Test method, draw the generated map to an offscreen buffer and return the buffer handle
        /// </summary>
        /// <param name="map"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        internal TCODConsole Draw(Array2D<Tile> map, TCODConsole buffer) {
            for (int x = 0; x < Map.Width; x++) {
                for (int y = 0; y < Map.Height; y++) {
                    var c = map[x, y].Glyph;
                    buffer.setChar(x, y, c);
                }
            }
            return buffer;
        }
    }

    public class BSPListener : ITCODBspCallback
    {
        public Func<TCODBsp, bool> CallBack { get; private set; }

        public BSPListener(Func<TCODBsp, bool> callBack)
        {
            CallBack = callBack;
        }

        public override bool visitNode(TCODBsp node)
        {
            return CallBack(node);
        }
    }
}
