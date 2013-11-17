using libtcod;
using OctoGhast.DataStructures.Entity;
using OctoGhast.Spatial;

namespace OctoGhast.Entity
{
    public class Player : GameObject
    {
        public Player(Vec position, char glyph, TCODColor color) : base(position, glyph, color) {}
    }
}