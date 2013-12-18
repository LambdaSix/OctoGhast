using OctoGhast.DataStructures.Renderer;
using OctoGhast.Spatial;

namespace OctoGhast.Renderer
{
    public class Camera : ICamera
    {
        public Vec CameraPosition { get; set; }

        /// <summary>
        /// Center point of the camera.
        /// </summary>
        public Vec CameraCenter {
            get {
                // CameraPosition is the top-left corner of the view, so move to the right and down
                return new Vec(CameraPosition.X + Width/2, CameraPosition.Y + Height/2);
            }
        }

        public Rect Dimensions { get; set; }

        public Rect MapSize { get; set; }

        public int MapWidth {
            get { return MapSize.Width; }
        }

        public int MapHeight {
            get { return MapSize.Height; }
        }

        public Vec ToWorldCoords(Vec position) {
            var targetX = position.X + CameraPosition.X;
            var targetY = position.Y + CameraPosition.Y;

            if (targetX < 0 || targetY < 0 || targetX >= MapSize.Width || targetY >= MapSize.Height) {
                return CameraPosition;
            }

            return new Vec(targetX, targetY);
        }

        public Vec ToViewCoords(Vec position) {
            var targetX = position.X - CameraPosition.X;
            var targetY = position.Y - CameraPosition.Y;

            if (targetX < 0 || targetY < 0 || targetX >= Dimensions.Width || targetY >= Dimensions.Height)
                return Vec.Zero;

            return new Vec(targetX, targetY);
        }

        public Camera(Vec cameraPositionVec, Rect dimensions, Rect mapSize) {
            CameraPosition = cameraPositionVec;
            Dimensions = dimensions;
            MapSize = mapSize;
        }

        public int Width {get { return Dimensions.Width; }}
        public int Height { get { return Dimensions.Height; }}
        public Rect ViewFrustum { get; private set; }

        public bool MoveTo(Vec position) {
            var targetX = position.X - Width/2;
            var targetY = position.Y - Height/2;

            if (targetX < 0) targetX = 0;
            if (targetY < 0) targetY = 0;

            if (targetX > MapWidth - Width - 1) targetX = MapWidth - Width - 1;
            if (targetY > MapHeight - Height - 1) targetY = MapHeight - Height - 1;

            var newPosition = new Vec(targetX, targetY);

            var isFovChanged = (targetX != CameraPosition.X || targetY != CameraPosition.Y);

            CameraPosition = newPosition;
            ViewFrustum = new Rect(CameraPosition, Width, Height);
            return isFovChanged;
        }
    }
}