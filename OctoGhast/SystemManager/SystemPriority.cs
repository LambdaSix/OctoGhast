namespace OctoGhast {
    public enum SystemPriority {
        /// <summary>
        /// Low priority systems can be called intermittently with gaps in gametime, for high-latency things like rot, fire, etc.
        /// </summary>
        Low,

        /// <summary>
        /// Normal priority systems rank above low priority but can still be intermittently called.
        /// Anything not requiring exact per-frame latency should use this ranking.
        /// This is intended mostly for the Activity and Chronology system.
        /// Camp AI can use this ranking.
        /// </summary>
        Normal,

        /// <summary>
        /// High priority systems are guaranteed to be called once per world-tick, every tick.
        /// Reserve this for systems that have to be processed every tick, like AI or precise world-effects.
        /// </summary>
        High
    }
}