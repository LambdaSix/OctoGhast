using OctoGhast.DataStructures.Renderer;
using OctoGhast.Spatial;

namespace OctoGhast.Renderer
{
	public class Camera : ICamera
	{
	    public Vec Position { get; set; }
	    public Size Size { get; set; }

	    public Camera(Vec position, Size size) {
	        Position = position;
	        Size = size;
	    }

	    public Rect ViewFrustum {
	        get { return Rect.FromCenter(Position, Size); }
	    }

	    public void MoveTo(Vec worldPosition) {
	        Position = worldPosition;
	    }
	}
}