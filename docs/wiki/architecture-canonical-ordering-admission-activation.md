# Architecture decision — canonical ordering, bounded admission and activation

Issue: [#95](https://github.com/LambdaSix/OctoGhast/issues/95)  
Reference review package: [issue-95-reviews/synthesis.md](./issue-95-reviews/synthesis.md)  
Pinned Cataclysm baseline: `LambdaSix/Cataclysm-DDA@e262adb299a7613b4aedc5f12c08fe0413c56a84`

## Status

**Adopted architecture.** This page resolves #95 and is normative for the affected specifications. The review package remains evidence and design history; where a supporting review differs from this page, this page controls.

The decision preserves the completed pinned-CDDA investigations. It reconciles OctoGhast's continuous authoritative server, bounded multiplayer intake, deterministic actor/work scheduling and active-region activation without making transport timing, worker timing or one privileged avatar part of gameplay semantics.

## 1. Core principles

1. The authoritative simulation owns canonical time, world mutation and ordering.
2. Network admission and gameplay execution are different responsibilities.
3. Core provides deterministic scheduling/phase capabilities; the active rules profile supplies its phase/lane plan and rule-specific causal order.
4. Equal snapshot, canonical admitted-input/lease trace, profile-plan version and RNG state must produce equal authoritative mutations regardless of socket callback order, hash/container iteration or worker completion order.
5. Determinism does not require physically serial execution of proven-independent work. Parallel execution is permitted only where the implementation can prove equivalent canonical results and publication order.
6. A profile plan is invalid if two potentially non-commutative authoritative writers can reach the same state without an explicit ordering relation or a declared commutativity/independence rule.
7. Transport, session, filesystem and worker readiness are not implicit gameplay inputs.

## 2. Bounded external admission

At each canonical intake boundary the server freezes the eligible external-player candidate set.

For configured positive limits `P` requests per PlayerId and `G` total requests per intake:

1. Snapshot the stable sorted PlayerId ring and each player's already-validated FIFO prefix.
2. Start from the persisted rotating cursor.
3. Take at most one queue head per eligible PlayerId per pass.
4. Stop when `G` requests are selected, every player has reached `P`, or a complete pass selects nothing.
5. Advance the cursor to the successor of the last player selected; if nothing is selected, retain it.
6. Assign authoritative per-player admission sequence numbers in each player's logical request order.
7. Revalidate authority and semantic-queue capacity before admission. Unselected work remains unadmitted under Spec 25's bounded overload policy.

A callback completing after the cut waits for a later intake boundary. Selection uses deterministic counts/cost units, never elapsed CPU time.

The authoritative per-player admission sequence is monotonic across reconnects. Socket/session sequence counters remain transport-local and are not durable gameplay order keys.

A duplicated/colliding logical request sequence is not ordered by message type as an accidental tiebreaker; it is rejected/reconciled by protocol validation and, where applicable, #96's operation-identity contract.

Rotating PlayerId selection provides bounded ingress fairness. It does **not** promise fair ownership of every contested gameplay resource.

## 3. Admission is not the domain winner

Once admitted, a player request joins the same authoritative work model as AI, activities, scheduled work, lifecycle hooks and administrative world operations.

PlayerId admission order does not decide same-tick gameplay contention.

The canonical execution relation is hierarchical:

1. profile phase/lane;
2. due coordinate where applicable;
3. owning subsystem/work-kind priority;
4. the domain's stable primary key;
5. the domain's authoritative origin/enqueue/admission sequence.

For actor opportunities the primary key is stable ActorId/EntityId. For recurring EOCs, equal-due order is the persisted schedule sequence. For the pinned timed-event manager, live insertion-list traversal remains its own queue rule. Synchronous lifecycle/EOC hooks execute at their owning semantic point and are not flattened into a generic queue.

A later contender always revalidates authoritative state after earlier work commits.

Host work budgets may yield between safe units, but required work for canonical tick N remains tick-N work. A host pump may continue the same tick later; unfinished due work is not silently moved to N+1.

## 4. Versioned profile phase plan

Core exposes a versioned deterministic phase/lane plan capability and explicit continuation/frontier semantics. Core does not hard-code Cataclysm actor roles or a permanent CDDA phase list.

The Cataclysm profile owns the concrete plan and persists/records its identity/version in compatibility metadata. An incompatible plan change requires an explicit migration/compatibility decision.

The plan must expose enough stable discriminators for deterministic tracing and persistence. Profile validation must reject unresolved authoritative write ambiguities.

## 5. Cataclysm causal plan and budget placement

Pinned reference evidence remains:

- due global/timed work occurs before ordinary actor/environment work;
- controlled-avatar activities/actions occur before the map environment phase;
- environment work precedes monster and NPC opportunities;
- monsters precede active NPCs;
- avatar move replenishment occurs after environment/monster/NPC processing and funds the next avatar opportunity.

OctoGhast removes the privileged-avatar assumption but **does not move all actors to one common early credit phase**. Budget settlement/accrual is itself part of the Cataclysm profile plan.

The normative coarse plan is:

1. **Closed boundary / host control**
   - service bounded permitted host controls;
   - freeze the next external candidate cut and admission metadata;
   - prepare finite known activation dependencies needed by the upcoming authoritative work.

2. **Begin/global due services**
   - advance/establish canonical chronology as specified by Spec 01;
   - run pinned timed-event, item-wakeup, mission and other owning global lanes at their declared cadences;
   - run recurring scheduled EOCs by due coordinate and persisted schedule sequence.

3. **Controlled-character band**
   - stable controlled ActorId order;
   - use carried budget previously settled for that actor;
   - actor `do_turn_eoc`/equivalent hook placement;
   - existing activity before fresh input;
   - one admitted action when eligible;
   - newly started activity may immediately consume remaining carried budget according to Spec 04;
   - synchronous hooks remain inside the owning operation.

4. **Map/environment band**
   - weather/setup at its explicitly specified position;
   - vehicles, fields, items, explosions, scent/exposure and other owning environment lanes in their documented stable order;
   - do not collapse all environment work into one undifferentiated callback.

5. **Monster band**
   - stable CreatureId order;
   - run the pinned/profile monster per-turn processing, including speed-derived budget settlement/accrual at its reference-relative point;
   - spend available budget under the monster opportunity rules.

6. **NPC band**
   - stable NPC/ActorId order;
   - run NPC per-turn/budget settlement at its profile-defined point;
   - spend available budget under NPC activity/AI rules.

7. **Controlled-character settlement / late maintenance**
   - settle/replenish each controlled Character's move budget exactly once for its **next** controlled-character opportunity;
   - run late physiology/morale/maintenance at their documented positions;
   - no second settlement/accrual for the same actor and interval.

8. **Close**
   - commit lease changes/publication boundaries;
   - build projections/events;
   - expose a coherent save boundary.

This preserves Cataclysm's important causal placement while supporting any number of controlled Characters. Different actor classes may therefore have different profile-owned budget-settlement positions. Generic Core only supplies deterministic budget/rate/scheduling primitives.

Signed move debt and fractional/rate-conversion remainders remain legal. A legal action may debit below zero; implementations must not introduce a full-cost affordability check unless the owning profile rule requires one.

A control-role transition during a tick cannot create a second actor opportunity or second budget settlement. Freeze role-band membership for the tick or maintain an equivalent once-per-tick opportunity/settlement record.

## 6. Activation state and publication

World activation is independent of visibility/presentation.

Logical region state:

`Inactive -> Loading -> CatchingUp -> Prepared -> Active -> Draining -> Inactive`

`Loading`, `CatchingUp` and `Prepared` are private. Ordinary authoritative queries, spatial indexes and client projections may observe a region only after atomic publication as `Active`.

One absolute region has one activation owner at a time. Compatible requests coalesce; overlapping player leases never cause duplicate activation or catch-up.

### Known finite dependencies

If an operation's finite read/write footprint is known before its first authoritative use, preflight/stage the required activation against a fixed semantic target frontier. Ordinary buffered movement through already available state stays on the fast path.

Worker I/O may run asynchronously, but worker completion time cannot select a gameplay tick. At the fixed frontier, if required staging is not ready, the host waits while retaining tick debt. Canonical time, moves and gameplay RNG do not advance because storage/generation is slow.

Read/validation failure and explicit administrative cancellation are recorded failures. Wall-clock lateness alone is not a gameplay rejection.

### Dynamic dependency discovered mid-operation

If a synchronous action/activity/EOC has already committed earlier effects or consumed authoritative RNG and later discovers an unavailable target:

1. preserve the already-committed prefix;
2. preserve the exact invocation/continuation point and RNG continuation;
3. suspend canonical simulation at that **same semantic point**;
4. activate/catch up the newly required finite dependency privately to the required frontier;
5. atomically publish the activation;
6. resume that same invocation exactly once.

Do not:
- transactionally roll back already-executed sequential effects;
- rewind or replay RNG;
- restart the invocation from its beginning;
- silently reject otherwise-valid pinned content merely because the dependency was dynamic;
- convert the remainder into later semantic work unless an owning feature explicitly specifies such an adaptation.

This synchronous semantic suspension is an availability tradeoff chosen to preserve sequential Cataclysm effect semantics. Bounded transport/control servicing may continue while the simulation waits, but no callback mutates world state.

An operator with genuinely unbounded dependency discovery must use a bounded activity/domain-specific mechanism or explicit validation failure; activation may not recursively grow without a declared bound.

## 7. Semantic frontier and exactly-once intervals

Canonical tick boundaries use half-open intervals:

`interval N = [N, N+1)`

For a domain marker `processedThrough = P`, intervals strictly before P are complete; P remains owed.

Activation/catch-up progress is not represented by one ambiguous timestamp. The required frontier is conceptually:

`(CanonicalTick, ProfilePhase, PhaseLane/Subphase)`

with the owning stable work/order coordinate where needed.

Before a boundary phase at C, catch-up processes work strictly before that frontier. Work due exactly at C remains for the one authoritative phase execution at C. If activation occurs after a phase at C has already closed, the newly activated domain settles what is owed for that domain exactly once without reopening already-completed global work for unrelated active state.

Per-domain progress markers and queue state prevent duplicate inclusive-due processing. Queries such as `dueAt <= currentBoundary` remain valid when paired with those markers.

Domains may use different catch-up algorithms:
- deterministic elapsed-time transforms where valid;
- stepwise deterministic catch-up where intermediate interactions matter;
- retained simulation leases where state cannot be safely compressed.

A subsystem may not claim arbitrary background equivalence without defining which historical inputs/interactions are required.

## 8. Failure, cancellation and publication semantics

Private failed staging cannot corrupt live state.

For movement/teleport:
- a load/validation failure leaves source WorldPosition, SpatialCell and indexes unchanged;
- a successful shared/published activation may remain materialized even if the later movement becomes stale/rejected;
- committed activation/generation RNG is not rolled back merely because a later command fails.

Previously committed EOC/action prefix effects likewise remain committed when later dynamic activation fails, unless that owning feature already specifies a compensating semantic action.

## 9. Pause and control servicing

Paused canonical gameplay time does not prevent bounded host-control servicing.

A reserved control lane may service permitted host/session policy such as pause/resume/shutdown while paused or waiting at an activation barrier. Its service ordering/counter is not a second simulation clock.

While paused:
- no gameplay tick advances;
- no actor budget accrues/settles;
- no gameplay RNG is consumed;
- no gameplay command executes merely because transport is pumped;
- queued gameplay input waits for the next normal admission/tick boundary.

World-mutating administration such as spawn/teleport/inventory mutation still uses the authoritative gameplay/work plan and cannot masquerade as infrastructure control.

## 10. Persistence and save quiescence

Persist the semantic state required for deterministic continuation, including as applicable:

- canonical tick/frontier and phase-plan identity/version;
- actor budget/debt/remainders;
- admitted/deferred semantic work and its stable PlayerId/ActorId/order keys;
- scheduler/EOC persisted sequence;
- per-domain activation/catch-up markers;
- RNG continuation/state required by the profile.

Do not persist raw sockets, transport buffers, worker objects, filesystem tasks or presentation state.

A coherent save **must not cut through a suspended synchronous authoritative operation** under this architecture. Complete/resume the operation to the next quiescent semantic boundary, then save. #95 does not introduce serialization of interpreter stacks or arbitrary in-flight handler frames.

#96 separately owns durable submitted-operation identity, retry/deduplication and lost-response outcome history.

## 11. Deterministic parallelism seam

The canonical order above defines semantic dependence and replay/publication order. It does not require every independent region or read-only system to execute physically on one thread forever.

An implementation may parallelize work only when:
- the profile plan establishes no ordering dependence between the work units, or explicitly declares them commutative;
- worker scheduling cannot affect RNG assignment or canonical mutation results;
- publication occurs through the same deterministic frontier/order;
- conformance tests prove equality under worker/thread permutations.

## 12. Required conformance scenarios

The following scenarios are normative additions to the affected specs/harness.

1. **Reversed PlayerId/CharacterId:** two players admitted in one cut with opposite PlayerId and controlled ActorId order; inventory, combat and construction use ActorId/profile execution ordering for contention, not PlayerId.
2. **Cross-source contention:** player action, AI opportunity, activity continuation and due EOC target one resource at one tick; phase/lane and queue-local rules define one trace and all later contenders revalidate.
3. **Over-budget admission:** callback/thread permutations with the same frozen candidates/cursor produce the same selected/deferred trace; reconnect does not reset the authoritative per-player sequence.
4. **Environment changes autonomous speed:** an environment effect before the monster band changes state used by monster per-turn/budget calculation. Controlled-character replenishment remains for the next controlled opportunity.
5. **Unavailable teleport:** load failure leaves source/index unchanged; success observes fully caught-up destination occupancy before movement commits.
6. **Periodic activation deadline:** activation immediately before and after a due phase produces exactly one due effect using the half-open interval/frontier markers.
7. **Dynamic EOC activation:** an EOC commits an earlier effect, consumes RNG, then discovers an unloaded finite target. Worker completion permutations produce one prefix, one RNG trace and one resumed continuation with no replay/rollback.
8. **Ambiguous profile writers:** a test profile registering potentially conflicting authoritative lanes without an explicit order/commutativity declaration fails plan validation.
9. **Pause/resume under queued gameplay:** resume is serviceable while gameplay time is paused; no callback mutation or hidden budget/RNG/time progression occurs.
10. **Save/load pending work:** a quiescent save preserves phase-plan version and all semantic order/frontier keys required for the same future result.
11. **Independent-region scheduling:** different safe worker/thread schedules for proven-independent active regions produce the same canonical mutation/publication trace.

## 13. Affected specifications

This decision is consumed by Specs 01, 04, 09, 10, 12, 14, 17, 20, 23, 25 and 26 and by implementation tickets #56–#62.

The existing review package remains useful background:
- [synthesis](./issue-95-reviews/synthesis.md)
- [intake review](./issue-95-reviews/intake-review.md)
- [phase review](./issue-95-reviews/phase-review.md)
- [activation review](./issue-95-reviews/activation-review.md)
- [activation qualification](./issue-95-reviews/activation-review-qualification.md)
- [timed-event evidence](./issue-95-reviews/timed-event-evidence.md)

The review package's proposed **common early budget credit** and **private transactional dynamic-EOC continuation** are not adopted. Reference-relative Cataclysm budget placement and same-point semantic suspension above are the normative choices.

## 14. Informative external design comparison

External engines/games were considered as design evidence, not as behavioural authority:

- ECS schedulers such as Flecs/Bevy/Unity reinforce explicit phase/dependency plans and deterministic synchronization rather than callback-driven mutation.
- Bevy's schedule-ambiguity diagnostics support rejecting unresolved authoritative write ordering rather than accepting incidental type/container order.
- OpenTTD's deterministic command/network-frame architecture illustrates separating network servicing/order from gameplay simulation and continuing control/network servicing while gameplay is paused.
- Factorio's deterministic lockstep reinforces the principle that nondeterministic I/O/presentation timing must remain outside canonical simulation input.

OctoGhast keeps its already-settled authoritative-server and player-specific projection model rather than copying any of those architectures wholesale.
