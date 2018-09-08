using System;

namespace OctoGhast.SystemMechanic {
    /// <summary>
    /// This class handles the advancement of time for the player/world in increments of seconds.
    /// It is not related to ordering of entity ticks and updates, that's <seealso cref="OctoGhast.SystemManager.TimeSysteManager"/>
    /// </summary>
    public class ChronologySystemMechanic : GameMechanic {
        public const int TurnsPerSecond = 4;
        private ulong _lastTick;

        //TODO: Mutable DateTime?
        private DateTime _currentDateTime = new DateTime();

        public ChronologySystemMechanic(DateTime epoch) {
            _currentDateTime = epoch;
        }

        /// <inheritdoc />
        public override void Update(ulong tickCount) {
            // Eh, something like this, idk
            var elapsedTicks = tickCount - _lastTick;

            if (elapsedTicks < TurnsPerSecond)
                return;

            var elapsedSeconds = elapsedTicks / TurnsPerSecond;
            _currentDateTime = _currentDateTime.AddSeconds(elapsedSeconds);

            _lastTick = tickCount;
        }
    }

    public abstract class GameMechanic {
        public abstract void Update(ulong tickCount);
    }
}