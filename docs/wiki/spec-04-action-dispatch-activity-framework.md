# Spec 04 — Action dispatch and long-running activity framework

Issue: #69  
Programme: #64 / #65  
Reference baseline: `LambdaSix/Cataclysm-DDA@e262adb299a7613b4aedc5f12c08fe0413c56a84`

## 1. Purpose and architectural context

This specification defines the implementation contract for player/NPC action dispatch and long-running activities. It preserves pinned-CDDA action costs, activity progress, interruption/resume semantics and hook ordering while adapting the turn-gated single-avatar implementation to OctoGhast's continuously advancing authoritative server. CDDA is the reference/completeness profile, not a permanent Core ceiling: Cataclysm-specific action IDs, activity data fields, one-primary-activity policy, 100-move economy and grid target semantics MUST remain profile policy unless independently justified as reusable platform invariants.

Prerequisites are Spec 01 (canonical time/action budget), Spec 02 (Character), Spec 05 (Item identity/lifecycle), Spec 06 (item locations/transfers), Spec 12 (coordinates/active regions), Spec 17 (events/talkers/EOCs), Spec 18 (typed IDs/registries) and Spec 20 (persistence). Dependent specs include crafting #72, construction #73, combat #74, monsters #75, NPCs #76, AI #81 and UI/input #86.

This is a behavioural specification. OctoGhast MUST NOT reproduce CDDA's C++ class layout or global-avatar coupling.

### 1.1 Reference behaviour, Cataclysm profile, Core contract and future seams

For this specification, requirements are classified as follows:

1. **Pinned CDDA reference behaviour** — evidence from `e262adb299a7613b4aedc5f12c08fe0413c56a84`, including action dispatch, `activity_type` definitions/defaults, `player_activity` lifecycle/progress, interruption/backlog and save semantics.
2. **OctoGhast Cataclysm profile** — the compatibility implementation of those rules under the authoritative 10-TPS continuous server model, stable IDs, multiplayer ownership and explicit projections.
3. **Generic Core/server contract** — ruleset-agnostic command admission, deterministic ordering, durable-work lifecycle primitives, stable references, persistence hooks, deterministic RNG access and projection boundaries. Core MUST NOT depend on Cataclysm action/activity IDs or schemas.
4. **Future evolution seams** — profiles may use different action currencies, spatial semantics, interruption policies, activity definition schemas, or multiple independent durable-work channels without replacing ECS identity, networking, persistence or scheduling infrastructure.

Reference parity remains mandatory for the Cataclysm profile. The future seam is architectural freedom, not permission to skip pinned behaviour.

## 2. Authoritative pinned-CDDA evidence

Primary anchors at the pinned commit:

- `src/action.h`, `src/action.cpp`: discrete action IDs, string identities, movement mappings and the baseline distinction used by `can_action_change_worldstate`.
- `src/handle_action.cpp`: input-context lookup and dispatch through `game::handle_action`; the baseline UI and action dispatcher are coupled and avatar-centric.
- `src/player_activity.h`, `src/player_activity.cpp`: activity runtime state, progress, start/do-turn/finish/cancel, interruption, backlog/resume and EOC/event ordering.
- `src/activity_type.h`, `src/activity_type.cpp`: immutable activity-type data, defaults, validation and legacy handler lookup.
- `src/activity_actor.h`, `src/activity_actor.cpp`, `src/activity_actor_definitions.h`: polymorphic activity state/lifecycle and actor-specific serialization registry.
- `src/activity_handlers.cpp`: legacy per-turn/completion handlers still present at this baseline.
- `src/savegame_json.cpp`: persisted `player_activity` and activity-actor payloads/migration.
- `data/json/player_activities.json`: core `activity_type` definitions.
- `data/mods/TEST_DATA/activity.json`: test activity definitions for per-turn/completion EOCs.
- `tests/player_activities_test.cpp`: activity duration, invalidation, tool/target loss and distraction behaviour.
- `tests/activity_tracker_test.cpp`: exertion/activity tracking evidence.

Important evidence-derived ordering is normative below. Where a feature-specific activity actor defines additional formulas, those formulas belong to its owning feature spec; this framework defines how such actors execute.

## 3. Domain separation

### 3.1 Presentation input versus authoritative action

Pinned CDDA maps local input strings/keybindings directly to an `action_id` and dispatches from the avatar UI loop. OctoGhast MUST split this into:

1. **Client input action** — local key/button/menu gesture; presentation-only and remappable.
2. **Server command/intent** — a request to mutate authoritative state, carrying stable actor identity and typed semantic parameters.
3. **Synchronous/read query** — asks for projected/known information and does not mutate simulation state.
4. **Server activity** — durable actor-owned work spanning canonical time/action budget.
5. **Authoritative result/event** — accepted/rejected command result and resulting gameplay events/messages/projection changes.

A client input identifier is not a persistence identity and MUST NOT be trusted as authority. UI-only actions such as camera shift, zoom, local menus and keybinding screens remain client-local. A menu that chooses a gameplay target is presentation; the resulting semantic choice becomes a command.

The baseline `can_action_change_worldstate` is evidence that CDDA itself distinguishes non-world-mutating actions, but its exact switch is not a network-security boundary and MUST NOT be copied as one.

### 3.2 Immediate action versus activity

An **immediate action** resolves atomically at one deterministic server admission/resolution point and charges its pinned-CDDA-compatible move cost to the actor. It may produce events/state mutations but has no durable in-progress record after resolution.

An **activity** is required when work persists across more than one scheduling opportunity, needs per-step validation/effects, can be interrupted/resumed, or must survive save/load/disconnect. Starting an activity is itself an authoritative command result. Feature specs may also model short work as an activity where the pinned baseline does so.

Opening a UI never starts, advances, pauses or cancels an activity by itself.

## 4. Immutable activity definitions

Within the Cataclysm profile, an `ActivityTypeDefinition` is registry-owned immutable content identified by stable typed `ActivityTypeId`. Generic Core registry/definition infrastructure MUST be able to host typed immutable definitions, but it MUST NOT know or require Cataclysm-specific fields such as `based_on`, `rooted`, `auto_needs`, distraction sets or EOC IDs.

Pinned `activity_type` JSON fields and defaults:

| Field | Requirement/default | Contract |
|---|---|---|
| `id` | mandatory | stable typed content identity |
| `activity_level` | mandatory | exertion category/value |
| `rooted` | false | actor is rooted while work is processed |
| `verb` | "THIS IS A BUG" fallback; consistency check rejects/diagnoses empty verb | presentation label only |
| `interruptable` | true | whether external distractions may interrupt |
| `interruptable_with_kb` | true | baseline keyboard-pause permission; OctoGhast maps to explicit cancel/pause command policy |
| `can_resume` | true | type-level permission to retain compatible partial work |
| `multi_activity` | false | parent/multi-zone style activity |
| `fetch_items_to_zone` | true | multi-zone/item-fetch policy input |
| `refuel_fires` | false | permits baseline automatic fire-refuel behaviour |
| `auto_needs` | false | permits baseline auto food/drink support |
| `completion_eoc` | null | EOC invoked on normal progress exhaustion before finish |
| `do_turn_eoc` | null | EOC invoked each activity processing step before actor handler |
| `ignored_distractions` | empty | initial ignored-distraction set |
| `based_on` | speed | `time`, `speed`, or `neither` progress basis |

Validation MUST reject/diagnose duplicate activity IDs. A `based_on=neither` definition requires executable actor/handler behaviour; the baseline consistency check diagnoses a neither activity with neither actor nor turn function. Referenced EOC/content IDs resolve under Spec 18.

Definitions are not copied into runtime instances or saves beyond their typed IDs/version-compatible parameters.

## 5. Mutable runtime activity state

**Pinned CDDA / Cataclysm-profile policy:** each Character has one primary current activity plus an ordered backlog/suspension stack/list. **Generic Core MUST NOT encode “one activity slot for every possible rules profile” as a platform invariant.** Core provides actor-owned durable-work state/lifecycle storage and deterministic execution; the Cataclysm profile configures one mutually exclusive primary activity lane and its backlog. A future profile may define independent work channels only with its own explicit scheduling/contention rules. Cataclysm runtime state includes:

- stable activity type ID;
- actor-specific payload/state;
- total and remaining work in move units where applicable;
- runtime interruptibility overrides;
- resumability/auto-resume state;
- actor-specific target/context references;
- absolute coordinates where semantic location is fixed;
- relative coordinates only where semantics genuinely move with a parent object;
- ignored-distraction state only where it is gameplay-semantic;
- any actor-specific deterministic progress counters/deadlines/RNG state required for continuation.

Legacy baseline fields (`index`, `position`, `name`, untyped `values`/`str_values`, raw target vectors) are evidence of state that must be represented, not a required OctoGhast schema. New OctoGhast activities SHOULD use typed actor payloads rather than generic positional bags.

Transient UI state, popup state, sound handles, connection IDs, socket state, render transforms and Godot object identities are not activity state.

## 6. Activity lifecycle and ordering

### 6.1 Start or resume

When an admitted command assigns activity B to Character C:

1. Compare B against the front suspended activity using type-specific resume equivalence.
2. If equivalent and resumable, restore the existing instance, remove it from backlog, apply allowed resume-value updates, and mark the start as resumed.
3. Otherwise, if C already has activity A, suspend A at the front of the backlog according to the replacement policy, then install B.
4. For a fresh actor-backed activity call its start hook exactly once. A resumed actor MUST NOT rerun fresh-start setup.
5. Synchronize/validate effective activity type after start.
6. Emit authoritative `CharacterStartedActivity(CharacterId, ActivityTypeId, Resumed)`.

A start hook may reject/terminate the activity because prerequisites are invalid. Such rejection must leave no partially authoritative side effect except explicitly documented atomic start effects.

### 6.2 Processing step

At each deterministic actor scheduling opportunity:

1. Resolve the current activity and immutable definition.
2. Apply framework-level periodic support that is enabled for the type (where implemented by the owning feature).
3. Advance generic work according to its progress basis.
4. Validate actor/target active-region accessibility under the OctoGhast rules below.
5. Set/log activity exertion for Character physiology.
6. Invoke `do_turn_eoc`, if any, with the activity owner as the actor/talker context.
7. If the EOC canceled/replaced the activity, stop processing the old activity immediately.
8. Invoke the typed activity actor/handler.
9. If the handler canceled/replaced the activity, stop processing the old activity immediately.
10. Evaluate framework interruption/rest transitions that are due at this boundary.
11. Apply rooted semantics while the activity remains active.
12. If remaining work is now <= 0, invoke `completion_eoc`.
13. If still semantically completing, emit `CharacterFinishedActivity(..., Cancelled=false)`, then invoke the finish hook.
14. The finish hook may clear, replace or chain work. If no custom finish hook exists, clear the activity.
15. After clear, resume eligible auto-resume backlog work only under the documented backlog policy.

No old activity hook may continue mutating state after its activity identity has been replaced.

### 6.3 Completion versus cancellation

**Normal completion** is reaching the activity's completion condition. Completion EOC runs before finish.  
**Cancellation** is an interruption/explicit cancel/invalidation path and invokes the actor's cancellation cleanup hook, emits the finished event with `Cancelled=true`, and does **not** invoke completion EOC or normal finish.

A feature actor that self-terminates because a prerequisite vanished must explicitly classify the outcome as cancellation/failure versus successful early completion. Silent success is forbidden.

## 7. Progress accounting

Pinned CDDA stores `moves_total` and `moves_left` as move units.

For a **time-based** activity, baseline processing removes nominally 100 work moves per CDDA world second/turn, multiplied by the Character exertion-adjusted activity multiplier; the actor's available moves for that turn are consumed. For a partial final interval, only the corresponding fraction is consumed.

For a **speed-based** activity, baseline processing converts the actor's available move budget into activity work, again applying the exertion-adjusted multiplier, and consumes no more action budget than the work remaining.

For **neither**, generic framework progress does not decrement remaining work; the actor/handler owns progress and completion.

OctoGhast adaptation:

- For the **Cataclysm profile**, Spec 01's selected mapping is authoritative: 10 canonical ticks = 1 CDDA world second = 100 baseline moves. Generic Core fixed-step/scheduler APIs MUST accept a profile-defined rate/action-currency mapping and MUST NOT hard-code 10 TPS or 100 moves/second.
- Time-based work advances from canonical elapsed simulation time, not wall-clock/render time. The equivalent baseline rate is 10 nominal work moves per canonical tick before the same Cataclysm activity/exertion modifiers.
- Speed-based work consumes the owner's available authoritative move budget as it becomes schedulable; actor speed therefore affects opportunity exactly through Spec 01's move accumulation.
- Neither activities are stepped only at deterministic declared scheduling points; they may use absolute deadlines/elapsed intervals where observationally equivalent.
- Integer/fractional remainder handling MUST be deterministic and must not lose work through per-tick rounding.
- A player opening a menu or disconnecting does not freeze work. Global pause/timewarp follows Spec 01.

Feature-specific duration formulas (crafting, construction, reading, safecracking, etc.) are owned by their feature specs/actors but produce framework-compatible move/deadline progress.

## 8. Interruption, cancellation and resume

Distraction categories evidenced by the baseline include noise, pain, attacked, hostile spotted near/far, talked-to, asthma, motion alarm, weather change, portal storm, EOC, dangerous field, hunger, thirst, temperature, mutation, oxygen, withdrawal and craft-step completion.

Pinned CDDA combines type defaults, runtime ignored-distraction state and global UI settings, and some checks directly read the global avatar. **That global-avatar/UI coupling is not portable behaviour.**

OctoGhast adaptation:

- authoritative interruption predicates are evaluated against the activity owner Character, that Character's authoritative senses/state, and relevant world state;
- no player's FOV/UI preference can suppress another actor's gameplay interruption;
- client preferences may control presentation/confirmation policy only where the rules permit a choice;
- if a choice is required, the server may place the activity in an explicit awaiting-decision/suspended state without stopping canonical time for other actors;
- explicit cancel is an authoritative command validated against current activity identity/version;
- stale cancel/continue requests are rejected without affecting a replacement activity.

A non-interruptible activity ignores ordinary distraction cancellation. Keyboard-specific upstream flags map to whether an explicit player cancel/pause command is permitted; they are not keyboard concepts in the server domain.

Low-stamina rest is a suspension/child-activity pattern: the parent may be marked for auto-resume and a wait-for-stamina activity installed. The decision and resulting state transition must be deterministic and actor-scoped.

### Resume compatibility

Type-level `can_resume=false` always prevents resume. Otherwise resume requires same activity type and type-specific semantic equivalence of targets/parameters. Actor-backed activities own their equivalence and allowed resume-value updates. Equality MUST use stable IDs/locations, not object references or collection indexes.

Explicit cancellation may retain a resumable partial activity in backlog, but it MUST NOT immediately auto-resume it. Auto-resume is reserved for framework-controlled suspension such as stamina rest/multi-activity chaining.

Backlog growth MUST be bounded/diagnosed; the pinned baseline diagnoses >100 entries as likely infinite looping. OctoGhast MUST impose a finite safety bound and fail deterministically rather than permit unbounded recursive activity replacement.

## 9. Target/reference stability and invalidation

Activity targets MUST use contracts from Specs 05/06/12/20:

- Character/NPC/creature targets: stable runtime entity ID.
- Externally referenced items: stable `ItemUid` plus authoritative item-location/ownership locator as required by Spec 06.
- Cataclysm-profile map targets: stable authoritative logical locations using the Spec 12 grid-aligned `WorldPosition`/`SpatialCell` mapping; never client/Godot transforms. Generic Core target/reference APIs MUST NOT require every future `WorldPosition` to be integer-grid identical to `SpatialCell`.
- Vehicle targets: stable vehicle/part identity where available; relative offsets are permitted only for semantics intentionally tied to a moving vehicle.
- Content definitions: typed string IDs.

Every activity actor declares target validity conditions and validation points: start, each processing step where needed, and finish. If a required target disappears, changes owner, moves out of allowed range, loses required state/tool/charges, or is concurrently consumed/transformed, the activity fails/cancels according to that actor's contract.

The baseline tests explicitly demonstrate activities cancel/fail when required tools or electronic-device targets disappear mid-activity. OctoGhast MUST never follow a stale object pointer.

An unloaded target is not inherently nonexistent. Server active-region/background rules determine whether the target can be materialized/caught up. If work requires active local simulation and its target cannot currently be resolved, suspend/defer or fail according to the activity contract; do not spin indefinitely. The pinned NPC out-of-bounds cancellation is evidence for the failure concern, not a requirement to copy one-avatar reality-bubble semantics.

## 10. Concurrency and contention

The server serializes authoritative mutations at deterministic simulation boundaries. Multiple players may target the same item, tile, creature, construction site or vehicle.

Each activity actor MUST declare its commit/claim semantics:

- **exclusive claim** where simultaneous work is invalid;
- **shared/cooperative claim** only where the feature spec explicitly defines combined work;
- **optimistic revalidation** where several actors may work but completion revalidates authoritative state.

Claims are server/world state, never client locks. The [canonical ordering/admission/activation architecture](./architecture-canonical-ordering-admission-activation.md) resolves the former #95 gate: Spec 25's PlayerId ordering selects a bounded external admission set only; it does not decide domain contention. Admitted activity/action work executes under the versioned profile plan using phase/lane, due coordinate, work-kind priority, stable actor/entity identity and authoritative origin sequence as applicable. The loser of contention receives a deterministic rejection/invalidation event and is not charged completion effects/resources that did not commit. Feature specs may define partial work costs already legitimately spent.

Overlapping player active regions never duplicate an activity or target.

## 11. EOC and event contract

Spec 17 owns EOC execution semantics. Activity integration is:

- start event after fresh-start/resume initialization;
- `do_turn_eoc` before actor/handler on each declared activity processing step;
- if that EOC cancels/replaces the activity, no old actor step follows;
- `completion_eoc` after completion condition and before finish hook;
- finished event carries owner Character ID, activity type ID and cancellation flag;
- cancellation hook runs before/with cancellation cleanup and no completion EOC fires.

All EOCs execute server-side with owner-specific talker/context. Effects/messages are projected only to authorized audiences. Simultaneous players' contexts MUST NOT leak variables/talkers between activities.

## 12. RNG and determinism

The framework itself should not consume RNG merely for dispatch, save/load, reference resolution or progress bookkeeping.

Activity actors may consume gameplay RNG. Such draws MUST use authoritative deterministic RNG streams/state and occur in stable hook order. Same initial world + RNG state + admitted command sequence + canonical ticks MUST produce the same activity trace and result independent of render FPS, transport type, connection timing after admission, or ECS/hash iteration.

Pure validation, failed stale commands, persistence encode/decode and projection MUST NOT consume gameplay RNG unless the pinned gameplay rule explicitly requires a draw as part of an admitted attempt.

## 13. Persistence and disconnect/reconnect

Persist, under Spec 20:

- current activity type and typed actor payload;
- total/remaining work or equivalent absolute deadline/progress;
- runtime interruptibility/resume flags that are semantic;
- ordered backlog/suspended activities;
- stable target references and coordinates;
- deterministic actor-specific counters/RNG state where required;
- actor move budget/remainder via Spec 01.

Do not persist UI prompts, ignored local notification preferences unless promoted to gameplay state, sockets/connections, RPC objects, presentation state or delegates/function pointers.

Loading resolves activity type/actor codecs by registered stable IDs after registry finalization. Missing required activity definitions/codecs are a structured load failure unless an explicit migration exists. The pinned baseline maps obsolete/pre-actor activity data to `ACT_MIGRATION_CANCEL`; OctoGhast may use an explicit migration/cancel record, but MUST diagnose it and MUST NOT silently reinterpret incompatible state.

Save occurs only at Spec 20's deterministic tick barrier, never halfway through an atomic activity step. Save/load must not call start/do-turn/finish hooks or consume moves/RNG.

Disconnect does not cancel activity. The Character remains world-owned; already-started work continues if its ordinary rules permit it. Reconnect binds the durable PlayerId to the same CharacterId and receives current projected activity state.

## 14. Projection/client contract

A client may receive only activity information authorized for that player. Minimum owner projection may include activity type/display verb, progress representation, interruption/decision state and relevant result messages. Other players receive only information their visibility/party/world rules permit; internal actor payloads, hidden targets, EOC variables, inventory references and ECS components are not exposed by default.

Client progress bars are derived presentation. They never author remaining work.

## 15. Failure and validation behaviour

Commands MUST return structured rejection reasons for at least: actor not controlled/authorized; actor dead/incapacitated/incompatible; unknown action; stale activity version; invalid/missing target; target not visible/known where the rule requires knowledge; out of range; missing tool/resource; target already claimed/changed; non-interruptible cancellation; and invalid activity definition.

Rejection is atomic: no partial state mutation, move charge or RNG draw unless the owning gameplay rule explicitly defines a cost for the attempted action.

Runtime invariant failures (unknown activity type after load, unresolved required stable target, actor codec mismatch) are diagnostics/failures, not undefined behaviour.

## 16. Implementation boundaries

**Generic Core/server infrastructure:** deterministic command admission and authorization envelopes; stable command sequencing; ruleset-neutral durable-work/activity lifecycle primitives; stable actor/work-instance/reference identity; deterministic scheduling hooks; persistence codec interfaces; contention/claim primitives; deterministic RNG access; and projection/event boundaries. Core MUST NOT reference Cataclysm action IDs, `ActivityTypeId` schema fields, distraction enums, EOC IDs, CDDA move constants or grid-only target types.

**OctoGhast.Cataclysm profile:** semantic action IDs and compatibility mapping; one primary activity lane plus backlog; CDDA move/action costs; `activity_type` JSON schema/defaults; `time`/`speed`/`neither` progress semantics; rooted/auto-needs/refuel/distraction policies; actor payloads; feature-specific validation/effects; EOC/event mapping; and grid-aligned target rules required by the pinned baseline.

**Server/session layer:** binds stable PlayerId/control permissions to actors, admits transport-neutral requests at deterministic simulation boundaries, applies #90 connection/session ordering and resource rules, and projects only authorized results. Socket/connection identity never becomes activity identity.

**Godot client:** input contexts/keybindings, menus/target selection, command construction, optimistic cosmetic feedback only, owner-visible progress/decision UI and presentation of results.

**Future evolution seam:** a later non-Cataclysm profile may use different action currency, non-grid authoritative positions, different activity definition data, or more than one independent durable-work channel. Such a profile must define its own scheduling/contention semantics but MUST be able to reuse Core identity, persistence, deterministic ordering, transport and projection boundaries.

The server domain MUST have no dependency on Godot input events, nodes, transforms, popup APIs or audio APIs.

## 17. Conformance and parity scenarios

### A04-01 Input/command separation
Bind two different client keys to the same semantic move/action. Assert identical admitted server command and authoritative result. Change keybinding during play and assert no simulation state changes.

### A04-02 Immediate action atomicity
Submit a valid immediate action with known CDDA cost. Assert one atomic mutation, exact move charge and no persistent activity. Submit an invalid variant and assert no mutation/RNG/cost unless the feature rule says attempts cost moves.

### A04-03 Time-based progress
Create a time-based 1000-move activity for an otherwise baseline Character. Advance canonical ticks without rendering. Assert progress follows the 10 TPS/100 moves-per-second mapping and completes on the same canonical boundary as equivalent pinned-CDDA work after modifiers.

### A04-04 Speed-based progress
Run the same speed-based activity with two actors of different authoritative speed budgets. Assert work consumes each actor's available moves, the faster actor progresses accordingly, and neither actor blocks the other.

### A04-05 Neither activity
Use a test actor with `based_on=neither`. Assert generic framework ticks do not decrement work and only actor logic changes/completes it.

### A04-06 Hook ordering
Test activity with both EOCs and actor hooks. Record: start -> per-step EOC -> actor step -> completion EOC -> finished event -> finish hook. Assert exact ordering.

### A04-07 EOC cancellation
Have `do_turn_eoc` cancel/replace the activity. Assert the old actor's do-turn and completion hook do not execute afterward.

### A04-08 Replacement during actor step
Actor A assigns B during its step. Assert A performs no subsequent rooted/completion/finish processing and B becomes the sole current activity.

### A04-09 Explicit cancellation
Cancel an interruptible resumable activity midway. Assert cancellation cleanup/event fires, completion EOC/finish do not, partial work may be retained as non-auto-resuming backlog, and peers/world time continue.

### A04-10 Non-interruptible cancellation
Attempt ordinary distraction and player cancel against a non-interruptible activity. Assert rule-defined refusal and unchanged current activity.

### A04-11 Resume equivalence
Suspend activity A, then assign an equivalent A request. Assert original remaining work is restored and fresh start is not rerun. Repeat with a different stable target/parameters and assert a fresh activity instead.

### A04-12 Stamina suspension
Drive an eligible activity below the stamina threshold under deterministic state. Assert parent is suspended, wait/rest child runs, and parent auto-resumes only through the framework path. Explicitly cancel during rest and assert no surprise auto-resume.

### A04-13 Distraction isolation
Two player-controlled Characters run activities simultaneously. Put only A in a dangerous field/hostile-visible state. Assert only A receives the authoritative distraction. B's client settings/FOV cannot affect A.

### A04-14 Missing tool/target
Start a representative tool/target activity, remove or transfer the required item through another authoritative action, then process the next validation boundary. Assert deterministic cancellation/failure and no stale-reference mutation. Mirror pinned `player_activities_test.cpp` cases.

### A04-15 Concurrent target contention
Two actors attempt exclusive work on the same target in the same tick. Vary packet arrival before admission/ECS insertion order while preserving admitted order key. Assert exactly one commit according to deterministic ordering and a stable rejection/invalidation for the other.

### A04-16 Active-region boundary
Run work near an active-region edge, then remove one player's coverage while another still overlaps. Assert activity/target exists once and continues. When no coverage remains, assert declared background/suspension semantics rather than avatar-bubble deletion/spin.

### A04-17 Save/load continuation
Save midway between activity steps, destroy runtime state, reload and continue. Assert final tick, remaining/total work trajectory, resource consumption, EOC/event sequence and RNG result equal uninterrupted execution.

### A04-18 Missing actor codec migration
Load a fixture whose activity actor is obsolete/missing. Assert explicit migration/cancel or structured failure with diagnostic; never instantiate an arbitrary CLR type or silently continue corrupt state.

### A04-19 Disconnect/reconnect
Player A starts a long activity then disconnects while B continues. Advance canonical time, reconnect A with a new connection. Assert same CharacterId/activity authoritative state and no socket identity in persistence.

### A04-20 One-player transport equivalence
Run identical command/activity sequence through in-process single-player and loopback network transport. Assert matching admitted commands, activity trace, state hash and owner projection.

### A04-21 UI does not pause
Open owner activity/progress/inventory UI while server remains running. Assert canonical time and activity progression continue unless explicit global pause policy from Spec 01 is activated.

### A04-22 Definition defaults/validation
Load fixture omitting every optional `activity_type` field and assert the defaults in section 4. Load duplicate ID and `based_on=neither` without executable behaviour and assert diagnostics/rejection per registry policy.

### A04-23 Backlog loop safety
Construct pathological activity replacement that recursively pushes work. Assert deterministic finite guard/diagnostic rather than unbounded memory growth or server hang.

### A04-24 Projection secrecy
Give owner and nearby/non-nearby peer clients the same running activity. Assert owner gets permitted progress; peers get only authorized visible state; hidden target IDs/internal actor payload/EOC variables are absent.

### A04-25 Core/profile dependency isolation
Enforce project/reference tests showing generic Core/server durable-work infrastructure compiles without Cataclysm action/activity definition types. Cataclysm may depend on Core command/scheduling/persistence/reference/projection contracts; Core must not reference `ACT_*`, CDDA distraction enums, `based_on`, EOC IDs or 100-move constants.

### A04-26 Profile-defined time/action currency
Run the Cataclysm profile and assert 10 ticks = 1 CDDA second = 100 moves with A04-03/04 results unchanged. Instantiate a Core test profile with a different exact fixed-step/action-currency mapping and prove the generic lifecycle runner, persistence and networking boundaries do not assume 10 TPS or 100 moves/second.

### A04-27 Primary activity lane is profile-local
For the Cataclysm profile, assign activity B while A is current and assert the documented suspend/replace/backlog semantics and single primary lane. Separately instantiate a Core test durable-work host capable of naming two independent channels without importing Cataclysm types; assert Core storage/identity APIs do not force a universal single-slot model. This does not add concurrent Cataclysm activities.

### A04-28 Grid target semantics remain profile-local
Execute a Cataclysm activity targeting a map square and assert stable grid-aligned Spec 12 resolution. Separately use a Core spatial fixture where precise `WorldPosition` is not one-to-one with `SpatialCell`; assert durable-work target/reference infrastructure can carry a stable logical location without requiring a Godot transform or integer-only Core position.

### A04-29 Cataclysm action IDs are not protocol/platform IDs
Map two transports/clients to the same Cataclysm semantic action and assert identical admitted command. Then load a non-Cataclysm test command type through the same Core admission envelope and assert no `action_id`/`ACT_*` dependency exists in transport, persistence or deterministic sequencing infrastructure.

### A04-30 Observer/interest invariance
Run one activity in an area simultaneously covered by two clients, then remove one observer's interest while preserving the owner's/other coverage. Assert one authoritative activity instance, one hook/RNG sequence and unchanged progress; only projection recipients change.

### A04-31 Save barrier across activity mutation
Request a save while an activity step is resolving EOC/actor/finish effects. Assert Spec 20 commits only at the quiescent simulation boundary; reload never observes half-applied progress, duplicated completion effects, a mismatched backlog transition or partially advanced gameplay RNG.

### A04-32 Reconnect identity is transport-neutral
Disconnect and reconnect the activity owner with a different socket/session connection object. Assert stable PlayerId/control rebinds to the same Character/activity/work identity, admitted stale requests from the old connection cannot mutate it, and no Cataclysm activity state stores transport objects.

## 18. Acceptance-criteria coverage for #69

- Immediate action vs activity boundary: sections 3 and 17 A04-01/02.
- Activity state machine, ownership and target references: sections 5, 6, 9, 10.
- Progress accounting and move/time semantics: section 7 and A04-03/04/05.
- Cancellation, interruption, resume and invalidation: sections 6, 8, 9 and A04-07 through A04-14.
- Serialization and map/item reference stability: sections 9, 13 and A04-14/17/18.
- Actor-generic vs avatar-specific behaviour: sections 3, 8, 16 and A04-13/19.
- EOC/event hooks: sections 6, 11 and A04-06/07.
- Completion/cancellation/interruption/save-load/disappearing-target black-box coverage: section 17.
- Core-vs-Cataclysm boundary: sections 1.1, 4, 5, 7, 9, 16 and A04-25 through A04-29.
- Multiplayer interest/save/reconnect refinements: A04-30 through A04-32.

## 19. Architectural decision status

No new unresolved cross-cutting architectural decision was discovered. The required adaptations follow already-settled contracts in Specs 01, 02, 05, 06, 12, 17, 18 and 20: one authoritative canonical clock, actor-owned durable work, stable IDs, server-owned world state, deterministic command admission, player-specific projection and persistence independent of transport/Godot identity.

The 2026-09-24 re-evaluation additionally resolves accidental platform-ceiling risk without changing pinned parity: Cataclysm owns the one-primary-activity/backlog policy, 10-TPS-to-100-move mapping, `activity_type` schema, interruption categories and grid-aligned target semantics; generic Core owns the reusable deterministic durable-work, reference, persistence, RNG and projection capabilities. This is consistent with #52/#57/#58/#64/#65 and does not require a new architecture ticket.

Feature-specific activity actors may still expose local rule questions while dependent feature specs are implemented; those belong to their owning feature specs and do not change this framework contract.


## 20. Re-evaluation record — 2026-09-24

Re-evaluated against #52, #57, #58, #64 and #65 plus completed prerequisite Specs 01, 02, 05, 06, 12, 17, 18 and 20. Pinned upstream evidence remains `LambdaSix/Cataclysm-DDA@e262adb299a7613b4aedc5f12c08fe0413c56a84`; source/data/tests were rechecked rather than substituting current upstream HEAD.

Key outcomes:

- Pinned lifecycle, progress, definition defaults, EOC ordering and invalidation evidence remains valid.
- Cataclysm reference-parity behaviour is now explicitly separated from generic Core capability and future evolution seams.
- The Cataclysm one-current-activity/backlog model is retained for parity but is no longer stated as a universal Core invariant.
- The selected 10-TPS/100-move mapping is explicitly Cataclysm-profile policy consumed from Spec 01.
- Grid-aligned activity target semantics remain Cataclysm-profile policy over Spec 12's WorldPosition/SpatialCell separation.
- Transport/session identity remains excluded from activity state and #90 remains the networking/session owner; this spec defines only request/result/projection semantics.
- Scenarios A04-25 through A04-32 add architecture conformance for profile isolation, alternative Core timing/spatial/work-channel capability, observer invariance, save barriers and reconnect identity.
- No gameplay/runtime implementation was performed and no new unresolved cross-cutting architecture decision was found.

## Post-spec integration dependencies

The former #95 shared ordering/activation gate is resolved by the [canonical ordering/admission/activation architecture](./architecture-canonical-ordering-admission-activation.md). Existing activity-before-input and immediate same-opportunity continuation semantics remain authoritative inside the actor opportunity; they are not frozen as separate external admission candidates.

[#96](https://github.com/LambdaSix/OctoGhast/issues/96) still owns bounded request identity, duplicate suppression and result recovery across reconnect/save; activity code must not invent a session-local substitute. Durable activity identity remains distinct from the request that started it.


**ACT95-01 — dynamic activation during sequential work:** if an already-running action/activity has committed a prefix and later discovers a finite unloaded dependency, preserve the prefix and RNG continuation, suspend at the same semantic point, activate/catch up to the required frontier, then resume exactly once. Do not replay or transactionally roll back the completed prefix.

## 19. #96 bounded operation-outcome integration — 2026-09-26

The cross-cutting retry/outcome architecture is now resolved by [Architecture — bounded command idempotency and outcome recovery](./architecture-bounded-command-idempotency-outcome-recovery.md).

This specification consumes that contract as follows:

- Every gameplay-affecting command/action request MUST declare exactly one retry policy: `StateReconciled`, `IntrinsicIdempotent`, or `DurableOutcome`. Missing classification is a specification/registration error.
- A transport correlation/request ID is not itself a durable operation identity.
- For `DurableOutcome` starts/cancels/resumes, `OperationId` identifies the logical submitted operation. It is distinct from the authoritative Activity instance identity and from any backlog/resume equivalence identity.
- Duplicate receipt, reconnect retry, result query, or committed save/restart recovery of one durable operation MUST NOT execute activity-start side effects, move/action costs, resource consumption, EOCs or gameplay RNG more than once.
- An admitted deterministic rejection is a terminal outcome for that logical operation and remains the outcome on retry; later world changes do not convert it into a fresh attempt.
- Activities that have successfully started continue to use this spec's existing durable activity lifecycle/persistence. The operation ledger records the command outcome; it is not a second copy of activity runtime state.
- `StateReconciled` actions use fresh authoritative state after ambiguous delivery rather than blind replay.

A04 conformance inherits OP96-01 through OP96-15 where commands are client-requested and retryable.

