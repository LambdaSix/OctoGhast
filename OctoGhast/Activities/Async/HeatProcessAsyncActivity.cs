using Capsicum;

namespace OctoGhast.Activities.Async {
    /// <summary>
    /// Handles activities involving heat and fuel, such as Kilns, Campfires or Smoke racks. 
    /// Shouldn't block the player actor.
    /// </summary>
    public class HeatProcessAsyncActivity : AsyncActivity {
        /// <inheritdoc />
        public HeatProcessAsyncActivity(int totalTime, Entity source, Entity target) : base(totalTime, source, target) { }

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