namespace OctoGhast.DataStructures.Map
{
    public interface ITile
    {
        bool IsExplored { get; set; }
        bool IsWalkable { get; set; }
        int Glyph { get; set; }
        bool IsTransparent { get; set; }
    }
}
