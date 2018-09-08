using Capsicum;

namespace OctoGhast.Activities {
    public class ReadActivity : Activity {
        private ReadableComponent _readable;
        private AttributesComponent _actorAttributes;

        public ReadActivity(int totalTime, Entity source, Entity target) : base(totalTime, source, target) { }

        /// <inheritdoc />
        public override bool Start() {
            if (Target.TryGetComponent<ReadableComponent>(out var readable))
                _readable = readable;
            else {
                // That is not readable!
                return false;
            }

            if (Source.TryGetComponent<AttributesComponent>(out var actorAttributes))
                _actorAttributes = actorAttributes;
            else {
                // Invalid actor
                return false;
            }

            // 1 Minute per page, multiplied by complexity, divided by 1/5th of the actors intelligence.
            var timeRequired = (readable.PageCount * (60 * readable.Complexity) / (_actorAttributes.Intelligence / 5));
            AddRequiredTime(timeRequired);

            return true;
        }

        /// <inheritdoc />
        public override bool Update(ulong turn) {
            // Check that Source (player) can still read Target (readable)
            // - Can Source reach Target?
            // - Does Source still satisfy conditions?
            // - Is Target still something that can read?

            // Check we haven't been Interrupted by something else.
            if (InterruptFlag) {
                // Something interrupted us!
                return false;
            }

            // Are we due to apply any buffs/debuffs to Source for reading this?
            // - Fun books give BUFF_HAPPY
            // - Not-fun (technical) books give DEBUFF_UNHAPPY, unless player is considered Smart or has perks
            // - Some technical books are tiring to read, possibly add a little fatigue

            // Did we finish reading the book? (CurrentPage >= TotalPages)
            // Invoke the Finish handler.

            return true;
        }

        /// <inheritdoc />
        public override void Finish() {
            // Mark the book as Read
            UpdateActorsBookLedger();
            base.Finish();
        }

        private void UpdateActorsBookLedger() {
            if (Source.TryGetComponent<ReadingLedgerComponent>(out var ledger)) {
                // Add this readable to the Fully Read pile
                if (!ledger.FullyRead.Contains(_readable.ReadableId)) {
                    ledger.FullyRead.Add(_readable.ReadableId);
                }
                
                // And remove it from the Partially Read pile
                if (ledger.PartiallyRead.Contains(_readable.ReadableId)) {
                    ledger.PartiallyRead.Remove(_readable.ReadableId);
                }

                // Reading the entire book implies it was skimmed. Though the first interaction with an unknown book should be
                // to skim it with the QuickReadActivity. Cover our asses anyway.
                if (!ledger.Skimmed.Contains(_readable.ReadableId)) {
                    ledger.Skimmed.Add(_readable.ReadableId);
                }
            }
            else {
                // Add a book ledger to this entity, it doesn't have one, probably never read or skimmed a Readable.
                var newLedger = new ReadingLedgerComponent()
                {
                    FullyRead = {_readable.ReadableId},
                    Skimmed = {_readable.ReadableId}
                };

                Source.AddComponent(newLedger);
            }
        }
    }
}