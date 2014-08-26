using OctoGhast.DataStructures.Map;

namespace OctoGhast.Map
{
    public class Tile : ITile
    {
        public bool IsWalkable { get; set; }
        public bool IsExplored { get; set; }
        public bool IsTransparent { get; set; }
        public int Glyph { get; set; }
    }
}