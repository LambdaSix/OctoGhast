using OctoGhast.DataStructures.Renderer;
using OctoGhast.Entity;

namespace OctoGhast.Renderer
{
    public static class CameraExtensions
    {
        public static void BindTo(this ICamera camera, IMobile mobile) {
            mobile.OnMove(camera.MoveTo);
        }
    }
}