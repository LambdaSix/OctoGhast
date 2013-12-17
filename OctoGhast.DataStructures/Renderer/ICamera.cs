using OctoGhast.Spatial;

namespace OctoGhast.DataStructures.Renderer
{
    public interface ICamera
    {
        Vec CameraPosition { get; set; }
        Rect Dimensions { get; set; }
        Rect MapSize { get; set; }
        int MapWidth { get; }
        int MapHeight { get; }
        int Width { get; }
        int Height { get; }
        Vec ToViewCoords(Vec position);
        bool MoveTo(Vec position);
    }
}