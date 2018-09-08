using Capsicum;

namespace OctoGhast.Activities {
    /// <summary>
    /// Activity for crafting items that require a workbench, these are assumed to be very long actions
    /// that the player would spend multiple sessions working on.
    /// 
    /// This doesn't cover 'AFK' crafts that are done via timed transformations.
    /// </summary>
    public class WorkbenchCraftActivity : Activity {
        /// <inheritdoc />
        public WorkbenchCraftActivity(int totalTime, Entity source, Entity target) : base(totalTime, source, target) { }

        /// <inheritdoc />
        public override bool Start() {
            // Much the same as HandCraftActivity, but we have a world-item to place the unfinished work item in.
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public override bool Update(ulong time) {
            // Basic checks

            // Check we have the required tools available to us still
            // - If the tools are battery powered, remove some power from it
            // - If the tool we're using has run out of power, stop with a message
            // - If the fire we're using goes out, we should attempt to put more fuel in the fire, stop if we can't.

            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public override void Finish() {
            base.Finish();
        }
    }
}