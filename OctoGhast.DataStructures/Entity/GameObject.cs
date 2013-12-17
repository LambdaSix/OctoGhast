using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using libtcod;
using OctoGhast.Spatial;
using OctoGhast.DataStructures.Map;

namespace OctoGhast.DataStructures.Entity
{
    public class GameObject
    {
        public Vec Position { get; private set; }
        public char Glyph { get; private set; }
        public TCODColor Color { get; private set; }

        public GameObject(Vec position, char glyph, TCODColor color) {
            Position = position;
            Glyph = glyph;
            Color = color;
        }

        public bool MoveTo(Vec position, GameMap gameMap) {
            var mapTile = gameMap.MapArray[position.X, position.Y];
            var mapWalkable = mapTile.IsWalkable;

            if (mapWalkable) {
                Position = position;
                return true;
            }
            return false;
        }

        public void Draw(TCODConsole buffer, Vec drawPosition)
        {
            buffer.putCharEx(drawPosition.X, drawPosition.Y, Glyph, Color, TCODColor.black);
        }

        public void Undraw(TCODConsole buffer, Vec drawPosition) {

            buffer.putChar(drawPosition.X, drawPosition.Y, ' ');
        }
    }
}
