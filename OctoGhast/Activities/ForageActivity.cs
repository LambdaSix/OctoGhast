using Capsicum;

namespace OctoGhast.Activities {
    /// <summary>
    /// Activity for foraging bushes or an area for resources.
    /// </summary>
    public class ForageActivity : Activity {
        /// <inheritdoc />
        public ForageActivity(int totalTime, Entity source, Entity target) : base(totalTime, source, target) { }

        /// <inheritdoc />
        public override bool Start() {
            // Target will be a meta-entity containing map information or an entity on the map
            // TODO: Decide if the user forages single items (a bush) or a general area
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public override bool Update(ulong time) {
            throw new System.NotImplementedException();
        }
    }
}