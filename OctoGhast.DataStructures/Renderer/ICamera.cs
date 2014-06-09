using OctoGhast.Spatial;

namespace OctoGhast.DataStructures.Renderer
{
    public interface ICamera
    {
        /// <summary>
        /// Center world-space position of the camera.
        /// </summary>
        Vec position { get; set; }

        /// <summary>
        /// Area visible by the camera.
        /// </summary>
        Size size { get; set; }

        /// <summary>
        /// Frustum area, world-space co-ordinates.
        /// </summary>
        Rect ViewFrustum { get; set; }

        /// <summary>
        /// Move the camera to the given world-space co-ordinates.
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        bool MoveTo(Vec worldPosition);
    }
}