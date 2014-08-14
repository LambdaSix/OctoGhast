using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using libtcod;
using OctoGhast.DataStructures.Map;
using OctoGhast.Spatial;

namespace OctoGhast.MapGeneration.Dungeons
{
    public class BSPDungeonGenerator : ITileMapGenerator
    {
        public Array2D<ITile> Map { get; private set; }

        public uint Seed { get; set; }

        /// <summary>
        /// Provides co-ordinates for the player in the form of the room size
        /// </summary>
        public Action<Rect> PlayerPlacementFunc { get; set; }

        /// <summary>
        /// Provides for placing NPC's or recording their suggested position
        /// </summary>
        public Action<Rect> MobilePlacementFunc { get; set; }
        public Func<Rect, bool> ItemPlacementFunc { get; set; } 
        public Func<ITile> TileFactory { get; set; }

        public void GenerateMap(Rect dimensions) {
            Map = new Array2D<ITile>(dimensions.Width, dimensions.Height);
            Map.Fill(vec => TileFactory());
            Dimensions = dimensions;

            Generate();
        }

        public Rect Dimensions { get; set; }

        public void Dig(Vec fromVec, Vec toVec) {
            if (toVec.X < fromVec.X) {
                int tmp = toVec.X;
                toVec.X = fromVec.X;
                fromVec.X = tmp;
            }

            if (toVec.Y < fromVec.Y) {
                int tmp = toVec.Y;
                toVec.Y = fromVec.Y;
                fromVec.Y = tmp;
            }

            for (int x = fromVec.X; x <= toVec.X; x++) {
                for (int y = fromVec.Y; y <= toVec.Y; y++) {
                    var tile = TileFactory();
                    tile.Glyph = '.';
                    tile.IsWalkable = true;
                    tile.IsTransparent = true;
                    Map[x, y] = tile;
                }
            }
        }

        private void MakeRoom(bool firstRoom, int x1, int y1, int x2, int y2) {
            MakeRoom(firstRoom, new Vec(x1, y1), new Vec(x2, y2));
        }

        private void MakeRoom(bool firstRoom, Vec a, Vec b) {
            Dig(a, b);

            var position = new Rect(a, b.X - a.X, b.Y - a.Y);

            if (firstRoom) {
                PlayerPlacementFunc(position);
            }
            else {
                MobilePlacementFunc(position);
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
