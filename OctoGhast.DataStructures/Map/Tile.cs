namespace OctoGhast.DataStructures.Map
{
    public interface ITile
    {
        bool IsExplored { get; set; }
        bool IsWalkable { get; set; }
        int Glyph { get; set; }
        bool IsTransparent { get; set; }
    }

    /* Implementations */

    public class Tile : ITile
    {
        public bool IsWalkable { get; set; }
        public bool IsExplored { get; set; }
        public bool IsTransparent { get; set; }
        public int Glyph { get; set; }
    }
}
