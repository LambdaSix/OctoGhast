# Spec 17 — Events, talkers and Effect-on-Condition scripting runtime

**Tracking issue:** #82  
**Parent:** #65  
**Reference baseline:** `LambdaSix/Cataclysm-DDA@e262adb299a7613b4aedc5f12c08fe0413c56a84`  
**Status:** investigation/specification complete; implementation not started

## 1. Purpose and parity boundary

OctoGhast needs a data-driven rules runtime compatible with Cataclysm:DDA's dialogue condition/effect and Effect-on-Condition (EOC) behavior. This runtime is infrastructure: items, mutations, bionics, activities, attacks/deaths, events, NPC dialogue, map changes and mods invoke it.

Parity is behavioral. OctoGhast does not need CDDA's C++ class hierarchy, but it must preserve the observable JSON contracts, actor/context binding, variable semantics, evaluation order, scheduling, persistence, and failure behavior required by content at the pinned baseline.

The runtime should be split conceptually into:

1. **Registry/definition layer** — validated EOC IDs and compiled condition/effect/expression definitions.
2. **Invocation frame** — alpha/beta talkers, context variables and call stack.
3. **Evaluator** — conditions, effects, values, math expressions and variable access.
4. **Scheduler** — queued recurring/activation EOCs and inactive recurring EOCs.
5. **Event bridge** — event-type subscription, payload-to-context conversion and actor resolution.
6. **Host adapters** — talker capabilities and world mutation/query operations.
7. **Persistence bridge** — durable global/actor variables and queued/inactive EOC state.

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

Core fields and defaults:

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

Global recurring EOCs are processed on the avatar turn. Non-global recurring EOCs are Character-owned. A global EOC with `run_for_npcs` evaluates the same rule separately for NPCs using NPC talkers.

### EVENT

The event bridge caches EVENT EOCs by `required_event`. On event delivery it constructs an invocation frame from supplied talkers where available, otherwise attempts actor resolution from recognized event payload character identifiers, then exposes event fields as context values and activates matching EOCs.

The compatibility requirement is not the exact cache implementation; it is that each matching EVENT EOC sees the correct event type, payload-derived context, actor bindings and deterministic subscription semantics.

### AVATAR_DEATH and NPC_DEATH

Death lifecycle hooks run at the corresponding host death points. They are not ordinary recurring jobs. The host integration must invoke them with the appropriate actor frame before/at the same semantic phase as CDDA.

### PREVENT_DEATH

These rules are invoked by the death-prevention host phase. Their effects can alter state such that death no longer proceeds. They therefore require synchronous execution before final death handling, not event-bus delivery after death.

## 5. Invocation frame and nesting

Every rule evaluates in a dialogue-like frame:

```
InvocationFrame {
  alpha: Talker?      // "u" scope
  beta: Talker?       // "npc"/"n" scope
  context: Map<String, DiagValue>
  callStack: List<String>
  conditionals: host-defined conditional bindings
}
```

Nested EOC calls copy the parent frame so actor bindings and context propagate. A child can extend/overwrite its own context without requiring the caller's ephemeral frame to be mutated.

The baseline records EOC call-stack entries and guards extreme recursion (the C++ implementation checks beyond depth 5000 and offers a debug abort). OctoGhast must provide a deterministic recursion/cycle safety limit. In automated/headless execution this must fail diagnostically rather than require interactive input.

## 6. Talker abstraction and capability matrix

A talker is a capability adapter over a host entity, not synonymous with an NPC. Conditions/effects operate against alpha and beta talkers and must tolerate absent or unsupported capabilities.

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

The baseline variable scopes are:

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

The condition/effect registry should be extensible: each JSON operator is a named compiler producing a typed predicate/effect closure or AST node. Unknown operators and malformed operands must produce source-located load errors.

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

The parser must preserve pinned precedence, including tested edge cases such as unary/exponent combinations. Do not substitute a host-language `eval`.

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

Scheduling is expressed in game-time durations and absolute due times using the time model from Spec 01.

Required properties:

- due when `due_time <= current_time`;
- stable deterministic ordering is required for jobs with equal due time (define insertion sequence as OctoGhast's tie-breaker);
- recurrence is re-evaluated when scheduling the next occurrence;
- next due time is based on current processing time plus evaluated recurrence;
- context captured for a queued EOC survives until execution;
- reentrant EOC execution may enqueue additional EOCs without corrupting the queue iteration;
- newly requeued recurring work must not be lost during nested processing.

A zero/negative recurrence can create pathological same-turn loops. The loader/runtime must match pinned accepted data where possible while retaining the global recursion/work budget safety mechanism.

## 12. Persistence

Persistence is shared with Spec 20.

Durable state includes:

- global variable map;
- actor-owned dialogue values;
- per-Character queued EOCs: ID, due time and captured context;
- per-Character inactive recurring EOC IDs;
- global queued EOCs and inactive global recurring EOC IDs.

EOC definitions themselves come from loaded content and are referenced by stable ID; saves should not serialize compiled rule bodies.

On load, reconcile saved IDs against the current pinned/content registry. Removed invalid EOCs are discarded diagnostically. Newly added recurring EOCs are scheduled. Ownership changes between global/local queues are reconciled as described in section 4.

Round-trip tests must prove no change in due time, context value types, inactive state, or variable scopes.

## 13. Host integration points

The EOC runtime must expose explicit host APIs rather than letting feature systems reach into evaluator internals:

- `activate(eocId, frame)`
- `queue(eocId, delay, owner, capturedContext)`
- `publishEvent(event, optionalAlpha, optionalBeta)`
- `processDue(character)`
- `reactivate(character/global)`
- `runAvatarDeath()`
- `runNpcDeath(npc)`
- `runPreventDeath()`

Items, recipes, mutations, bionics, activities, attacks/deaths, mapgen updates and later feature specs bind to these APIs. Inline EOC loading should return a registry ID so callers do not own compiled rule objects.

Mapgen-update and world-mutating effects must route through the local-map/world APIs from Specs 12/13; the scripting runtime is orchestration, not a second world model.

## 14. Error and diagnostic behavior

Separate three classes:

**Load/compile errors:** malformed JSON shape, unknown required operator, invalid expression syntax, contradictory EOC lifecycle fields. These fail validation with source path and EOC/operator context.

**Consistency errors:** referenced EOC ID does not exist after all content loads. Report during registry consistency checking.

**Runtime errors:** invalid dynamic conversion, unavailable required runtime object, expression runtime error, recursion/work-budget breach. Report EOC ID, operator/expression and call stack. The engine must return to a valid state; partial effects already executed are not automatically transactional unless the specific upstream operation is transactional.

Debug tracing should record at least EOC ID, whether the true branch activated, elapsed runtime and nesting depth. This mirrors useful baseline observability without requiring identical UI.

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
10. Global recurring EOCs process on the avatar time path; `run_for_npcs` separately evaluates NPC frames.
11. EVENT EOCs receive the exact required event only, payload fields appear in context with correct types, explicit talkers are preserved, and actor IDs resolve where supported.
12. Actor-resolution failure still permits payload-only EVENT EOCs to run.
13. Character/NPC/monster/item/vehicle/furniture/zone/topic talkers pass representative supported queries/effects and return safe baseline-compatible fallbacks for unsupported capabilities.
14. PREVENT_DEATH executes synchronously before final death and can change the death outcome; avatar/NPC death hooks execute at their defined host phase.
15. Save/load round trips preserve global/actor variables, queued due times, typed context and inactive recurring state.
16. Reentrant/nested execution cannot invalidate queue iteration; a recursion/work budget prevents unbounded EOC cycles with a diagnostic call stack.
17. Invalid referenced EOC IDs are reported by post-load consistency validation.
18. At least one end-to-end fixture drives each major host family: item use, mutation/bionic hook, activity completion, attack/death, event payload, NPC dialogue, and map/world mutation.
19. Differential fixtures against the pinned CDDA executable/content produce equivalent externally observable state for the representative scenarios above.

## 18. Non-goals and decisions deferred to dependent specs

This spec does not enumerate every individual CDDA condition/effect operator. The implementation must inventory and port the operators needed by the pinned core-data/mod compatibility target; that operator coverage matrix belongs beside implementation/conformance work and should be generated from pinned JSON plus the condition/effect registries.

NPC conversation UI belongs to Spec 11/21. Event producers belong to their domain systems. Mapgen semantics belong to Spec 13. This spec owns the common invocation/evaluation contract those systems call.

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
- black-box parity fixtures.

Implementation completion is separate from specification completion.
