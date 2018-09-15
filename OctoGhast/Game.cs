using System;
using System.Collections.Generic;

namespace OctoGhast {
    public class CoreGame {
        public EventHandler BeforeTurn { get; set; }
        public EventHandler DuringTurn { get; set; }
        public EventHandler AfterTurn { get; set; }

        public bool DoTurn() {
            // TODO: Priority ordering on turn handlers?
            BeforeTurn?.Invoke(this, null);

            // TODO: RegisterScheduledEvent(TimeSpan.FromMinutes(5), () => Foo());

            throw new NotImplementedException();
        }

        public void RegisterScheduledEvent(TimeSpan interval, Action callback) {

        }
    }
}