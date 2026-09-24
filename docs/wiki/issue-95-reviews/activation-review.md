# #95 activation, catch-up and spatial atomicity review

> Supporting proposal, not an adopted contract. The [synthesis](./synthesis.md) controls where reviews disagree. Read the [activation qualification](./activation-review-qualification.md) before using the dynamic-effect continuation proposal.

## Recommendation

Adopt a hybrid activation protocol. Each command declares a deterministic finite read/write footprint. If that footprint is unavailable, the server runs an activation transaction at the command’s authoritative boundary before resolving it. Ordinary movement stays on the fast path; teleports, long movement and commands with unknown destinations become deferred work: activate, then revalidate and resolve at a later fixed boundary. Failed or cancelled private work leaves source position, spatial index, leases and gameplay RNG unchanged.

Activation is a world mutation transaction, separate from visibility or presentation. Projection interest may request data, but only `PlayerPresence` and explicit server leases make a region simulation-active. This preserves Spec 12’s set-union/exactly-once requirements (§Loaded map and ownership, lines 161–189) and Spec 26 §§5–7 (lines 198–334). Overlap adds a reference and projection; it does not repeat catch-up.

Use a deterministic publication barrier for asynchronous I/O. Workers may stage data, but staged state, catch-up effects and RNG become authoritative only when the simulation reaches the transaction’s fixed target boundary and publishes the complete activation atomically. If data is late, the host waits and retains tick debt; worker lateness never chooses gameplay failure. Only explicit administrative/server cancellation, shutdown, or deterministic resource admission rejection may abort it. This follows Spec 13’s rule that canonical time cannot advance because generation is slow (§21, lines 482–503). A ready-tick completion journal would expand the replay input and must be a deliberate architecture change.

## State and interval contract

Use `Inactive`, `Loading`, `CatchingUp`, `Prepared`, `Active`, `Draining` and `Failed`. `Loading`, `CatchingUp` and `Prepared` are private; `Prepared` means staging completed, not queryability. Only published `Active` state is queryable, indexable or projectable, as Spec 12 requires. Key one transaction by absolute region and fixed target boundary so concurrent requests share one operation/result.

Canonical ticks are boundaries; interval `N` is `[N,N+1)`, and every activation target includes a phase. At boundary `N`, global due work drains occurrences with `dueAt == N` (an implementation may query `dueAt <= currentBoundary` with a persisted already-drained marker). `processedThrough = P` means interval work in `[0,P)` is complete; `P` remains owed. A pre-due target `(C,before-due)` catches up `[P,C)`, leaving `dueAt == C` to the one global due phase at C. The published region then performs active interval `[C,C+1)` once and advances to `C+1`. An after-due target `(C,after-due)` includes `dueAt == C` exactly once. This aligns half-open intervals with inclusive due queries instead of leaving an off-by-one choice to subsystems.

Markers are per domain. Environment/item decay, monsters, activities/EOCs, vehicles and generation may use different catch-up algorithms; a domain either supplies a deterministic elapsed-time transform or holds an explicit active lease/timewarp barrier. Order catch-up by domain phase, absolute region key, stable entity ID and persisted schedule key; draw RNG only in that order (Spec 01 lines 499–509, 625–638; Spec 10 lines 472–512; Spec 17’s exactly-once rule). Existing saved markers are retained. Newly generated/materialized state receives its birth/materialization marker from the owning profile; it must not start at zero and receive fictional history.

Dynamic effects require a prepare/commit seam. An EOC, activity or action that may discover an unavailable target declares a finite dependency. If deterministic planning or RNG is needed to discover it, run that plan privately and yield `NeedsActivation`; retain handler state, RNG cursor, order key and phase-prefix continuation until the next safe publication boundary. Never load a region inside an already-running effect phase. Catch up from normal markers, then resume and commit the continuation atomically. Save waits for this in-flight operation at the quiescent barrier and never serializes a partial handler frame. Unbounded dependencies become bounded activities or deterministic rejection.

Private staged RNG/plans may be discarded on failure. Once activation is published or shared with another request, its effects and RNG are committed world state; a later movement/EOC rejection does not roll them back. This avoids rerunning earlier effects or RNG after a mid-operation dynamic dependency.

## Alternatives

| Option | Assessment |
| --- | --- |
| Preflight every command | Keep for bounded movement/read sets; long teleports need an accurate finite footprint and can stall intake. |
| Always defer | Uniform, but adds latency and stale-command risk to ordinary movement. |
| Hybrid preflight/deferred + barrier | Recommended: fast common path, bounded dynamic handling, deterministic trace. |
| Async ready-tick journal | Only as an explicit new contract; worker completion becomes replay-visible input. |

The barrier may reduce availability during slow storage, but preserves equal results for equal snapshot, canonical admission/lease trace and RNG state. Availability optimizations must record completion events, target ticks, content fingerprints and ordering effects explicitly.

## Worked traces and failure rules

* Buffered movement: preflight succeeds; atomically update `WorldPosition`, `SpatialCell` and index, recompute leases, run active tick once, then project.

* Unloaded teleport: assign its target boundary and finite footprint before workers run. Catch up privately, revalidate, then publish activation and movement. Failure preserves source/index. If activation is shared or already published, retain its committed catch-up/RNG even when movement rejects; only movement is absent.

* Same-region requests share one absolute-region transaction. One catch-up/index publication serves both; failure returns one deterministic class without partial activation. This is separate from #96 retry history.

* At a periodic deadline, activation before the due phase leaves the deadline to active tick `D`; activation after it includes `D` once. Final-lease removal normalizes adds/removes first, completes the boundary, persists markers and drains indexes. Reactivation catches up before projection.

* Save during deferred movement captures the pre-activation state through its admission cut. Staged work is excluded until publication; published activation is included atomically even if the later command rejects. Save advances neither time nor RNG.

## Required amendments/tests and dependencies

Amend Specs 01, 12 and 26 with this state machine, finite footprint declaration, boundary-phase markers, publication/queryability rule and deterministic wait. Amend Specs 04/09/10/14/17 for phase-plan/continuation dependencies, Spec 20 for save cuts, and Spec 25 so async callbacks can enqueue readiness but never publish world state. A deferred request records its semantic target boundary and finite dependencies at admission; it cannot grow an unbounded queue when workers discover more space. Preserve WorldPosition/SpatialCell atomicity.

Test buffered movement; teleport success/failure; same-region acquisition; activation immediately before/after a deadline; overlap acquisition/release; final lease removal; save during staged activation; cancellation after loading; and replay under varied worker completion order. Add dynamic-EOC tests where private earlier effects/RNG survive a continuation, save waits for completion, and an already-published shared activation is not rolled back by later command rejection. Include a domain matrix for leap-safe environment/items versus AI/vehicle lease barriers. Remaining dependencies are #95’s canonical phase/admission plan and #96’s operation-outcome history; this review does not choose retry semantics.
