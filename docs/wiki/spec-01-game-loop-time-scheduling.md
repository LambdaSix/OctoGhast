# Spec 01 investigation — application shell, game loop, turns, time and scheduling

Parent: #65  
Tracking issue: #66  
Reference baseline: `LambdaSix/Cataclysm-DDA@e262adb299a7613b4aedc5f12c08fe0413c56a84`

## Status and purpose

This page records the investigation required by #66 and converts the pinned Cataclysm:DDA behavior into an implementation-facing contract for OctoGhast. It is intentionally behavioral: parity requires reproducing externally observable ordering, time/move semantics, interruption and persistence behavior, not CDDA's concrete class layout.

## Authoritative reference anchors

Primary runtime sources at the pinned baseline:

- `src/main.cpp` — process/application initialization and the outer menu/game loop.
- `src/do_turn.cpp` — authoritative per-turn orchestration and cleanup/game-over behavior.
- `src/calendar.h`, `src/calendar.cpp` — time representation, conversion, season/calendar rules and `once_every` cadence.
- `src/timed_event.h`, `src/timed_event.cpp` — scheduled event queue and due-event processing.
- `src/player_activity.h`, `src/player_activity.cpp` — long-running activity progress in move units, completion/cancellation and per-turn EOC hooks.

Secondary dependencies to specify in later child tickets include Character/avatar `process_turn` and `update_body`, monster/NPC turn logic, map field/item/vehicle processing, weather, scent, missions, item wakeups, autosave/persistence and EOC/event-bus consumers.

## Application state machine

At process level the observable state progression is:

1. Runtime/platform/path/debug/locale/interface initialization.
2. RNG seeded from CLI/default seed.
3. Create the single game instance and load static game data.
4. Enter main-menu loop.
5. A successful menu selection begins a game session and emits `game_begin`.
6. Repeatedly call the game-turn function until it reports session completion.
7. Return to main menu; process exit performs global cleanup.

The gameplay session therefore has a natural OctoGhast boundary: **ApplicationShell** owns boot/configuration/menu/session lifetime, while **GameSession** owns simulation state and exposes a single “advance one global turn / interact until turn exhausted” operation.

## Time model

### Base units

CDDA's `time_duration` stores an integer number of turns.

- 1 turn = 1 second.
- 1 turn = 100 moves for duration conversion purposes.
- 1 minute = 60 turns.
- 1 hour = 3,600 turns.
- 1 day = 86,400 turns.
- `time_point` stores an integer turn coordinate.

This is a critical compatibility contract. “Moves” are action-budget units, not the world clock itself. Actor speeds change the number of moves regained/available, while the global clock advances in one-second turns.

### Calendar cadence

`calendar::once_every(frequency)` is true when:

`(calendar::turn - calendar::turn_zero) % frequency == 0`

Consequences:

- Periodic systems are phase-locked to `turn_zero`, not to when a subsystem was instantiated.
- Saving/loading must restore the absolute turn so periodic phases do not drift.
- Systems using the same frequency observe the same global cadence unless they add independent state/RNG.

### Seasons and date presentation

Season length is configurable and the year is four seasons. Time points themselves remain integer turns; changing season length changes interpretation of dates/seasons, not stored elapsed-turn values. The initial season and start-of-game/cataclysm anchors are separate calendar concepts and must be persisted/configured consistently.

## Global turn semantics

### New-game special case

On the first invocation after starting a game:

- `new_game` is cleared.
- weather receives `on_game_start()`.
- the global calendar is **not** incremented.

On subsequent invocations:

- game-mode per-turn logic runs.
- `calendar::turn += 1_turn`.

This means the initial playable state is processed at the configured starting turn before the first one-second increment.

### Authoritative per-turn ordering

The following ordering is observable enough to preserve as a compatibility contract.

1. **Game-over gate**
   - If the session is already over, execute end/session cleanup and return to shell.

2. **Begin global turn**
   - New-game special handling or increment the global turn by one second.
   - Clear one-turn/dimension-swap transient state.
   - Update music selection.
   - Clear weather temperature cache.
   - Reload NPCs if marked dirty.

3. **Top-of-turn scheduled/global services**
   - Process timed-event queue.
   - Process item wakeups due at current turn.
   - Hourly sweep of expired craft reservations.
   - Process all missions.
   - Resolve controlled-vehicle theft check.
   - Correct avatar trapped in invalid/impassable position.
   - Check mounted creature spook.
   - Daily overmap monster-group processing.
   - Move hordes (internally rate-limited).
   - Move nemesis on its cadence.
   - Update avatar body state.

4. **Autosave hook**
   - If autosave is enabled, current turn matches the configured autosave-turn cadence, and avatar is not dead: autosave.

5. **Weather/light/spawn setup**
   - Update weather.
   - Reset light and invalidate/set lightmap caches dirty.
   - Potentially add random NPC.

6. **Avatar pre-input activity consumption**
   - While avatar has positive moves and an activity, execute activity turns.
   - Activities can consume all available moves and may finish, replace themselves, cancel or enqueue resumable work.

7. **Pre-input sound processing**
   - Nearby NPCs process sound markers.
   - Avatar processes sound markers.
   - Hearing-loss audio state is updated.

8. **Avatar input/action phase**
   - If not sleeping (or watch mode), and avatar has moves:
     - Before each action: process falling, dead cleanup, monster-info refresh, new sounds and pending explosions.
     - Redraw/wait UI as appropriate.
     - `handle_action()` executes one user-visible action.
     - If an action was taken, increment moves-since-save and invoke avatar action hook.
     - Re-check game-over immediately.
     - Activities started by the action consume remaining moves immediately in a loop.
   - If avatar has no moves, input polling is rate-limited; activity distractions can trigger cancellation/ignore queries.

9. **Post-avatar environment phase**
   - Reconcile driving view offset.
   - Deposit avatar scent, then update scent map.
   - Build floor caches.
   - Process falling.
   - Move vehicles.
   - Process map fields.
   - Process map items.
   - Resolve queued explosions.
   - Apply field effects to avatar.

10. **AI perception and actor phase**
    - Convert sounds from the previous/action phase into AI-consumable sound state.
    - Build map/vision cache for creature AI.
    - Process monsters, then active NPCs.
      - Each creature first receives per-turn processing.
      - Each then spends its available moves in its internal movement/decision loop.
      - Dead cleanup and nonlocal-monster despawn occur around this work.
    - Move travelling overmap NPCs on their cadence.
    - Emit fields from furniture/terrain.
    - Refresh monster information.

11. **Avatar move replenishment**
    - Call avatar `process_turn()`.
    - This is deliberately after monsters/NPCs and environment processing.
    - The replenished moves are for the next action opportunity/global-turn cycle.

12. **Late-turn avatar/environment/UI maintenance**
    - Optional redraw if moves remain negative and force-redraw enabled.
    - Apply weather effects.
    - Refresh activity/progress UI.
    - Invalidate visibility cache.
    - Update body temperature/wetness/wetness morale.
    - Minute cadence morale update for avatar/NPCs.
    - 9-turn cadence avatar morale recovery check.
    - Audio-state maintenance.
    - Reset avatar noise.
    - Calculate bionic power balance.
    - Mark browser build unsaved and tick debug capture.

13. Return “session continues”.

### Ordering invariants that should be explicit in OctoGhast

- Due timed events occur after the world-clock increment but before ordinary actor/environment work for that global turn.
- Autosave happens before weather/environment/AI processing later in the same turn.
- Avatar activities can consume available moves before input and immediately after a user action that starts an activity.
- Map environment processing occurs after the avatar action phase but before monster/NPC movement.
- Monsters are processed before active NPCs in the shared actor phase.
- Avatar move replenishment occurs after environment + monster/NPC processing, not at the beginning of the turn.
- Late physiological/morale/UI updates can therefore observe state resulting from actor/environment processing in the same global turn.

## Move-budget semantics

The global clock and actor move budgets are related but distinct.

Activities document move counts as 1/100-second-equivalent work units. For activity types based on time, a normal activity turn consumes 100 activity moves per global turn (modified by exertion multiplier) and zeroes the actor's current moves. Speed-based activities consume according to the actor's available moves.

Required OctoGhast abstractions:

- `WorldTime`: integer global-turn time point and typed duration conversion.
- `MoveBudget`: signed integer action budget for an actor.
- `ActorSpeedProvider`: derives per-turn move replenishment from actor state.
- `ActivityProgress`: separate remaining/total work measured in move units with an explicit basis (time, speed, neither).

Do not conflate “100 moves” with a guarantee that every action lasts exactly one second. Actions may consume more/less than a turn's budget and can leave a negative budget that delays the next opportunity to act.

## Scheduling contracts

### Periodic cadence

Use a deterministic absolute-turn predicate equivalent to `once_every`. It must not be implemented using wall-clock timers.

### Timed event queue

A timed event contains at minimum:

- type/handler identity
- due `time_point`
- optional faction/context identifiers
- absolute map location/context
- strength/payload/string data
- optional key for bulk rescheduling

Every global turn, the manager iterates queued events:

1. run each event's `per_turn` hook;
2. if `when <= current_turn`, run `actualize`;
3. remove the event after actualization.

An event may enqueue another event during actualization. Queue semantics should preserve deterministic iteration/order for events with the same due turn. CDDA uses an ordered list rather than a priority queue; parity tests should pin same-turn insertion/processing behavior where relevant.

Timed events are serializable and therefore part of save state.

### Item wakeups and other schedulers

Item wakeups are processed near timed events but are a distinct subsystem. EOCs/activities/missions may also have their own scheduled-state mechanisms. OctoGhast should expose a common clock service while allowing feature-local schedulers when their persistence/ordering differs.

## Activity interruption and re-entry contract

Long-running activities are stateful and serializable.

Relevant observable behavior:

- Activities have total/remaining work and can be interruptible independently for generic distractions vs keyboard pause.
- Per-turn EOC runs before the activity actor/legacy handler. It may cancel the activity.
- The actor/handler may replace the current activity; processing stops immediately if the activity identity changes.
- Completion EOC fires when remaining work reaches zero before the actor/legacy finish hook.
- Completion and cancellation emit a `character_finished_activity` event with cancellation status.
- A finished activity is cleared and may resume compatible backlog work unless destination/autotravel rules suppress it.
- Low stamina can interrupt work, optionally enqueue rest and mark the previous activity for auto-resume.
- NPC activity code must defend against out-of-bounds/unloaded targets to avoid infinite loops.

The deeper activity actor matrix belongs in #69, but #66 must guarantee that the game loop repeatedly invokes activity processing while moves remain and honors immediate cancellation/replacement.

## Game-over/session-end behavior

When the session ends due to death/suicide, cleanup includes persistent-world handling before returning to the shell: monsters are despawned appropriately, NPC dispositions/factions/missions/maps/achievements are saved, death screen/event/memorial/graveyard handling runs, then world-retention/reset/delete policy is applied. Generic session cleanup resets transient view/audio/zones/map/overmap state.

OctoGhast should model session termination as a state transition with a single idempotent cleanup path. “Game over detected during avatar input” must take this path immediately rather than finishing the remainder of the turn.

## Persistence requirements

At minimum the following timing state is parity-significant and must survive a save/load round trip:

- absolute global `time_point`
- start-of-game/cataclysm anchors and world calendar settings
- scheduled timed events including due time and payload/key
- activity remaining/total progress and actor-specific state
- move-budget values where CDDA persists them through Character serialization
- subsystem-local next-run/wakeup records where used
- configuration needed to retain periodic phase, including season length and relevant world options

A save loaded at turn N must not shift hourly/daily/`once_every` events to a new phase.

## RNG and determinism

- CLI supports a deterministic RNG seed.
- Turn ordering determines RNG consumption order; changing system ordering can change downstream outcomes even when individual formulas match.
- Conformance tests should therefore separate:
  - exact deterministic tests under a fixed seed when call order is controlled;
  - statistical/tolerance tests for systems whose unrelated RNG consumers make exact streams fragile.
- The game loop itself should avoid adding hidden random draws merely for scheduling.

## Proposed OctoGhast boundaries

Recommended interfaces are behavioral, not CDDA-class copies:

- **ApplicationShell**
  - boot/configure/load static content
  - menu/session lifecycle
  - create/destroy `GameSession`
- **GameSession**
  - `advance_turn()`
  - game-over/session state
  - ordered phase coordinator
- **WorldClock**
  - current absolute turn
  - typed duration conversion
  - `is_due_every(duration)`
- **Scheduler**
  - persistent due-time events with deterministic same-turn ordering
- **ActorTurnService**
  - avatar action opportunity
  - monster phase
  - NPC phase
  - move replenishment
- **EnvironmentTurnService**
  - scent/vehicles/fields/items/explosions/weather hooks
- **AutosavePolicy**
  - cadence predicate and safe save invocation
- **TurnHooks/EventBus**
  - explicit integration points so dependent specs do not call arbitrary loop internals

Implementation may combine these physically, but tests should address them as contracts.

## Black-box parity/conformance scenarios

### Time and cadence

1. Start a new game at a known starting turn; first `do_turn` does not advance calendar time, second advances exactly one second.
2. Verify 60 successive increments equal one minute and 3,600 equal one hour.
3. Verify `to_moves(1_second) == 100` and time-duration round trips.
4. Set current time around an hourly boundary and prove `once_every(1_hour)` triggers only on the absolute phase boundary.
5. Save one turn before an hourly boundary, load, and prove the next trigger occurs on the same absolute turn.

### Scheduled events

6. Queue an event for turn N; verify its per-turn hook runs before N and actualization runs when `current_turn >= N`.
7. Queue two same-turn events and pin deterministic execution order.
8. Have one event enqueue a follow-up and verify whether it can run in the same manager pass or only a later turn; match pinned CDDA behavior.
9. Save/load pending events and verify due times/payloads survive.

### Actor/order behavior

10. Give avatar a positive move budget and an existing activity; prove activity work is performed before normal input.
11. Start an activity from a user action; prove it immediately spends remaining moves in the same global turn.
12. Construct a field/environment effect and hostile monster where ordering is observable; verify field/item/vehicle phase precedes monster/NPC phase.
13. Verify monster processing precedes active NPC processing for a scenario where interaction order changes the result.
14. Verify avatar move replenishment occurs after monster/NPC processing, so newly replenished moves are not spent earlier in the same turn.

### Autosave/game-over

15. Configure autosave every N turns; prove save happens on the phase-locked cadence and is suppressed for a dead avatar.
16. Trigger death during avatar action; prove cleanup/session end happens immediately without running remaining environment/AI phases.
17. Save/load a long-running activity and verify remaining progress and interruption flags are preserved.

### Deterministic replay fixture

18. With a fixed seed, fixed small map, fixed actor states and no asynchronous input, record a per-phase trace for several turns and compare OctoGhast trace against a reference harness. Trace should include current turn, phase name, actor move budgets, due schedulers and stable state hashes.

## Edge cases / compatibility decisions still to pin with focused tests

- Exact same-pass behavior when a timed event appends another event to the manager list during `actualize`.
- Precise move replenishment formula and modifier ordering belongs to Character (#67), but #66 depends on its phase position.
- Whether all move-budget fields are serialized in current CDDA Character format should be verified as part of persistence (#85).
- Some periodic functions internally rate-limit themselves even though the outer loop calls them every turn; dependent specs must not infer “every turn” simulation cost from call frequency.
- Wall-clock UI/input throttling (for example 100 ms polling while out of moves) is presentation/runtime behavior and must not affect simulation time.

## Dependency/sequence implications

Specs that should treat this page as foundational:

- #67 Character model — owns speed/move replenishment and physiological subroutines called by the loop.
- #69 Activity framework — owns activity state machine invoked by the avatar/NPC turn phases.
- #79 Environment — owns weather/scent/fields/fire/rot cadence inside the established phases.
- #80 Vehicles — `vehmove` occurs in the environment phase before actor AI.
- #82 Events/EOC runtime — scheduled/recurring EOCs must bind to the same absolute clock and deterministic ordering.
- #85 Persistence — must preserve absolute time and scheduler/activity state.
- #88 Test harness — should provide phase tracing, fixed RNG seed and reference differential execution.

## Acceptance status against #66

- [x] Turn, move and calendar units/conversions identified.
- [x] Processing/order guarantees and interruption points enumerated.
- [x] External hook classes for actors, activities, items, environment and schedulers identified.
- [x] Save/load implications identified.
- [x] Black-box parity and deterministic conformance scenarios listed.
- [x] Dependencies/sequencing constraints identified.
- [x] Ambiguous behavior called out for focused reference tests rather than guessed.

## Source links

All links below are pinned to the programme baseline:

- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/main.cpp
- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/do_turn.cpp
- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/calendar.h
- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/calendar.cpp
- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/timed_event.h
- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/timed_event.cpp
- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/player_activity.h
- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/player_activity.cpp
