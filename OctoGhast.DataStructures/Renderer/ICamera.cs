using OctoGhast.Spatial;

namespace OctoGhast.DataStructures.Renderer
{
    public interface ICamera
    {
	    Vec CameraPosition { get; set; }

	    /// <summary>
	    /// Center point of the camera.
	    /// </summary>
	    Vec CameraCenter { get; }

	    Rect Dimensions { get; set; }
	    Rect MapSize { get; set; }
	    int MapWidth { get; }
	    int MapHeight { get; }
	    int Width { get; }
	    int Height { get; }
	    Rect ViewFrustum { get; }
	    Vec ToWorldCoords(Vec position);
	    Vec ToViewCoords(Vec position);
	    bool MoveTo(Vec position);
    }
}