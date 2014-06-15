using OctoGhast.Spatial;

namespace OctoGhast.DataStructures.Renderer
{
    public interface ICamera
    {
        /// <summary>
        /// Center world-space position of the camera.
        /// </summary>
        Vec Position { get; set; }

        /// <summary>
        /// Area visible by the camera.
        /// </summary>
        Size Size { get; set; }

        /// <summary>
        /// Frustum area, world-space co-ordinates.
        /// </summary>
        Rect ViewFrustum { get; }

        /// <summary>
        /// Move the camera to the given world-space co-ordinates.
        /// </summary>
        /// <param name="worldPosition">World position</param>
        void MoveTo(Vec worldPosition);
    }
}