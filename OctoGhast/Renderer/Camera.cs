using OctoGhast.Spatial;

namespace OctoGhast.Renderer
{
    public class Camera
    {
        public Vec Position { get; set; }

        public Rect Dimensions { get; set; }

        public Rect MapSize { get; set; }

        public int MapWidth {
            get { return MapSize.Width; }
        }

        public int MapHeight {
            get { return MapSize.Height; }
        }

        public Vec ToViewCoords(Vec position) {
            var targetX = position.X - Position.X;
            var targetY = position.Y - Position.Y;

            if (targetX < 0 || targetY < 0 || targetX >= Dimensions.Width || targetY >= Dimensions.Height)
                return Vec.Zero;

            return new Vec(targetX, targetY);
        }

        public Camera(Vec positionVec, Rect dimensions, Rect mapSize) {
            Position = positionVec;
            Dimensions = dimensions;
            MapSize = mapSize;
        }

        public int Width {get { return Dimensions.Width; }}
        public int Height { get { return Dimensions.Height; }}

        public bool MoveTo(Vec position) {
            var targetX = position.X - Width/2;
            var targetY = position.Y - Height/2;

            if (targetX < 0) targetX = 0;
            if (targetY < 0) targetY = 0;

            if (targetX > MapWidth - Width - 1) targetX = MapWidth - Width - 1;
            if (targetY > MapHeight - Height - 1) targetY = MapHeight - Height - 1;

            var newPosition = new Vec(targetX, targetY);

            var isFovChanged = (targetX != Position.X || targetY != Position.Y);

            Position = newPosition;
            return isFovChanged;
        }
    }
}