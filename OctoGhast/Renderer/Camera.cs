using OctoGhast.DataStructures.Renderer;
using OctoGhast.Spatial;

namespace OctoGhast.Renderer
{
	public class Camera : ICamera
	{
	    public Vec position { get; set; }
	    public Size size { get; set; }
	    public Rect ViewFrustum { get; set; }

	    public bool MoveTo(Vec worldPosition) {
	        throw new System.NotImplementedException();
	    }
	}
}