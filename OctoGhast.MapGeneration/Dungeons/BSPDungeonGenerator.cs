using System;
using System.Linq;
using Microsoft.Xna.Framework;
using OctoGhast.DataStructures.Map;
using OctoGhast.Spatial;
using RenderLike;
using RenderLike.BSP;

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
        public Size Dimensions { get; set; }

        public void GenerateMap(Size dimensions) {
            Map = new Array2D<ITile>(dimensions.Width, dimensions.Height);
            Map.Fill(vec => TileFactory());
            Dimensions = dimensions;

            Generate();
        }

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
            var rand = new Rand();

            var bsp = new BSPTree(new Rectangle(0, 0, Dimensions.Width, Dimensions.Height));
            bsp.SplitRecursive(8, RoomMaxSize, RoomMinSize, 1.5f, 1.5f, rand);

            int lastX = 0, lastY = 0;

            foreach (var node in bsp.LevelOrder.Reverse()) {

                int x, y, w, h;

                if (node.IsLeaf) {
                    w = rand.Next(RoomMinSize, Math.Max(node.Rect.Width - 2, RoomMinSize));
                    h = rand.Next(RoomMinSize, Math.Max(node.Rect.Height - 2, RoomMinSize));
                    x = rand.Next(node.Rect.X + 1, node.Rect.X + node.Rect.Width - w - 1);
                    y = rand.Next(node.Rect.Y + 1, Math.Max(node.Rect.Y + node.Rect.Height - h - 1, node.Rect.Y));

                    MakeRoom(roomNumber == 0, x, y, x + w - 1, y + h - 1);

                    if (roomNumber != 0) {
                        Dig(new Vec(lastX, lastY), new Vec(x + w / 2, lastY));
                        Dig(new Vec(x + w / 2, lastY), new Vec(x + w / 2, y + h / 2));
                    }

                    lastX = x + w / 2;
                    lastY = y + h / 2;
                    roomNumber++;
                }
            }
        }
    }
}
