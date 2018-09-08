using Capsicum;

namespace OctoGhast.Activities {
    /// <summary>
    /// Activity for salvaging large items like cars, rubbish piles, etc
    /// </summary>
    public class SalvageActivity : Activity {
        /// <inheritdoc />
        public SalvageActivity(int totalTime, Entity source, Entity target) : base(totalTime, source, target) { }

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