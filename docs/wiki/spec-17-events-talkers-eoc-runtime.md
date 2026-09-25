# Spec 17 — Events, talkers and Effect-on-Condition scripting runtime

**Tracking issue:** #82  
**Parent:** #65  
**Reference baseline:** `LambdaSix/Cataclysm-DDA@e262adb299a7613b4aedc5f12c08fe0413c56a84`  
**Status:** investigation/specification, real-time/co-op review, and Core-vs-Cataclysm architecture/programme re-evaluation complete; implementation not started

## 1. Purpose and parity boundary

OctoGhast needs a Cataclysm-profile data-driven rules runtime compatible with Cataclysm:DDA's dialogue condition/effect and Effect-on-Condition (EOC) behavior. Items, mutations, bionics, activities, attacks/deaths, events, NPC dialogue, map changes and mods may invoke that profile runtime, but **EOC itself is not a generic Core concept**.

Parity is behavioral. OctoGhast does not need CDDA's C++ class hierarchy, but the **Cataclysm profile** must preserve the observable JSON contracts, actor/context binding, variable semantics, evaluation order, scheduling, persistence, and failure behavior required by content at the pinned baseline. Generic Core supplies reusable execution capabilities underneath that profile without permanently depending on CDDA's EOC names, talker taxonomy, alpha/beta conventions, variable naming, operator vocabulary, JSON schema or chronology constants.

The Cataclysm-profile runtime should be split conceptually into:

1. **Cataclysm registry/definition layer** — validated EOC IDs and compiled CDDA condition/effect/expression definitions.
2. **Cataclysm invocation adapter** — alpha/beta talkers, CDDA context variables and call stack mapped onto a stable Core invocation context.
3. **Cataclysm evaluator** — CDDA conditions, effects, values, math expressions, coercions and variable access.
4. **Core scheduler adapter** — Cataclysm recurring/queued EOC semantics represented as deterministic scheduled jobs with stable ownership/context.
5. **Event bridge** — Core typed event dispatch mapped to CDDA event subscriptions, payload-to-context conversion and talker resolution.
6. **Cataclysm host adapters** — CDDA talker capabilities and effect adapters that invoke owning authoritative systems.
7. **Persistence bridge** — Cataclysm durable variable/EOC state encoded through the world-save and scheduler contracts owned by Spec 20.

### 1A. Four-way architecture classification

Every implementation decision in this spec belongs to one of four layers:

| Classification | Contract in Spec 17 |
|---|---|
| **1. Pinned CDDA reference behaviour** | The exact baseline evidence for EOC lifecycle types, alpha/beta talkers, variable scopes/shorthands, condition/effect operators, expression/coercion/RNG semantics, recurrence and JSON scripting contracts. This is evidence to reproduce, not generic platform API design. |
| **2. OctoGhast Cataclysm-profile contract** | A faithful managed implementation of those pinned semantics, adapted only where already established for authoritative continuous time, co-op identity/authority, persistence and projection. Cataclysm content still observes the pinned behaviour through this layer. |
| **3. Generic Core runtime contract** | Deterministic typed event dispatch; deterministic scheduled jobs; stable invocation context and typed/stable entity references; scoped state-storage primitives; deterministic RNG access; authoritative side-effect execution/command handoff; and audience-aware result projection. Core does not know EOC lifecycle names, CDDA talker categories, `u`/`npc` scope names, CDDA operators, CDDA JSON shapes or Cataclysm time/move constants. |
| **4. Future evolution seams** | Other rules profiles may define different script languages, actor-role models, operator vocabularies, state scopes, recurrence semantics and data formats while reusing the same Core event/scheduler/context/RNG/authority/projection infrastructure. |

The dependency direction is therefore **Core capabilities → rules-profile runtime → profile host adapters/domain systems**, never Core → Cataclysm scripting vocabulary.

## 2. Authoritative evidence at the pinned baseline

Primary runtime sources:

- `src/effect_on_condition.h/.cpp` — EOC types, loading, activation, recurrence, event subscription, death hooks, queue processing and reactivation.
- `src/dialogue.h` — dialogue/invocation context, alpha/beta actors, context values, conditionals and call stack.
- `src/dialogue_helpers.h/.cpp` — variable scopes, indirection, typed dynamic values and value-or-variable helpers.
- `src/condition.h/.cpp` — JSON condition compilation and composition.
- `src/talker.h` and `src/talker_*.{h,cpp}` — host abstraction and concrete actor adapters.
- `src/math_parser*.{h,cpp}` — expression lexer/parser/evaluator, dialogue functions and typed values.
- `src/event.h/.cpp`, `src/event_bus.*`, `src/event_subscriber.h` — event types, payloads and subscriber delivery.
- `src/global_vars.h` — durable global variable store.

Concrete talker adapters present at the baseline include avatar, character, NPC, monster, item, furniture, vehicle, zone and topic adapters.

Important regression evidence includes `tests/math_parser_test.cpp` plus EOC/dialogue/condition tests and JSON validation exercised by the upstream test suite. OctoGhast conformance tests should pin fixtures to this baseline rather than to moving upstream behavior.

## 3. EOC definition contract

An EOC is a registered object identified by a stable string ID. Inline EOCs are accepted wherever the loader allows either an ID string or an object; references are consistency-checked after loading.

Pinned CDDA fields and defaults (therefore Cataclysm-profile schema, not Core schema):

| Field | Semantics |
|---|---|
| `id` | mandatory stable EOC ID |
| `eoc_type` | lifecycle; defaults to `ACTIVATION` when omitted |
| `condition` | optional predicate; absent means true |
| `effect` | true branch effect sequence |
| `false_effect` | optional false branch |
| `recurrence` | dynamic duration; presence implies `RECURRING` and conflicts with another explicit type |
| `deactivate_condition` | recurring-only inactivity gate |
| `global` | recurring state is owned by the global queue rather than one Character |
| `run_for_npcs` | for global EOCs, additionally evaluate for NPCs; invalid when `global=false` |
| `required_event` | mandatory for `EVENT` EOCs |

OctoGhast must reject structurally contradictory definitions during load/finalization, not defer them to arbitrary runtime behavior.

## 4. Lifecycle types and execution model

The baseline defines six EOC types:

### ACTIVATION

Explicitly invoked by another rule or host system. It executes once in the supplied invocation frame. APIs that require an activation-only EOC must reject/warn on other lifecycle types rather than silently treating them as activation rules.

### RECURRING

Scheduled against game time. Each instance has an EOC ID, due turn/time and captured context map.

On due execution:

1. clone/build a nested invocation frame;
2. restore the queued context values;
3. evaluate the EOC condition;
4. if true, apply `effect` and schedule the next recurrence;
5. if false and `false_effect` exists, apply it;
6. if false and `deactivate_condition` is false, schedule the next recurrence;
7. if false and `deactivate_condition` is true, remove it from the active queue and place its ID in the inactive set.

Reactivation scans inactive recurring EOCs. When their deactivate condition becomes false, they are queued again for `now + recurrence` with the current context.

A newly created Character queues applicable recurring EOCs. Existing saves reconcile definitions with saved active/inactive state: invalid/removed IDs disappear, global/local ownership changes are reconciled, and newly introduced recurring definitions are scheduled.

At the pinned CDDA baseline, global recurring EOCs are processed on the avatar turn and non-global recurring EOCs are Character-owned; a global EOC with `run_for_npcs` evaluates the same rule separately for NPCs using NPC talkers. **OctoGhast adaptation:** the lifecycle/content semantics are preserved, but no authoritative scheduling path is owned by a privileged avatar. Global recurring EOCs are world-owned scheduler jobs; actor-owned recurring EOCs are keyed by stable authoritative actor identity. `run_for_npcs` expands into deterministic actor-scoped invocations selected by the server, and player-controlled Characters participate by the same Character/talker rules as other eligible actors.

### EVENT

The event bridge caches EVENT EOCs by `required_event`. On event delivery it constructs an invocation frame from supplied talkers where available, otherwise attempts actor resolution from recognized event payload character identifiers, then exposes event fields as context values and activates matching EOCs.

The compatibility requirement is not the exact cache implementation; it is that each matching EVENT EOC sees the correct event type, payload-derived context, actor bindings and deterministic subscription semantics.

### AVATAR_DEATH and NPC_DEATH

Death lifecycle hooks run at the corresponding host death points. They are not ordinary recurring jobs. The host integration must invoke them with the appropriate actor frame before/at the same semantic phase as CDDA.

### PREVENT_DEATH

These rules are invoked by the death-prevention host phase. Their effects can alter state such that death no longer proceeds. They therefore require synchronous execution before final death handling, not event-bus delivery after death.

## 5. Invocation frame and nesting

Every Cataclysm rule evaluates in a dialogue-like profile frame:

```
CataclysmInvocationFrame {
  alpha: Talker?      // CDDA "u" role
  beta: Talker?       // CDDA "npc"/"n" role
  context: Map<String, DiagValue>
  callStack: List<String>
  conditionals: Cataclysm-profile conditional bindings
}
```

This shape is **not** the generic Core invocation API. Core requires only a stable invocation context able to carry a profile/runtime identifier, stable typed entity references/role bindings, immutable triggering metadata, scoped contextual values and deterministic execution metadata. The Cataclysm profile projects that generic context into alpha/beta talkers and CDDA variable conventions. A future profile may use named roles or no talker abstraction at all.

Nested EOC calls copy the parent frame so actor bindings and context propagate. A child can extend/overwrite its own context without requiring the caller's ephemeral frame to be mutated.

The baseline records EOC call-stack entries and guards extreme recursion (the C++ implementation checks beyond depth 5000 and offers a debug abort). OctoGhast must provide a deterministic recursion/cycle safety limit. In automated/headless execution this must fail diagnostically rather than require interactive input.

## 6. Talker abstraction and capability matrix

Within the Cataclysm profile, a talker is a capability adapter over a host entity, not synonymous with an NPC. Conditions/effects operate against alpha and beta talkers and must tolerate absent or unsupported capabilities. **Generic Core does not define these talker categories or the alpha/beta convention**; it supplies stable entity references and capability/host-service boundaries from which this profile builds its adapters.

Minimum actor families for parity:

| Talker | Identity/location queries | actor variables | Character stats/effects/inventory | world/object mutation |
|---|---:|---:|---:|---:|
| Avatar/Character | yes | yes | yes | actor-scoped |
| NPC | yes | yes | yes | actor/NPC-scoped |
| Monster | yes | where supported | creature capabilities | creature-scoped |
| Item | item identity/location | item values where supported | no Character-only operations | item-scoped |
| Vehicle | vehicle identity/location | where supported | no | vehicle-scoped |
| Furniture | location/object identity | where supported | no | furniture/map-scoped |
| Zone | zone identity/location | where supported | no | zone-scoped |
| Topic/synthetic | limited | context-oriented | no | normally none |
| Missing/base talker | neutral/default query results | none | none | no-op/unsupported |

Do not implement a universal entity with fake fields. Define explicit capability interfaces and make each compiled condition/effect declare or dynamically check what it needs. Unsupported operations must follow the source operation's observable fallback (false/default/no-op/diagnostic) and must never become memory/type errors.

## 7. Variable model

The pinned CDDA variable scopes, implemented by the Cataclysm profile, are:

- **global** — world/global variable store;
- **context** — invocation-local map;
- **u** — alpha talker's durable values;
- **npc** — beta talker's durable values;
- **var** — indirection: a context value names another scoped variable.

String shorthand parsing uses:

- `u_<name>` → alpha scope;
- `n_<name>` → beta scope;
- `_<name>` → context scope;
- otherwise → global scope.

Reads of a missing value return no value at the low-level optional API. Higher-level helpers may convert that to a null/default dynamic value; numeric math variable evaluation falls back to `0` when the variable is absent. Type conversion failures in math are runtime expression errors, not silent arbitrary coercions.

Writes must preserve the selected scope. Context lifetime is the invocation/queued captured context lifetime. Actor values live with their owning entity. Global values live with world/global state.

### Dynamic value type

Implement a tagged `DiagValue` sufficient for the baseline dialogue/math APIs: numeric, string and coordinate/tripoint values, plus any additional variants required by pinned fixtures. Do not reduce all values to strings; the math parser performs typed conversions and coordinate member access.

### Indirection

Indirect variables resolve the first context value to a variable descriptor/name, then read/write the target scope. Conformance tests must cover indirection into global, alpha, beta and context targets, including missing targets.

## 8. Conditions and effects

Conditions compile from JSON into predicates over a read-only invocation frame. Effects compile into ordered mutations over a mutable frame/host.

Required composition semantics:

- all/AND composition evaluates as conjunction;
- any/OR composition evaluates as disjunction;
- negation inverts the child result;
- condition/effect arrays preserve source order;
- branches are selected exactly once from the condition result;
- absent EOC condition is true;
- false branch executes only when condition is false and `false_effect` exists.

Where upstream condition implementations short-circuit through native boolean composition, OctoGhast must preserve short-circuiting because later predicates can contain expensive queries, RNG or diagnostics.

Effects within one effect list execute sequentially; later effects observe earlier mutations unless a specific effect contract states otherwise.

The **Cataclysm-profile** condition/effect registry should be extensible: each pinned CDDA JSON operator is a named compiler producing a typed predicate/effect closure or AST node. Unknown operators and malformed operands must produce source-located load errors. Generic Core must not register, enumerate or switch on CDDA condition/effect operator names; another rules profile may supply an entirely different operator vocabulary or no JSON scripting at all.

## 9. Math/expression runtime

The pinned math parser demonstrates:

- numeric literals including scientific notation;
- locale-independent decimal syntax;
- arithmetic, comparison and boolean-like numeric results;
- prefix unary operators;
- exponentiation and modulo;
- parentheses and precedence;
- ternary expressions;
- functions, nested calls and variadic functions;
- string and array arguments for dialogue functions;
- keyword arguments;
- constants including `pi`/π and `e`;
- variable reads and assignments;
- coordinate member access `.x/.y/.z` and assignment;
- dialogue-scoped functions;
- explicit parse/runtime diagnostics;
- IEEE-style infinity/NaN behavior for applicable floating operations.

The Cataclysm-profile parser must preserve pinned precedence, including tested edge cases such as unary/exponent combinations. Do not substitute a host-language `eval`. The expression grammar, numeric/coercion rules, `DiagValue` variants and dialogue functions are Cataclysm-profile compatibility behaviour; Core only supplies deterministic RNG/state/reference services required by whichever evaluator a profile installs.

Random functions (for example `rng`) must use OctoGhast's gameplay RNG service, not a parser-private RNG. A single expression evaluation consumes RNG in evaluation order. Seeded conformance tests must verify bounds and deterministic replay under the same seed/state.

## 10. Event bridge and payload propagation

Events are typed records delivered through the event bus. EVENT EOCs subscribe by exact `required_event`.

For each event invocation:

1. identify matching EOCs;
2. establish alpha/beta talkers from explicitly supplied actors when the publisher has them;
3. otherwise resolve recognized actor IDs in the event payload where the baseline does so;
4. copy payload fields into invocation context using stable names and typed values;
5. execute matching EOCs synchronously in event delivery order.

Do not expose mutable references into the event record. Context receives values.

Actor resolution failure must not prevent non-actor payload conditions from running. Actor-dependent predicates then see a missing/neutral talker according to their normal semantics.

## 11. Scheduling and time semantics

Cataclysm EOC scheduling is expressed in profile game-time durations and absolute due times using the authoritative canonical simulation-time model from Spec 01. CDDA's game-time recurrence/duration rules remain the Cataclysm rules baseline; OctoGhast changes only the scheduling owner and progression context. The server advances canonical time continuously at fixed simulation steps independent of render/network frame rate, and EOC due checks occur at deterministic simulation boundaries rather than on a local player's turn.

**Core timing contract:** the scheduler accepts canonical due coordinates/deadlines and deterministic order keys, but conversion between a profile's chronology/duration/action units and canonical scheduler coordinates is supplied by the active rules profile. Cataclysm currently consumes Spec 01's selected Cataclysm mapping (including its 10-tick/world-second and move-economy relationship); Core must not treat that mapping as a universal constant. A future profile may use a different chronology/rate conversion while retaining the same deterministic scheduler.

Required properties:

- due when `due_time <= current_time`;
- stable deterministic ordering is required for jobs with equal due time; the authoritative scheduler assigns a persisted monotonic schedule sequence/order key at enqueue time, and equal-due work orders by that key rather than connection arrival, container iteration or client identity;
- recurrence is re-evaluated when scheduling the next occurrence;
- next due time is based on the canonical processing time plus evaluated recurrence; host wall-clock delay or render lag never changes the logical due time;
- context captured for a queued EOC survives until execution;
- reentrant EOC execution may enqueue additional EOCs without corrupting the queue iteration;
- newly requeued recurring work must not be lost during nested processing;
- when a fixed step advances across multiple due times, all due EOCs are drained in deterministic due-time/order-key order at that canonical boundary; catch-up must not skip a recurrence merely because no player input occurred;
- an EOC executes at most once for each authoritative scheduled occurrence, even when several players observe or overlap the affected region;
- client requests may cause authoritative EOC activation only after normal server admission/order assignment; clients never advance, dequeue or execute authoritative EOCs locally.

The [canonical ordering/admission/activation architecture](./architecture-canonical-ordering-admission-activation.md) additionally fixes cross-source integration. Recurring/queued EOCs retain due-coordinate plus persisted schedule-sequence ordering inside their owning scheduler lane; they are not re-sorted by PlayerId or ActorId. Synchronous event/lifecycle EOCs remain at their owning operation's semantic point.

If a synchronous EOC has already committed earlier sequential effects or consumed authoritative RNG and later discovers a finite unloaded dependency, OctoGhast suspends canonical simulation at that same semantic point, activates/catches up the dependency privately to the required semantic frontier, publishes it atomically, then resumes the same invocation exactly once. Earlier effects/RNG are neither transactionally rolled back nor replayed. This is a deliberate server-availability tradeoff to preserve the pinned sequential-effect contract.

A zero/negative recurrence can create pathological same-turn loops. The loader/runtime must match pinned accepted data where possible while retaining the global recursion/work budget safety mechanism.

## 12. Persistence

Persistence ownership and save transaction semantics are owned by Spec 20/#85. Spec 17 only defines the Cataclysm-profile state that must participate in that contract; it does not create a second persistence subsystem.

Durable state includes:

- canonical due times plus scheduler order keys needed to preserve deterministic pending-work order;
- global variable map;
- actor-owned dialogue values;
- per-Character queued EOCs: ID, due time and captured context;
- per-Character inactive recurring EOC IDs;
- global queued EOCs and inactive global recurring EOC IDs.

EOC definitions themselves come from loaded content and are referenced by stable ID; saves should not serialize compiled rule bodies.

On load, reconcile saved IDs against the current pinned/content registry. Removed invalid EOCs are discarded diagnostically. Newly added recurring EOCs are scheduled. Ownership changes between global/local queues are reconciled as described in section 4.

Round-trip tests must prove no change in due time, deterministic queue order, context value types, inactive state, or variable scopes. Scheduled EOCs remain world/actor state across disconnect: a connection disappearing neither cancels nor pauses them. Reconnect creates a new transport session and projection only; it does not create a new player variable namespace or duplicate queued work. Stable `PlayerId`/`CharacterId` ownership comes from Spec 20, never socket/session identity.

For OctoGhast, **global** variables are world-scoped and shared by all actors by definition. **Actor/talker** variables are stored on the authoritative entity identified by stable entity/Character ID. **Context** variables remain invocation-local or captured queue context. Any product-level "player-scoped" variable that is not a CDDA actor variable must be explicitly keyed by stable `PlayerId` in the owning system; it must not be silently mapped to global scope or connection identity.

## 13. Host integration points

The Cataclysm profile should expose an EOC-facing façade rather than letting feature systems reach into evaluator internals:

- `activate(eocId, cataclysmFrame)`
- `queue(eocId, profileDelay, owner, capturedContext)`
- `publishCataclysmEvent(event, optionalAlpha, optionalBeta)`
- `processDue(owner)`
- `reactivate(owner/global)`
- `runDeathHooks(actor, actorKind)`
- `runPreventDeath(actor)`

Those are **profile APIs**, not generic Core interfaces. Underneath them, Core exposes reusable capabilities conceptually equivalent to typed event dispatch, deterministic job scheduling/cancellation, stable invocation-context construction, scoped state access, deterministic RNG, authoritative command/effect execution and audience-aware result publication. Core API names and data types should remain profile-neutral and must not accept `EocId`, `Talker`, `ACTIVATION`, `u`/`npc` scopes or CDDA JSON nodes.

Items, recipes, mutations, bionics, activities, attacks/deaths, mapgen updates and later Cataclysm feature specs bind to the profile façade where they require EOC compatibility. Inline EOC loading should return a Cataclysm registry ID so callers do not own compiled rule objects.

Mapgen-update and world-mutating effects must route through the local-map/world APIs from Specs 12/13; the scripting runtime is orchestration, not a second world model. The Cataclysm evaluator may request a map/world mutation, but the owning map/world system validates and commits it through the same authoritative mutation path used by non-scripted gameplay. No rules runtime owns a shadow map, inventory, actor model or alternative source of truth.

These host APIs are server-internal authoritative operations. A client may request an action whose accepted resolution invokes an EOC, but it never receives a mutable invocation frame or permission to apply EOC effects directly. Transport/session lifecycle, request admission, reconnect and connection identity remain owned by #90; Spec 17 consumes only stable player/entity identity and projection contracts exposed above that boundary.

## 14. Error and diagnostic behavior

Separate three classes:

**Load/compile errors:** malformed JSON shape, unknown required operator, invalid expression syntax, contradictory EOC lifecycle fields. These fail validation with source path and EOC/operator context.

**Consistency errors:** referenced EOC ID does not exist after all content loads. Report during registry consistency checking.

**Runtime errors:** invalid dynamic conversion, unavailable required runtime object, expression runtime error, recursion/work-budget breach. Report EOC ID, operator/expression and call stack. The engine must return to a valid state; partial effects already executed are not automatically transactional unless the specific upstream operation is transactional.

Debug tracing should record at least EOC ID, whether the true branch activated, elapsed runtime and nesting depth. This mirrors useful baseline observability without requiring identical UI.

## 14A. Authoritative multiplayer execution and projection

This section is an **intentional OctoGhast adaptation**, not a claim about upstream CDDA multiplayer behavior.

The authoritative server is the sole executor of EOC conditions, effects, variable mutation, RNG consumption, scheduling and world mutation in both one-player and cooperative operation. Clients may predict presentation, but predicted EOC results are never authoritative and cannot commit variables, inventory/map state, actor effects, event publication or follow-up schedules.

Talker resolution is invocation-scoped. Alpha/beta mean the actors bound by the triggering rule/event, not "the local player" and "the NPC". Any player-controlled Character can occupy either role where the pinned operator permits a Character talker. Actor IDs in event payloads resolve through the authoritative identity map at execution time. Missing/despawned actors use the pinned missing-talker fallback; the runtime MUST NOT substitute whichever player happens to be local/connected.

Each invocation owns an isolated context map. Concurrent invocations for Players A and B cannot see or overwrite one another's context values unless both intentionally address the same global or authoritative entity-scoped variable. Mutations of shared state are serialized by the deterministic server execution order; later invocations observe earlier committed mutations.

EOC output is separated into **authoritative results** and **client projection**. State changes first mutate server state. Messages/events then carry an explicit audience derived from their semantic source: owner/private actor, addressed participants, party/team where a later domain rule explicitly permits it, spatial/world-visible observers satisfying interest/visibility rules, or global broadcast for genuinely global announcements. The server MUST NOT broadcast invocation context, hidden variable values, unseen actor state or internal EOC traces merely because an EOC executed. Per-client DTOs/events contain only the minimum player-visible result; ECS/talker internals remain server-only. Debug/admin tracing is a separate privileged channel.

A single authoritative event/EOC may therefore produce different projections for different clients. Projection filtering does not change whether the EOC ran, its RNG consumption, its effects, or its schedule. Joining/reconnecting clients receive current permitted authoritative state plus durable player knowledge; they do not replay private messages that were never defined as durable history.

## 15. Determinism and ordering

For a fixed content set, initial world state, event sequence and RNG state:

- condition evaluation order is deterministic;
- effect order is deterministic;
- equal-time scheduled EOCs have a documented stable order;
- event subscriber ordering is stable within OctoGhast;
- RNG is consumed only by nodes/functions that evaluate (short-circuited branches consume none);
- save/load does not reorder pending EOCs in a way that changes observable results.

Differential fixtures should compare final state and emitted events/messages, not implementation-internal AST shapes.

## 16. Implementation slices

Recommended order:

1. `DiagValue`, variable descriptors/scopes and invocation frame.
2. Read-only talker capability interfaces + Character/NPC adapters.
3. Condition compiler with boolean composition.
4. Effect compiler and ordered effect execution.
5. Math parser/evaluator and dialogue function registry.
6. EOC registry, ACTIVATION execution and nested calls.
7. Scheduler, recurrence, inactivity/reactivation and persistence DTOs.
8. Event bridge and payload context.
9. Remaining talkers: monster, item, vehicle, furniture, zone, topic.
10. Death/prevent-death hooks.
11. Cross-system effect adapters including map/world updates.
12. Trace/debug tooling and differential fixture runner.

Each slice should land with black-box fixtures before dependent gameplay systems consume it.

## 17. Required conformance fixtures / acceptance criteria

The implementation is conformant when automated tests demonstrate all of the following:

1. Loading an EOC without `eoc_type` behaves as ACTIVATION; `recurrence` implies RECURRING; contradictory lifecycle declarations fail validation.
2. True and false branches execute correctly, in order, with absent condition treated as true.
3. Nested EOCs inherit alpha, beta and context, while child context mutation does not accidentally rewrite the caller's ephemeral frame.
4. Global/context/alpha/beta variables read and write the correct stores; shorthand scope names and variable indirection resolve correctly.
5. Missing numeric variables evaluate as zero where the pinned math API does; invalid typed conversions produce diagnostics.
6. Math fixtures reproduce pinned precedence, unary/exponent behavior, ternary behavior, functions, arrays/kwargs, coordinate member reads/writes, NaN/Inf cases and parse failures from `tests/math_parser_test.cpp`.
7. Seeded random expressions are deterministic and short-circuited branches do not consume RNG.
8. Recurring EOCs fire at due time, recompute recurrence, preserve queued context, deactivate when required, and reactivate when the deactivate condition becomes false.
9. New-character and existing-save reconciliation adds new recurring definitions and removes/re-homes invalid or ownership-changed entries.
10. Global recurring EOCs preserve pinned recurrence semantics while running as world-owned authoritative scheduled work with no privileged-avatar dependency; `run_for_npcs` separately evaluates eligible NPC frames in deterministic actor order.
11. EVENT EOCs receive the exact required event only, payload fields appear in context with correct types, explicit talkers are preserved, and actor IDs resolve where supported.
12. Actor-resolution failure still permits payload-only EVENT EOCs to run.
13. Character/NPC/monster/item/vehicle/furniture/zone/topic talkers pass representative supported queries/effects and return safe baseline-compatible fallbacks for unsupported capabilities.
14. PREVENT_DEATH executes synchronously before final death and can change the death outcome; avatar/NPC death hooks execute at their defined host phase.
15. Save/load round trips preserve global/actor variables, queued due times, typed context and inactive recurring state.
16. Reentrant/nested execution cannot invalidate queue iteration; a recursion/work budget prevents unbounded EOC cycles with a diagnostic call stack.
17. Invalid referenced EOC IDs are reported by post-load consistency validation.
18. At least one end-to-end fixture drives each major host family: item use, mutation/bionic hook, activity completion, attack/death, event payload, NPC dialogue, and map/world mutation.
19. Differential fixtures against the pinned CDDA executable/content produce equivalent externally observable state for the representative scenarios above.
20. Two simultaneous player-controlled Characters trigger the same EOC family with distinct alpha/beta bindings and context; each invocation resolves its own actors/context, and neither inherits a privileged/local avatar.
21. Two player invocations use the same context-variable names concurrently; context writes remain isolated, while deliberate writes to one shared global variable serialize in deterministic authoritative order.
22. A scheduled recurring EOC becomes due while no controlling player submits input. Advancing canonical simulation time alone fires it at the deterministic simulation boundary and schedules its next recurrence from canonical processing time.
23. Advance one fixed step across several overdue EOCs with equal and unequal due times. Assert deterministic due-time/order-key execution, exactly-once processing, stable RNG consumption and identical results after replay/save-load.
24. Two players have overlapping interest in an actor/world effect caused by one EOC. Assert the server executes the EOC once; each client receives only its permitted projection, with no duplicate world mutation.
25. An EOC emits an owner-private message, a participant-visible interaction event and a spatially visible world event. Assert only the intended clients receive each result and no invocation context/hidden variable store is leaked.
26. Disconnect a player whose Character owns queued EOCs and actor variables, advance canonical time, save/reload, then reconnect through a new session. Assert the same stable actor/player identity, variables and schedule continue without pause, duplication or socket identity in persistence.
27. Queue a global EOC and actor-owned EOCs, save before equal-time execution, reload and advance. Assert due times and persisted order keys reproduce the uninterrupted authoritative execution order.
28. Deliver an EVENT payload naming Player A's Character while Player B is also connected. Assert actor resolution binds A by stable authoritative ID; failure to resolve A never substitutes B, while payload-only conditions retain pinned missing-talker behavior.
29. Compare one-player in-process transport and network/co-op execution for the same admitted EOC-triggering command/event trace. Assert identical authoritative EOC state, RNG/schedule results and only transport-appropriate projection differences.
30. Install a minimal non-CDDA rules profile that defines its own rule/job type and named invocation roles, with no EOC lifecycle enum, Talker classes, alpha/beta names, CDDA variable shorthand or CDDA JSON operators. Assert it can dispatch a typed Core event, schedule deterministic work, resolve stable entity references/context, consume deterministic RNG, commit an authoritative side effect through an owning system and publish a filtered result.
31. Run representative pinned Cataclysm ACTIVATION, RECURRING and EVENT fixtures through the Cataclysm profile façade backed by the generic Core event/scheduler/context services. Assert externally observable Cataclysm state, branch/order, talker/variable binding, RNG use and recurrence results are unchanged from scenarios 1–19.
32. With two simultaneous player-controlled Characters, execute equal rule payloads through separate Cataclysm invocation frames backed by Core contexts. Assert stable role/entity bindings and ephemeral context remain isolated; intentional shared state still serializes by authoritative execution order.
33. Enqueue equal-due jobs from both the Cataclysm profile and a non-CDDA profile. Assert Core orders them by the documented canonical due coordinate plus persisted monotonic order key, independent of profile type, connection arrival, dictionary iteration or client identity.
34. Save with scheduled Cataclysm and non-CDDA invocations pending, reload under Spec 20, disconnect/reconnect an owning player through a new #90 session, and advance time. Assert stable invocation ownership/entity references and order keys survive; no socket/session identity enters the save; each rule executes exactly once.
35. Have Cataclysm and non-CDDA rule executions each produce private and world-visible authoritative results. Assert audience filtering is applied by the shared projection boundary and does not depend on the rules-runtime implementation, while hidden context/state remains server-only.

## 18. Non-goals and decisions deferred to dependent specs

This spec does not enumerate every individual CDDA condition/effect operator. The implementation must inventory and port the operators needed by the pinned core-data/mod compatibility target; that operator coverage matrix belongs beside implementation/conformance work and should be generated from pinned JSON plus the condition/effect registries.

NPC conversation UI belongs to Spec 11/21. Event producers belong to their domain systems. Mapgen semantics belong to Spec 13. This spec owns the common invocation/evaluation contract those systems call.

## 18A. Architecture-review and Core/profile re-evaluation summary

### 1. Pinned CDDA reference behaviour
The completed investigation remains authoritative evidence for EOC lifecycle names/types, talker capability matrix, alpha/beta conventions, CDDA variable scopes and shorthand/indirection, condition/effect operator vocabulary, JSON forms, expression grammar/coercion behaviour, recurrence/deactivation rules, event binding and observable failure semantics.

### 2. OctoGhast Cataclysm-profile contract
The Cataclysm profile must preserve that behaviour for pinned content. The already-reviewed adaptations remain unchanged: canonical continuous server time replaces avatar-turn scheduling; stable world/actor identities replace local-avatar/session assumptions; server-side execution exclusively owns effects/RNG/schedules; persistence uses Spec 20; and results cross the client boundary only through audience-filtered projection. These adaptations do not redefine Cataclysm condition/effect semantics.

### 3. Generic Core runtime contract
Core supplies deterministic event dispatch and ordering, deterministic scheduled jobs with stable ownership/order keys, stable typed invocation/entity references, generic scoped state-storage primitives, deterministic gameplay RNG access, authoritative side-effect/command execution boundaries, and audience-aware projection. Core chronology APIs accept profile-defined rate/chronology conversion. Core has no permanent dependency on CDDA EOC lifecycle names, talker categories, alpha/beta role names, `u`/`npc` variable syntax, CDDA operator sets, `DiagValue` details, CDDA JSON shapes or 10-TPS/100-move constants.

### 4. Future evolution seams
A future rules profile may install a different scripting runtime, DSL, bytecode evaluator or direct compiled rules; use named/multi-party roles rather than alpha/beta; define different scoped state; use different recurrence semantics or chronology mapping; and expose different operator/data vocabularies. It should still be able to reuse Core event delivery, scheduler, stable context/reference, RNG, authority and projection facilities without importing Cataclysm namespaces or translating its rules into fake EOCs.

No unresolved cross-cutting decision was found by this re-evaluation. The boundary follows #52/#57/#58/#64/#65, persistence remains owned by #85/Spec 20, and session/network concerns inherit #90.

## 19. Definition of done for #82

#82 is specification-complete when this document is accepted as the contract and follow-on implementation work can be sliced without rediscovering:

- EOC lifecycle and scheduling semantics;
- talker actor abstraction;
- variable scope/lifetime/indirection;
- condition/effect composition and ordering;
- math/coercion/RNG behavior;
- event payload and actor propagation;
- persistence/reconciliation behavior;
- failure/diagnostic expectations; and
- black-box parity fixtures; and
- the authoritative real-time/co-op scheduling, talker isolation, persistence and audience/projection contract plus scenarios 20–29 above; and
- the explicit pinned-reference / Cataclysm-profile / generic-Core / future-seam boundary plus cross-profile scenarios 30–35.

Implementation completion is separate from specification completion.


**EOC95-01 — dynamic activation continuation:** an EOC commits a variable mutation and consumes RNG, then computes a finite unloaded target. Vary worker completion order and assert one prefix mutation, one RNG trace, one activation publication and one resumed suffix. Saving cannot cut through the suspended invocation; it occurs at the next quiescent semantic boundary.
