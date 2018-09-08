using Capsicum;

namespace OctoGhast.Activities {
    public class FishingActivity : Activity {
        /// <inheritdoc />
        public FishingActivity(int totalTime, Entity source, Entity target) : base(totalTime, source, target) { }

        /// <inheritdoc />
        public override bool Start() {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public override bool Update(ulong time) {
            throw new System.NotImplementedException();
        }
    }
}