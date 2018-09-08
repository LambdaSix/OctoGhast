using Capsicum;

namespace OctoGhast.Activities {
    /// <summary>
    /// Activity for crafting items that do not need a workbench.
    /// These are small craft actions that take a single session
    /// </summary>
    public class HandCraftActivity : Activity {
        // Target is the desired item.

        /// <inheritdoc />
        public HandCraftActivity(int totalTime, Entity source, Entity target) : base(totalTime, source, target) { }

        /// <inheritdoc />
        public override bool Start() {
            // Grab components required and do basic checks.

            // If the Target has an UnfinishedItemComponent then we should continue working on it.

            // Check we have all tools required initially.

            // If it's a new item (not a continuation)
            // Check we have all the items to hand
            // 'Consume' the items by moving them into a new "Unfinished Item" entity
            // This allows the actor to stop crafting and be left with a bundle of the work in progress.
            // They can continue crafting the item

            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public override bool Update(ulong time) {
            // Basic checks

            // Check we have the required tools available to us still
            // - If the tools are battery powered, remove some power from it
            // - If the tool we're using has run out of power, we should stop with a message
            // - If the fire we're using goes out, we should attempt to put more fuel in the fire, stop if we can't.

            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public override void Finish() {
            base.Finish();
        }
    }
}