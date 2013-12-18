using libtcod;
using OctoGhast.DataStructures.Entity;
using OctoGhast.Spatial;

namespace OctoGhast.Entity
{
    public class Player : Mobile
    {
        public Player(Vec position, char glyph, TCODColor color) : base(position, glyph, color) {}
    }
}