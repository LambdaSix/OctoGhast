# #95 — Synthesis of three Luna 5.6 High reviews

**Status: historical review proposal. #95 was resolved on 2026-09-25; the normative decision is [../architecture-canonical-ordering-admission-activation.md](../architecture-canonical-ordering-admission-activation.md). Where this proposal differs from the adopted decision, the adopted decision controls.**  
Reviewed OctoGhast `7057611f96ec08817203880869f256a94c55870f` on 2026-09-24.  
Issue: [#95 — canonical ordering, bounded admission and activation](https://github.com/LambdaSix/OctoGhast/issues/95).

## Final decision follow-up — 2026-09-25

The final architecture adopted the review's layered bounded-admission/execution split, queue-local scheduler semantics, deterministic activation publication barriers, per-domain frontier markers and reserved host-control servicing.

Two important proposal points were resolved differently/fully:
- **budget placement:** Cataclysm preserves reference-relative budget settlement rather than adopting a universal common early-credit phase;
- **dynamic activation:** a finite dependency discovered after committed sequential effects uses same-point semantic suspension and exact-once resume, not a universal private transaction/rollback.

See [../architecture-canonical-ordering-admission-activation.md](../architecture-canonical-ordering-admission-activation.md).

## Outcome

Recommend **fair bounded admission, layered actor/work execution, a versioned Cataclysm phase plan, and deterministic activation barriers**. Keep these as separate responsibilities.

The three reviewers initially disagreed about execution order and left several timing ambiguities. After targeted challenges, they converged on layered ordering. This synthesis accepts that convergence, corrects remaining imprecise wording, and rejects treating every dynamic EOC as a private transaction.

At publication time, **#95 was not yet ready to close**: mid-operation activation after earlier EOC effects and the final phase/budget adaptation remained unresolved. Those choices were later resolved by the normative architecture page. #96 remains a separate outcome/retry decision.

The review stage was read-only. This package is published as a proposed decision record; publication does not adopt its recommendations or amend normative specifications. No gameplay/runtime code is included.

## Review method and evidence

Three sub-agents ran with model `gpt-5.6-luna`, reasoning effort `high`:

| Review | Scope | Report |
|---|---|---|
| Intake and ordering | Bounded selection, admission versus execution, fairness, pending work | [Intake review](./intake-review.md) |
| Phases and causality | Reference ordering, accrual, EOC hooks, pause and save boundaries | [Phase review](./phase-review.md) |
| Activation | Loading, catch-up intervals, publication, failures and dynamic dependencies | [Activation review](./activation-review.md), with mandatory [qualification](./activation-review-qualification.md) |

The parent reviewed the actual contracts, challenged each proposal, and reconciled the revised reports. The synthesis controls where the component reports disagree.

All reviewers used the same repository snapshot. The 29 local wiki pages were checked against the published Git tree: amended pages matched byte-for-byte; unchanged extracted pages differed only by a final newline. Fresh issue bodies for #52/#57/#58/#64/#65/#90/#95/#96 supplied current direction.

Completed #65 investigations remain authoritative for CDDA `e262adb299a7613b4aedc5f12c08fe0413c56a84`. One narrow source check was warranted: Spec 01 explicitly leaves nested timed-event insertion unpinned. The pinned [timed_event.cpp, lines 399–447](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/timed_event.cpp#L399-L447) resolves its live-list traversal behaviour. This was code inspection, not an executed conformance test or a repeat of the upstream investigation. See [evidence note](./timed-event-evidence.md).

## Decision table

| Area | Recommended proposal | Reason / rejected alternative |
|---|---|---|
| External admission | Freeze bounded per-player queue prefixes; select with a rotating stable-PlayerId ring and explicit per-player/global quotas. | A permanently ascending PlayerId scan can starve later players under load. Sorting a race-truncated set afterward does not fix candidate selection. |
| Authoritative execution | Use profile phases and actor/work keys after admission. PlayerId is not the domain contention winner. | Preserves existing OctoGhast Specs 01/04/09 direction. A universal admission-order winner needs larger changes and awkward AI/scheduler integration. |
| Actor opportunities | Order opportunities; preserve activity-before-input and immediate activity continuation inside an opportunity. | Freezing every derived operation would change existing activity semantics. |
| Scheduled queues | Preserve each queue's documented local ordering. | The timed-event manager and recurring EOC scheduler are not interchangeable priority queues. |
| Host work limits | Yield between safe units and continue the same canonical tick until required work completes. | Moving unfinished due work to the next simulation tick changes deadlines and outcomes. |
| Cataclysm phases | Controlled actors before map environment; environment before autonomous monsters, then NPCs; exactly one credit operation per actor per tick. | Retains important reference causal bands while extending beyond one avatar. Moving credit earlier is an explicit adaptation, not reference parity. |
| Known activation dependencies | Stage privately and publish at a fixed logical boundary; wait at that boundary if required I/O is late. | Worker readiness must not determine a different gameplay tick or an arbitrary rejection. |
| Activation markers | Per-domain completed intervals plus boundary/phase progress. | One ambiguous “caught up through N” timestamp is insufficient for inclusive deadlines and activation during a tick. |
| Pause | Reserved bounded host-control service independent of gameplay admission. | Resume must remain serviceable without advancing the simulation or allowing callbacks to mutate it. |
| Dynamic EOC activation | Keep a decision gate in #95. | Universal private transactions would contradict Spec 17's sequential effects and error semantics. |

These are proposals. Neither three reviewers nor a majority vote supplies architectural approval.

## 1. Bounded admission and execution

### Candidate selection

At a canonical intake boundary, freeze the eligible player set and each authorized attachment's already-validated queue prefix. A callback completing afterward waits for the next cut. Parser, authentication and queue limits remain those of Spec 25/#92.

Use positive configured limits P requests per player and G total requests per intake:

1. Traverse a stable sorted PlayerId ring starting from the stored next-player cursor.
2. In each pass select at most one queue head from each player whose quota is not exhausted.
3. Stop at G, or after a complete pass makes no selection. Empty queues do not cause an endless pass.
4. Set the next cursor to the successor of the last player selected; leave it unchanged if nothing was selected.
5. Within the selected set, assign authoritative per-player admission sequences in each player's logical request order. The existing PlayerId order can serialize this admitted batch; it does not override the cursor's selection fairness.
6. Revalidate authority and bounded semantic-queue capacity before admitting. Unselected requests remain unadmitted, subject to existing throttling/overload rules.

Use counts or deterministic declared cost units, not elapsed CPU time, to decide the logical cut. Every legal request must fit its service policy; an oversized head must receive an explicit bounded disposition rather than permanently blocking a queue.

The ingress review's phrase “persisted ingress rank” should not imply persisting transport queues or client sequence counters. Persist the authoritative next-admission counters, semantic pending-work keys and any continuation-relevant selection state. Reconnect attachment/transport state remains ephemeral. #96 decides whether a submitted retry identifies an old operation.

With a fixed finite eligible population, positive quotas and serviceable queue heads, rotation prevents admission starvation. It does **not** promise fair ownership of every contested gameplay resource. Stable actor order can repeatedly favour the same actor; that is an explicit gameplay-order tradeoff.

### Authoritative order

Use a hierarchical total order, not one misleading universal sort tuple:

- The profile plan fixes phase and lane order.
- Actor opportunities use their due coordinate, declared subsystem priority, stable typed actor identity and authoritative enqueue/admission sequence.
- Within an actor opportunity, the activity/action contract controls execution. Existing activities can consume budget before input; an action can start an activity that immediately consumes remaining budget.
- Recurring/queued EOCs use due coordinate and persisted schedule sequence. Do not insert actor ID ahead of that sequence inside the queue.
- The pinned timed-event manager instead visits its live insertion-ordered list, calls per-turn work, tests due time and appends new entries to the tail. Tail additions can be visited during the same pass. Preserve its declared cadence and this queue policy separately.
- Lifecycle hooks such as PREVENT_DEATH and synchronous event EOCs execute at their owning operation's semantic point. They are not automatically deferred into a generic end-of-tick queue.
- Administrative world mutations use an explicit profile lane; infrastructure control messages cannot masquerade as gameplay mutations.

The plan must give every lane a stable discriminator and comparator; no hash order, socket identity or callback order supplies a missing tie-breaker. Stable actor sorting is established OctoGhast adaptation, not a claim about upstream CDDA runtime IDs.

AI, activities and due events do not consume the external-player ingress quota. A host CPU/work limit may suspend execution of tick N across host pumps; it must not silently advance to N+1 with N's required work outstanding. Script recursion/work-limit failures retain their separately specified diagnostic/error semantics.

## 2. Proposed canonical flow and reference consequences

The phase reviewer recommends this coarse Cataclysm plan:

| Step | Responsibility |
|---|---|
| Closed boundary | Service permitted host controls; freeze the next external candidate cut and record admission metadata. |
| Activation preflight | Prepare finite known dependencies at a fixed target boundary before their first authoritative use. No private staging is queryable. |
| Begin tick | Establish canonical chronology; run due world services and the relevant profile scheduler lanes at their declared cadences. |
| Credit | Credit every simulated action-budget participant once, including actors with negative balance. Credit must not be conditioned on already being action-eligible. |
| Controlled actor band | Stable actor opportunities: existing activity, input action, immediate derived work and owning synchronous hooks. |
| Map environment band | Required map/field/item/vehicle/explosion work and effects, with stable cell/domain ordering. |
| Autonomous bands | Monsters, then NPCs, each once under the profile's actor opportunity rules. |
| Close | Required late physiology/maintenance, committed lease changes, projections and a coherent save boundary. No second budget credit. |

This is a proposed coarse flow, not a replacement for every owning subsystem's cadence and internal hook list. Weather sampling/setup and late exposure/physiology must retain explicitly assigned positions; do not move all work labelled “environment” into one undifferentiated phase. Timed-event per-turn processing is not permission to run it ten times per CDDA second.

**Reference consequence requiring explicit adoption:** moving avatar move replenishment from the end of the reference turn to the common credit phase lets newly earned budget fund an earlier opportunity. The existing Spec 01 reference scenario 14 must remain as evidence; add a separate adaptation fixture and explain the changed result. The alternative is actor-class-specific credit timing, which preserves more reference ordering but makes the continuous-time profile more complex. The reviewers favour common credit; this synthesis treats that as a product choice, not an editorial correction.

At the proposed credit phase, retain signed debt and fractional remainders. Example: an actor with -5 moves earns 10, becomes eligible with +5, legally spends 100 and ends at -95. It receives no second late credit. Other actors continue independently. Use the owning profile's exact eligibility predicate; do not impose one universal comparison or full-cost affordability gate.

Freeze role-band membership for the tick, or equivalently maintain a once-per-tick actor-opportunity record, so a control transition cannot run an actor in two bands. Multiple actors controlled by one player still have distinct actor identities.

Persist a phase-plan identity/version through profile compatibility metadata. An incompatible change requires an explicit compatibility/migration decision. Core needs deterministic phase/lane execution and continuation capabilities; it does not need a general-purpose phase-graph language or hard-coded Cataclysm role names.

## 3. Activation and catch-up

Adopt the activation review's private-state distinction:

`Inactive → Loading → CatchingUp → Prepared → Active → Draining → Inactive`

Prepared remains private. Only a complete published activation is available to ordinary authoritative queries, spatial indexes and projections. The world owns activation; observer projections do not create a second simulation.

Known dependencies must have a finite footprint and a target logical boundary chosen by semantic policy before worker completion. Buffered ordinary movement uses the fast path. More extensive operations may declare a fixed deferred boundary, but storage/cache warmth must not silently choose between “this tick” and “whenever ready.”

At the target, late I/O causes a host wait with tick debt retained. It does not advance canonical time, consume moves, or turn into a gameplay rejection solely because a wall-clock timeout elapsed. Actual read/validation failure and explicit server cancellation have separate recorded outcomes. Keep bounded control/network servicing available while waiting.

This follows [Spec 13 §21](https://github.com/LambdaSix/OctoGhast/blob/7057611f96ec08817203880869f256a94c55870f/docs/wiki/spec-13-overmap-world-generation-local-mapgen.md): generation cost is not player move cost, and slow generation must not advance canonical time. A readiness-event journal would deliberately enlarge replay inputs and should not be introduced as an invisible optimization.

### Interval convention

The activation review proposes interval N = [N,N+1), with per-domain `processedThrough=P` meaning intervals strictly before P are complete.

- Before boundary C's due phase, catch-up processes [P,C); occurrences due exactly at C remain owed to that phase.
- Activation after that due phase must settle those occurrences exactly once and record that boundary progress.
- A test for `dueAt <= currentBoundary` still includes overdue work. Markers and queue state prevent duplicates; replacing it with equality would lose overdue work.
- Other phases at the same boundary need their own completed-prefix information. A single “due phase complete” bit is not enough for arbitrary mid-tick activation.
- Saved entities keep their domain timestamps. Newly materialized entities use the profile's birth/materialization state, not a fabricated history starting at tick zero.

This convention is compatible with inclusive due checks only if Spec 01's clock/save vocabulary and every domain adapter use the same meaning. The phase review's informal “through T, then strictly after T” must be replaced by the precise shared convention before implementation.

Do not impose one catch-up formula on all domains. Domains needing intermediate interactions/RNG retain a simulation lease or a documented step-by-step contract. Overlap does not repeat catch-up; final-lease removal completes the defined work before persisting markers and detaching registrations.

One region has one activation owner at a time. Coalesce compatible requests and serialize incompatible target boundaries; keying transactions by region plus boundary must not permit two independently published copies of the same region.

Private failed staging must not corrupt live state. Conversely, once activation/generation and its RNG changes have committed or become shared, later movement rejection cannot roll them back. A stale teleport may leave a newly materialized world region while leaving the actor at its source. This is distinct from charging an attempted-action cost.

## 4. Remaining decision: a synchronous effect discovers an unloaded target

The difficult case is concrete:

1. An EOC changes a variable or consumes authoritative RNG.
2. A later operator computes a target that was not known at initial preflight.
3. That target requires region activation.
4. The global due phase or part of the actor phase has already completed.
5. A save, failure or cancellation occurs while loading.

The revised activation report proposed private earlier effects and atomic continuation. On challenge, the reviewer confirmed that this is a new architectural choice. [Spec 17, Runtime errors and authoritative mutation](https://github.com/LambdaSix/OctoGhast/blob/7057611f96ec08817203880869f256a94c55870f/docs/wiki/spec-17-events-talkers-eoc-runtime.md) does not automatically roll back partial effects or define such a continuation.

**Do not accept universal transactional EOCs to close this gap.** Likewise, do not silently reject valid pinned effects because their dependencies are dynamic.

Viable choices for #95:

| Option | Benefit | Required decision / risk |
|---|---|---|
| Suspend the current invocation at the same canonical execution point | Can preserve earlier sequential effects and RNG without replaying them. | Define activation catch-up relative to the phase prefix; prevent retroactive global effects, forbid partial saves, and resume the same invocation exactly once. Host availability may suffer. |
| Require preflightable dependencies for a defined operator subset | Simpler atomic activation before mutation. | Prove which pinned operators fit; specify a compatible fallback for others. Cannot be a blanket new restriction on accepted content. |
| Commit a prefix and create explicit deferred semantic work | Supports long dependencies and durable continuation. | Changes synchronous visibility/order; needs continuation persistence and #96 outcome/retry treatment. Must be a deliberate profile adaptation. |

Recommend investigating **same-point suspension** first because it offers the closest fit to sequential EOC semantics. It is not yet specified sufficiently to endorse. A catch-up prefix cannot simply replay global phases that have already affected other actors. World-interacting historical work may need a retained lease or a declared owning-domain rule.

This remains inside #95, which already owns activation/phase integration. It does not justify reopening the completed EOC investigation or creating a duplicate owner.

## 5. Pause and persistence

Service pause/resume through a bounded reserved control lane at safe host boundaries, including while canonical time is paused or a required activation barrier is waiting. Sequence its decisions explicitly; its service counter is not a second simulation clock.

Only host policy and permitted ephemeral session operations belong here. Spawn, teleport, inventory changes or control changes with world effects still use authoritative gameplay processing. Pure queries read a permitted coherent projection. No transport callback mutates the world.

While paused, do not accrue tick debt from paused wall time. Resume permits the next defined intake/tick; it does not suddenly spend an accumulated pause duration. Preserve any legitimately outstanding pre-pause work according to the host continuation contract.

For saves, distinguish queued semantic work from an in-flight atomic operation:

- A coherent boundary may serialize admitted deferred work, including stable player/actor IDs, target/order keys, profile plan identity and continuation-relevant state.
- Merely received bytes, attachments, socket sequences, worker objects and projection baselines remain excluded.
- Save cannot serialize half an EOC, half activation or a partial tick lacking an explicit save-safe representation.
- A save requested during such work waits for quiescence; the previous committed save remains recoverable.
- Persisted admitted work does not by itself settle whether a lost-response retry is the same operation. #96 still owns operation identity, retention, outcomes and rollback history.

## 6. Worked integration traces and acceptance matrix

| #95 requirement | Proposed trace / expected outcome | Coverage |
|---|---|---|
| Opposite PlayerId and CharacterId order | P10 controls C200; P20 controls C100. Both pass ingress. With the same due/role/priority, C100 executes first; P20 wins the item, P10 revalidates stale. | Covered by layered proposal; NET25-09 must change. |
| Player/AI/activity/EOC contention | Due EOC lane runs first by queue sequence. Otherwise actor opportunity/role bands decide; an existing activity precedes that actor's input. Every later contender revalidates. | Proposed phase contract; exact sub-lane ranks must be documented. |
| Over-budget intake | P=2, G=3; A offers A1–A3 and B offers B1. Starting at A selects A1,B1,A2; A3 waits unadmitted. AI/due EOCs still run independently. Empty queues terminate a pass. | Covered; add join/leave, byte-cap and changing-cursor tests. |
| Callback permutations/reconnect | Same frozen candidates, cursor, admission trace and initial state produce the same winner. Reconnect cannot reset admitted sequence ordering. Different physical arrival cuts need not be identical. | Covered for ordering; duplicate meaning remains #96. |
| Hazard and action at cadence boundary | Controlled actor opportunity precedes map environment; monster/NPC opportunities follow it. Credit occurs exactly once. Preserve owning weather/physiology subphases. | Proposed adaptation ledger required, especially old avatar replenishment. |
| Unavailable teleport and failure | Fixed target boundary; cold workers delay the host, not logical resolution. Failure leaves source position/index intact. Shared committed activation is retained on later stale rejection. | Covered for known footprint. |
| Periodic activation deadline | Test activation just before and after due phase D with overdue and exactly-D work; process each occurrence once and retain other phase-prefix markers. | Covered as contract; dynamic mid-operation case remains open. |
| Pause/resume under flood | Pause at closed boundary, flood bounded gameplay queues, service authorized resume with reserved capacity, then admit next frozen batch. No time/RNG/budget advancement while paused. | Covered. |
| Save/load pending work | Save at a coherent cut with semantic pending commands and order counters; restart reconstructs identical future ordering. No raw transport data required. | Covered for scheduling; #96 recovery guarantee unresolved. |
| Cross-spec/ticket updates | Apply agreed clauses to the amendment map below, then qualify programme readiness. | Not performed in this read-only review. |

Additional required fixtures:

- Timed-event tail append is visited in the same manager pass; recurring EOCs retain their different due/sequence order.
- Same-opportunity activity continuation survives frozen external intake.
- Negative-budget actors still earn budget; no double credit from late process-turn bookkeeping.
- Role/control change does not duplicate an actor opportunity.
- A host work limit pauses tick N and completes it before N+1.
- Identical logical activation targets with reversed worker completion order produce identical state/RNG.
- A dynamic EOC with a prior effect, RNG draw and cold target covers success, load failure, cancellation and save request. This fixture blocks claiming the dynamic continuation decision is complete.

These are proposed acceptance scenarios, not tests executed against a runtime.

## 7. Exact amendment map

| Document | Proposed normative change |
|---|---|
| Spec 01 — Fixed-step progression/order; Pause; Persistence | Replace illustrative pipeline with chosen versioned profile plan, scoped lane keys, exactly-once accrual, external-only freeze and same-tick host continuation. Retain original reference phase evidence and add adaptation fixtures. |
| Specs 04/09 — contention/action resolution | Consume actor-opportunity order after admission; preserve activity/attack-local hooks and revalidation. State dynamic-dependency policy only after #95 decides it. |
| Spec 25 §7, NET25-09/10/27 | Define frozen candidate selection and rotating quotas; make PlayerId an admission rule, not a gameplay winner. Keep trace-conditional transport invariance. |
| Specs 12/26 — activation lifecycle | Add private Prepared versus published Active, fixed target boundary, unique region ownership, per-domain interval/phase markers and load-failure rules. Replace movement-before-loading outline. |
| Spec 13 §§19–22 | Cross-reference deterministic activation barrier and generation ordering; preserve worker-latency, RNG and atomic materialization requirements. |
| Specs 02/10/14/16 | Map accrual, physiology, environment and actor cadences to the chosen profile phases without double work or renamed units. |
| Spec 17 §11 and runtime-error contract | Preserve EOC due/sequence and synchronous hooks; explicitly settle the dynamic activation case without blanket rollback. |
| Spec 20 — save barrier/queued state | Persist semantic ordering/plan metadata; distinguish queued deferred work from in-flight private/partial operations; maintain raw-transport exclusions. |
| Spec 23 — conformance harness | Add cross-contract traces above with admission cut, phase/lane, queue sequence, activation marker and RNG checkpoints. |
| Audit report and #52/#57/#58/#90/#64/#65 | Update only after the decisions and corresponding specification clauses are adopted; historical investigations remain complete. |

## Readiness

- **Ready to recommend now:** layered admission/execution; rotating bounded selection; per-domain queue semantics; external-only intake freeze; deterministic known-footprint activation; a reserved pause-control lane; coherent semantic save cuts.
- **Requires explicit product/profile adoption:** common early budget credit versus original late avatar replenishment, and the fully enumerated Cataclysm subphase plan. These change outcomes and deserve named adaptation scenarios.
- **Still architecturally unresolved:** mid-operation dynamic activation after earlier synchronous effects and its interaction with closed phases, failure and saves.
- **Separate gates at review time:** #96 for bounded operation identity/outcome recovery; #91 for account/meta-progression and #92 for authentication/security. #91 was subsequently resolved by [Spec 28](../spec-28-server-accounts-meta-progression.md); #92 remains separate.

#95 should remain open. The evidence supports a focused proposed resolution, not an assertion that all integration decisions have already been made. Core remains reusable; Cataclysm rules and queues remain concrete contracts rather than optional examples.

