using OctoGhast.DataStructures.Renderer;
using OctoGhast.Spatial;

namespace OctoGhast.Renderer
{
	public class Camera : ICamera
	{
		public Camera(Vec cameraPositionVec, Rect dimensions, Rect mapSize) {
			CameraPosition = cameraPositionVec;
			Dimensions = dimensions;
			MapSize = mapSize;
		}

		public Vec CameraPosition { get; set; }

		/// <summary>
		///     Center point of the camera.
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

		/// <summary>
		/// Determine if <paramref name="target"/> is outside of <paramref name="container"/>
		/// </summary>
		/// <param name="target">The point vector to check</param>
		/// <param name="container">The bounding rectangle to check against</param>
		/// <returns>True if the target is outside the container</returns>
		private static bool IsOutside(Vec target, Rect container) {
			return (target.X < 0 || target.Y < 0 || target.X >= container.Width || target.Y >= container.Height);
		}

		public Vec ToWorldCoords(Vec position) {
			var target = position + CameraPosition;

			if (IsOutside(target,MapSize)) {
				return CameraPosition;
			}

			return target;
		}

		public Vec ToViewCoords(Vec position) {
			var target = position - CameraPosition;

			if (IsOutside(target, Dimensions)) {
				return Vec.Zero;
			}

			return target;
		}

		public int Width {
			get { return Dimensions.Width; }
		}

		public int Height {
			get { return Dimensions.Height; }
		}

		/// <summary>
		/// Returns the area this camera can see (The View Frustum)
		/// </summary>
		public Rect ViewFrustum {
			get { return new Rect(CameraPosition, Width, Height); }
		}

		public bool MoveTo(Vec position) {
			int targetX = position.X - Width/2;
			int targetY = position.Y - Height/2;

			if (targetX < 0) {
				targetX = 0;
			}
			if (targetY < 0) {
				targetY = 0;
			}

			if (targetX > MapWidth - Width - 1) {
				targetX = MapWidth - Width - 1;
			}
			if (targetY > MapHeight - Height - 1) {
				targetY = MapHeight - Height - 1;
			}

			var newPosition = new Vec(targetX, targetY);

			bool isFovChanged = (targetX != CameraPosition.X || targetY != CameraPosition.Y);

			CameraPosition = newPosition;
			return isFovChanged;
		}
	}
}