using Capsicum;

namespace OctoGhast.Activities {
    /// <summary>
    /// Activity for filling containers with fluids, i.e. filling a canteen from a river.
    /// </summary>
    public class MoveFluidActivity : Activity {
        /// <inheritdoc />
        public MoveFluidActivity(int totalTime, Entity source, Entity target) : base(totalTime, source, target) { }

        /// <inheritdoc />
        public override bool Start() {
            // Target will be the container to transfer water from, either an item or a map entity or tile
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public override bool Update(ulong time) {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public override void Finish() {
            base.Finish();
        }
    }
}