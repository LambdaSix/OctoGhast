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


---

# Architecture review — authoritative real-time/co-op adaptation

Review basis: #52, #57, #64 and #65. The completed pinned-CDDA investigation above remains authoritative evidence for `LambdaSix/Cataclysm-DDA@e262adb299a7613b4aedc5f12c08fe0413c56a84`. This section records the intentional OctoGhast adaptation and resulting implementation contract; it does not rewrite the reference findings.

## 1. Pinned CDDA reference behaviour

Preserve these rules as parity evidence:

- world time is integer turns: 1 turn = 1 second;
- duration conversion is 100 moves per second/turn; moves are action/work currency, not milliseconds;
- speed controls move opportunity, actions/activities consume moves, and negative move balance is valid;
- calendar cadence is absolute-phase from the world epoch/turn zero;
- timed events, activity state, interruption and relevant ordering are persistent/deterministic gameplay state;
- the detailed global-turn ordering, first-turn exception, autosave/game-over placement and activity hooks documented above remain reference behaviour.

OctoGhast targets these rules, not literal local-avatar input gating.

## 2. Intentional OctoGhast adaptation

- One authoritative server owns one continuously advancing canonical simulation clock.
- The world does not normally stop because one player has no input.
- Multiple players, NPCs and monsters progress independently during the same world-time interval.
- Single-player is a one-player authoritative server over in-process transport; multiplayer changes transport, not simulation semantics.
- Godot sends requests and consumes projections/events. Render/interpolation rate is never authoritative.

## 3. Canonical time and move mapping

`SimulationTick` is a monotonically increasing integer server coordinate. Initial contract:

- `SimulationTicksPerSecond = 10`;
- one tick = 100 ms of nominal real-time pacing;
- 10 ticks = 1 pinned-CDDA turn = 1 world second = 100 moves;
- a baseline speed-100 actor therefore accrues 10 moves per tick.

10 TPS is selected because it maps exactly into CDDA's 100-move-per-second economy while allowing sub-second eligibility and without copying Ghast's 20 TPS by assumption. Tick rate is world/protocol compatibility data; changing it later requires explicit versioning/migration.

Chronology is derived from canonical ticks and persisted epoch/calendar configuration. There is no independently advancing calendar clock.

## 4. Action-budget contract

Each actor owns signed action budget plus deterministic fractional remainder.

For elapsed ticks, earn `speed * elapsedTicks / 10` moves. Retain fractional remainder rather than rounding it away per tick. Actions subtract the Cataclysm rule layer's pinned-CDDA-compatible cost. Negative balance delays that actor's next action while other actors continue. Positive unused budget may carry; any anti-burst cap must be explicit policy in #57/#67 and must not alter individual action costs.

Speed/cost formulas are Cataclysm policy. Budget accumulation, remainder accounting and eligibility are generic scheduling infrastructure.

## 5. Fixed-step progression and ordering

The server exposes an operation equivalent to `AdvanceOneSimulationTick()`. Each tick uses a fixed delta and a deterministic phase pipeline:

1. establish canonical tick N;
2. admit previously received client requests at the defined boundary;
3. accrue actor budgets and identify due absolute-time work;
4. run due scheduler/world cadence;
5. progress activities;
6. resolve eligible player/AI work;
7. settle due environment/world systems;
8. publish authoritative events/projections;
9. pump transport at the defined boundary for later admission.

Dependent specs may refine sub-phases, but pinned observable ordering constraints remain binding.

Same-tick work must never depend on ECS/hash iteration, thread scheduling, socket callback timing or Godot node order. Order by: explicit phase; due tick; subsystem stable priority where required; stable actor/entity ID; then monotonic server enqueue sequence. Player and AI requests use the same authoritative resolution path. Same initial state + RNG state + admitted input sequence must yield the same trace.

## 6. Host pacing and catch-up

A host runner converts monotonic host elapsed time into owed fixed ticks with an accumulator.

- gameplay never receives variable render-frame delta as authoritative simulation time;
- late hosts execute owed fixed ticks sequentially;
- bound catch-up work per host pump to avoid an unresponsive spiral;
- retain excess tick debt for later pumps: never drop authoritative ticks and never stretch tick duration;
- network I/O may be pumped between ticks only at the defined deterministic boundary and cannot mutate an already-running tick;
- headless tests can step ticks directly without sleeping or consulting wall time.

Thus 30/60/144 FPS, irregular frames and a headless host produce the same result after the same canonical tick/input sequence.

## 7. Activities

Activities are actor-owned persistent work, not global clock controls.

- speed-based work consumes that actor's earned move budget;
- time-based work advances from canonical elapsed time using the pinned 100-moves-per-world-second basis plus Cataclysm modifiers;
- one player's long activity never blocks another player/actor/world system;
- completion/cancellation/replacement/backlog/EOC ordering preserves the pinned evidence above;
- activity progress never uses render frames.

Prefer absolute deadlines or elapsed-interval formulas for large advances. Systems requiring intermediate collisions/interactions/RNG must declare a timewarp barrier and be deterministically stepped/settled rather than skipped.

## 8. Pause, acceleration and timewarp

Pause is server clock policy.

Single-player may explicitly pause the one-player server. While paused, canonical tick, chronology, budgets, activities and due events do not advance; transport/UI may still pump. A client panel pauses only if explicit single-player UI policy says so.

In multiplayer, one client cannot pause global time. Default policy with multiple active players is no unilateral pause. An optional unanimous/admin policy may pause the one global server clock. Disconnect/lag does not create a pause or second clock.

Acceleration changes host pacing, not tick/move/second meaning. One client cannot locally accelerate global time. Initial multiplayer policy requires unanimous consent and all active players in compatible states (sleep/wait/long activity); return to 1x on the first deterministic withdrawal, threat/interrupt, or incompatible request.

A future leap optimization may jump canonical time only across systems declared leap-safe. Absolute deadlines and elapsed-interval state are leap-friendly; intermediate-simulation systems are barriers. Optimized timewarp must be observationally equivalent to ordinary accelerated fixed stepping for in-scope gameplay.

## 9. Persistence

Persist timing state required for phase-correct deterministic continuation:

- canonical tick and tick-rate/version;
- calendar/world epoch anchors/options;
- actor move budgets and fractional remainders;
- stable actor/entity IDs;
- scheduler due ticks and same-tick order/enqueue sequence where relevant;
- activity progress/interruption/resume state;
- required RNG state/streams.

Do not persist transient host accumulator debt. Loading restores a simulation boundary, not a render frame.

## 10. Headless/client/server boundary

Authoritative clock, scheduler, budgets, activities, RNG and chronology are non-Godot server/Core concerns.

`client input -> transport request -> deterministic server admission -> authoritative resolution -> projection/event -> client presentation`

The client never writes world/ECS state or advances time. It may interpolate/animate projections. A plain .NET/headless host must be able to load, advance and save the simulation. In-process single-player and network multiplayer use the same logical protocol.

Ghast is reference evidence here: ADR-0017 demonstrates a sole monotonic canonical tick and absolute deadlines; ADR-0019 demonstrates a hostable fixed-step server with deterministic network pumping and in-process/network transports; ADR-0011 keeps interpolation/presentation outside authoritative simulation. OctoGhast deliberately does not inherit Ghast's 20 TPS or exact networking implementation.

## 11. Ownership

Generic Core/server: canonical tick, fixed-step runner/host accumulator, deterministic scheduler/order keys, budget accumulation/remainders, time-policy state machine, transport admission boundary, persistence primitives and replay tracing.

Cataclysm: speed modifiers, action/movement costs, activity basis/modifiers, calendar interpretation/cadence, gameplay interruption rules, and classification of systems requiring intermediate simulation.

Godot client: input-to-request translation, projection rendering, interpolation/animation, and pause/acceleration request/consent UI.

## 12. Acceptance tests

19. Ten canonical ticks advance chronology exactly one second and give a speed-100 idle actor exactly 100 moves.
20. A speed not divisible by 10 retains fractional accrual with no systematic rounding loss.
21. Representative movement/actions subtract the pinned baseline move cost despite non-turn-gated input.
22. An expensive action may leave actor A negative while actor B continues acting.
23. Identical admitted inputs/RNG at 30, 60, 144 FPS and irregular host frames yield identical state hashes/event traces at the same tick.
24. Direct headless stepping and in-process hosted stepping yield the same authoritative trace.
25. Same-tick player/NPC/monster work remains identical despite different ECS insertion/hash iteration order.
26. Vary asynchronous packet callback timing while preserving boundary admission; authoritative result is unchanged.
27. A late host catching up N fixed ticks matches N on-time ticks exactly.
28. Catch-up above the per-pump bound retains debt until simulated; no ticks are dropped/stretched.
29. Rendering may stop while a running server continues canonical progression.
30. Player A performs a long activity while player B moves/acts independently; A progresses from A's budget/canonical time.
31. Save/load mid-activity including fractional budget remainder completes on the same tick/result as uninterrupted execution.
32. An authoritative interrupt cancels/pauses an interruptible activity at a deterministic tick while peers continue.
33. Single-player pause for arbitrary wall time changes no canonical simulation state.
34. A non-pausing client UI does not stop the server.
35. One of two players cannot unilaterally pause global time under default policy.
36. Approved global pause freezes the single shared clock for every actor.
37. N ticks at accelerated host pacing equal N ticks at normal pacing for identical inputs/RNG.
38. One multiplayer client cannot unilaterally accelerate global time.
39. Unanimous compatible acceleration returns to 1x on the first deterministic interrupt/incompatible request.
40. A leap-safe absolute expiry crossed by approved timewarp is settled correctly without every skipped tick.
41. A declared intermediate-simulation barrier prevents unsafe skipping; optimized timewarp matches accelerated stepping observably.
42. One-player in-process and loopback network transports produce matching admitted commands, authoritative traces and projections.
43. Two players with different speeds accrue/spend independent budgets against one chronology.
44. A representative simulation can load/advance/save with no Godot runtime or scene tree.
45. Client interpolation cannot alter collision, action cost, FOV, scheduler state or the next authoritative position.

## 13. Review decisions and dependent work

Resolved by #66: 10 TPS canonical fixed step; exact CDDA move mapping; deterministic explicit same-tick order; non-dropping bounded catch-up; actor-independent activities; one-global-clock pause/acceleration/timewarp; Godot/render independence; one logical server contract for single/multiplayer.

Still owned by dependent specs: #67 exact speed formula/modifier order; #69 full activity state machine; #79 environment cadence/timewarp barriers; #82 EOC/event scheduling detail; #85 persistence schema/migration; #86 UI surfaces; #57 implementation decomposition of time/scheduling infrastructure.

Changing tick rate, same-tick ordering keys or the one-global-clock rule is an architectural compatibility change and must update #52/#57/#64/#65 and this spec before implementation diverges.

## Architecture-review status

- [x] Separate pinned CDDA turn/move/calendar semantics from OctoGhast's continuously advancing authoritative server clock.
- [x] Define canonical fixed-step progression and mapping between simulation ticks, CDDA move/action budget and chronology.
- [x] Define deterministic same-tick ordering and host catch-up independent of render frame rate.
- [x] Define single-player pause semantics and multiplayer pause/acceleration/timewarp policy.
- [x] Define long-activity progression when other players/world actors continue acting.
- [x] Ensure the loop is headless and Godot-free; Godot consumes projections and supplies requests only.
- [x] Add one-player in-process-server and multi-player timing acceptance scenarios.
- [x] Repository spec updated; architecture review complete.
