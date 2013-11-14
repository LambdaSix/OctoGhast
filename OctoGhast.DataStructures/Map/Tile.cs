namespace OctoGhast.DataStructures.Map
{
    public interface ITile
    {
        bool IsVisible { get; set; }
        bool IsWalkable { get; set; }
        int Glyph { get; set; }
    }

    /* Implementations */

    public class Tile : ITile
    {
        public bool IsVisible { get; set; }
        public bool IsWalkable { get; set; }
        public int Glyph { get; set; }
    }
}
